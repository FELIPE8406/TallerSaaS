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
        _userProvider = userProvider;
        _tenantService = tenantService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId = _tenantService.TenantId;
        if (tenantId == null) return RedirectToAction("Upgrade", "Nomina");

        var staffList = await _userProvider.GetStaffAsync(tenantId.Value);
        // We'll keep the Index action simple as the table will be loaded via AJAX.
        // But we still need these for the limits and the New Employee modal.
        
        var unconfiguredUsers = new List<object>();
        int activeEmployeesCount = 0;

        foreach (var staff in staffList)
        {
            var contrato = await _contratoService.GetByUserIdAsync(staff.Id);
            if (contrato != null && contrato.Activo) activeEmployeesCount++;

            if (contrato == null)
            {
                unconfiguredUsers.Add(new { staff.Id, staff.Name });
            }
        }

        // Plan Limit Logic Check
        var tenant = await _db.Tenants.FindAsync(tenantId.Value);
        var plan = tenant != null ? await _db.PlanesSuscripcion.FindAsync(tenant.PlanSuscripcionId) : null;
        int planLimit = plan?.LimiteUsuarios ?? 9999;

        ViewBag.UnconfiguredUsers = unconfiguredUsers;
        ViewBag.LimiteAlcanzado = activeEmployeesCount >= planLimit;
        ViewBag.ActiveCount = activeEmployeesCount;
        ViewBag.PlanLimit = planLimit;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var tenantId = _tenantService.TenantId;
        if (tenantId == null) return Unauthorized();

        var staffList = await _userProvider.GetStaffAsync(tenantId.Value);
        var result = new List<object>();

        foreach (var staff in staffList)
        {
            var contrato = await _db.EmpleadoContratos.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == staff.Id && c.TenantId == tenantId);

            result.Add(new {
                staff.Id,
                staff.Name,
                staff.Role,
                TieneContrato = contrato != null,
                ContratoActivo = contrato?.Activo ?? false,
                SalarioBase = contrato?.SalarioBase ?? 0,
                Comision = contrato?.PorcentajeComision ?? 0,
                TipoEmpleado = contrato?.TipoEmpleado ?? "",
                StatusTexto = (contrato == null) ? "Sin Contrato" : (contrato.Activo ? "Activo" : "Inactivo")
            });
        }
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetContrato(string userId)
    {
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

    [HttpPost]
    public async Task<IActionResult> GuardarContrato(string userId, decimal salarioBase, decimal comision, bool activo, string tipoEmpleado, string urlPdf)
    {
        if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "ID de usuario inválido." });

        try
        {
            var success = await _contratoService.SaveContratoAsync(userId, salarioBase, comision, activo, tipoEmpleado, urlPdf);
            if (success)
                return Json(new { success = true, message = "Contrato guardado exitosamente." });
            
            return Json(new { success = false, message = "No se pudo guardar el contrato." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error interno: " + ex.Message });
        }
    }
}
