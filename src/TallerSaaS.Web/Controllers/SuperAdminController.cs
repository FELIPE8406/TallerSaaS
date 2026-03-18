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

        ViewBag.Planes = await _db.PlanesSuscripcion
            .Where(p => p.Activo)
            .OrderBy(p => p.Precio)
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

    // ── Cambiar Plan ────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPlan(Guid tenantId, int nuevoPlanId)
    {
        var tenant = await _db.Tenants.Include(t => t.PlanSuscripcion).FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        var plan = await _db.PlanesSuscripcion.FindAsync(nuevoPlanId);
        if (plan == null) return NotFound();

        var planAnterior = tenant.PlanSuscripcion?.Nombre ?? "Ninguno";
        tenant.PlanSuscripcionId = nuevoPlanId;
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Plan de '{tenant.Nombre}' actualizado de {planAnterior} a {plan.Nombre}.";
        return RedirectToAction(nameof(Tenants));
    }

    // ── Planes ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Planes()
    {
        var planes = await _db.PlanesSuscripcion
            .OrderBy(p => p.Precio)
            .ToListAsync();
        return View(planes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPlan(PlanViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Datos del plan inválidos.";
            return RedirectToAction(nameof(Planes));
        }

        var plan = new PlanSuscripcion
        {
            Nombre = vm.Nombre,
            Precio = vm.Precio,
            LimiteUsuarios = vm.LimiteUsuarios,
            Descripcion = vm.Descripcion,
            Beneficios = vm.Beneficios,
            ColorHex = vm.ColorHex,
            Activo = true
        };

        _db.PlanesSuscripcion.Add(plan);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Plan '{vm.Nombre}' creado exitosamente.";
        return RedirectToAction(nameof(Planes));
    }

    [HttpGet]
    public async Task<IActionResult> GetPlan(int id)
    {
        var plan = await _db.PlanesSuscripcion.FindAsync(id);
        if (plan == null) return NotFound();
        return Json(plan);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPlan(PlanViewModel vm)
    {
        if (vm.Id == 0) return BadRequest();
        
        var plan = await _db.PlanesSuscripcion.FindAsync(vm.Id);
        if (plan == null) return NotFound();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Error al validar los datos del plan.";
            return RedirectToAction(nameof(Planes));
        }

        plan.Nombre = vm.Nombre;
        plan.Precio = vm.Precio;
        plan.LimiteUsuarios = vm.LimiteUsuarios;
        plan.Descripcion = vm.Descripcion;
        plan.Beneficios = vm.Beneficios;
        plan.ColorHex = vm.ColorHex;

        await _db.SaveChangesAsync();
        TempData["Exito"] = $"Plan '{vm.Nombre}' actualizado correctamente.";
        return RedirectToAction(nameof(Planes));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarPlan(int id)
    {
        var plan = await _db.PlanesSuscripcion.FindAsync(id);
        if (plan == null) return NotFound();

        // Restriction: Cannot delete plan if active tenants are linked
        bool hasTenants = await _db.Tenants.AnyAsync(t => t.PlanSuscripcionId == id);
        if (hasTenants)
        {
            TempData["Error"] = "No se puede eliminar el plan porque hay talleres activos vinculados a él.";
            return RedirectToAction(nameof(Planes));
        }

        _db.PlanesSuscripcion.Remove(plan);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Plan '{plan.Nombre}' eliminado correctamente.";
        return RedirectToAction(nameof(Planes));
    }

    // ── Pagos ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Pagos()
    {
        var summary = await _dashboardService.GetPagosSummaryAsync();
        
        var transacciones = await _db.Pagos
            .Include(p => p.Tenant)
            .Include(p => p.PlanSuscripcion)
            .OrderByDescending(p => p.Fecha)
            .Take(100)
            .ToListAsync();

        var talleres = await _db.Tenants
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        var vm = new TransaccionesViewModel
        {
            Transacciones = transacciones,
            RecaudoMes = summary.RecaudoMes,
            IngresosMesAnterior = summary.IngresosMesAnt,
            PagosPendientes = summary.PagosPendientes,
            TasaRenovacion = summary.TasaRenovacion,
            Talleres = talleres
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPagoManual(PagoManualViewModel vm)
    {
        var tenant = await _db.Tenants
            .Include(t => t.PlanSuscripcion)
            .FirstOrDefaultAsync(t => t.Id == vm.TenantId);

        if (tenant == null)
        {
            TempData["Error"] = "Taller no encontrado.";
            return RedirectToAction(nameof(Pagos));
        }

        var pago = new Pago
        {
            TenantId = vm.TenantId,
            Monto = vm.Monto > 0 ? vm.Monto : (tenant.PlanSuscripcion?.Precio ?? 0m),
            Fecha = vm.Fecha,
            Estado = "Completado",
            Concepto = string.IsNullOrEmpty(vm.Concepto) ? $"Pago Manual — {tenant.PlanSuscripcion?.Nombre ?? "Suscripción"}" : vm.Concepto,
            Referencia = $"MAN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0,4).ToUpper()}",
            PlanSuscripcionId = tenant.PlanSuscripcionId
        };

        _db.Pagos.Add(pago);
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Pago de ${pago.Monto:N0} registrado para {tenant.Nombre}.";
        return RedirectToAction(nameof(Pagos));
    }

    public async Task<IActionResult> ExportarPagosCSV()
    {
        var pagos = await _db.Pagos
            .Include(p => p.Tenant)
            .Include(p => p.PlanSuscripcion)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Fecha,Taller,NIT,Plan,Monto,Estado,Referencia,Concepto");

        foreach (var p in pagos)
        {
            builder.AppendLine($"{p.Fecha:yyyy-MM-dd HH:mm},{p.Tenant?.Nombre},{p.Tenant?.RFC},{p.PlanSuscripcion?.Nombre},{p.Monto:F0},{p.Estado},{p.Referencia},{p.Concepto}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Reporte_Pagos_{DateTime.Now:yyyyMMdd}.csv");
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

        // Handle Branding: Logo Upload
        if (vm.LogoArchivo != null && vm.LogoArchivo.Length > 0)
        {
            var fileName = $"{tenant.Id}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(vm.LogoArchivo.FileName)}";
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
            
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var filePath = Path.Combine(directoryPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await vm.LogoArchivo.CopyToAsync(stream);
            }
            tenant.Logo = $"/uploads/logos/{fileName}";
        }
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
