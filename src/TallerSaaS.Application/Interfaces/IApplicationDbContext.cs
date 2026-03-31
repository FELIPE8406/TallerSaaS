using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using TallerSaaS.Domain.Entities;
using System.Data;

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
    DbSet<Bodega> Bodegas { get; }
    DbSet<MovimientoInventario> MovimientosInventario { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<PlanSuscripcion> PlanesSuscripcion { get; }
    DbSet<Pago> Pagos { get; }
    DbSet<Factura> Facturas { get; }
    DbSet<EventoTrazabilidad> EventosTrazabilidad { get; }

    // Accounting Module
    DbSet<CuentaContable> CuentasContables { get; }
    DbSet<AsientoContable> AsientosContables { get; }
    DbSet<LineaAsientoContable> LineasAsientosContables { get; }

    DbSet<Appointment> Appointments { get; }
    DbSet<MechanicAvailability> MechanicAvailabilities { get; }

    // Payroll Module
    DbSet<NominaRegistro> NominaRegistros { get; }
    DbSet<EmpleadoContrato> EmpleadoContratos { get; }

    /// <summary>
    /// Exposes transactional boundaries for critical operations (stock/facturación).
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Exposes the EF change tracker entry for a given entity instance,
    /// enabling ReloadAsync() during DbUpdateConcurrencyException handling.</summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
