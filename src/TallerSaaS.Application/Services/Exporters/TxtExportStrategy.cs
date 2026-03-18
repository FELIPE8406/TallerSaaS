using System.Text;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;

namespace TallerSaaS.Application.Services.Exporters;

/// <summary>
/// Exporta datos a texto plano (.txt) con columnas de ancho fijo.
/// Útil para impresión directa o sistemas de legado.
/// </summary>
public class TxtExportStrategy : IExportStrategy
{
    private readonly IApplicationDbContext _db;
    public string ContentType => "text/plain";
    public string FileExtension => "txt";

    public TxtExportStrategy(IApplicationDbContext db) => _db = db;

    public async Task<byte[]> ExportarOrdenesAsync(ReporteFilter filtro)
    {
        var ordenes = await _db.Ordenes
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Where(o => o.FechaEntrada >= filtro.Desde && o.FechaEntrada <= filtro.Hasta)
            .OrderByDescending(o => o.FechaEntrada)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"REPORTE DE ÓRDENES — Período: {filtro.Desde:dd/MM/yyyy} al {filtro.Hasta:dd/MM/yyyy}");
        sb.AppendLine(new string('=', 110));
        sb.AppendLine($"{"No. Orden",-18} {"Cliente",-28} {"Estado",-22} {"Fecha Entrada",-15} {"Total",12}");
        sb.AppendLine(new string('-', 110));

        foreach (var o in ordenes)
            sb.AppendLine($"{o.NumeroOrden,-18} {Truncate(o.Vehiculo?.Cliente?.NombreCompleto, 28),-28} " +
                          $"{o.EstadoTexto,-22} {o.FechaEntrada:dd/MM/yyyy,-15} {o.Total,12:N0}");

        sb.AppendLine(new string('=', 110));
        sb.AppendLine($"Total registros: {ordenes.Count}   |   " +
                      $"Total: ${ordenes.Sum(o => o.Total):N0}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportarFacturasAsync(ReporteFilter filtro)
    {
        var facturas = await _db.Facturas
            .Include(f => f.Ordenes)
            .Where(f => f.FechaEmision >= filtro.Desde && f.FechaEmision <= filtro.Hasta)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"REPORTE DE FACTURAS — Período: {filtro.Desde:dd/MM/yyyy} al {filtro.Hasta:dd/MM/yyyy}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"{"No. Factura",-20} {"Fecha Emisión",-15} {"Órdenes",8} {"Subtotal",14} {"Descuento",12} {"IVA",12} {"Total",14}");
        sb.AppendLine(new string('-', 80));

        foreach (var f in facturas)
            sb.AppendLine($"{f.NumeroFactura,-20} {f.FechaEmision:dd/MM/yyyy,-15} {f.Ordenes.Count,8} " +
                          $"{f.Subtotal,14:N0} {f.Descuento,12:N0} {f.IVA,12:N0} {f.Total,14:N0}");

        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"Total facturas: {facturas.Count}   |   " +
                      $"Total facturado: ${facturas.Sum(f => f.Total):N0}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportarClientesVehiculosAsync(ReporteFilter filtro)
    {
        var clientes = await _db.Clientes
            .Include(c => c.Vehiculos)
            .Where(c => c.Activo && c.FechaRegistro >= filtro.Desde && c.FechaRegistro <= filtro.Hasta)
            .OrderBy(c => c.NombreCompleto)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"RELACIÓN CLIENTE - VEHÍCULO — Período: {filtro.Desde:dd/MM/yyyy} al {filtro.Hasta:dd/MM/yyyy}");
        sb.AppendLine(new string('=', 90));
        sb.AppendLine($"{"Cliente",-30} {"Teléfono",-16} {"Vehículo",-30} {"Placa",-10}");
        sb.AppendLine(new string('-', 90));

        foreach (var c in clientes)
        {
            if (!c.Vehiculos.Any())
                sb.AppendLine($"{Truncate(c.NombreCompleto, 30),-30} {c.Telefono ?? "—",-16} {"Sin vehículo",-30}");
            else
                foreach (var v in c.Vehiculos)
                    sb.AppendLine($"{Truncate(c.NombreCompleto, 30),-30} {c.Telefono ?? "—",-16} " +
                                  $"{Truncate($"{v.Anio} {v.Marca} {v.Modelo}", 30),-30} {v.Placa ?? "N/A",-10}");
        }

        sb.AppendLine(new string('=', 90));
        sb.AppendLine($"Total clientes: {clientes.Count}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Truncate(string? val, int max) =>
        val == null ? "" : val.Length <= max ? val : val[..(max - 1)] + "…";
}
