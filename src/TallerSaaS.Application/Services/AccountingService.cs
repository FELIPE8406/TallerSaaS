using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;

namespace TallerSaaS.Application.Services;

public class AccountingService : IAccountingService
{
    private readonly IApplicationDbContext _db;

    public AccountingService(IApplicationDbContext db)
    {
        _db = db;
    }

    private async Task<bool> IsPremiumAsync(Guid tenantId)
    {
        var tenant = await _db.Tenants
            .Include(t => t.PlanSuscripcion)
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        
        // Only "Empresarial" (PlanId 3) has accounting enabled in UI/Reports.
        // But we record "Dark Data" for everyone if it's Básico/Profesional to encourage upgrades.
        return tenant?.PlanSuscripcionId == 3;
    }

    private async Task<CuentaContable> GetOrCreateAccountAsync(Guid tenantId, string codigo, string nombre, int clase)
    {
        // 1. Check if it's already in the database
        var cuenta = await _db.CuentasContables
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Codigo == codigo);

        if (cuenta != null) return cuenta;

        // 2. Check if it was added to the ChangeTracker in this same request/operation
        cuenta = _db.CuentasContables.Local
            .FirstOrDefault(c => c.TenantId == tenantId && c.Codigo == codigo);

        if (cuenta == null)
        {
            cuenta = new CuentaContable
            {
                TenantId          = tenantId,
                Codigo            = codigo,
                Nombre            = nombre,
                Clase             = clase,
                EsActiva          = true,
                PermiteMovimiento = true
            };
            _db.CuentasContables.Add(cuenta);
        }

