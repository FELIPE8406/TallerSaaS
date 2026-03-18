namespace TallerSaaS.Domain.Enums;

/// <summary>
/// Estado de envío electrónico de una factura a la DIAN.
/// </summary>
public enum EstadoEnvioFactura
{
    /// <summary>Factura interna — no requiere envío electrónico.</summary>
    NoAplica = 0,

    /// <summary>Factura electrónica registrada, pendiente de envío a la DIAN.</summary>
    PendienteEnvio = 1,

    /// <summary>Enviada exitosamente a la DIAN (para uso futuro al habilitar integración).</summary>
    Enviada = 2
}
