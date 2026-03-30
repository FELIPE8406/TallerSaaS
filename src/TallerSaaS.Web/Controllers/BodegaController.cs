using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class BodegaController : Controller
{
    private readonly BodegaService _bodegaService;
    private readonly ICurrentTenantService _tenantService;

    public BodegaController(BodegaService bodegaService, ICurrentTenantService tenantService)
    {
        _bodegaService  = bodegaService;
        _tenantService  = tenantService;
    }

    public async Task<IActionResult> Index()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var bodegas = await _bodegaService.GetAllAsync();
        return View(bodegas);
    }

    public IActionResult Crear()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        return View(new BodegaDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(BodegaDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        if (!_tenantService.TenantId.HasValue) return Forbid();
        await _bodegaService.CreateAsync(dto, _tenantService.TenantId.Value);
        TempData["Exito"] = $"Bodega '{dto.Nombre}' creada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(Guid id)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var bodega = await _bodegaService.GetByIdAsync(id);
        if (bodega == null) return NotFound();
        if (bodega.TenantId != _tenantService.TenantId.Value) return Forbid();
        return View(bodega);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(BodegaDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var existing = await _bodegaService.GetByIdAsync(dto.Id);
        if (existing == null) return NotFound();
        if (existing.TenantId != _tenantService.TenantId.Value) return Forbid();
        await _bodegaService.UpdateAsync(dto);
        TempData["Exito"] = "Bodega actualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Movimientos(Guid? bodegaId)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var bodegas     = await _bodegaService.GetAllAsync();
        ViewBag.BodegaId = bodegaId;
        ViewBag.Bodegas  = bodegas;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetMovimientosPaged(int page = 1, int size = 10, Guid? bodegaId = null)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var paged = await _bodegaService.GetMovimientosPagedAsync(page, size, bodegaId);
        return Json(paged);
    }

    public async Task<IActionResult> Traslado()
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        ViewBag.Bodegas = await _bodegaService.GetAllAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Traslado(Guid productoId, Guid bodegaOrigenId,
        Guid bodegaDestinoId, int cantidad, string? observaciones)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var producto = await _bodegaService.GetProductoStockInfoAsync(productoId);
        if (producto == null) return NotFound();
        try
        {
            await _bodegaService.TrasladarAsync(
                productoId, bodegaOrigenId, bodegaDestinoId,
                cantidad, _tenantService.TenantId.Value, observaciones);
            TempData["Exito"] = $"Traslado de {cantidad} unidad(es) realizado correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Movimientos));
    }

    [HttpGet]
    public async Task<IActionResult> StockInfo(Guid productoId)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        var producto = await _bodegaService.GetProductoStockInfoAsync(productoId);
        if (producto == null) return NotFound();
        return Json(producto);
    }
}
