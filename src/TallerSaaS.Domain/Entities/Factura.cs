namespace TallerSaaS.Domain.Entities;

public class Factura
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    /// <summary>Consecutivo legible: A-2026-0001</summary>
    public string NumeroFactura { get; set; } = string.Empty;

    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }

    /// <summary>Código QR generado (texto/URL) para validación.</summary>
    public string? CodigoQR { get; set; }

    /// <summary>Indica si el cliente firmó digitalmente en tablet.</summary>
    public bool FirmadaDigitalmente { get; set; } = false;

    public string? Observaciones { get; set; }

    /// <summary>Órdenes agrupadas en esta factura.</summary>
    public ICollection<Orden> Ordenes { get; set; } = new List<Orden>();
}
