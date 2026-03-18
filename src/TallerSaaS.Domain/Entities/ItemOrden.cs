namespace TallerSaaS.Domain.Entities;

public class ItemOrden
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenId { get; set; }
    public Orden? Orden { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Servicio"; // Servicio o Refaccion
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
    public Guid? ProductoInventarioId { get; set; }
    public ProductoInventario? ProductoInventario { get; set; }
}
