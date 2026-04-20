using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Application.Services;
using TallerSaaS.Application.Services.Exporters;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Infrastructure.Data;
using TallerSaaS.Infrastructure.Middleware;
using TallerSaaS.Infrastructure.Repositories;
using TallerSaaS.Infrastructure.Services;
using TallerSaaS.Web.Infrastructure;
using TallerSaaS.Web.Filters;
using QuestPDF.Infrastructure;

// ─── Cultures: es-CO for DISPLAY only; model binder uses InvariantCulture ───────
// HTML <input type="number"> always sends dot-decimal (87500.50), not comma.
// Setting thread culture to es-CO causes decimal model binding to silently fail
// (PrecioUnitario, Cantidad → 0). We keep es-CO for ToString/formatting only.
var copCulture = new CultureInfo("es-CO");
CultureInfo.DefaultThreadCurrentCulture   = copCulture;
CultureInfo.DefaultThreadCurrentUICulture = copCulture;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ─── EF Core + Identity ───────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Retry logic: hasta 6 reintentos ante errores transitorios (cortes de red,
            // failover, login failed transitorio). Espera máxima de 20s entre intentos.
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 6,
                maxRetryDelay: TimeSpan.FromSeconds(20),
                errorNumbersToAdd: null  // null = lista default de errores transitorios de SQL Server
            );
            // 120s: margen amplio para el startup/migrations en entornos de Azure SQL con cold start.
            sqlOptions.CommandTimeout(120);
        });

    // Solo en desarrollo: log de todos los comandos SQL para detectar queries lentas.
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.LogTo(
            Console.WriteLine,
            new[] { DbLoggerCategory.Database.Command.Name },
            Microsoft.Extensions.Logging.LogLevel.Information);
    }
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<TenantClaimsFactory>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        // Se recomienda mover estos valores a appsettings.json o Secret Manager
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "TU_CLIENT_ID_GOOGLE";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "TU_CLIENT_SECRET_GOOGLE";
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "TU_CLIENT_ID_MICROSOFT";
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "TU_CLIENT_SECRET_MICROSOFT";
    });

// ─── Authentication / Authorization ──────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ─── Multi-tenant: scoped so each request gets its own instance ───────────────
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
// Register IApplicationDbContext → ApplicationDbContext (same instance per scope)
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// ─── Repositories ────────────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

// ─── Application Services ────────────────────────────────────────────────────
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<VehiculoService>();
builder.Services.AddScoped<TrazabilidadService>();
builder.Services.AddScoped<InventarioService>();
builder.Services.AddScoped<BodegaService>();
builder.Services.AddScoped<OrdenService>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<AccountingService>(); 
builder.Services.AddScoped<IAccountingService>(sp => sp.GetRequiredService<AccountingService>());
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<CsvExportStrategy>();
builder.Services.AddScoped<TxtExportStrategy>();
builder.Services.AddScoped<PdfExportStrategy>();
builder.Services.AddScoped<IUserProvider, UserProvider>();
builder.Services.AddScoped<INominaService, NominaService>();
builder.Services.AddScoped<IEmpleadoContratoService, EmpleadoContratoService>();

// ─── MVC — model binder uses InvariantCulture for all numeric types ───────────
// This ensures HTML number inputs (always dot-decimal) bind correctly even when
// the thread culture is es-CO (comma-decimal). Display formatting is unaffected.
builder.Services.AddControllersWithViews(options =>
{
    // Parse decimal/double/float with InvariantCulture (dot-decimal)
    // so HTML number inputs work correctly regardless of thread culture (es-CO)
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
    
    // Performance: Global filter to handle partial content for AJAX/SPA requests
    options.Filters.Add<AjaxLayoutFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
});

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// ── Database migration & seed ───────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = new[] { "SuperAdmin", "Admin", "Mecanico" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var email = "superadmin@tallersaas.com";
        var password = "SuperAdmin123!";

        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NombreCompleto = "Super Administrador",
                EsSuperAdmin = true,
                Activo = true
            };

            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, "SuperAdmin");
        }
        else
        {
            // Forzar estado óptimo y rol
            user.EmailConfirmed = true;
            user.EsSuperAdmin = true;
            user.Activo = true;

            await userManager.UpdateAsync(user);

            // Reset password seguro para garantizar acceso
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, password);

            // Asegurar pertenencia al rol
            if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(user, "SuperAdmin");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error crítico en seed de SuperAdmin o Migración");
        // No relanzamos para permitir el arranque de la app en MonsterASP
    }
}


// ─── Middleware pipeline ──────────────────────────────────────────────────────

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();   // ← must be before Authentication
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();   // ← Tenant detection AFTER auth
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();



