namespace TallerSaaS.Domain.Entities;

/// <summary>
/// Representa una bodega o almacén físico de inventario dentro de un Tenant.
/// Soporta múltiples bodegas con control independiente de stock.
/// </summary>
public class Bodega
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Ubicacion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<ProductoInventario> Productos { get; set; } = new List<ProductoInventario>();
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
}
