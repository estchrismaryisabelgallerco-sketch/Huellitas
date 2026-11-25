
using Huellitas.Data;
using Huellitas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Huellitas.Controllers
{
    [Authorize(Roles = "Administrador,Veterinario")]
    public class ConsultasController : Controller
    {
        private readonly HuellitasContext _context;

        public ConsultasController(HuellitasContext context)
        {
            _context = context;
        }

        // GET: Consultas
        // Administrador y Veterinario pueden visualizar.
        public async Task<IActionResult> Index()
        {
            var consultas = await _context.Consultas
                .Include(c => c.Mascota)
                    .ThenInclude(m => m!.Propietario)
                .Include(c => c.Cita)
                .OrderByDescending(c => c.FechaHora)
                .ToListAsync();

            return View(consultas);
        }

        // GET: Consultas/Create
        // Solo Veterinario.
        [Authorize(Roles = "Veterinario")]
        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();

            return View();
        }

        // POST: Consultas/Create
        // Solo Veterinario.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Veterinario")]
        public async Task<IActionResult> Create(Consulta consulta)
        {
            ModelState.Remove(nameof(Consulta.Mascota));
            ModelState.Remove(nameof(Consulta.Cita));

            if (consulta.FechaHora == DateTime.MinValue)
            {
                ModelState.AddModelError(
                    nameof(consulta.FechaHora),
                    "Debe ingresar una fecha y hora válidas."
                );
            }

            Cita? citaSeleccionada = null;

            // Validar la cita seleccionada.
            if (consulta.CitaId.HasValue)
            {
                citaSeleccionada = await _context.Citas
                    .FirstOrDefaultAsync(c => c.Id == consulta.CitaId.Value);

                if (citaSeleccionada == null)
                {
                    ModelState.AddModelError(
                        nameof(consulta.CitaId),
                        "La cita seleccionada no existe."
                    );
                }
                else
                {
                    if (citaSeleccionada.MascotaId != consulta.MascotaId)
                    {
                        ModelState.AddModelError(
                            nameof(consulta.CitaId),
                            "La cita no pertenece a la mascota seleccionada."
                        );
                    }

                    if (citaSeleccionada.Estado == "Cancelada")
                    {
                        ModelState.AddModelError(
                            nameof(consulta.CitaId),
                            "No se puede asociar una cita cancelada."
                        );
                    }

                    bool citaYaUtilizada = await _context.Consultas
                        .AnyAsync(c => c.CitaId == consulta.CitaId);

                    if (citaYaUtilizada)
                    {
                        ModelState.AddModelError(
                            nameof(consulta.CitaId),
                            "Esta cita ya tiene una consulta registrada."
                        );
                    }
                }
            }

            // Validar que la mascota exista.
            bool mascotaExiste = await _context.Mascotas
                .AnyAsync(m => m.Id == consulta.MascotaId);

            if (!mascotaExiste)
            {
                ModelState.AddModelError(
                    nameof(consulta.MascotaId),
                    "La mascota seleccionada no existe."
                );
            }

            if (ModelState.IsValid)
            {
                // Registrar la consulta.
                _context.Consultas.Add(consulta);

                // Cambiar automáticamente la cita a Atendida.
                if (citaSeleccionada != null)
                {
                    citaSeleccionada.Estado = "Atendida";
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CargarListasAsync(
                consulta.MascotaId,
                consulta.CitaId
            );

            return View(consulta);
        }

        // GET: Consultas/Edit/5
        // Solo Veterinario.
        [Authorize(Roles = "Veterinario")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var consulta = await _context.Consultas.FindAsync(id);

            if (consulta == null)
                return NotFound();

            await CargarListasAsync(
                consulta.MascotaId,
                consulta.CitaId
            );

            return View(consulta);
        }

        // POST: Consultas/Edit/5
        // Solo Veterinario.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Veterinario")]
        public async Task<IActionResult> Edit(int id, Consulta consulta)
        {
            if (id != consulta.Id)
                return NotFound();

            ModelState.Remove(nameof(Consulta.Mascota));
            ModelState.Remove(nameof(Consulta.Cita));

            if (consulta.FechaHora == DateTime.MinValue)
            {
                ModelState.AddModelError(
                    nameof(consulta.FechaHora),
                    "Debe ingresar una fecha y hora válidas."
                );
            }

            Cita? citaSeleccionada = null;

            // Validar la cita seleccionada.
            if (consulta.CitaId.HasValue)
            {
                citaSeleccionada = await _context.Citas
                    .FirstOrDefaultAsync(c => c.Id == consulta.CitaId.Value);

                if (citaSeleccionada == null)
                {
                    ModelState.AddModelError(
                        nameof(consulta.CitaId),
                        "La cita seleccionada no existe."
                    );
                }
                else
                {
                    if (citaSeleccionada.MascotaId != consulta.MascotaId)
                    {
                        ModelState.AddModelError(
                            nameof(consulta.CitaId),
                            "La cita no pertenece a la mascota seleccionada."
                        );
                    }

                    if (citaSeleccionada.Estado == "Cancelada")
                    {
                        ModelState.AddModelError(
                            nameof(consulta.CitaId),
                            "No se puede asociar una cita cancelada."
                        );
                    }

                    bool citaYaUtilizada = await _context.Consultas
                        .AnyAsync(c =>
                            c.CitaId == consulta.CitaId &&
                            c.Id != consulta.Id
                        );

                    if (citaYaUtilizada)
                    {
                        ModelState.AddModelError(
                            nameof(consulta.CitaId),
                            "Esta cita ya está asociada a otra consulta."
                        );
                    }
                }
            }

            // Validar que la mascota exista.
            bool mascotaExiste = await _context.Mascotas
                .AnyAsync(m => m.Id == consulta.MascotaId);

            if (!mascotaExiste)
            {
                ModelState.AddModelError(
                    nameof(consulta.MascotaId),
                    "La mascota seleccionada no existe."
                );
            }

            if (ModelState.IsValid)
            {
                var consultaExistente = await _context.Consultas
                    .FindAsync(id);

                if (consultaExistente == null)
                    return NotFound();

                // Actualizar los datos de la consulta.
                consultaExistente.MascotaId = consulta.MascotaId;
                consultaExistente.CitaId = consulta.CitaId;
                consultaExistente.FechaHora = consulta.FechaHora;
                consultaExistente.Motivo = consulta.Motivo;
                consultaExistente.Sintomas = consulta.Sintomas;
                consultaExistente.Diagnostico = consulta.Diagnostico;
                consultaExistente.Tratamiento = consulta.Tratamiento;
                consultaExistente.Observaciones = consulta.Observaciones;

                // La cita asociada debe quedar atendida.
                if (citaSeleccionada != null)
                {
                    citaSeleccionada.Estado = "Atendida";
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await CargarListasAsync(
                consulta.MascotaId,
                consulta.CitaId
            );

            return View(consulta);
        }

        // GET: Consultas/Details/5
        // Administrador y Veterinario pueden visualizar.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var consulta = await _context.Consultas
                .Include(c => c.Mascota)
                    .ThenInclude(m => m!.Propietario)
                .Include(c => c.Cita)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consulta == null)
                return NotFound();

            return View(consulta);
        }

        // GET: Consultas/Delete/5
        // Solo Veterinario.
        [Authorize(Roles = "Veterinario")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var consulta = await _context.Consultas
                .Include(c => c.Mascota)
                    .ThenInclude(m => m!.Propietario)
                .Include(c => c.Cita)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consulta == null)
                return NotFound();

            return View(consulta);
        }

        // POST: Consultas/Delete/5
        // Solo Veterinario.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Veterinario")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consulta = await _context.Consultas.FindAsync(id);

            if (consulta == null)
                return NotFound();

            _context.Consultas.Remove(consulta);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Cargar listas desplegables.
        private async Task CargarListasAsync(
            int? mascotaSeleccionada = null,
            int? citaSeleccionada = null)
        {
            // Cargar mascotas con sus propietarios.
            var mascotas = await _context.Mascotas
                .Include(m => m.Propietario)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            ViewBag.MascotaId = new SelectList(
                mascotas.Select(m => new
                {
                    m.Id,
                    NombreCompleto = m.Nombre + " - " +
                        m.Propietario!.Nombres + " " +
                        m.Propietario.Apellidos
                }),
                "Id",
                "NombreCompleto",
                mascotaSeleccionada
            );

            // Cargar citas no canceladas y todavía disponibles.
            var citas = await _context.Citas
                .Include(c => c.Mascota)
                .Where(c =>
                    c.Estado != "Cancelada" &&
                    (!_context.Consultas.Any(x => x.CitaId == c.Id) ||
                     c.Id == citaSeleccionada)
                )
                .OrderByDescending(c => c.FechaHora)
                .ToListAsync();

            ViewBag.CitaId = new SelectList(
                citas.Select(c => new
                {
                    c.Id,
                    Descripcion = "Cita #" + c.Id + " - " +
                        c.Mascota!.Nombre + " - " +
                        c.FechaHora.ToString("dd/MM/yyyy HH:mm") +
                        " (" + c.Estado + ")"
                }),
                "Id",
                "Descripcion",
                citaSeleccionada
            );
        }
    }
}
