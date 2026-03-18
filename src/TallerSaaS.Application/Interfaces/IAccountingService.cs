using TallerSaaS.Domain.Entities;

namespace TallerSaaS.Application.Interfaces;

public interface IAccountingService
{
    /// <summary>
    /// Registra el asiento contable por la generación de una factura (Ingresos y Cuentas por Cobrar).
    /// </summary>
    Task RegistrarFacturaAsync(Factura factura);

    /// <summary>
    /// Registra el asiento contable por el consumo de repuestos (Costo de Ventas e Inventario).
    /// </summary>
    Task RegistrarSalidaInventarioAsync(Orden orden);

    /// <summary>
    /// Registra el asiento contable por el recaudo de una factura (Caja/Bancos y Cuentas por Cobrar).
    /// </summary>
    Task RegistrarPagoAsync(Pago pago, Factura factura);
}
