namespace TallerSaaS.Domain.Entities;

/// <summary>
/// Cabecera de un asiento contable (comprobante).
/// </summary>
public class AsientoContable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Referencia al documento origen (Factura #, Orden #, Pago #).
    /// </summary>
    public string? Referencia { get; set; }
    
    public string? Descripcion { get; set; }

    /// <summary>
    /// Tipo de evento que originó el asiento (Facturacion, CostoVenta, Recaudo).
    /// </summary>
    public string? TipoEvento { get; set; }

    public ICollection<LineaAsientoContable> Lineas { get; set; } = new List<LineaAsientoContable>();
}