        return cuenta;
    }

    public async Task RegistrarFacturaAsync(Factura factura)
    {
        // Evento 1: Generación de Factura
        // Débito: 130505 (Cuentas por Cobrar) -> Total
        // Crédito: 413505 (Ingresos por Servicios) -> Subtotal Servicios
        // Crédito: 417505 (Ingresos por Repuestos) -> Subtotal Repuestos
        // Crédito: 240801 (IVA Generado 19%) -> IVA

        var asiento = new AsientoContable
        {
            TenantId = factura.TenantId,
            Fecha = factura.FechaEmision,
            Referencia = factura.NumeroFactura,
            Descripcion = $"Facturación: {factura.NumeroFactura}",
            TipoEvento = "Facturacion"
        };

        // Identificar ingresos por tipo (Servicio vs Repuesto) y acumular retenciones
        decimal subtotalServicios = 0;
        decimal subtotalRepuestos = 0;
        decimal totalRetencion    = 0;

        foreach (var orden in factura.Ordenes)
        {
            totalRetencion += orden.MontoRetencion;
            foreach (var item in orden.Items)
            {
                if (item.Tipo == "Servicio")
                    subtotalServicios += (item.Cantidad * item.PrecioUnitario);
                else
                    subtotalRepuestos += (item.Cantidad * item.PrecioUnitario);
            }
        }

        // 1. Cuentas por Cobrar (130505) - Neto a recibir
        var cuentaAR = await GetOrCreateAccountAsync(factura.TenantId, "130505", "Clientes - Nacionales", 1);
        asiento.Lineas.Add(new LineaAsientoContable
        {
            CuentaContable = cuentaAR,
            Debito = factura.Total, // Factura.Total ya viene neto (Base + IVA - Rete)
            TerceroId = factura.Ordenes.FirstOrDefault()?.Vehiculo?.ClienteId
        });

        // 2. Retención en la Fuente (135515) - Anticipo de impuesto
        if (totalRetencion > 0)
        {
            var cuentaRet = await GetOrCreateAccountAsync(factura.TenantId, "135515", "Anticipo de Impuestos - Retención", 1);
            asiento.Lineas.Add(new LineaAsientoContable
            {
                CuentaContable = cuentaRet,
                Debito = totalRetencion,
                TerceroId = factura.Ordenes.FirstOrDefault()?.Vehiculo?.ClienteId
            });
        }

        // 3. Ingresos (4135 / 4175)
        if (subtotalServicios > 0)
        {
            var cuentaIngresoServ = await GetOrCreateAccountAsync(factura.TenantId, "413505", "Ingresos Servicios Mecánica", 4);
            asiento.Lineas.Add(new LineaAsientoContable
            {
                CuentaContable = cuentaIngresoServ,
                Credito = subtotalServicios
            });
        }

        if (subtotalRepuestos > 0)
        {
            var cuentaIngresoRep = await GetOrCreateAccountAsync(factura.TenantId, "417505", "Venta de Repuestos", 4);
            asiento.Lineas.Add(new LineaAsientoContable
            {
                CuentaContable = cuentaIngresoRep,
                Credito = subtotalRepuestos
            });
        }

        // 3. IVA (240801)
        if (factura.IVA > 0)
        {
            var cuentaIVA = await GetOrCreateAccountAsync(factura.TenantId, "240801", "IVA Generado 19%", 2);
            asiento.Lineas.Add(new LineaAsientoContable
            {
                CuentaContable = cuentaIVA,
                Credito = factura.IVA
            });
        }

        _db.AsientosContables.Add(asiento);
        // Persistence handled by the context that saved the factura
    }

    public async Task RegistrarSalidaInventarioAsync(Orden orden)
    {
        // Evento 2: Costo de Ventas (Salida de Inventario)
        // Se dispara cuando se factura (y por ende se cierra) la orden.
        // Débito: 613505 (Costo de Ventas - Repuestos)
        // Crédito: 143505 (Inventario - Repuestos)

        var itemsRepuestos = orden.Items.Where(i => i.Tipo != "Servicio" && i.ProductoInventarioId.HasValue).ToList();
        if (!itemsRepuestos.Any()) return;

        var asiento = new AsientoContable
        {
            TenantId = orden.TenantId,
            Fecha = DateTime.UtcNow,
            Referencia = orden.NumeroOrden,
            Descripcion = $"Costo de Venta: {orden.NumeroOrden}",
            TipoEvento = "CostoVenta"
        };

        decimal costoTotal = 0;
        foreach (var item in itemsRepuestos)
        {
            // Nota: Aquí se asume que ProductoInventario ya tiene el costo cargado o se consulta.
            // En un sistema real usaríamos el costo promedio ponderado.
            var producto = await _db.Inventario.FindAsync(item.ProductoInventarioId);
            if (producto != null)
            {
                costoTotal += (item.Cantidad * producto.PrecioCompra); 
            }
        }

        if (costoTotal == 0) return;

        // 1. Costo de Ventas (613505)
        var cuentaCosto = await GetOrCreateAccountAsync(orden.TenantId, "613505", "Costo de Ventas - Repuestos", 6);
        asiento.Lineas.Add(new LineaAsientoContable
        {
            CuentaContable = cuentaCosto,
            Debito = costoTotal
        });

        // 2. Inventario (143505)
        var cuentaInv = await GetOrCreateAccountAsync(orden.TenantId, "143505", "Inventario - Repuestos", 1);
        asiento.Lineas.Add(new LineaAsientoContable
        {
            CuentaContable = cuentaInv,
            Credito = costoTotal
        });

        _db.AsientosContables.Add(asiento);
    }

    public async Task RegistrarPagoAsync(Pago pago, Factura factura)
    {
        // Evento 3: Recaudo
        // Débito: 111005 (Bancos) -> Total Recibido
        // Crédito: 130505 (Clientes) -> Total Factura
        // Débito: 135515 (Retención en la Fuente) -> Si aplica

        var asiento = new AsientoContable
        {
            TenantId = pago.TenantId,
            Fecha = pago.Fecha,
            Referencia = pago.Referencia ?? factura.NumeroFactura,
            Descripcion = $"Recaudo Factura: {factura.NumeroFactura}",
            TipoEvento = "Recaudo"
        };

        // 1. Banco (111005)
        var cuentaBanco = await GetOrCreateAccountAsync(pago.TenantId, "111005", "Bancos - Moneda Nacional", 1);
        asiento.Lineas.Add(new LineaAsientoContable
        {
            CuentaContable = cuentaBanco,
            Debito = pago.Monto
        });

        // 2. Cuentas por Cobrar (130505)
        var cuentaAR = await GetOrCreateAccountAsync(pago.TenantId, "130505", "Clientes - Nacionales", 1);
        asiento.Lineas.Add(new LineaAsientoContable
        {
            CuentaContable = cuentaAR,
            Credito = factura.Total, // Se cancela la deuda completa
            TerceroId = factura.Ordenes.FirstOrDefault()?.Vehiculo?.ClienteId
        });

        // 3. Diferencia (Retenciones o Descuentos)
        var diferencia = (factura.Total - pago.Monto);
        if (diferencia > 0)
        {
            // Asumimos retención en la fuente por simplicidad en este MVP
            var cuentaRetencion = await GetOrCreateAccountAsync(pago.TenantId, "135515", "Anticipo de Impuestos - Retención", 1);
            asiento.Lineas.Add(new LineaAsientoContable
            {
                CuentaContable = cuentaRetencion,
                Debito = diferencia
            });
        }

        _db.AsientosContables.Add(asiento);
    }
}
