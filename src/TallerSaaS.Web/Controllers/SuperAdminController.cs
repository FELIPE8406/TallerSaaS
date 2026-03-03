using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Infrastructure.Data;
using TallerSaaS.Web.Models;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : Controller
{
    private readonly DashboardService _dashboardService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SuperAdminController(DashboardService dashboardSvc,
                                ApplicationDbContext db,
                                UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardSvc;
        _db               = db;
        _userManager      = userManager;
    }

    // ── Index — Executive Dashboard ────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var dashboard = await _dashboardService.GetSuperAdminDashboardAsync();
        return View(dashboard);
    }

    // ── Tenants — full list ────────────────────────────────────────────────
    public async Task<IActionResult> Tenants()
    {
        var tenants = await _db.Tenants
            .Include(t => t.PlanSuscripcion)
            .OrderByDescending(t => t.FechaAlta)
            .ToListAsync();
        return View(tenants);
    }

    // ── Toggle activo/inactivo ─────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActivo(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        tenant.Activo = !tenant.Activo;
        await _db.SaveChangesAsync();
        TempData["Exito"] = $"Taller '{tenant.Nombre}' {(tenant.Activo ? "activado" : "desactivado")}.";
        return RedirectToAction(nameof(Tenants));
    }

    // ── Planes ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Planes()
    {
        var planes = await _db.PlanesSuscripcion.ToListAsync();
        return View(planes);
    }

    // ── Pagos ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Pagos()
    {
        var pagos = await _db.Pagos
            .Include(p => p.Tenant)
            .Include(p => p.PlanSuscripcion)
            .OrderByDescending(p => p.Fecha)
            .Take(100)
            .ToListAsync();
        return View(pagos);
    }

    // ── NuevoTenant — GET ──────────────────────────────────────────────────
    public async Task<IActionResult> NuevoTenant()
    {
        await PopulatePlanes();
        return View(new NuevoTenantViewModel());
    }

    // ── NuevoTenant — POST ─────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoTenant(NuevoTenantViewModel vm)
    {
        // Extra server-side check: email must not already exist
        if (await _userManager.FindByEmailAsync(vm.AdminEmail) != null)
            ModelState.AddModelError(nameof(vm.AdminEmail),
                "Ya existe un usuario con ese correo.");

        if (!ModelState.IsValid)
        {
            await PopulatePlanes();
            return View(vm);
        }

        // 1. Create tenant record
        var tenant = new Tenant
        {
            Id               = Guid.NewGuid(),
            Nombre           = vm.Nombre,
            RFC              = vm.RFC,
            Email            = vm.Email,
            Telefono         = vm.Telefono,
            Direccion        = vm.Direccion,
            PlanSuscripcionId = vm.PlanSuscripcionId,
            Activo           = true,
            FechaAlta        = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();   // commit tenant first to get its Id

        // 2. Create the initial Admin user bound to this tenant
        var adminUser = new ApplicationUser
        {
            UserName       = vm.AdminEmail,
            Email          = vm.AdminEmail,
            NombreCompleto = $"Administrador — {vm.Nombre}",
            TenantId       = tenant.Id,
            EmailConfirmed = true,
            Activo         = true
        };

        var createResult = await _userManager.CreateAsync(adminUser, vm.AdminPassword);

        if (createResult.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
            TempData["Exito"] =
                $"Taller '{tenant.Nombre}' creado. Administrador {vm.AdminEmail} vinculado con rol Admin.";
            return RedirectToAction(nameof(Tenants));
        }

        // If user creation fails — rollback the tenant
        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync();

        foreach (var err in createResult.Errors)
            ModelState.AddModelError(string.Empty, err.Description);

        await PopulatePlanes();
        return View(vm);
    }

    // ── AccesoSoporte — SuperAdmin entra como taller específico ────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AccesoSoporte(Guid tenantId)
    {
        // Extra security guard: must be SuperAdmin
        if (!User.IsInRole("SuperAdmin")) return Forbid();

        var tenant = await _db.Tenants
            .Include(t => t.PlanSuscripcion)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null) return NotFound();

        // Write to session — TenantMiddleware reads this before claims on every request
        HttpContext.Session.SetString("ImpersonatedTenantId",     tenantId.ToString());
        HttpContext.Session.SetString("ImpersonatedTenantNombre", tenant.Nombre);

        TempData["AccesoSoporte"] = tenant.Nombre;
        TempData["Exito"] = $"🛡️ Acceso de Soporte activo para: {tenant.Nombre}";
        return RedirectToAction("Index", "Dashboard");
    }

    // ── TerminarSoporte — volver al panel SuperAdmin ────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult TerminarSoporte()
    {
        HttpContext.Session.Remove("ImpersonatedTenantId");
        HttpContext.Session.Remove("ImpersonatedTenantNombre");
        TempData["Exito"] = "Sesión de soporte finalizada.";
        return RedirectToAction(nameof(Tenants));
    }

    // ── Legacy alias — redirect old route to new one ───────────────────────
    public IActionResult CrearTenant() => RedirectToAction(nameof(NuevoTenant));

    // ── Private helpers ────────────────────────────────────────────────────
    private async Task PopulatePlanes()
    {
        var planes = await _db.PlanesSuscripcion
            .Where(p => p.Activo)
            .OrderBy(p => p.Precio)
            .ToListAsync();

        ViewBag.Planes = new SelectList(
            planes.Select(p => new
            {
                p.Id,
                Texto = $"{p.Nombre} — ${p.Precio:N0}/mes · {p.LimiteUsuarios} usuarios"
            }),
            "Id", "Texto");

        // Also pass full objects for the price summary card
        ViewBag.PlanesData = planes;
    }
}
