using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Web.Filters;
using Microsoft.EntityFrameworkCore;

namespace TallerSaaS.Web.Controllers;

[Authorize]
[PlanEmpresarial]
public class EmpleadoContratoController : Controller
{
    private readonly IEmpleadoContratoService _contratoService;
    private readonly IUserProvider _userProvider;
    private readonly ICurrentTenantService _tenantService;
    private readonly TallerSaaS.Infrastructure.Data.ApplicationDbContext _db;

    public EmpleadoContratoController(
        IEmpleadoContratoService contratoService,
        IUserProvider userProvider,
        ICurrentTenantService tenantService,
        TallerSaaS.Infrastructure.Data.ApplicationDbContext db)
    {
        _contratoService = contratoService;
        _userProvider    = userProvider;
        _tenantService   = tenantService;
        _db              = db;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue) return Forbid();

        var staffList = await _userProvider.GetStaffAsync(tenantId.Value);
        var unconfiguredUsers = new List<object>();
        int activeEmployeesCount = 0;

        foreach (var staff in staffList)
        {
            var contrato = await _contratoService.GetByUserIdAsync(staff.Id);
            if (contrato != null && contrato.Activo) activeEmployeesCount++;
            if (contrato == null)
                unconfiguredUsers.Add(new { staff.Id, staff.Name });
        }

        var tenant  = await _db.Tenants.FindAsync(tenantId.Value);
        var plan    = tenant != null ? await _db.PlanesSuscripcion.FindAsync(tenant.PlanSuscripcionId) : null;
        int planLimit = plan?.LimiteUsuarios ?? 9999;

        ViewBag.UnconfiguredUsers = unconfiguredUsers;
        ViewBag.LimiteAlcanzado   = activeEmployeesCount >= planLimit;
        ViewBag.ActiveCount       = activeEmployeesCount;
        ViewBag.PlanLimit         = planLimit;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue) return Forbid();

        var staffList = await _userProvider.GetStaffAsync(tenantId.Value);
        var result    = new List<object>();

        foreach (var staff in staffList)
        {
            var contrato = await _db.EmpleadoContratos.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == staff.Id && c.TenantId == tenantId.Value);

            result.Add(new {
                staff.Id,
                staff.Name,
                staff.Role,
                TieneContrato  = contrato != null,
                ContratoActivo = contrato?.Activo ?? false,
                SalarioBase    = contrato?.SalarioBase ?? 0,
                Comision       = contrato?.PorcentajeComision ?? 0,
                TipoEmpleado   = contrato?.TipoEmpleado ?? "",
                StatusTexto    = (contrato == null) ? "Sin Contrato" : (contrato.Activo ? "Activo" : "Inactivo")
            });
        }
        return Json(result);
    }

    // ATTACK FIX: userId accepted from client — validate it belongs to current tenant
    [HttpGet]
    public async Task<IActionResult> GetContrato(string userId)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue) return Forbid();
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest();

        // Ensure the requested user belongs to this tenant
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId.Value);
        if (user == null) return Forbid();

        var contrato = await _contratoService.GetByUserIdAsync(userId);
        if (contrato == null) return Json(null);

        return Json(new {
            contrato.SalarioBase,
            contrato.PorcentajeComision,
            contrato.Activo,
            contrato.TipoEmpleado,
            UrlPdf = contrato.URLContratoPDF
        });
    }

    // ATTACK FIX: userId from client; validate tenant ownership before saving
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarContrato(
        string userId, decimal salarioBase, decimal comision,
        bool activo, string tipoEmpleado, string urlPdf)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Json(new { success = false, message = "ID de usuario inválido." });

        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue)
            return Json(new { success = false, message = "Tenant no identificado." });

        // CRITICAL: verify userId belongs to current tenant before mutation
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId.Value);
        if (user == null)
            return Json(new { success = false, message = "Acceso no autorizado." });

        try
        {
            var success = await _contratoService.SaveContratoAsync(userId, salarioBase, comision, activo, tipoEmpleado, urlPdf);
            return success
                ? Json(new { success = true, message = "Contrato guardado exitosamente." })
                : Json(new { success = false, message = "No se pudo guardar el contrato." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error interno: " + ex.Message });
        }
    }
}
