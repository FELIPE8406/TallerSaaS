using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Shared.Helpers;

namespace TallerSaaS.Web.Controllers;

[Authorize]
public class ReportesController : Controller
{
    private readonly ReporteService _reporteService;
    private readonly ICurrentTenantService _tenantService;

    public ReportesController(ReporteService reporteSvc, ICurrentTenantService tenantService)
    {
        _reporteService = reporteSvc;
        _tenantService = tenantService;
    }

    [Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
    public async Task<IActionResult> FacturaPdf(Guid ordenId)
    {
        var tenantNombre = User.FindFirst(TenantClaimTypes.TenantNombre)?.Value ?? "Taller";
        var pdf = await _reporteService.GenerarFacturaPdfAsync(ordenId, tenantNombre);
        return File(pdf, "application/pdf", $"Factura-{DateTime.Now:yyyyMMdd}.pdf");
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ClientesExcel()
    {
        var excel = await _reporteService.ExportarClientesExcelAsync();
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Clientes-{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> VentasExcel(DateTime? desde, DateTime? hasta)
    {
        var excel = await _reporteService.ExportarVentasExcelAsync(desde, hasta);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Ventas-{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
