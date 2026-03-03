using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TallerSaaS.Domain.Entities;

namespace TallerSaaS.Application.Interfaces;

/// <summary>
/// Abstraction of the DB context for use in Application layer services.
/// Implemented by ApplicationDbContext in the Infrastructure layer.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Cliente> Clientes { get; }
    DbSet<Vehiculo> Vehiculos { get; }
    DbSet<Orden> Ordenes { get; }
    DbSet<ItemOrden> ItemsOrden { get; }
    DbSet<ProductoInventario> Inventario { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<PlanSuscripcion> PlanesSuscripcion { get; }
    DbSet<Pago> Pagos { get; }
    DbSet<Factura> Facturas { get; }
    DbSet<EventoTrazabilidad> EventosTrazabilidad { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Exposes the EF change tracker entry for a given entity instance,
    /// enabling ReloadAsync() during DbUpdateConcurrencyException handling.</summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
