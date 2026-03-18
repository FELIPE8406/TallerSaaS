using TallerSaaS.Domain.Enums;

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

    public string? Observaciones { get; set; }

    /// <summary>
    /// Tipo de facturación seleccionado por el usuario al emitir.
    /// NoElectronica = documento interno; Electronica = pendiente de envío a DIAN.
    /// </summary>
    public TipoFacturacion TipoFacturacion { get; set; } = TipoFacturacion.NoElectronica;

    /// <summary>
    /// Estado de envío electrónico. Solo relevante si TipoFacturacion = Electronica.
    /// </summary>
    public EstadoEnvioFactura EstadoEnvio { get; set; } = EstadoEnvioFactura.NoAplica;

    /// <summary>Órdenes agrupadas en esta factura.</summary>
    public ICollection<Orden> Ordenes { get; set; } = new List<Orden>();
}
