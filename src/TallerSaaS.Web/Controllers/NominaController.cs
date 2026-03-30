using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Web.Filters;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize]
public class NominaController : Controller
{
    private readonly INominaService _nominaService;
    private readonly IUserProvider _userProvider;
    private readonly ICurrentTenantService _tenantService;
    private readonly TallerSaaS.Infrastructure.Data.ApplicationDbContext _db;

    public NominaController(
        INominaService nominaService,
        IUserProvider userProvider,
        ICurrentTenantService tenantService,
        TallerSaaS.Infrastructure.Data.ApplicationDbContext db)
    {
        _nominaService = nominaService;
        _userProvider  = userProvider;
        _tenantService = tenantService;
        _db            = db;
    }

    [PlanEmpresarial]
    public async Task<IActionResult> Index()
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue) return Forbid();
        ViewBag.Mechanics = await _userProvider.GetMechanicsAsync(tenantId.Value);
        return View();
    }

    [HttpGet, PlanEmpresarial]
    public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string period = "", NominaStatus? status = null, string mechanicId = "")
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();

        // ATTACK FIX: mechanicId accepted from client — if provided, validate it belongs to current tenant
        if (!string.IsNullOrWhiteSpace(mechanicId))
        {
            var belongsToTenant = await _db.Users
                .AnyAsync(u => u.Id == mechanicId && u.TenantId == _tenantService.TenantId.Value);
            if (!belongsToTenant) return Forbid();
        }

        if (string.IsNullOrEmpty(period)) period = DateTime.Now.ToString("yyyy-MM");

        var result = await _nominaService.GetPagedAsync(page, pageSize, period, status, mechanicId);
        var kpis   = await _nominaService.GetKpiSummaryAsync(period, status, mechanicId);

        var data = new List<object>();
        foreach (var item in result.Data)
        {
            var empName = await _userProvider.GetUserNameAsync(item.UserId) ?? "Desconocido";
            data.Add(new
            {
                item.Id,
                Empleado          = empName,
                item.Periodo,
                SalarioBase       = item.SalarioBase.ToString("C0"),
                Comisiones        = item.Comisiones.ToString("C0"),
                Deducciones       = item.Deducciones.ToString("C0"),
                TotalNeto         = item.TotalNeto.ToString("C0"),
                IngresosGenerados = item.IngresosGenerados.ToString("C0"),
                item.EsRentable,
                StatusKey         = item.Estado.ToString(),
                Estado            = item.Estado switch
                {
                    NominaStatus.Draft    => "Borrador",
                    NominaStatus.Paid     => "Pagado",
                    NominaStatus.Reported => "Reportado a la DIAN",
                    _                     => item.Estado.ToString()
                },
                EstadoClase = item.Estado switch
                {
                    NominaStatus.Draft    => "bg-warning text-dark",
                    NominaStatus.Paid     => "bg-success",
                    NominaStatus.Reported => "bg-info",
                    _                     => "bg-dark"
                }
            });
        }

        return Json(new { data, total = result.TotalCount, kpis });
    }

    [HttpPost, PlanEmpresarial, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBatch(string periodo)
    {
        if (!_tenantService.TenantId.HasValue) return Json(new { success = false, message = "Tenant no identificado." });
        if (string.IsNullOrEmpty(periodo)) return Json(new { success = false, message = "Período requerido." });

        try
        {
            await _nominaService.GenerateBatchAsync(periodo);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ATTACK FIX: id from client — validate nomina belongs to current tenant
    [HttpPost, PlanEmpresarial, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportarDian(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Json(new { success = false, message = "Tenant no identificado." });

        var registro = await _nominaService.GetByIdAsync(id);
        if (registro == null) return Json(new { success = false, message = "Registro no encontrado." });
        if (registro.TenantId != _tenantService.TenantId.Value) return Json(new { success = false, message = "Acceso no autorizado." });

        var result = await _nominaService.EnviarNominaDIANAsync(id);
        return result
            ? Json(new { success = true, message = "Nómina reportada exitosamente ante la DIAN." })
            : Json(new { success = false, message = "No se pudo reportar la nómina." });
    }

    // ATTACK FIX: id from client — validate nomina belongs to current tenant
    [HttpGet, PlanEmpresarial]
    public async Task<IActionResult> Detalle(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();

        var registro = await _nominaService.GetByIdAsync(id);
        if (registro == null) return NotFound();
        if (registro.TenantId != _tenantService.TenantId.Value) return Forbid();

        var tenant = await _db.Tenants.FindAsync(registro.TenantId);
        ViewBag.TenantNombre = tenant?.Nombre ?? "Taller sin nombre";
        string nitVal = tenant?.NIT ?? "[Completar en Configuración]";
        ViewBag.TenantNIT = nitVal.StartsWith("NIT", StringComparison.OrdinalIgnoreCase) ? nitVal : "NIT: " + nitVal;
        ViewBag.EmpleadoNombre = await _userProvider.GetUserNameAsync(registro.UserId);

        return PartialView("_DetalleModal", registro);
    }

    // ATTACK FIX: id from client — validate nomina belongs to current tenant before serving PDF
    [HttpGet, PlanEmpresarial]
    public async Task<IActionResult> DescargarPdf(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();

        var registro = await _nominaService.GetByIdAsync(id);
        if (registro == null) return NotFound();
        if (registro.TenantId != _tenantService.TenantId.Value) return Forbid();

        var empName  = await _userProvider.GetUserNameAsync(registro.UserId) ?? "Empleado";
        var tenant   = await _db.Tenants.FindAsync(registro.TenantId);

        string empTaller = tenant?.Nombre ?? "Taller sin nombre";
        string nitStr    = tenant?.NIT ?? "[Completar en Configuración]";
        string empNIT    = nitStr.StartsWith("NIT", StringComparison.OrdinalIgnoreCase) ? nitStr : "NIT: " + nitStr;
        string? logoPath = !string.IsNullOrEmpty(tenant?.Logo)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logos", tenant.Logo)
            : null;

        using var stream = new MemoryStream();
        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                page.Header().Row(row =>
                {
                    if (logoPath != null && System.IO.File.Exists(logoPath))
                        row.ConstantItem(80).Image(logoPath);

                    row.RelativeItem().PaddingLeft(logoPath != null ? 15 : 0).Column(col =>
                    {
                        col.Item().Text(empTaller).FontSize(20).SemiBold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);
                        col.Item().Text(empNIT).FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        col.Item().PaddingTop(5).Text("Comprobante de Pago de Nómina").FontSize(14);
                    });
                });

                page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Text($"Empleado: {empName}");
                    col.Item().Text($"Período: {registro.Periodo}");
                    col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                    col.Item().LineHorizontal(1);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                        });
                        table.Cell().Text("Salario Base");
                        table.Cell().AlignRight().Text(registro.SalarioBase.ToString("C0"));
                        table.Cell().Text("Comisiones");
                        table.Cell().AlignRight().Text(registro.Comisiones.ToString("C0"));
                        table.Cell().Text("Deducciones");
                        table.Cell().AlignRight().Text($"-{registro.Deducciones:C0}");
                        table.Cell().Text("TOTAL NETO").Bold();
                        table.Cell().AlignRight().Text(registro.TotalNeto.ToString("C0")).Bold();
                    });
                });
            });
        }).GeneratePdf(stream);

        return File(stream.ToArray(), "application/pdf", $"Nomina_{empName}_{registro.Periodo}.pdf");
    }

    public IActionResult Upgrade() => View();
}
