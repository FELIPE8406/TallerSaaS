namespace TallerSaaS.Domain.Enums;

/// <summary>
/// Tipo de facturación seleccionado al emitir una factura.
/// </summary>
public enum TipoFacturacion
{
    /// <summary>Factura interna (documento sin envío a la DIAN).</summary>
    NoElectronica = 0,

    /// <summary>Factura electrónica — pendiente de integración con la DIAN.</summary>
    Electronica = 1
}
