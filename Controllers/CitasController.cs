using Huellitas.Data;
using Huellitas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Huellitas.Controllers
{
    [Authorize(Roles = "Administrador,Veterinario,Recepcionista")]
    public class CitasController : Controller
    {
        private readonly HuellitasContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CitasController(
            HuellitasContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // 1. LISTAR CITAS
        // ==========================================
        // Administrador y Recepcionista: ven todas las citas.
        // Veterinario: visualiza exclusivamente sus propias citas.
        public async Task<IActionResult> Index()
        {
            var query = _context.Citas
                .Include(c => c.Mascota)
                    .ThenInclude(m => m!.Propietario)
                .Include(c => c.Personal)
                .AsQueryable();

            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                if (personalActual != null)
                {
                    query = query.Where(c => c.PersonalId == personalActual.Id);
                }
                else
                {
                    query = query.Where(c => false);
                }
            }

            var citas = await query
                .OrderByDescending(c => c.FechaHora)
                .ToListAsync();

            return View(citas);
        }

        // ==========================================
        // 2. REGISTRAR CITA - GET
        // ==========================================
        // Administrador no tiene acceso. Recepcionista y Veterinario sí.
        [Authorize(Roles = "Recepcionista,Veterinario")]
        public async Task<IActionResult> Create()
        {
            await CargarMascotas();

            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                if (personalActual == null)
                {
                    TempData["Error"] = "No se encontró un registro de personal asociado a su cuenta.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.EsVeterinario = true;
                ViewBag.VeterinarioNombre = $"{personalActual.Nombres} {personalActual.Apellidos}";
                return View(new Cita { PersonalId = personalActual.Id });
            }

            ViewBag.EsVeterinario = false;
            await CargarVeterinarios();
            return View();
        }

        // ==========================================
        // 3. REGISTRAR CITA - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Recepcionista,Veterinario")]
        public async Task<IActionResult> Create(Cita cita)
        {
            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                if (personalActual == null)
                    return Forbid();

                cita.PersonalId = personalActual.Id;
            }
            else
            {
                if (!cita.PersonalId.HasValue || cita.PersonalId.Value <= 0)
                {
                    ModelState.AddModelError(
                        nameof(cita.PersonalId),
                        "Debe seleccionar un veterinario para la cita.");
                }
            }

            bool mascotaExiste = await _context.Mascotas
                .AnyAsync(m => m.Id == cita.MascotaId);

            if (!mascotaExiste)
            {
                ModelState.AddModelError(
                    nameof(cita.MascotaId),
                    "Debe seleccionar una mascota válida.");
            }

            if (!EstadosValidos(cita.Estado))
            {
                ModelState.AddModelError(
                    nameof(cita.Estado),
                    "El estado seleccionado no es válido.");
            }

            if (ModelState.IsValid)
            {
                await ValidarYAsignarDisponibilidadAsync(cita);
            }

            if (ModelState.IsValid)
            {
                _context.Citas.Add(cita);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CargarMascotas(cita.MascotaId);

            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                ViewBag.EsVeterinario = true;
                ViewBag.VeterinarioNombre = personalActual != null
                    ? $"{personalActual.Nombres} {personalActual.Apellidos}"
                    : "Veterinario actual";
            }
            else
            {
                ViewBag.EsVeterinario = false;
                await CargarVeterinarios(cita.PersonalId);
            }

            return View(cita);
        }

        // ==========================================
        // 4. EDITAR CITA - GET
        // ==========================================
        [Authorize(Roles = "Recepcionista,Veterinario")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var cita = await _context.Citas
                .Include(c => c.Personal)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cita == null)
                return NotFound();

            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                if (personalActual == null || cita.PersonalId != personalActual.Id)
                    return Forbid();

                ViewBag.EsVeterinario = true;
                ViewBag.VeterinarioNombre = $"{personalActual.Nombres} {personalActual.Apellidos}";
            }
            else
            {
                ViewBag.EsVeterinario = false;
                await CargarVeterinarios(cita.PersonalId);
            }

            await CargarMascotas(cita.MascotaId);
            return View(cita);
        }

        // ==========================================
        // 5. EDITAR CITA - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Recepcionista,Veterinario")]
        public async Task<IActionResult> Edit(int id, Cita cita)
        {
            if (id != cita.Id)
                return NotFound();

            var citaExistente = await _context.Citas.FindAsync(id);
            if (citaExistente == null)
                return NotFound();

            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                if (personalActual == null || citaExistente.PersonalId != personalActual.Id)
                    return Forbid();

                cita.PersonalId = personalActual.Id;
            }
            else
            {
                if (!cita.PersonalId.HasValue || cita.PersonalId.Value <= 0)
                {
                    ModelState.AddModelError(
                        nameof(cita.PersonalId),
                        "Debe seleccionar un veterinario para la cita.");
                }
            }

            bool mascotaExiste = await _context.Mascotas
                .AnyAsync(m => m.Id == cita.MascotaId);

            if (!mascotaExiste)
            {
                ModelState.AddModelError(
                    nameof(cita.MascotaId),
                    "Debe seleccionar una mascota válida.");
            }

            if (!EstadosValidos(cita.Estado))
            {
                ModelState.AddModelError(
                    nameof(cita.Estado),
                    "El estado seleccionado no es válido.");
            }

            if (ModelState.IsValid)
            {
                await ValidarYAsignarDisponibilidadAsync(cita, id);
            }

            if (ModelState.IsValid)
            {
                citaExistente.MascotaId = cita.MascotaId;
                citaExistente.PersonalId = cita.PersonalId;
                citaExistente.FechaHora = cita.FechaHora;
                citaExistente.Motivo = cita.Motivo;
                citaExistente.Estado = cita.Estado;
                citaExistente.NumeroConsultorio = cita.NumeroConsultorio;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CargarMascotas(cita.MascotaId);

            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                ViewBag.EsVeterinario = true;
                ViewBag.VeterinarioNombre = personalActual != null
                    ? $"{personalActual.Nombres} {personalActual.Apellidos}"
                    : "Veterinario actual";
            }
            else
            {
                ViewBag.EsVeterinario = false;
                await CargarVeterinarios(cita.PersonalId);
            }

            return View(cita);
        }

        // ==========================================
        // 6. CONFIRMAR ELIMINACIÓN - GET
        // ==========================================
        [Authorize(Roles = "Recepcionista,Veterinario")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var cita = await _context.Citas
                .Include(c => c.Mascota)
                    .ThenInclude(m => m!.Propietario)
                .Include(c => c.Personal)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cita == null)
                return NotFound();

            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                if (personalActual == null || cita.PersonalId != personalActual.Id)
                    return Forbid();
            }

            bool tieneConsulta = await _context.Consultas
                .AnyAsync(c => c.CitaId == cita.Id);

            ViewBag.TieneConsulta = tieneConsulta;

            return View(cita);
        }

        // ==========================================
        // 7. ELIMINAR CITA - POST
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Recepcionista,Veterinario")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita == null)
                return NotFound();

            if (User.IsInRole("Veterinario"))
            {
                var personalActual = await ObtenerPersonalUsuarioActualAsync();
                if (personalActual == null || cita.PersonalId != personalActual.Id)
                    return Forbid();
            }

            bool tieneConsulta = await _context.Consultas
                .AnyAsync(c => c.CitaId == id);

            if (tieneConsulta)
            {
                TempData["Error"] = "No se puede eliminar esta cita porque tiene una consulta " +
                    "médica registrada. Primero debe eliminar la consulta vinculada.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Citas.Remove(cita);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se pudo eliminar la cita porque tiene registros " +
                    "relacionados en el sistema.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // MÉTODOS PRIVADOS DE APOYO Y VALIDACIÓN
        // ==========================================

        private async Task<Personal?> ObtenerPersonalUsuarioActualAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return null;

            return await _context.Personal
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);
        }

        private async Task ValidarYAsignarDisponibilidadAsync(Cita cita, int? excluirCitaId = null)
        {
            if (!cita.PersonalId.HasValue)
                return;

            var veterinario = await _context.Personal
                .Include(p => p.Usuario)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == cita.PersonalId.Value);

            bool existe = veterinario != null;
            bool tieneCuenta = existe && veterinario!.Usuario != null;
            bool esVeterinario = tieneCuenta && await _userManager.IsInRoleAsync(veterinario!.Usuario!, "Veterinario");
            bool estaActivo = existe && veterinario!.EstadoLaboral == "Activo";

            if (!existe || !tieneCuenta || !esVeterinario || !estaActivo)
            {
                ModelState.AddModelError(
                    nameof(cita.PersonalId),
                    "El profesional seleccionado debe ser un veterinario activo con cuenta vinculada.");
                return;
            }

            TimeSpan inicio = cita.FechaHora.TimeOfDay;
            TimeSpan fin = inicio.Add(TimeSpan.FromMinutes(45));
            DayOfWeek dia = cita.FechaHora.DayOfWeek;

            if (fin > TimeSpan.FromDays(1))
            {
                ModelState.AddModelError(
                    nameof(cita.FechaHora),
                    "La cita no puede finalizar después de la medianoche.");
                return;
            }

            var turno = await _context.HorariosPersonal
                .AsNoTracking()
                .FirstOrDefaultAsync(h =>
                    h.PersonalId == cita.PersonalId.Value &&
                    h.DiaSemana == dia &&
                    h.HoraEntrada <= inicio &&
                    h.HoraSalida >= fin);

            if (turno == null)
            {
                ModelState.AddModelError(
                    nameof(cita.FechaHora),
                    "El veterinario no tiene un turno laboral que cubra la cita completa (45 minutos) en el horario seleccionado.");
                return;
            }

            // Asignación automática del consultorio desde el horario de trabajo
            cita.NumeroConsultorio = turno.NumeroConsultorio;

            // Detección de citas superpuestas (45 minutos de duración)
            DateTime nuevaInicio = cita.FechaHora;
            DateTime nuevaFin = nuevaInicio.AddMinutes(45);
            DateTime ventanaInicio = nuevaInicio.AddMinutes(-45);

            bool haySolapamiento = await _context.Citas
                .AnyAsync(c =>
                    c.PersonalId == cita.PersonalId.Value &&
                    (!excluirCitaId.HasValue || c.Id != excluirCitaId.Value) &&
                    c.FechaHora < nuevaFin &&
                    c.FechaHora > ventanaInicio &&
                    (c.Estado == "Pendiente" || c.Estado == "Confirmada" || c.Estado == "Atendida"));

            if (haySolapamiento)
            {
                ModelState.AddModelError(
                    nameof(cita.FechaHora),
                    "El veterinario ya tiene otra cita agendada en ese horario (las citas tienen una duración fija de 45 minutos).");
            }
        }

        private async Task CargarMascotas(int? mascotaSeleccionada = null)
        {
            ViewBag.MascotaId = new SelectList(
                await _context.Mascotas
                    .Include(m => m.Propietario)
                    .OrderBy(m => m.Nombre)
                    .Select(m => new
                    {
                        m.Id,
                        NombreCompleto = m.Nombre + " - " +
                            (m.Propietario != null
                                ? m.Propietario.Nombres + " " + m.Propietario.Apellidos
                                : "Sin propietario")
                    })
                    .ToListAsync(),
                "Id",
                "NombreCompleto",
                mascotaSeleccionada
            );
        }

        private async Task CargarVeterinarios(int? veterinarioSeleccionado = null)
        {
            var personalActivo = await _context.Personal
                .Include(p => p.Usuario)
                .Where(p => p.EstadoLaboral == "Activo")
                .OrderBy(p => p.Apellidos)
                .ThenBy(p => p.Nombres)
                .ToListAsync();

            var veterinarios = new List<object>();

            foreach (var persona in personalActivo)
            {
                if (persona.Usuario != null && await _userManager.IsInRoleAsync(persona.Usuario, "Veterinario"))
                {
                    veterinarios.Add(new
                    {
                        persona.Id,
                        NombreCompleto = $"{persona.Nombres} {persona.Apellidos}" +
                            (!string.IsNullOrWhiteSpace(persona.Especialidad) ? $" ({persona.Especialidad})" : "")
                    });
                }
            }

            ViewBag.PersonalId = new SelectList(
                veterinarios,
                "Id",
                "NombreCompleto",
                veterinarioSeleccionado
            );
        }

        private bool EstadosValidos(string estado)
        {
            return estado == "Pendiente" ||
                   estado == "Confirmada" ||
                   estado == "Atendida" ||
                   estado == "Cancelada";
        }
    }
}
