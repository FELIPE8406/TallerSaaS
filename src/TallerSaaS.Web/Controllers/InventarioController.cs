using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Services;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Controllers;

[Authorize(Roles = "Admin,Mecanico,SuperAdmin")]
public class InventarioController : Controller
{
    private readonly InventarioService _inventarioService;
    private readonly BodegaService _bodegaService;
    private readonly ICurrentTenantService _tenantService;

    public InventarioController(InventarioService inventarioSvc,
                                BodegaService bodegaSvc,
                                ICurrentTenantService tenantService)
    {
        _inventarioService = inventarioSvc;
        _bodegaService     = bodegaSvc;
        _tenantService     = tenantService;
    }

    public async Task<IActionResult> Index(string? buscar, string? categoria)
    {
        var categorias = await _inventarioService.GetCategoriasAsync();
        var bajoStock = await _inventarioService.GetBajoStockAsync();
        ViewBag.Buscar = buscar;
        ViewBag.CategoriaFiltro = categoria;
        ViewBag.Categorias = categorias;
        ViewBag.AlertasBajoStock = bajoStock.Count;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(int page = 1, int size = 10, string? buscar = null, string? categoria = null)
    {
        var paged = await _inventarioService.GetAllPagedAsync(page, size, buscar, categoria);
        return Json(paged);
    }

    public async Task<IActionResult> Crear()
    {
        ViewBag.Bodegas = await _bodegaService.GetAllAsync();
        return View(new InventarioDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(InventarioDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        if (!_tenantService.TenantId.HasValue) return Forbid();
        await _inventarioService.CreateAsync(dto, _tenantService.TenantId.Value);
        TempData["Exito"] = "Producto agregado al inventario.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(Guid id)
    {
        var producto = await _inventarioService.GetByIdAsync(id);
        if (producto == null) return NotFound();
        ViewBag.Bodegas = await _bodegaService.GetAllAsync();
        return View(producto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(InventarioDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _inventarioService.UpdateAsync(dto);
        TempData["Exito"] = "Producto actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AjustarStock(Guid id, int cantidad, string tipo, string? observaciones = null)
    {
        if (!_tenantService.TenantId.HasValue) return Forbid();
        await _inventarioService.AjustarStockAsync(id, cantidad, tipo, _tenantService.TenantId.Value, observaciones);
        TempData["Exito"] = $"Stock {(tipo == "entrada" ? "aumentado" : "reducido")} correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ── AJAX: Buscador de productos para Traslado autocomplete ───────────────
    [HttpGet]
    public async Task<IActionResult> BuscarProductos(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(Array.Empty<object>());

        var resultados = await _inventarioService.BuscarAsync(q);
        return Json(resultados.Select(p => new
        {
            p.Id,
            p.Nombre,
            p.SKU,
            p.Stock,
            p.StockMinimo,
            p.BodegaId,
            p.BodegaNombre,
            label = $"{p.Nombre}{(p.SKU != null ? $" [{p.SKU}]" : "")} — Stock: {p.Stock}"
        }));
    }
}
