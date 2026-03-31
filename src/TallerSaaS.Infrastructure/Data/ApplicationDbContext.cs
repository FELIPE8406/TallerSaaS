using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Interfaces;
using System.Data;

namespace TallerSaaS.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly ICurrentTenantService _tenantService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        ICurrentTenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<PlanSuscripcion> PlanesSuscripcion => Set<PlanSuscripcion>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Orden> Ordenes => Set<Orden>();
    public DbSet<ItemOrden> ItemsOrden => Set<ItemOrden>();
    public DbSet<ProductoInventario> Inventario => Set<ProductoInventario>();
    public DbSet<Bodega> Bodegas => Set<Bodega>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<EventoTrazabilidad> EventosTrazabilidad => Set<EventoTrazabilidad>();

    // Accounting Module
    public DbSet<CuentaContable> CuentasContables => Set<CuentaContable>();
    public DbSet<AsientoContable> AsientosContables => Set<AsientoContable>();
    public DbSet<LineaAsientoContable> LineasAsientosContables => Set<LineaAsientoContable>();

    // Agenda Module
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MechanicAvailability> MechanicAvailabilities => Set<MechanicAvailability>();
    public DbSet<NominaRegistro> NominaRegistros => Set<NominaRegistro>();
    public DbSet<EmpleadoContrato> EmpleadoContratos => Set<EmpleadoContrato>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Tenant ──────────────────────────────────────────────────────────────
        builder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Nombre).HasMaxLength(200).IsRequired();
            e.Property(t => t.NIT).HasMaxLength(30);
            e.Property(t => t.Ciudad).HasMaxLength(100);
            e.HasOne(t => t.PlanSuscripcion).WithMany(p => p.Tenants)
             .HasForeignKey(t => t.PlanSuscripcionId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── PlanSuscripcion ──────────────────────────────────────────────────────
        builder.Entity<PlanSuscripcion>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            e.Property(p => p.Precio).HasColumnType("decimal(10,2)");
        });

        // ── Pago ─────────────────────────────────────────────────────────────────
        builder.Entity<Pago>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Monto).HasColumnType("decimal(10,2)");
            e.HasIndex(p => p.TenantId).HasDatabaseName("IX_Pagos_TenantId");
            e.HasQueryFilter(p => _tenantService.TenantId != null && p.TenantId == _tenantService.TenantId);
        });

        // ── Cliente ──────────────────────────────────────────────────────────────
        builder.Entity<Cliente>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.NombreCompleto).HasMaxLength(300).IsRequired();
            
            // Composite index for fast tenant-specific filtering by registration date
            e.HasIndex(c => new { c.TenantId, c.FechaRegistro })
             .HasDatabaseName("IX_Clientes_Tenant_Date");

            e.HasQueryFilter(c => _tenantService.TenantId != null && c.TenantId == _tenantService.TenantId);
        });

        // ── Vehiculo ─────────────────────────────────────────────────────────────
        builder.Entity<Vehiculo>(e =>
        {
            e.HasKey(v => v.Id);
            
            // Composite index to speed up the Vehicles grid
            e.HasIndex(v => new { v.TenantId, v.FechaRegistro })
             .HasDatabaseName("IX_Vehiculos_Tenant_Date");

            e.HasQueryFilter(v => _tenantService.TenantId != null && v.TenantId == _tenantService.TenantId);
            e.Ignore(v => v.Descripcion);
            e.HasOne(v => v.Cliente).WithMany(c => c.Vehiculos)
             .HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Orden ─────────────────────────────────────────────────────────────────
        builder.Entity<Orden>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Subtotal).HasColumnType("decimal(12,2)");
            e.Property(o => o.Descuento).HasColumnType("decimal(12,2)");
            e.Property(o => o.IVA).HasColumnType("decimal(12,2)");
            e.Property(o => o.Total).HasColumnType("decimal(12,2)");
            
            // Critical index for Ordenes grid (sorted by entry date)
            e.HasIndex(o => new { o.TenantId, o.FechaEntrada, o.Estado })
             .HasDatabaseName("IX_Ordenes_Tenant_Date_State");

            e.HasQueryFilter(o => _tenantService.TenantId != null && o.TenantId == _tenantService.TenantId);
            e.Ignore(o => o.EstadoTexto);
            e.Ignore(o => o.EstadoClase);
            e.HasOne(o => o.Vehiculo).WithMany(v => v.Ordenes)
             .HasForeignKey(o => o.VehiculoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Appointment).WithMany()
             .HasForeignKey(o => o.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── ItemOrden ─────────────────────────────────────────────────────────────
        builder.Entity<ItemOrden>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Cantidad).HasColumnType("decimal(10,2)");
            e.Property(i => i.PrecioUnitario).HasColumnType("decimal(12,2)");
            e.Ignore(i => i.Subtotal);
            e.HasOne(i => i.Orden).WithMany(o => o.Items)
             .HasForeignKey(i => i.OrdenId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProductoInventario ────────────────────────────────────────────────────
        builder.Entity<ProductoInventario>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.PrecioCompra).HasColumnType("decimal(12,2)");
            e.Property(p => p.PrecioVenta).HasColumnType("decimal(12,2)");

            // Índice compuesto (TenantId, Activo): cada consulta filtra por ambas columnas.
            // Reemplaza el índice simple de TenantId — más selectivo, ceros falsos positivos.
            e.HasIndex(p => new { p.TenantId, p.Activo })
             .HasDatabaseName("IX_Inventario_TenantId_Activo");

            // Índice en SKU: acelera el CONTAINS(buscar) en GetAllAsync y BuscarAsync.
            e.HasIndex(p => p.SKU)
             .HasDatabaseName("IX_Inventario_SKU");

            e.HasQueryFilter(p => _tenantService.TenantId != null && p.TenantId == _tenantService.TenantId);
            e.Ignore(p => p.NivelStock);
            e.Ignore(p => p.NivelStockClase);
        });

        // ── Factura ──────────────────────────────────────────────────────────────────────
        builder.Entity<Factura>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.NumeroFactura).HasMaxLength(20).IsRequired();
            e.Property(f => f.Subtotal).HasColumnType("decimal(12,2)");
            e.Property(f => f.Descuento).HasColumnType("decimal(12,2)");
            e.Property(f => f.IVA).HasColumnType("decimal(12,2)");
            e.Property(f => f.Total).HasColumnType("decimal(12,2)");
            e.HasIndex(f => f.TenantId).HasDatabaseName("IX_Facturas_TenantId");
            e.HasQueryFilter(f => _tenantService.TenantId != null && f.TenantId == _tenantService.TenantId);
            // Ordenes reference their Factura via FacturaId (optional FK on Orden)
            e.HasMany(f => f.Ordenes).WithOne(o => o.Factura)
             .HasForeignKey(o => o.FacturaId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── Bodega ────────────────────────────────────────────────────────
        builder.Entity<Bodega>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Nombre).HasMaxLength(200).IsRequired();
            e.HasIndex(b => b.TenantId).HasDatabaseName("IX_Bodegas_TenantId");
            e.HasQueryFilter(b => _tenantService.TenantId != null && b.TenantId == _tenantService.TenantId);
        });

        // ── MovimientoInventario ───────────────────────────────────────
        builder.Entity<MovimientoInventario>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Tipo).HasMaxLength(30).IsRequired();
            e.HasIndex(m => m.TenantId).HasDatabaseName("IX_Movimientos_TenantId");
            e.HasIndex(m => m.ProductoId).HasDatabaseName("IX_Movimientos_ProductoId");
            e.HasQueryFilter(m => _tenantService.TenantId != null && m.TenantId == _tenantService.TenantId);
            e.HasOne(m => m.Producto).WithMany()
             .HasForeignKey(m => m.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.BodegaOrigen).WithMany()
             .HasForeignKey(m => m.BodegaOrigenId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(m => m.BodegaDestino).WithMany()
             .HasForeignKey(m => m.BodegaDestinoId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProductoInventario: relación con Bodega ───────────────────────
        builder.Entity<ProductoInventario>(e =>
        {
            e.HasOne(p => p.Bodega).WithMany(b => b.Productos)
             .HasForeignKey(p => p.BodegaId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── Accounting: CuentaContable ────────────────────────────────────────────────
        builder.Entity<CuentaContable>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Codigo).HasMaxLength(20).IsRequired();
            e.Property(c => c.Nombre).HasMaxLength(200).IsRequired();
            e.HasIndex(c => new { c.TenantId, c.Codigo }).IsUnique();
            e.HasQueryFilter(c => _tenantService.TenantId != null && c.TenantId == _tenantService.TenantId);
        });

        // ── Accounting: AsientoContable ───────────────────────────────────────────────
        builder.Entity<AsientoContable>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Referencia).HasMaxLength(50);
            e.HasIndex(a => a.TenantId);
            e.HasQueryFilter(a => _tenantService.TenantId != null && a.TenantId == _tenantService.TenantId);
            e.HasMany(a => a.Lineas).WithOne(l => l.AsientoContable)
             .HasForeignKey(l => l.AsientoContableId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Accounting: LineaAsientoContable ──────────────────────────────────────────
        builder.Entity<LineaAsientoContable>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Debito).HasColumnType("decimal(14,2)");
            e.Property(l => l.Credito).HasColumnType("decimal(14,2)");
            e.HasOne(l => l.CuentaContable).WithMany()
             .HasForeignKey(l => l.CuentaContableId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Tercero).WithMany()
             .HasForeignKey(l => l.TerceroId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Agenda: Appointment ──────────────────────────────────────────────────
        builder.Entity<Appointment>(e =>
        {
            e.HasKey(a => a.Id);
            // Optimized index for Mechanic schedule queries
            e.HasIndex(a => new { a.TenantId, a.MechanicId, a.StartDateTime, a.EndDateTime })
             .HasDatabaseName("IX_Appointments_Tenant_Mechanic_Dates")
             .IncludeProperties(a => new { a.Status });
            e.HasQueryFilter(a => _tenantService.TenantId != null && a.TenantId == _tenantService.TenantId);
            e.HasOne(a => a.Cliente).WithMany()
             .HasForeignKey(a => a.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Vehiculo).WithMany()
             .HasForeignKey(a => a.VehiculoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Agenda: MechanicAvailability ──────────────────────────────────────────
        builder.Entity<MechanicAvailability>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.TenantId, m.MechanicId, m.DayOfWeek }).HasDatabaseName("IX_MechanicAvailabilities_Mechanic_Day_Active");
            e.HasQueryFilter(m => _tenantService.TenantId != null && m.TenantId == _tenantService.TenantId);
        });

        // ── EventoTrazabilidad ────────────────────────────────────────────────────────
        builder.Entity<EventoTrazabilidad>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Descripcion).HasMaxLength(500).IsRequired();
            e.HasIndex(ev => ev.TenantId).HasDatabaseName("IX_EventosTrazabilidad_TenantId");
            e.HasIndex(ev => ev.VehiculoId).HasDatabaseName("IX_EventosTrazabilidad_VehiculoId");
            e.HasQueryFilter(ev => _tenantService.TenantId != null && ev.TenantId == _tenantService.TenantId);
            e.Ignore(ev => ev.TipoIcono);
            e.Ignore(ev => ev.TipoClase);
            e.HasOne(ev => ev.Vehiculo).WithMany()
             .HasForeignKey(ev => ev.VehiculoId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Payroll: NominaRegistro ────────────────────────────────────────────────
        builder.Entity<NominaRegistro>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Periodo).HasMaxLength(7).IsRequired();
            e.Property(n => n.SalarioBase).HasColumnType("decimal(12,2)");
            e.Property(n => n.Comisiones).HasColumnType("decimal(12,2)");
            e.Property(n => n.Deducciones).HasColumnType("decimal(12,2)");
            
            // Critical index for Payroll list/filtering
            e.HasIndex(n => new { n.TenantId, n.Periodo, n.Estado, n.UserId })
             .HasDatabaseName("IX_NominaRegistros_Tenant_Period_Status_User");
             
            e.HasQueryFilter(n => _tenantService.TenantId != null && n.TenantId == _tenantService.TenantId);
        });

        // ── Payroll: EmpleadoContrato ─────────────────────────────────────────────
        builder.Entity<EmpleadoContrato>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.SalarioBase).HasColumnType("decimal(12,2)");
            e.Property(c => c.PorcentajeComision).HasColumnType("decimal(5,2)");
            e.HasIndex(c => c.TenantId);
            e.HasQueryFilter(c => _tenantService.TenantId != null && c.TenantId == _tenantService.TenantId);
            e.HasOne<ApplicationUser>().WithOne(u => u.EmpleadoContrato)
             .HasForeignKey<EmpleadoContrato>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasOne(u => u.Tenant)
             .WithMany()
             .HasForeignKey(u => u.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed subscription plans
        builder.Entity<PlanSuscripcion>().HasData(
            new PlanSuscripcion { Id = 1, Nombre = "Básico", LimiteUsuarios = 3, Precio = 299m, Descripcion = "Hasta 3 usuarios, módulos esenciales" },
            new PlanSuscripcion { Id = 2, Nombre = "Profesional", LimiteUsuarios = 10, Precio = 699m, Descripcion = "Hasta 10 usuarios, reportes PDF/Excel" },
            new PlanSuscripcion { Id = 3, Nombre = "Empresarial", LimiteUsuarios = 50, Precio = 1499m, Descripcion = "Usuarios ilimitados, soporte prioritario" }
        );
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(isolationLevel, cancellationToken);
}
