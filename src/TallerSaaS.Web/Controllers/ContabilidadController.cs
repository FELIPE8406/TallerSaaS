using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize]
public class ContabilidadController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly ReporteService _reporteService;
    private readonly ICurrentTenantService _tenantService;

    public ContabilidadController(
        IApplicationDbContext db,
        ReporteService reporteService,
        ICurrentTenantService tenantService)
    {
        _db             = db;
        _reporteService = reporteService;
        _tenantService  = tenantService;
    }

    private async Task<bool> IsPremiumAsync()
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue) return false;

        var tenant = await _db.Tenants
            .Include(t => t.PlanSuscripcion)
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

        return tenant?.PlanSuscripcionId == 3;
    }

    public async Task<IActionResult> Index()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();

        ViewBag.IsPremium = await IsPremiumAsync();

        var cuentas   = await _db.CuentasContables.CountAsync();
        var asientos  = await _db.AsientosContables.CountAsync();
        ViewBag.TotalCuentas  = cuentas;
        ViewBag.TotalAsientos = asientos;

        // ATTACK FIX: Only show clients belonging to the current tenant
        var tenantId = _tenantService.TenantId.Value;
        ViewBag.Terceros = await _db.Clientes
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.NombreCompleto)
            .Select(c => new { c.Id, c.NombreCompleto })
            .ToListAsync();

        return View();
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> LibroAuxiliarExcel(Guid? terceroId, DateTime? desde, DateTime? hasta)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        if (!await IsPremiumAsync()) return Forbid();

        // ATTACK FIX: if terceroId provided, verify it belongs to current tenant
        if (terceroId.HasValue)
        {
            var owned = await _db.Clientes.AnyAsync(c => c.Id == terceroId.Value && c.TenantId == _tenantService.TenantId.Value);
            if (!owned) return Forbid();
        }

        try
        {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
            var tNombre = tenant?.Nombre ?? "Taller";
            var tNIT    = tenant?.NIT ?? "N/A";

            var excel = await _reporteService.ExportarLibroAuxiliarExcelAsync(tNombre, tNIT, terceroId, desde, hasta);
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Libro_Auxiliar_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Libro Auxiliar: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> BalancePruebaExcel(DateTime? desde, DateTime? hasta)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        if (!await IsPremiumAsync()) return Forbid();

        try
        {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
            var tNombre = tenant?.Nombre ?? "Taller";
            var tNIT    = tenant?.NIT ?? "N/A";

            var excel = await _reporteService.ExportarBalancePruebaExcelAsync(tNombre, tNIT, desde, hasta);
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Balance_Prueba_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Balance de Prueba: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ATTACK FIX: terceroId accepted from client — validate ownership before generating PDF
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CertificadoRetencionesPdf(Guid terceroId, int anio)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        if (!await IsPremiumAsync()) return Forbid();

        var owned = await _db.Clientes.AnyAsync(c => c.Id == terceroId && c.TenantId == _tenantService.TenantId.Value);
        if (!owned) return Forbid();

        try
        {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
            var tNombre = tenant?.Nombre ?? "Taller";
            var tNIT    = tenant?.NIT ?? "N/A";
            var tCiudad = tenant?.Ciudad ?? "N/A";

            var pdf = await _reporteService.GenerarCertificadoRetencionesPdfAsync(terceroId, anio, tNombre, tNIT, tCiudad);
            return File(pdf, "application/pdf", $"CertificadoRetencion_{terceroId}_{anio}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Certificado: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}
