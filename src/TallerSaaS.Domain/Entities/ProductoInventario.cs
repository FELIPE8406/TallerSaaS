namespace TallerSaaS.Domain.Entities;

public class ProductoInventario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Descripcion { get; set; }
    public string? Categoria { get; set; }
    public int Stock { get; set; } = 0;
    public int StockMinimo { get; set; } = 5;
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public string? Proveedor { get; set; }
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;

    public string NivelStock => Stock <= 0 ? "Agotado"
        : Stock <= StockMinimo ? "Bajo"
        : "OK";

    public string NivelStockClase => Stock <= 0 ? "danger"
        : Stock <= StockMinimo ? "warning"
        : "success";
}
