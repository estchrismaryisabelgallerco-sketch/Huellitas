
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Huellitas.Data;
using Huellitas.Models;

namespace Huellitas.Controllers
{
    public class PropietariosController : Controller
    {
        private readonly HuellitasContext _context;

        public PropietariosController(HuellitasContext context)
        {
            _context = context;
        }

        // GET: Propietarios
        // Los tres roles pueden consultar propietarios.
        [Authorize(Roles = "Administrador,Veterinario,Recepcionista")]
        public async Task<IActionResult> Index()
        {
            var propietarios = await _context.Propietarios.ToListAsync();
            return View(propietarios);
        }

        // GET: Propietarios/Create
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Propietarios/Create
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Propietario propietario)
        {
            if (ModelState.IsValid)
            {
                _context.Propietarios.Add(propietario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(propietario);
        }

        // GET: Propietarios/Edit/5
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var propietario = await _context.Propietarios.FindAsync(id);

            if (propietario == null)
            {
                return NotFound();
            }

            return View(propietario);
        }

        // POST: Propietarios/Edit/5
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Propietario propietario)
        {
            if (id != propietario.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(propietario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(propietario);
        }

        // GET: Propietarios/Delete/5
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var propietario = await _context.Propietarios
                .FirstOrDefaultAsync(p => p.Id == id);

            if (propietario == null)
            {
                return NotFound();
            }

            return View(propietario);
        }

        // POST: Propietarios/Delete/5
        [Authorize(Roles = "Administrador,Recepcionista")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var propietario = await _context.Propietarios
                .Include(p => p.Mascotas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (propietario == null)
            {
                return NotFound();
            }

            if (propietario.Mascotas.Any())
            {
                TempData["Error"] = "No se puede eliminar este propietario porque tiene mascotas registradas.";
                return RedirectToAction(nameof(Index));
            }

            _context.Propietarios.Remove(propietario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}