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

    // ── Lista de Bodegas ───────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var bodegas = await _bodegaService.GetAllAsync();
        return View(bodegas);
    }

    // ── Crear Bodega ───────────────────────────────────────────────────────────
    public IActionResult Crear() => View(new BodegaDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(BodegaDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        if (!_tenantService.TenantId.HasValue) return Forbid();
        await _bodegaService.CreateAsync(dto, _tenantService.TenantId.Value);
        TempData["Exito"] = $"Bodega '{dto.Nombre}' creada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ── Editar Bodega ──────────────────────────────────────────────────────────
    public async Task<IActionResult> Editar(Guid id)
    {
        var bodega = await _bodegaService.GetByIdAsync(id);
        if (bodega == null) return NotFound();
        return View(bodega);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(BodegaDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _bodegaService.UpdateAsync(dto);
        TempData["Exito"] = "Bodega actualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Movimientos(Guid? bodegaId)
    {
        var bodegas     = await _bodegaService.GetAllAsync();
        ViewBag.BodegaId = bodegaId;
        ViewBag.Bodegas  = bodegas;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetMovimientosPaged(int page = 1, int size = 10, Guid? bodegaId = null)
    {
        var paged = await _bodegaService.GetMovimientosPagedAsync(page, size, bodegaId);
        return Json(paged);
    }

    // ── Traslado entre Bodegas ─────────────────────────────────────────────────
    public async Task<IActionResult> Traslado()
    {
        ViewBag.Bodegas = await _bodegaService.GetAllAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Traslado(Guid productoId, Guid bodegaOrigenId,
        Guid bodegaDestinoId, int cantidad, string? observaciones)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
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

    // ── AJAX: Stock info para el panel de resumen dinámico en Traslado ────────
    [HttpGet]
    public async Task<IActionResult> StockInfo(Guid productoId)
    {
        var producto = await _bodegaService.GetProductoStockInfoAsync(productoId);
        if (producto == null) return NotFound();
        return Json(producto);
    }
}
