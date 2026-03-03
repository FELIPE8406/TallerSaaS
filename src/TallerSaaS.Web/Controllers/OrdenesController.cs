using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class OrdenesController : Controller
{
    private readonly OrdenService _ordenService;
    private readonly ClienteService _clienteService;
    private readonly VehiculoService _vehiculoService;
    private readonly ICurrentTenantService _tenantService;
    private readonly ILogger<OrdenesController> _logger;

    public OrdenesController(OrdenService ordenSvc,
                             ClienteService clienteSvc,
                             VehiculoService vehiculoSvc,
                             ICurrentTenantService tenantService,
                             ILogger<OrdenesController> logger)
    {
        _ordenService    = ordenSvc;
        _clienteService  = clienteSvc;
        _vehiculoService = vehiculoSvc;
        _tenantService   = tenantService;
        _logger          = logger;
    }

    public async Task<IActionResult> Index(int? estado)
    {
        var ordenes = await _ordenService.GetAllAsync(estado.HasValue ? (EstadoOrden)estado.Value : null);
        ViewBag.EstadoFiltro = estado;
        return View(ordenes);
    }

    public async Task<IActionResult> Detalle(Guid id)
    {
        var orden = await _ordenService.GetByIdAsync(id);
        if (orden == null) return NotFound();
        return View(orden);
    }

    public async Task<IActionResult> Crear()
    {
        ViewBag.Clientes  = await _clienteService.GetAllAsync();
        ViewBag.Vehiculos = await _vehiculoService.GetAllAsync();
        return View(new OrdenDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(OrdenDto dto)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var orden = await _ordenService.CreateAsync(dto, _tenantService.TenantId.Value);
        TempData["Exito"] = $"Orden {orden.NumeroOrden} creada.";
        return RedirectToAction(nameof(Detalle), new { id = orden.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(Guid id, int estado)
    {
        await _ordenService.CambiarEstadoAsync(id, (EstadoOrden)estado);
        TempData["Exito"] = "Estado actualizado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarItem(Guid ordenId, ItemOrdenDto dto)
    {
        await _ordenService.AddItemAsync(ordenId, dto);
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarItemJson(Guid ordenId, ItemOrdenDto dto)
    {
        try
        {
            await _ordenService.AddItemAsync(ordenId, dto);
            var orden = await _ordenService.GetByIdAsync(ordenId);
            if (orden == null)
                return Json(new { ok = false, error = $"Ítem guardado pero orden {ordenId} no se pudo releer. Recarga la página." });

            return Json(new { ok = true, orden });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            // Log the real exception — visible in dotnet run console and VS Output
            _logger.LogError(ex, "Error en AgregarItemJson para orden {OrdenId}. DTO: Desc={Desc} Tipo={Tipo} Cant={Cant} Precio={Precio}",
                ordenId, dto.Descripcion, dto.Tipo, dto.Cantidad, dto.PrecioUnitario);

            // Return real message to UI so user can see root cause
            return Json(new { ok = false, error = $"[{ex.GetType().Name}] {ex.Message}" });
        }
    }

    /// <summary>
    /// AJAX endpoint: returns current order state (items + totals) as JSON.
    /// Used by EliminarItem redirect to refresh the table.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOrdenJson(Guid id)
    {
        var orden = await _ordenService.GetByIdAsync(id);
        if (orden == null) return NotFound();
        return Json(orden);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarItem(Guid ordenId, Guid itemId)
    {
        await _ordenService.RemoveItemAsync(ordenId, itemId);
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    public async Task<IActionResult> DescargarPdf(Guid id)
    {
        // Will be handled by ReportesController
        return RedirectToAction("FacturaPdf", "Reportes", new { ordenId = id });
    }
}

