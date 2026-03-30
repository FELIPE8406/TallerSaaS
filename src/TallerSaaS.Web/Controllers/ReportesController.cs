using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Application.Services;
using TallerSaaS.Application.Services.Exporters;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TallerSaaS.Web.Controllers;

[Authorize]
public class ReportesController : Controller
{
    private readonly ReporteService _reporteService;
    private readonly ICurrentTenantService _tenantService;
    private readonly CsvExportStrategy _csv;
    private readonly TxtExportStrategy _txt;
    private readonly PdfExportStrategy _pdf;
    private readonly OrdenService _ordenService;
    private readonly IApplicationDbContext _db;

    public ReportesController(
        ReporteService reporteSvc,
        ICurrentTenantService tenantService,
        CsvExportStrategy csv,
        TxtExportStrategy txt,
        PdfExportStrategy pdf,
        OrdenService ordenService,
        IApplicationDbContext db)
    {
        _reporteService = reporteSvc;
        _tenantService  = tenantService;
        _csv            = csv;
        _txt            = txt;
        _pdf            = pdf;
        _ordenService   = ordenService;
        _db             = db;
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult Index()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        return View();
    }

    // ATTACK VECTOR: ordenId accepted from client — must validate ownership
    [Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
    public async Task<IActionResult> FacturaPdf(Guid ordenId)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        try
        {
            // Validate ownership before generating PDF — prevents cross-tenant PDF access by ID
            var orden = await _ordenService.GetByIdAsync(ordenId);
            if (orden == null) return NotFound();
            if (orden.TenantId != _tenantService.TenantId.Value) return Forbid();

            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
            var tenantNombre = tenant?.Nombre ?? "Taller";
            var tenantNIT    = tenant?.NIT ?? "N/A";

            var pdf = await _reporteService.GenerarFacturaPdfAsync(ordenId, tenantNombre, tenantNIT);
            return File(pdf, "application/pdf", $"Factura_Orden_{ordenId}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ClientesExcel()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        try
        {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
            var excel = await _reporteService.ExportarClientesExcelAsync(tenant?.Nombre ?? "Taller", tenant?.NIT ?? "N/A");
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte_Clientes_GearDash.xlsx");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Excel de clientes: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> VentasPdf(DateTime? desde, DateTime? hasta)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        try
        {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
            var filtro = ReporteFilter.FromPeriodo("personalizado", desde, hasta);
            var pdf = await _pdf.ExportarOrdenesAsync(filtro, tenant?.Nombre ?? "Taller", tenant?.NIT ?? "N/A");
            return File(pdf, "application/pdf", $"Reporte_Ventas_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF de ventas: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> VentasExcel(DateTime? desde, DateTime? hasta)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        try
        {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
            var excel = await _reporteService.ExportarVentasExcelAsync(tenant?.Nombre ?? "Taller", tenant?.NIT ?? "N/A", desde, hasta);
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte_Ventas_GearDash.xlsx");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Excel de ventas: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ExportarOrdenes(
        string formato = "excel",
        string periodo = "trimestral",
        DateTime? desde = null, DateTime? hasta = null)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        try
        {
            var filtro = ReporteFilter.FromPeriodo(periodo, desde, hasta);
            return await Exportar(formato, filtro, TipoReporte.Ordenes,
                $"Ordenes-{filtro.Desde:yyyyMMdd}-{filtro.Hasta:yyyyMMdd}");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al exportar Órdenes: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ExportarFacturas(
        string formato = "excel",
        string periodo = "trimestral",
        DateTime? desde = null, DateTime? hasta = null)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        try
        {
            var filtro = ReporteFilter.FromPeriodo(periodo, desde, hasta);
            return await Exportar(formato, filtro, TipoReporte.Facturas,
                $"Facturas-{filtro.Desde:yyyyMMdd}-{filtro.Hasta:yyyyMMdd}");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al exportar Facturas: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ExportarClientesVehiculos(
        string formato = "excel",
        string periodo = "trimestral",
        DateTime? desde = null, DateTime? hasta = null)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        try
        {
            var filtro = ReporteFilter.FromPeriodo(periodo, desde, hasta);
            return await Exportar(formato, filtro, TipoReporte.ClientesVehiculos,
                $"ClientesVehiculos-{filtro.Desde:yyyyMMdd}-{filtro.Hasta:yyyyMMdd}");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al exportar Clientes/Vehículos: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<IActionResult> Exportar(
        string formato, ReporteFilter filtro, TipoReporte tipo, string filePrefix)
    {
        byte[] data;
        string contentType;
        string ext;

        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenantService.TenantId.Value);
        var tNombre = tenant?.Nombre ?? "Taller";
        var tNIT    = tenant?.NIT ?? "N/A";

        switch (formato.ToLowerInvariant())
        {
            case "csv":
                data = tipo switch
                {
                    TipoReporte.Ordenes           => await _csv.ExportarOrdenesAsync(filtro, tNombre, tNIT),
                    TipoReporte.Facturas          => await _csv.ExportarFacturasAsync(filtro, tNombre, tNIT),
                    _                             => await _csv.ExportarClientesVehiculosAsync(filtro, tNombre, tNIT)
                };
                contentType = _csv.ContentType;
                ext         = _csv.FileExtension;
                break;

            case "txt":
                data = tipo switch
                {
                    TipoReporte.Ordenes           => await _txt.ExportarOrdenesAsync(filtro, tNombre, tNIT),
                    TipoReporte.Facturas          => await _txt.ExportarFacturasAsync(filtro, tNombre, tNIT),
                    _                             => await _txt.ExportarClientesVehiculosAsync(filtro, tNombre, tNIT)
                };
                contentType = _txt.ContentType;
                ext         = _txt.FileExtension;
                break;

            case "pdf":
                data = tipo switch
                {
                    TipoReporte.Ordenes           => await _pdf.ExportarOrdenesAsync(filtro, tNombre, tNIT),
                    TipoReporte.Facturas          => await _pdf.ExportarFacturasAsync(filtro, tNombre, tNIT),
                    _                             => await _pdf.ExportarClientesVehiculosAsync(filtro, tNombre, tNIT)
                };
                contentType = _pdf.ContentType;
                ext         = "pdf";
                break;

            default: // excel
                data = tipo switch
                {
                    TipoReporte.Ordenes  => await _reporteService.ExportarVentasExcelAsync(tNombre, tNIT, filtro.Desde, filtro.Hasta),
                    TipoReporte.Facturas => await _reporteService.ExportarFacturasExcelAsync(tNombre, tNIT, filtro.Desde, filtro.Hasta),
                    _                   => await _reporteService.ExportarClientesExcelAsync(tNombre, tNIT, filtro.Desde, filtro.Hasta)
                };
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                ext         = "xlsx";
                break;
        }

        return File(data, contentType, $"{filePrefix}.{ext}");
    }
}
