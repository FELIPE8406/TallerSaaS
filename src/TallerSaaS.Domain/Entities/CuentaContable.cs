namespace TallerSaaS.Domain.Entities;

/// <summary>
/// Representa una cuenta del Plan Único de Cuentas (PUC) de Colombia.
/// </summary>
public class CuentaContable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    /// <summary>
    /// Código contable (ej: 110505). 
    /// Nivel 1: Clase, Nivel 2: Grupo, Nivel 3: Cuenta, Nivel 4: Subcuenta.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// 1: Activo, 2: Pasivo, 3: Patrimonio, 4: Ingresos, 5: Gastos, 6: Costos
    /// </summary>
    public int Clase { get; set; }

    public bool EsActiva { get; set; } = true;

    /// <summary>
    /// Indica si es una cuenta de detalle (permite movimientos) o de grupo (solo sumatoria).
    /// </summary>
    public bool PermiteMovimiento { get; set; } = true;
}
