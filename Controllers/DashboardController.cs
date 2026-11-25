
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Huellitas.Data;

namespace Huellitas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class DashboardController : Controller
    {
        private readonly HuellitasContext _context;

        public DashboardController(HuellitasContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ==========================================
            // 1. INDICADORES GENERALES
            // ==========================================

            ViewBag.TotalPropietarios =
                await _context.Propietarios.CountAsync();

            ViewBag.TotalMascotas =
                await _context.Mascotas.CountAsync();

            ViewBag.TotalCitas =
                await _context.Citas.CountAsync();

            ViewBag.TotalConsultas =
                await _context.Consultas.CountAsync();

            // ==========================================
            // 2. CONSULTAS POR PERIODO
            // ==========================================

            DateTime ahora = DateTime.Now;

            ViewBag.Consultas7Dias =
                await _context.Consultas
                    .CountAsync(c => c.FechaHora >= ahora.AddDays(-7));

            ViewBag.Consultas30Dias =
                await _context.Consultas
                    .CountAsync(c => c.FechaHora >= ahora.AddDays(-30));

            ViewBag.Consultas90Dias =
                await _context.Consultas
                    .CountAsync(c => c.FechaHora >= ahora.AddDays(-90));

            ViewBag.EtiquetasPeriodos = new[]
            {
                "Últimos 7 días",
                "Últimos 30 días",
                "Últimos 90 días"
            };

            ViewBag.DatosPeriodos = new[]
            {
                (int)ViewBag.Consultas7Dias,
                (int)ViewBag.Consultas30Dias,
                (int)ViewBag.Consultas90Dias
            };

            // ==========================================
            // 3. CITAS AGRUPADAS POR ESTADO
            // ==========================================

            var citasAgrupadas = await _context.Citas
                .AsNoTracking()
                .GroupBy(c => c.Estado)
                .Select(g => new
                {
                    Estado = g.Key.ToString(),
                    Total = g.Count()
                })
                .ToListAsync();

            string[] estados =
            {
                "Pendiente",
                "Confirmada",
                "Atendida",
                "Cancelada"
            };

            ViewBag.EtiquetasCitas = estados;

            ViewBag.DatosCitas = estados
                .Select(estado => citasAgrupadas
                    .Where(c => c.Estado == estado)
                    .Select(c => c.Total)
                    .FirstOrDefault())
                .ToArray();

            // ==========================================
            // 4. MASCOTAS AGRUPADAS POR ESPECIE
            // ==========================================

            var mascotasAgrupadas = await _context.Mascotas
                .AsNoTracking()
                .GroupBy(m => m.Especie)
                .Select(g => new
                {
                    Especie = g.Key,
                    Total = g.Count()
                })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            ViewBag.EtiquetasMascotas = mascotasAgrupadas
                .Select(m => string.IsNullOrWhiteSpace(m.Especie)
                    ? "Sin especificar"
                    : m.Especie)
                .ToArray();

            ViewBag.DatosMascotas = mascotasAgrupadas
                .Select(m => m.Total)
                .ToArray();

            // ==========================================
            // 5. DIAGNOSTICOS MAS FRECUENTES
            // ==========================================

            var diagnosticosAgrupados = await _context.Consultas
                .AsNoTracking()
                .Where(c => c.Diagnostico != null &&
                            c.Diagnostico != "")
                .GroupBy(c => c.Diagnostico)
                .Select(g => new
                {
                    Diagnostico = g.Key,
                    Total = g.Count()
                })
                .OrderByDescending(g => g.Total)
                .Take(7)
                .ToListAsync();

            ViewBag.EtiquetasDiagnosticos = diagnosticosAgrupados
                .Select(d => d.Diagnostico)
                .ToArray();

            ViewBag.DatosDiagnosticos = diagnosticosAgrupados
                .Select(d => d.Total)
                .ToArray();

            return View();
        }
    }
}