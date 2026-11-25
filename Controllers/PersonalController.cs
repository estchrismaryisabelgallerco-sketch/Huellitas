
using Huellitas.Data;
using Huellitas.Models;
using Huellitas.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Huellitas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class PersonalController : Controller
    {
        private readonly HuellitasContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PersonalController(
            HuellitasContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var personal = await _context.Personal
                .Include(p => p.Usuario)
                .OrderBy(p => p.Apellidos)
                .ThenBy(p => p.Nombres)
                .ToListAsync();

            var cargos = new Dictionary<int, string>();

            foreach (var persona in personal)
            {
                if (persona.Usuario == null)
                {
                    cargos[persona.Id] = "Sin cargo";
                    continue;
                }

                var roles = await _userManager.GetRolesAsync(
                    persona.Usuario
                );

                if (roles.Contains("Veterinario"))
                {
                    cargos[persona.Id] = "Veterinario";
                }
                else if (roles.Contains("Recepcionista"))
                {
                    cargos[persona.Id] = "Recepcionista";
                }
                else
                {
                    cargos[persona.Id] = "Sin cargo";
                }
            }

            ViewBag.Cargos = cargos;

            return View(personal);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarDatosFormularioAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearPersonalViewModel model)
        {
            if (model.Rol != "Veterinario" &&
                model.Rol != "Recepcionista")
            {
                ModelState.AddModelError(
                    nameof(model.Rol),
                    "Seleccione un tipo de personal válido."
                );
            }

            if (model.Rol == "Veterinario" &&
                string.IsNullOrWhiteSpace(model.Especialidad))
            {
                ModelState.AddModelError(
                    nameof(model.Especialidad),
                    "La especialidad es obligatoria para el veterinario."
                );
            }

            if (model.Rol == "Recepcionista")
            {
                model.Especialidad = null;
            }

            if (!ModelState.IsValid)
            {
                await CargarDatosFormularioAsync();
                return View(model);
            }

            var usuario = await _userManager.FindByIdAsync(
                model.IdentityUserId
            );

            if (usuario == null)
            {
                ModelState.AddModelError(
                    nameof(model.IdentityUserId),
                    "La cuenta seleccionada no existe."
                );

                await CargarDatosFormularioAsync();
                return View(model);
            }

            var tieneRol = await _userManager.IsInRoleAsync(
                usuario,
                model.Rol
            );

            if (!tieneRol)
            {
                ModelState.AddModelError(
                    nameof(model.IdentityUserId),
                    "La cuenta seleccionada no tiene el rol indicado."
                );

                await CargarDatosFormularioAsync();
                return View(model);
            }

            var yaRegistrado = await _context.Personal
                .AnyAsync(p => p.IdentityUserId == model.IdentityUserId);

            if (yaRegistrado)
            {
                ModelState.AddModelError(
                    nameof(model.IdentityUserId),
                    "Esta cuenta ya está asociada a un registro de personal."
                );

                await CargarDatosFormularioAsync();
                return View(model);
            }

            var personal = new Personal
            {
                IdentityUserId = model.IdentityUserId,
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim(),
                Telefono = model.Telefono.Trim(),
                Especialidad = model.Rol == "Veterinario"
                    ? model.Especialidad?.Trim()
                    : null,
                FechaContratacion = model.FechaContratacion,
                EstadoLaboral = model.EstadoLaboral
            };

            _context.Personal.Add(personal);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Personal registrado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var personal = await _context.Personal
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (personal == null)
                return NotFound();

            await CargarTipoPersonalAsync(personal);

            return View(personal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var personal = await _context.Personal
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (personal == null)
                return NotFound();

            var actualizado = await TryUpdateModelAsync(
                personal,
                "",
                p => p.Nombres,
                p => p.Apellidos,
                p => p.Telefono,
                p => p.Especialidad,
                p => p.FechaContratacion,
                p => p.EstadoLaboral
            );

            if (!actualizado)
            {
                await CargarTipoPersonalAsync(personal);
                return View(personal);
            }

            var esVeterinario = personal.Usuario != null &&
                await _userManager.IsInRoleAsync(
                    personal.Usuario,
                    "Veterinario"
                );

            if (esVeterinario &&
                string.IsNullOrWhiteSpace(personal.Especialidad))
            {
                ModelState.AddModelError(
                    nameof(personal.Especialidad),
                    "La especialidad es obligatoria para el veterinario."
                );
            }

            if (!esVeterinario)
            {
                personal.Especialidad = null;
            }

            if (!new[] { "Activo", "Inactivo", "Vacaciones" }
                .Contains(personal.EstadoLaboral))
            {
                ModelState.AddModelError(
                    nameof(personal.EstadoLaboral),
                    "Seleccione un estado laboral válido."
                );
            }

            if (!ModelState.IsValid)
            {
                await CargarTipoPersonalAsync(personal);
                return View(personal);
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "Datos del personal actualizados correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var personal = await _context.Personal
                .Include(p => p.Usuario)
                .Include(p => p.Horarios)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (personal == null)
                return NotFound();

            int cantidadCitas = await _context.Citas
                .CountAsync(c => c.PersonalId == personal.Id);

            ViewBag.CantidadHorarios = personal.Horarios.Count;
            ViewBag.CantidadCitas = cantidadCitas;
            ViewBag.TieneCitas = cantidadCitas > 0;

            return View(personal);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var personal = await _context.Personal
                .FirstOrDefaultAsync(p => p.Id == id);

            if (personal == null)
                return NotFound();

            int cantidadCitas = await _context.Citas
                .CountAsync(c => c.PersonalId == id);

            if (cantidadCitas > 0)
            {
                TempData["Error"] = $"No se puede eliminar este registro de personal porque tiene " +
                    $"{cantidadCitas} cita(s) asociada(s). " +
                    $"Primero debe reasignar o eliminar esas citas.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Personal.Remove(personal);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se pudo eliminar el registro de personal porque tiene " +
                    "registros relacionados en el sistema.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Mensaje"] =
                "Registro laboral eliminado. La cuenta de usuario continúa existiendo.";

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarTipoPersonalAsync(Personal personal)
        {
            ViewBag.EsVeterinario = personal.Usuario != null &&
                await _userManager.IsInRoleAsync(
                    personal.Usuario,
                    "Veterinario"
                );
        }

        private async Task CargarDatosFormularioAsync()
        {
            var usuarios = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var disponibles = new List<object>();

            foreach (var usuario in usuarios)
            {
                string? rol = null;

                if (await _userManager.IsInRoleAsync(usuario, "Veterinario"))
                {
                    rol = "Veterinario";
                }
                else if (await _userManager.IsInRoleAsync(usuario, "Recepcionista"))
                {
                    rol = "Recepcionista";
                }

                if (rol == null)
                    continue;

                var yaRegistrado = await _context.Personal
                    .AnyAsync(p => p.IdentityUserId == usuario.Id);

                if (!yaRegistrado)
                {
                    disponibles.Add(new
                    {
                        Id = usuario.Id,
                        CorreoRol = $"{usuario.Email} ({rol})"
                    });
                }
            }

            ViewBag.Usuarios = disponibles;

            ViewBag.RolesPersonal = new[]
            {
                "Veterinario",
                "Recepcionista"
            };

            ViewBag.EstadosLaborales = new[]
            {
                "Activo",
                "Inactivo",
                "Vacaciones"
            };
        }
    }
}