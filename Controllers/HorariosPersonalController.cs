
using Huellitas.Data;
using Huellitas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Huellitas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class HorariosPersonalController : Controller
    {
        private readonly HuellitasContext _context;

        public HorariosPersonalController(HuellitasContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int personalId)
        {
            var personal = await _context.Personal
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == personalId);

            if (personal == null)
                return NotFound();

            var horarios = await _context.HorariosPersonal
                .Where(h => h.PersonalId == personalId)
                .OrderBy(h => h.DiaSemana)
                .ThenBy(h => h.HoraEntrada)
                .ToListAsync();

            ViewBag.Personal = personal;
            ViewBag.DiasSemana = ObtenerNombreDias();

            return View(horarios);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int personalId)
        {
            var personal = await _context.Personal
                .FirstOrDefaultAsync(p => p.Id == personalId);

            if (personal == null)
                return NotFound();

            ViewBag.Personal = personal;
            CargarDiasSemana();

            return View(new HorarioPersonal
            {
                PersonalId = personalId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int personalId,
            HorarioPersonal modelo)
        {
            var personalExiste = await _context.Personal
                .AnyAsync(p => p.Id == personalId);

            if (!personalExiste)
                return NotFound();

            modelo.PersonalId = personalId;

            ValidarHorario(modelo);

            if (ModelState.IsValid)
            {
                var seSuperpone = await ExisteSuperposicionAsync(
                    personalId,
                    modelo.DiaSemana,
                    modelo.HoraEntrada,
                    modelo.HoraSalida);

                if (seSuperpone)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Este turno se superpone con otro horario del empleado.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Personal = await _context.Personal
                    .FirstAsync(p => p.Id == personalId);

                CargarDiasSemana();
                return View(modelo);
            }

            _context.HorariosPersonal.Add(new HorarioPersonal
            {
                PersonalId = personalId,
                DiaSemana = modelo.DiaSemana,
                HoraEntrada = modelo.HoraEntrada,
                HoraSalida = modelo.HoraSalida,
                NumeroConsultorio = modelo.NumeroConsultorio
            });

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Horario registrado correctamente.";

            return RedirectToAction(
                nameof(Index),
                new { personalId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int personalId, int id)
        {
            var horario = await _context.HorariosPersonal
                .FirstOrDefaultAsync(h =>
                    h.Id == id && h.PersonalId == personalId);

            if (horario == null)
                return NotFound();

            ViewBag.Personal = await _context.Personal
                .FirstAsync(p => p.Id == personalId);

            CargarDiasSemana();

            return View(horario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public async Task<IActionResult> EditConfirmed(
            int personalId,
            int id)
        {
            var horario = await _context.HorariosPersonal
                .FirstOrDefaultAsync(h =>
                    h.Id == id && h.PersonalId == personalId);

            if (horario == null)
                return NotFound();

            var actualizado = await TryUpdateModelAsync(
                horario,
                "",
                h => h.DiaSemana,
                h => h.HoraEntrada,
                h => h.HoraSalida,
                h => h.NumeroConsultorio);

            if (!actualizado)
            {
                ViewBag.Personal = await _context.Personal
                    .FirstAsync(p => p.Id == personalId);

                CargarDiasSemana();
                return View("Edit", horario);
            }

            ValidarHorario(horario);

            if (ModelState.IsValid)
            {
                var seSuperpone = await ExisteSuperposicionAsync(
                    personalId,
                    horario.DiaSemana,
                    horario.HoraEntrada,
                    horario.HoraSalida,
                    horario.Id);

                if (seSuperpone)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Este turno se superpone con otro horario del empleado.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Personal = await _context.Personal
                    .FirstAsync(p => p.Id == personalId);

                CargarDiasSemana();
                return View("Edit", horario);
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Horario actualizado correctamente.";

            return RedirectToAction(
                nameof(Index),
                new { personalId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int personalId, int id)
        {
            var horario = await _context.HorariosPersonal
                .Include(h => h.Personal)
                .FirstOrDefaultAsync(h =>
                    h.Id == id && h.PersonalId == personalId);

            if (horario == null)
                return NotFound();

            ViewBag.Dia = ObtenerNombreDia(horario.DiaSemana);

            return View(horario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int personalId,
            int id)
        {
            var horario = await _context.HorariosPersonal
                .FirstOrDefaultAsync(h =>
                    h.Id == id && h.PersonalId == personalId);

            if (horario == null)
                return NotFound();

            _context.HorariosPersonal.Remove(horario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Horario eliminado correctamente.";

            return RedirectToAction(
                nameof(Index),
                new { personalId });
        }

        private void ValidarHorario(HorarioPersonal horario)
        {
            if (horario.NumeroConsultorio < 1 || horario.NumeroConsultorio > 3)
            {
                ModelState.AddModelError(
                    nameof(horario.NumeroConsultorio),
                    "El número de consultorio debe ser 1, 2 o 3.");
            }

            if (!Enum.IsDefined(typeof(DayOfWeek), horario.DiaSemana))
            {
                ModelState.AddModelError(
                    nameof(horario.DiaSemana),
                    "Seleccione un día válido.");
            }

            if (horario.HoraEntrada < TimeSpan.Zero ||
                horario.HoraEntrada >= TimeSpan.FromDays(1))
            {
                ModelState.AddModelError(
                    nameof(horario.HoraEntrada),
                    "La hora de entrada no es válida.");
            }

            if (horario.HoraSalida <= horario.HoraEntrada ||
                horario.HoraSalida > TimeSpan.FromDays(1))
            {
                ModelState.AddModelError(
                    nameof(horario.HoraSalida),
                    "La hora de salida debe ser posterior a la entrada.");
            }
        }

        private async Task<bool> ExisteSuperposicionAsync(
            int personalId,
            DayOfWeek dia,
            TimeSpan entrada,
            TimeSpan salida,
            int? excluirId = null)
        {
            return await _context.HorariosPersonal.AnyAsync(h =>
                h.PersonalId == personalId &&
                h.DiaSemana == dia &&
                (!excluirId.HasValue || h.Id != excluirId.Value) &&
                h.HoraEntrada < salida &&
                h.HoraSalida > entrada);
        }

        private void CargarDiasSemana()
        {
            ViewBag.DiasSemana = ObtenerNombreDias()
                .Select(d => new SelectListItem
                {
                    Value = ((int)d.Key).ToString(),
                    Text = d.Value
                })
                .ToList();
        }

        private Dictionary<DayOfWeek, string> ObtenerNombreDias()
        {
            return new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Lunes" },
                { DayOfWeek.Tuesday, "Martes" },
                { DayOfWeek.Wednesday, "Miércoles" },
                { DayOfWeek.Thursday, "Jueves" },
                { DayOfWeek.Friday, "Viernes" },
                { DayOfWeek.Saturday, "Sábado" },
                { DayOfWeek.Sunday, "Domingo" }
            };
        }

        private string ObtenerNombreDia(DayOfWeek dia)
        {
            return ObtenerNombreDias().TryGetValue(dia, out var nombre)
                ? nombre
                : dia.ToString();
        }
    }
}
