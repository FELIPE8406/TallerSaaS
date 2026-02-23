using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Application.Services;

public class DashboardService
{
    private readonly IApplicationDbContext _db;

    public DashboardService(IApplicationDbContext db) => _db = db;

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

        var totalClientes = await _db.Clientes.CountAsync(c => c.Activo);
        var totalVehiculos = await _db.Vehiculos.CountAsync();
        var ordenesAbiertas = await _db.Ordenes.CountAsync(o =>
            o.Estado != EstadoOrden.Entregado);
        var ventasMes = await _db.Ordenes
            .Where(o => o.FechaEntrada >= inicioMes && o.Pagada)
            .SumAsync(o => (decimal?)o.Total) ?? 0m;

        // Last 6 months sales
        var ventas6Meses = new List<VentaMensualDto>();
        for (int i = 5; i >= 0; i--)
        {
            var mes = ahora.AddMonths(-i);
            var inicio = new DateTime(mes.Year, mes.Month, 1);
            var fin = inicio.AddMonths(1);
            var total = await _db.Ordenes
                .Where(o => o.FechaEntrada >= inicio && o.FechaEntrada < fin && o.Pagada)
                .SumAsync(o => (decimal?)o.Total) ?? 0m;
            ventas6Meses.Add(new VentaMensualDto { Mes = mes.ToString("MMM"), Total = total });
        }

        // Orders by status
        var estados = await _db.Ordenes
            .GroupBy(o => o.Estado)
            .Select(g => new EstadoOrdenConteoDto { Estado = g.Key.ToString(), Conteo = g.Count() })
            .ToListAsync();

        // Low stock
        var bajoStock = await _db.Inventario
            .Where(p => p.Stock <= p.StockMinimo && p.Activo)
            .Take(5)
            .Select(p => new InventarioDto
            {
                Id = p.Id, Nombre = p.Nombre, Stock = p.Stock, StockMinimo = p.StockMinimo,
                NivelStock = p.Stock <= 0 ? "Agotado" : "Bajo",
                NivelStockClase = p.Stock <= 0 ? "danger" : "warning"
            }).ToListAsync();

        return new DashboardDto
        {
            TotalClientes = totalClientes, TotalVehiculos = totalVehiculos,
            OrdenesAbiertas = ordenesAbiertas, VentasMes = ventasMes,
            VentasMensuales = ventas6Meses, OrdenesPorEstado = estados,
            ProductosBajoStock = bajoStock
        };
    }

    public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
    {
        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

        var tenants = await _db.Tenants
            .Include(t => t.PlanSuscripcion)
            .OrderByDescending(t => t.FechaAlta)
            .ToListAsync();

        var ingresosTotales = await _db.Pagos
            .Where(p => p.Estado == "Completado")
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

        var ingresosMes = await _db.Pagos
            .Where(p => p.Estado == "Completado" && p.Fecha >= inicioMes)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

        return new SuperAdminDashboardDto
        {
            TotalTenants = tenants.Count,
            TenantsActivos = tenants.Count(t => t.Activo),
            IngresosTotales = ingresosTotales,
            IngresosMes = ingresosMes,
            Tenants = tenants.Select(t => new TenantResumenDto
            {
                Id = t.Id, Nombre = t.Nombre, Email = t.Email, Activo = t.Activo,
                PlanNombre = t.PlanSuscripcion?.Nombre, PrecioPlan = t.PlanSuscripcion?.Precio ?? 0m,
                FechaAlta = t.FechaAlta
            }).ToList()
        };
    }
}
