namespace TallerSaaS.Domain.Entities;

/// <summary>
/// Línea individual de un asiento contable (Débito o Crédito).
/// </summary>
public class LineaAsientoContable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid AsientoContableId { get; set; }
    public AsientoContable? AsientoContable { get; set; }

    public Guid CuentaContableId { get; set; }
    public CuentaContable? CuentaContable { get; set; }

    public decimal Debito { get; set; }
    public decimal Credito { get; set; }

    /// <summary>
    /// ID del cliente o proveedor asociado a este movimiento (opcional pero recomendado para auxiliares).
    /// </summary>
    public Guid? TerceroId { get; set; }
    public Cliente? Tercero { get; set; }

    /// <summary>
    /// ID del centro de costos (Mecánica, Latonería, etc.).
    /// </summary>
    public Guid? CentroCostoId { get; set; }
}
