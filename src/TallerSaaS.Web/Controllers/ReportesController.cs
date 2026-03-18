using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Application.Services;
using TallerSaaS.Application.Services.Exporters;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Shared.Helpers;

namespace TallerSaaS.Web.Controllers;

/// <summary>
/// Controlador de exportación de reportes.
/// Soporta múltiples formatos (Excel, CSV, TXT) con filtros de periodo.
/// <list type="bullet">
///   <item>Excel se devuelve con Content-Disposition: inline para abrir en nueva pestaña.</item>
///   <item>CSV y TXT se devuelven con Content-Disposition: attachment (descarga directa).</item>
///   <item>Todos los endpoints están protegidos con try-catch para evitar 500 en producción.</item>
/// </list>
/// </summary>
[Authorize]
public class ReportesController : Controller
{
    private readonly ReporteService _reporteService;
    private readonly ICurrentTenantService _tenantService;
    private readonly CsvExportStrategy _csv;
    private readonly TxtExportStrategy _txt;
    private readonly PdfExportStrategy _pdf;

    public ReportesController(
        ReporteService reporteSvc,
        ICurrentTenantService tenantService,
        CsvExportStrategy csv,
        TxtExportStrategy txt,
        PdfExportStrategy pdf)
    {
        _reporteService = reporteSvc;
        _tenantService  = tenantService;
        _csv            = csv;
        _txt            = txt;
        _pdf            = pdf;
    }

    // ── Dashboard de exportaciones ─────────────────────────────────────────────
    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult Index() => View();

    // ── Legado: PDF de una orden ───────────────────────────────────────────────
    [Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
    public async Task<IActionResult> FacturaPdf(Guid ordenId)
    {
        try
        {
            var tenantNombre = User.FindFirst(TenantClaimTypes.TenantNombre)?.Value ?? "Taller";
            var pdf = await _reporteService.GenerarFacturaPdfAsync(ordenId, tenantNombre);
            // PDF abre inline para ser visualizado en el navegador
            Response.Headers["Content-Disposition"] = "inline";
            return File(pdf, "application/pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── Legado: Excel Clientes ─────────────────────────────────────────────────
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ClientesExcel()
    {
        try
        {
            var excel = await _reporteService.ExportarClientesExcelAsync();
            Response.Headers["Content-Disposition"] = "inline";
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Excel de clientes: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── Legado: Excel Ventas ───────────────────────────────────────────────────
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> VentasExcel(DateTime? desde, DateTime? hasta)
    {
        try
        {
            var excel = await _reporteService.ExportarVentasExcelAsync(desde, hasta);
            Response.Headers["Content-Disposition"] = "inline";
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar Excel de ventas: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── Multi-formato: Órdenes ─────────────────────────────────────────────────
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ExportarOrdenes(
        string formato = "excel",
        string periodo = "trimestral",
        DateTime? desde = null, DateTime? hasta = null)
    {
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

    // ── Multi-formato: Facturas ────────────────────────────────────────────────
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ExportarFacturas(
        string formato = "excel",
        string periodo = "trimestral",
        DateTime? desde = null, DateTime? hasta = null)
    {
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

    // ── Multi-formato: Clientes–Vehículos ──────────────────────────────────────
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ExportarClientesVehiculos(
        string formato = "excel",
        string periodo = "trimestral",
        DateTime? desde = null, DateTime? hasta = null)
    {
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

    // ── Helper privado: Dispatch al Strategy correcto ─────────────────────────
    /// <summary>
    /// Genera el archivo de exportación según formato y tipo de reporte.
    /// Excel se sirve con Content-Disposition: inline para abrir en nueva pestaña del navegador.
    /// CSV y TXT se sirven con Content-Disposition: attachment (descarga directa).
    /// </summary>
    private async Task<IActionResult> Exportar(
        string formato, ReporteFilter filtro, TipoReporte tipo, string filePrefix)
    {
        byte[] data;
        string contentType;
        string ext;
        bool esExcel;

        switch (formato.ToLowerInvariant())
        {
            case "csv":
                data = tipo switch
                {
                    TipoReporte.Ordenes           => await _csv.ExportarOrdenesAsync(filtro),
                    TipoReporte.Facturas          => await _csv.ExportarFacturasAsync(filtro),
                    _                             => await _csv.ExportarClientesVehiculosAsync(filtro)
                };
                contentType = _csv.ContentType;
                ext         = _csv.FileExtension;
                esExcel     = false;
                break;

            case "txt":
                data = tipo switch
                {
                    TipoReporte.Ordenes           => await _txt.ExportarOrdenesAsync(filtro),
                    TipoReporte.Facturas          => await _txt.ExportarFacturasAsync(filtro),
                    _                             => await _txt.ExportarClientesVehiculosAsync(filtro)
                };
                contentType = _txt.ContentType;
                ext         = _txt.FileExtension;
                esExcel     = false;
                break;

            case "pdf":
                // PDF: se sirve inline para que el navegador use su visor nativo (nueva pestaña).
                data = tipo switch
                {
                    TipoReporte.Ordenes           => await _pdf.ExportarOrdenesAsync(filtro),
                    TipoReporte.Facturas          => await _pdf.ExportarFacturasAsync(filtro),
                    _                             => await _pdf.ExportarClientesVehiculosAsync(filtro)
                };
                Response.Headers["Content-Disposition"] = "inline";
                return File(data, _pdf.ContentType);

            default: // excel
                data = tipo switch
                {
                    TipoReporte.Ordenes           => await _reporteService.ExportarVentasExcelAsync(filtro.Desde, filtro.Hasta),
                    TipoReporte.Facturas          => await _reporteService.ExportarFacturasExcelAsync(filtro.Desde, filtro.Hasta),
                    _                             => await _reporteService.ExportarClientesExcelAsync(filtro.Desde, filtro.Hasta)
                };
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                ext         = "xlsx";
                esExcel     = true;
                break;
        }

        if (esExcel)
        {
            // inline: el navegador decide si mostrar el visor o pedir permiso; no descarga automático
            Response.Headers["Content-Disposition"] = "inline";
            return File(data, contentType);
        }

        // CSV / TXT: descarga directa con nombre de archivo
        return File(data, contentType, $"{filePrefix}.{ext}");
    }
}
