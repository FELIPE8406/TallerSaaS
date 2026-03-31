namespace TallerSaaS.Domain.Entities;

/// <summary>
/// Registro histórico de todos los movimientos de inventario:
/// Entradas (compras, ajustes), Salidas (uso en órdenes) y Traslados entre bodegas.
/// </summary>
public class MovimientoInventario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    public Guid ProductoId { get; set; }
    public ProductoInventario? Producto { get; set; }

    /// <summary>Bodega de origen. Null si es una Entrada directa.</summary>
    public Guid? BodegaOrigenId { get; set; }
    public Bodega? BodegaOrigen { get; set; }

    /// <summary>Bodega de destino. Null si es una Salida directa.</summary>
    public Guid? BodegaDestinoId { get; set; }
    public Bodega? BodegaDestino { get; set; }

    /// <summary>Tipo de movimiento: Entrada, Salida, Traslado, AjusteEntrada, AjusteSalida</summary>
    public string Tipo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    /// <summary>Referencia al documento de origen (NumeroOrden, NumeroFactura, etc.).</summary>
    public string? Referencia { get; set; }

    public string? Observaciones { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

/// <summary>Valores válidos para el campo Tipo de MovimientoInventario.</summary>
public static class TipoMovimiento
{
    public const string Entrada       = "Entrada";
    public const string Salida        = "Salida";
    /// <summary>
    /// Movimiento contable de consumo al momento de facturar.
    /// No necesariamente implica un cambio adicional de stock (ej: si el stock ya fue descontado preventivamente).
    /// </summary>
    public const string Consumo       = "Consumo";
    public const string Traslado      = "Traslado";
    public const string AjusteEntrada = "AjusteEntrada";
    public const string AjusteSalida  = "AjusteSalida";
}
