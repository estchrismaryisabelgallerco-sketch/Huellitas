using System.Security.Claims;
using Huellitas.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Huellitas.Filters
{
    /// <summary>
    /// Controla de forma continua el estado laboral del personal autenticado
    /// en cada petición MVC, sin afectar al rol Administrador.
    /// </summary>
    public class EstadoLaboralFilter : IAsyncAuthorizationFilter
    {
        private readonly HuellitasContext _context;

        public EstadoLaboralFilter(HuellitasContext context)
        {
            _context = context;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return;
            }

            // Respetar acciones con [AllowAnonymous] (incluye Home/Error).
            bool permiteAnonimo = context.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>()
                .Any();

            if (permiteAnonimo)
            {
                return;
            }

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // Mantener siempre accesibles Login, Logout y AccessDenied.
            if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // El Administrador conserva todos sus permisos y no depende de Personal.
            if (user.IsInRole("Administrador"))
            {
                return;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                await CerrarSesionYRedirigirAsync(context);
                return;
            }

            var estadoLaboral = await _context.Personal
                .AsNoTracking()
                .Where(p => p.IdentityUserId == userId)
                .Select(p => p.EstadoLaboral)
                .FirstOrDefaultAsync();

            // Sin ficha Personal o en estado Inactivo (o desconocido): cerrar sesión de la petición actual.
            if (estadoLaboral == null ||
                string.Equals(estadoLaboral, "Inactivo", StringComparison.Ordinal) ||
                (!string.Equals(estadoLaboral, "Activo", StringComparison.Ordinal) &&
                 !string.Equals(estadoLaboral, "Vacaciones", StringComparison.Ordinal)))
            {
                await CerrarSesionYRedirigirAsync(context);
                return;
            }

            // En Vacaciones: mantener sesión, pero permitir solo consultas de inicio permitidas.
            if (string.Equals(estadoLaboral, "Vacaciones", StringComparison.Ordinal))
            {
                bool esGet = HttpMethods.IsGet(context.HttpContext.Request.Method);
                bool esRutaPermitida = esGet &&
                    string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase) &&
                    (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(action, "Privacy", StringComparison.OrdinalIgnoreCase));

                if (!esRutaPermitida)
                {
                    context.Result = new ForbidResult();
                }
            }
        }

        private static async Task CerrarSesionYRedirigirAsync(
            AuthorizationFilterContext context)
        {
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
