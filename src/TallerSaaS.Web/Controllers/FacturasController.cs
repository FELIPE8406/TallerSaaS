using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class FacturasController : Controller
{
    private readonly FacturaService _facturaService;
    private readonly OrdenService _ordenService;
    private readonly ReporteService _reporteService;
    private readonly ICurrentTenantService _tenantService;
    private readonly IApplicationDbContext _db;

    public FacturasController(
        FacturaService facturaService,
        OrdenService ordenService,
        ReporteService reporteService,
        ICurrentTenantService tenantService,
        IApplicationDbContext db)
    {
        _facturaService  = facturaService;
        _ordenService    = ordenService;
        _reporteService  = reporteService;
        _tenantService   = tenantService;
        _db              = db;
    }

    public async Task<IActionResult> Index()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var facturas = await _facturaService.GetAllAsync();
        return View(facturas);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(int page = 1, int size = 10)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var paged = await _facturaService.GetPagedAsync(page, size);
        return Json(new
        {
            items       = paged.Data,
            totalPages  = paged.TotalPages,
            currentPage = paged.PageNumber,
            totalCount  = paged.TotalCount
        });
    }

    public async Task<IActionResult> Nueva()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var ordenesElegibles = await _ordenService.GetAllAsync(Domain.Enums.EstadoOrden.Terminado);
        var noFacturadas = ordenesElegibles.Where(o => !o.Bloqueada).ToList();
        return View(noFacturadas);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Nueva(List<Guid> ordenIds, string tipoFacturacion = "NoElectronica")
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        if (!ordenIds.Any())
        {
            TempData["Error"] = "Debe seleccionar al menos una orden.";
            return RedirectToAction(nameof(Nueva));
        }

        var ordenes = await Task.WhenAll(ordenIds.Select(oid => _ordenService.GetByIdAsync(oid)));
        if (ordenes.Any(o => o == null || o.TenantId != _tenantService.TenantId.Value)) return Forbid();

        var tipo = tipoFacturacion == "Electronica"
            ? Domain.Enums.TipoFacturacion.Electronica
            : Domain.Enums.TipoFacturacion.NoElectronica;

        try
        {
            var factura = await _facturaService.GenerarFacturaAsync(
                ordenIds, _tenantService.TenantId.Value, tipo);

            if (tipo == Domain.Enums.TipoFacturacion.Electronica)
            {
                TempData["Exito"] = $"Factura {factura.NumeroFactura} registrada como " +
                                    "<strong>Factura Electrónica — Pendiente de Envío a la DIAN</strong>. " +
                                    "La orden fue marcada como entregada. Use el botón \"Enviar a DIAN\" cuando la integración esté activa.";
                return RedirectToAction(nameof(Detalle), new { id = factura.Id });
            }

            TempData["Exito"] = $"Factura {factura.NumeroFactura} generada exitosamente.";
            return RedirectToAction(nameof(Detalle), new { id = factura.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Nueva));
        }
    }

    public async Task<IActionResult> Detalle(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura == null) return NotFound();
        if (factura.TenantId != _tenantService.TenantId.Value) return Forbid();
        return View(factura);
    }

    public async Task<IActionResult> DescargarPdf(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura == null) return NotFound();
        if (factura.TenantId != _tenantService.TenantId.Value) return Forbid();
        
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
        var tNombre = tenant?.Nombre ?? "Taller";
        var tNIT    = tenant?.NIT ?? "N/A";

        var pdf = await _reporteService.GenerarFacturaPdfPorFacturaAsync(id, tNombre, tNIT);
        return File(pdf, "application/pdf", $"Factura-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DescargarExcel()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
        var tNombre = tenant?.Nombre ?? "Taller";
        var tNIT    = tenant?.NIT ?? "N/A";

        var excel = await _reporteService.ExportarFacturasExcelAsync(tNombre, tNIT);
        return File(excel,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Facturas-{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarADian(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura == null) return NotFound();
        if (factura.TenantId != _tenantService.TenantId.Value) return Forbid();

        TempData["Info"] = $"Factura <strong>{factura.NumeroFactura}</strong>: la integración con la DIAN " +
                           "está <strong>en construcción</strong>. Cuando esté activa, este botón " +
                           "enviará el documento electrónico a la DIAN automáticamente.";
        return RedirectToAction("Index", "Dashboard");
    }
}
