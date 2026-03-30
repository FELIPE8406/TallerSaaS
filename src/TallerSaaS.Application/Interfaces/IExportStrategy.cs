using TallerSaaS.Application.DTOs;

namespace TallerSaaS.Application.Interfaces;

/// <summary>
/// Strategy Pattern para exportaciones de reportes en múltiples formatos.
/// Cada implementación produce un formato diferente (Excel, CSV, PDF, TXT).
/// </summary>
public interface IExportStrategy
{
    string ContentType { get; }
    string FileExtension { get; }

    Task<byte[]> ExportarOrdenesAsync(ReporteFilter filtro, string tenantNombre, string tenantNIT);
    Task<byte[]> ExportarFacturasAsync(ReporteFilter filtro, string tenantNombre, string tenantNIT);
    Task<byte[]> ExportarClientesVehiculosAsync(ReporteFilter filtro, string tenantNombre, string tenantNIT);
}

/// <summary>Tipos de reporte disponibles para exportación.</summary>
public enum TipoReporte
{
    Ordenes,
    Facturas,
    ClientesVehiculos
}
