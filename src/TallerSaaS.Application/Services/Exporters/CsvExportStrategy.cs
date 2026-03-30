using System.Text;
using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Application.Services.Exporters;

/// <summary>
/// Exporta datos a CSV (separador semicolón).
/// Implementa IExportStrategy según el Patrón Estrategia.
/// </summary>
public class CsvExportStrategy : IExportStrategy
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;
    public string ContentType => "text/csv";
    public string FileExtension => "csv";

    public CsvExportStrategy(IApplicationDbContext db, ICurrentTenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
    }

    public async Task<byte[]> ExportarOrdenesAsync(ReporteFilter filtro, string tenantNombre, string tenantNIT)
    {
        var ordenes = await _db.Ordenes
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Where(o => o.FechaEntrada >= filtro.Desde && o.FechaEntrada <= filtro.Hasta)
            .OrderByDescending(o => o.FechaEntrada)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"{tenantNombre} — NIT: {tenantNIT} — REPORTE DE ÓRDENES");
        sb.AppendLine("No. Orden;Cliente;Vehículo;Placa;Estado;Fecha Entrada;Fecha Salida;Subtotal;Descuento;IVA;Total;Pagada");

        foreach (var o in ordenes)
            sb.AppendLine(string.Join(";",
                o.NumeroOrden,
                Csv(o.Vehiculo?.Cliente?.NombreCompleto),
                Csv(o.Vehiculo?.Descripcion),
                o.Vehiculo?.Placa ?? "",
                o.EstadoTexto,
                o.FechaEntrada.ToString("dd/MM/yyyy"),
                o.FechaSalida?.ToString("dd/MM/yyyy") ?? "",
                o.Subtotal, o.Descuento, o.IVA, o.Total,
                o.Pagada ? "Sí" : "No"));

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportarFacturasAsync(ReporteFilter filtro, string tenantNombre, string tenantNIT)
    {
        var facturas = await _db.Facturas
            .Include(f => f.Ordenes)
            .Where(f => f.FechaEmision >= filtro.Desde && f.FechaEmision <= filtro.Hasta)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"{tenantNombre} — NIT: {tenantNIT} — REPORTE DE FACTURAS");
        sb.AppendLine("No. Factura;Fecha Emisión;Órdenes Incluidas;Subtotal;Descuento;IVA;Total");

        foreach (var f in facturas)
            sb.AppendLine(string.Join(";",
                f.NumeroFactura,
                f.FechaEmision.ToString("dd/MM/yyyy"),
                f.Ordenes.Count,
                f.Subtotal, f.Descuento, f.IVA, f.Total));

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportarClientesVehiculosAsync(ReporteFilter filtro, string tenantNombre, string tenantNIT)
    {
        var clientes = await _db.Clientes
            .Include(c => c.Vehiculos)
            .Where(c => c.Activo && c.FechaRegistro >= filtro.Desde && c.FechaRegistro <= filtro.Hasta)
            .OrderBy(c => c.NombreCompleto)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"{tenantNombre} — NIT: {tenantNIT} — RELACIÓN CLIENTE - VEHÍCULO");
        sb.AppendLine("Cliente;Cédula;Email;Teléfono;Vehículo;Placa;VIN;Año;Kilometraje");

        foreach (var c in clientes)
        {
            if (!c.Vehiculos.Any())
                sb.AppendLine(string.Join(";", Csv(c.NombreCompleto), c.Cedula ?? "", c.Email ?? "", c.Telefono ?? "", "", "", "", "", ""));
            else
                foreach (var v in c.Vehiculos)
                    sb.AppendLine(string.Join(";",
                        Csv(c.NombreCompleto), c.Cedula ?? "", c.Email ?? "", c.Telefono ?? "",
                        $"{v.Anio} {v.Marca} {v.Modelo}", v.Placa ?? "", v.VIN ?? "", v.Anio, v.Kilometraje ?? ""));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Csv(string? val) =>
        val == null ? "" : val.Contains(';') ? $"\"{val}\"" : val;
}
