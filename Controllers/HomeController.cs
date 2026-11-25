
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Huellitas.Data;
using Huellitas.Models;

namespace Huellitas.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HuellitasContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        HuellitasContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Indicadores generales: datos reales de MariaDB.
        ViewBag.TotalPropietarios =
            await _context.Propietarios.CountAsync();

        ViewBag.TotalMascotas =
            await _context.Mascotas.CountAsync();

        ViewBag.TotalCitas =
            await _context.Citas.CountAsync();

        ViewBag.TotalConsultas =
            await _context.Consultas.CountAsync();

        // Agrupar las citas según su estado.
        var citasAgrupadas = await _context.Citas
            .AsNoTracking()
            .GroupBy(c => c.Estado)
            .Select(g => new
            {
                Estado = g.Key,
                Total = g.Count()
            })
            .ToListAsync();

        // Incluimos todos los estados, incluso si tienen cero citas.
        string[] estados = 
        {
            "Pendiente",
            "Confirmada",
            "Atendida",
            "Cancelada"
        };

        int ObtenerTotalEstado(string estado)
        {
            return citasAgrupadas
                .Where(c => c.Estado.ToString() == estado)
                .Select(c => c.Total)
                .FirstOrDefault();
        }

        ViewBag.EtiquetasCitas = estados;
        ViewBag.DatosCitas = estados
            .Select(ObtenerTotalEstado)
            .ToArray();

        // Agrupar mascotas por especie.
        var mascotasAgrupadas = await _context.Mascotas
            .AsNoTracking()
            .GroupBy(m => m.Especie)
            .Select(g => new
            {
                Especie = g.Key,
                Total = g.Count()
            })
            .ToListAsync();

        var especiesOrdenadas = mascotasAgrupadas
            .OrderByDescending(m => m.Total)
            .ToList();

        ViewBag.EtiquetasMascotas = especiesOrdenadas
            .Select(m => string.IsNullOrWhiteSpace(m.Especie)
                ? "Sin especificar"
                : m.Especie)
            .ToArray();

        ViewBag.DatosMascotas = especiesOrdenadas
            .Select(m => m.Total)
            .ToArray();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}
