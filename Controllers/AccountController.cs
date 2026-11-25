
using Huellitas.Data;
using Huellitas.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Huellitas.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly HuellitasContext _context;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            HuellitasContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        // MOSTRAR FORMULARIO DE INICIO DE SESION
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // PROCESAR INICIO DE SESION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _userManager.FindByEmailAsync(model.Email);

            if (usuario == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Correo o contraseña incorrectos."
                );

                return View(model);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                usuario,
                model.Password,
                lockoutOnFailure: true
            );

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La cuenta está temporalmente bloqueada. Intenta más tarde."
                );

                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Correo o contraseña incorrectos."
                );

                return View(model);
            }

            var roles = await _userManager.GetRolesAsync(usuario);
            bool esAdministrador = roles.Contains("Administrador");
            bool esRolOperativo = roles.Contains("Veterinario") ||
                                  roles.Contains("Recepcionista");

            if (!esAdministrador && !esRolOperativo)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La cuenta no tiene un rol autorizado para acceder al sistema."
                );

                return View(model);
            }

            if (!esAdministrador)
            {
                var personal = await _context.Personal
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdentityUserId == usuario.Id);

                if (personal == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No existe una ficha de personal vinculada a esta cuenta. Contacta al administrador."
                    );

                    return View(model);
                }

                if (personal.EstadoLaboral == "Inactivo")
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Tu cuenta laboral se encuentra inactiva. Contacta al administrador."
                    );

                    return View(model);
                }

                if (personal.EstadoLaboral != "Activo" &&
                    personal.EstadoLaboral != "Vacaciones")
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "El estado laboral de tu cuenta no permite iniciar sesión. Contacta al administrador."
                    );

                    return View(model);
                }
            }

            await _signInManager.SignInAsync(
                usuario,
                isPersistent: model.RememberMe
            );

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // CERRAR SESION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        // ACCESO DENEGADO
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
