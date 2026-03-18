using System.ComponentModel.DataAnnotations;
using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Domain.Entities;

public class ProductoInventario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    [MaxLength(300)]
    public string Nombre { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? SKU { get; set; }
    public string? Descripcion { get; set; }
    [MaxLength(100)]
    public string? Categoria { get; set; }
    public int Stock { get; set; } = 0;
    public int StockMinimo { get; set; } = 5;
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    [MaxLength(300)]
    public string? Proveedor { get; set; }
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;

    /// <summary>Bodega principal donde se almacena este producto. Null = sin bodega asignada.</summary>
    public Guid? BodegaId { get; set; }
    public Bodega? Bodega { get; set; }

    /// <summary>
    /// Tipo de ítem: Refaccion (parte física, stock limitado) o Servicio (disponibilidad infinita).
    /// </summary>
    public TipoItemProducto TipoItem { get; set; } = TipoItemProducto.Refaccion;

    public string NivelStock => Stock <= 0 ? "Agotado"
        : Stock <= StockMinimo ? "Bajo"
        : "OK";

    public string NivelStockClase => Stock <= 0 ? "danger"
        : Stock <= StockMinimo ? "warning"
        : "success";
}

