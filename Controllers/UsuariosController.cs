
using Huellitas.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Huellitas.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuariosController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Usuarios/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = new[]
            {
                "Administrador",
                "Veterinario",
                "Recepcionista"
            };

            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[]
                {
                    "Administrador",
                    "Veterinario",
                    "Recepcionista"
                };

                return View(model);
            }

            // Comprobar que el rol seleccionado exista.
            var rolExiste = await _roleManager.RoleExistsAsync(model.Rol);

            if (!rolExiste)
            {
                ModelState.AddModelError(
                    nameof(model.Rol),
                    "El rol seleccionado no existe."
                );

                ViewBag.Roles = new[]
                {
                    "Administrador",
                    "Veterinario",
                    "Recepcionista"
                };

                return View(model);
            }

            // Evitar registrar correos duplicados.
            var usuarioExistente = await _userManager.FindByEmailAsync(model.Email);

            if (usuarioExistente != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Ya existe un usuario registrado con este correo."
                );

                ViewBag.Roles = new[]
                {
                    "Administrador",
                    "Veterinario",
                    "Recepcionista"
                };

                return View(model);
            }

            // Crear el usuario.
            var usuario = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var resultado = await _userManager.CreateAsync(
                usuario,
                model.Password
            );

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ViewBag.Roles = new[]
                {
                    "Administrador",
                    "Veterinario",
                    "Recepcionista"
                };

                return View(model);
            }

            // Asignar el rol seleccionado.
            var resultadoRol = await _userManager.AddToRoleAsync(
                usuario,
                model.Rol
            );

            if (!resultadoRol.Succeeded)
            {
                // Si falla la asignación, eliminar la cuenta recién creada.
                await _userManager.DeleteAsync(usuario);

                foreach (var error in resultadoRol.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ViewBag.Roles = new[]
                {
                    "Administrador",
                    "Veterinario",
                    "Recepcionista"
                };

                return View(model);
            }

            TempData["Mensaje"] = "Usuario registrado correctamente.";

            return RedirectToAction(nameof(Create));
        }
    }
}
