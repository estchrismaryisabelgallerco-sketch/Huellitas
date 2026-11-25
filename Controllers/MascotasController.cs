
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Huellitas.Data;
using Huellitas.Models;

namespace Huellitas.Controllers
{
    [Authorize(Roles = "Administrador,Veterinario,Recepcionista")]
    public class MascotasController : Controller
    {
        private readonly HuellitasContext _context;

        public MascotasController(HuellitasContext context)
        {
            _context = context;
        }

        // GET: Mascotas
        public async Task<IActionResult> Index()
        {
            var mascotas = await _context.Mascotas
                .Include(m => m.Propietario)
                .ToListAsync();

            return View(mascotas);
        }

        // GET: Mascotas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mascota = await _context.Mascotas
                .Include(m => m.Propietario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mascota == null)
            {
                return NotFound();
            }

            return View(mascota);
        }

        // GET: Mascotas/Historial/5
        [Authorize(Roles = "Administrador,Veterinario")]
        public async Task<IActionResult> Historial(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Obtener la mascota y su propietario.
            var mascota = await _context.Mascotas
                .Include(m => m.Propietario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mascota == null)
            {
                return NotFound();
            }

            // Obtener todas las consultas de esta mascota.
            var consultas = await _context.Consultas
                .Include(c => c.Cita)
                .Where(c => c.MascotaId == id)
                .OrderByDescending(c => c.FechaHora)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Consultas = consultas;

            return View(mascota);
        }

        // GET: Mascotas/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarPropietariosAsync();
            return View();
        }

        // POST: Mascotas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Mascota mascota)
        {
            bool propietarioExiste = await _context.Propietarios
                .AnyAsync(p => p.Id == mascota.PropietarioId);

            if (!propietarioExiste)
            {
                ModelState.AddModelError(
                    "PropietarioId",
                    "Debe seleccionar un propietario válido."
                );
            }

            if (ModelState.IsValid)
            {
                _context.Mascotas.Add(mascota);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await CargarPropietariosAsync(mascota.PropietarioId);
            return View(mascota);
        }

        // GET: Mascotas/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mascota = await _context.Mascotas.FindAsync(id);

            if (mascota == null)
            {
                return NotFound();
            }

            await CargarPropietariosAsync(mascota.PropietarioId);
            return View(mascota);
        }

        // POST: Mascotas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Mascota mascota)
        {
            if (id != mascota.Id)
            {
                return NotFound();
            }

            bool propietarioExiste = await _context.Propietarios
                .AnyAsync(p => p.Id == mascota.PropietarioId);

            if (!propietarioExiste)
            {
                ModelState.AddModelError(
                    "PropietarioId",
                    "Debe seleccionar un propietario válido."
                );
            }

            if (ModelState.IsValid)
            {
                var mascotaExistente = await _context.Mascotas
                    .FindAsync(id);

                if (mascotaExistente == null)
                {
                    return NotFound();
                }

                mascotaExistente.Nombre = mascota.Nombre;
                mascotaExistente.Especie = mascota.Especie;
                mascotaExistente.Raza = mascota.Raza;
                mascotaExistente.Sexo = mascota.Sexo;
                mascotaExistente.FechaNacimiento = mascota.FechaNacimiento;
                mascotaExistente.FotoUrl = mascota.FotoUrl;
                mascotaExistente.PropietarioId = mascota.PropietarioId;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await CargarPropietariosAsync(mascota.PropietarioId);
            return View(mascota);
        }

        // GET: Mascotas/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mascota = await _context.Mascotas
                .Include(m => m.Propietario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mascota == null)
            {
                return NotFound();
            }

            int cantidadCitas = await _context.Citas
                .CountAsync(c => c.MascotaId == mascota.Id);

            int cantidadConsultas = await _context.Consultas
                .CountAsync(c => c.MascotaId == mascota.Id);

            ViewBag.CantidadCitas = cantidadCitas;
            ViewBag.CantidadConsultas = cantidadConsultas;
            ViewBag.TieneDependencias = cantidadCitas > 0 || cantidadConsultas > 0;

            return View(mascota);
        }

        // POST: Mascotas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mascota = await _context.Mascotas.FindAsync(id);

            if (mascota == null)
            {
                return NotFound();
            }

            int cantidadCitas = await _context.Citas
                .CountAsync(c => c.MascotaId == id);

            int cantidadConsultas = await _context.Consultas
                .CountAsync(c => c.MascotaId == id);

            if (cantidadCitas > 0 || cantidadConsultas > 0)
            {
                var partes = new List<string>();
                if (cantidadCitas > 0)
                    partes.Add($"{cantidadCitas} cita(s)");
                if (cantidadConsultas > 0)
                    partes.Add($"{cantidadConsultas} consulta(s)");

                TempData["Error"] = $"No se puede eliminar esta mascota porque tiene " +
                    $"{string.Join(" y ", partes)} registradas. " +
                    $"Primero debe cancelar o eliminar esos registros.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Mascotas.Remove(mascota);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se pudo eliminar la mascota porque tiene registros " +
                    "relacionados en el sistema. Elimine primero las citas y consultas asociadas.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarPropietariosAsync(
            int? propietarioSeleccionado = null)
        {
            var propietarios = await _context.Propietarios
                .OrderBy(p => p.Nombres)
                .ThenBy(p => p.Apellidos)
                .ToListAsync();

            ViewData["PropietarioId"] = new SelectList(
                propietarios.Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombres + " " + p.Apellidos
                }),
                "Id",
                "NombreCompleto",
                propietarioSeleccionado
            );
        }
    }
}