
using Huellitas.Data;
using Huellitas.Filters;
using Huellitas.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<EstadoLaboralFilter>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<EstadoLaboralFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var serverVersion = new MariaDbServerVersion(new Version(13, 0, 2));

builder.Services.AddDbContext<HuellitasContext>(options =>
    options.UseMySql(
        connectionString,
        serverVersion
    )
);

// Configuración de ASP.NET Core Identity
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<HuellitasContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Servicio automático de cancelación de citas vencidas.
builder.Services.AddHostedService<CitasVencidasService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        "Administrador",
        "Veterinario",
        "Recepcionista"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// CREAR EL PRIMER USUARIO ADMINISTRADOR

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    var configuration = services.GetRequiredService<IConfiguration>();

    var adminEmail = configuration["AdminSeed:Email"];
    var adminPassword = configuration["AdminSeed:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) ||
        string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "No se configuraron las credenciales del administrador en User Secrets."
        );
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(
            adminUser,
            adminPassword
        );

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(e => e.Description)
            );

            throw new InvalidOperationException(
                "No se pudo crear el administrador: " + errors
            );
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
    {
        var roleResult = await userManager.AddToRoleAsync(
            adminUser,
            "Administrador"
        );

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                roleResult.Errors.Select(e => e.Description)
            );

            throw new InvalidOperationException(
                "No se pudo asignar el rol Administrador: " + errors
            );
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
