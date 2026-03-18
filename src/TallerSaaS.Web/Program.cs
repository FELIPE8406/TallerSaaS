using Microsoft.AspNetCore.Identity;
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

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// ── Database migration & seed ───────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var config      = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger      = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 3;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            logger.LogInformation("Migración DB — intento {Attempt}/{Max}…", attempt, maxAttempts);
            await db.Database.MigrateAsync();
            logger.LogInformation("✅ Migraciones aplicadas correctamente.");
            break;  // éxito — salir del loop
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
            when (attempt < maxAttempts &&
                  (ex.Number == -2 ||   // Connection Timeout Expired
                   ex.Number ==  0 ||   // General network / transport error
                   ex.Number == 53 ||   // Named Pipes Provider error
                   ex.Number == 18456)) // Login failed (transitorio en Azure SQL)
        {
            var delay = TimeSpan.FromSeconds(attempt * 5); // backoff: 5 s, 10 s
            logger.LogWarning(ex,
                "⚠ SqlException #{SqlError} en migración (intento {Attempt}/{Max}). "
                + "Reintentando en {Delay}s…",
                ex.Number, attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay);
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(attempt * 5);
            logger.LogWarning(ex,
                "⚠ Error inesperado en migración (intento {Attempt}/{Max}). "
                + "Reintentando en {Delay}s…",
                attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            // Último intento fallido → crash controlado con log crítico
            logger.LogCritical(ex,
                "❌ Fallo definitivo en la migración DB tras {Max} intentos. "
                + "La aplicación no puede iniciar.", maxAttempts);
            throw; // re-throw: el host registra correctamente y no silencia el error
        }
    }

    await SeedDataAsync(userManager, roleManager, config);
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

// ─── Seed: Roles + SuperAdmin ─────────────────────────────────────────────────
static async Task SeedDataAsync(UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager, IConfiguration config)
{
    string[] roles = ["SuperAdmin", "Admin", "Mecanico"];
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    var superAdminEmail = config["SuperAdmin:Email"] ?? "superadmin@tallersaas.com";
    var superAdminPass = config["SuperAdmin:Password"] ?? "SuperAdmin@2025!";

    if (await userManager.FindByEmailAsync(superAdminEmail) == null)
    {
        var superAdmin = new ApplicationUser
        {
            UserName = superAdminEmail, Email = superAdminEmail,
            NombreCompleto = "Super Administrador", EsSuperAdmin = true, Activo = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(superAdmin, superAdminPass);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
    }
}
