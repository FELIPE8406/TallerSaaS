namespace TallerSaaS.Application.DTOs;

public class ClienteDto
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Cedula { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; }
    public int TotalVehiculos { get; set; }
}

public class VehiculoDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string? Placa { get; set; }
    public string? VIN { get; set; }
    public string? Color { get; set; }
    public string? Kilometraje { get; set; }
    public string Descripcion => $"{Anio} {Marca} {Modelo}";
}

public class OrdenDto
{
    public Guid Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public Guid VehiculoId { get; set; }
    public string VehiculoDescripcion { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;
    public int Estado { get; set; }
    public string EstadoTexto { get; set; } = string.Empty;
    public string EstadoClase { get; set; } = string.Empty;
    public DateTime FechaEntrada { get; set; }
    public DateTime? FechaSalida { get; set; }
    public string? DiagnosticoInicial { get; set; }
    public string? TrabajoRealizado { get; set; }
    public string? Observaciones { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal IVA { get; set; }
    public bool AplicarRetencion { get; set; }
    public decimal PorcentajeRetencion { get; set; }
    public decimal MontoRetencion { get; set; }
    public decimal Total { get; set; }
    public bool Pagada { get; set; }
    public bool Bloqueada { get; set; }
    public Guid? FacturaId { get; set; }
    public Guid? AppointmentId { get; set; }
    public List<ItemOrdenDto> Items { get; set; } = new();
}

public class FacturaDto
{
    public Guid Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }
    public string? Observaciones { get; set; }
    /// <summary>"NoElectronica" o "Electronica"</summary>
    public string TipoFacturacion { get; set; } = "NoElectronica";
    /// <summary>"NoAplica", "PendienteEnvio" o "Enviada"</summary>
    public string EstadoEnvio { get; set; } = "NoAplica";
    public List<OrdenDto> Ordenes { get; set; } = new();
}

public class EventoTrazabilidadDto
{
    public Guid Id { get; set; }
    public Guid VehiculoId { get; set; }
    public int Tipo { get; set; }
    public string TipoIcono { get; set; } = string.Empty;
    public string TipoClase { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Guid ReferenciaId { get; set; }
    public DateTime FechaEvento { get; set; }
}

public class TimelineVehiculoDto
{
    public Guid VehiculoId { get; set; }
    public string VehiculoDescripcion { get; set; } = string.Empty;
    public string? ClienteNombre { get; set; }
    public List<EventoTrazabilidadDto> Eventos { get; set; } = new();
}

public class ItemOrdenDto
{
    public Guid Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Servicio";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
    public Guid? ProductoInventarioId { get; set; }
}

public class InventarioDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Descripcion { get; set; }
    public string? Categoria { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public string? Proveedor { get; set; }
    public string NivelStock { get; set; } = string.Empty;
    public string NivelStockClase { get; set; } = string.Empty;
    public Guid? BodegaId { get; set; }
    public string? BodegaNombre { get; set; }
    /// <summary>"Refaccion" o "Servicio" — mapea a TipoItemProducto enum.</summary>
    public string TipoItem { get; set; } = "Refaccion";
}

public class BodegaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Ubicacion { get; set; }
    public bool Activo { get; set; } = true;
    public int TotalProductos { get; set; }
}

public class MovimientoInventarioDto
{
    public Guid Id { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string? BodegaOrigen { get; set; }
    public string? BodegaDestino { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }
    public DateTime Fecha { get; set; }
}

public class DashboardDto
{
    public int TotalClientes { get; set; }
    public int TotalVehiculos { get; set; }
    public int OrdenesAbiertas { get; set; }
    public decimal VentasMes { get; set; }
    /// <summary>Facturas electrónicas con EstadoEnvio = PendienteEnvio (pendientes de envío a la DIAN).</summary>
    public int FacturasPendientesDian { get; set; }
    public List<VentaMensualDto> VentasMensuales { get; set; } = new();
    public List<EstadoOrdenConteoDto> OrdenesPorEstado { get; set; } = new();
    public List<InventarioDto> ProductosBajoStock { get; set; } = new();
}

public class VentaMensualDto
{
    public string Mes { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class EstadoOrdenConteoDto
{
    public string Estado { get; set; } = string.Empty;
    public int Conteo { get; set; }
}

public class SuperAdminDashboardDto
{
    public int TotalTenants { get; set; }
    public int TenantsActivos { get; set; }
    public decimal IngresosTotales { get; set; }
    public decimal IngresosMes { get; set; }
    public decimal IngresosMesAnterior { get; set; }
    public decimal PagosPendientes { get; set; }
    public double TasaRenovacion { get; set; }
    public List<TenantResumenDto> Tenants { get; set; } = new();
}

public class TenantResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Activo { get; set; }
    public string? PlanNombre { get; set; }
    public decimal PrecioPlan { get; set; }
    public DateTime FechaAlta { get; set; }
    public int TotalUsuarios { get; set; }
}

/// <summary>Resultado compacto del AJAX de búsqueda de productos para el selector de ítems en Órdenes.</summary>
public class ProductoBusquedaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public decimal PrecioVenta { get; set; }
    public string TipoItem { get; set; } = "Refaccion";
    public Guid? BodegaId { get; set; }
    public string? BodegaNombre { get; set; }
}

// ── Accounting DTOs ─────────────────────────────────────────────────────────

public class CuentaContableDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Clase { get; set; }
    public bool EsActiva { get; set; }
    public bool PermiteMovimiento { get; set; }
}

public class AsientoContableDto
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; }
    public string? Referencia { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoEvento { get; set; }
    public List<LineaAsientoContableDto> Lineas { get; set; } = new();
}

public class LineaAsientoContableDto
{
    public Guid Id { get; set; }
    public Guid CuentaContableId { get; set; }
    public string CuentaCodigo { get; set; } = string.Empty;
    public string CuentaNombre { get; set; } = string.Empty;
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
    public Guid? TerceroId { get; set; }
    public string? TerceroNombre { get; set; }
}

public class LibroAuxiliarDto
{
    public string Cuenta { get; set; } = string.Empty;
    public string Tercero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Documento { get; set; }
    public string? Descripcion { get; set; }
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
    public decimal Saldo { get; set; }
}

public class BalancePruebaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal SaldoAnterior { get; set; }
    public decimal Debitos { get; set; }
    public decimal Creditos { get; set; }
    public decimal NuevoSaldo { get; set; }
}

// ── Agenda DTOs ─────────────────────────────────────────────────────────────

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public Guid VehiculoId { get; set; }
    public string VehiculoDescripcion { get; set; } = string.Empty;
    public string MechanicId { get; set; } = string.Empty;
    public string MechanicNombre { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int EstimatedDuration { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusTexto { get; set; } = string.Empty;
    public bool WhatsappReminderSent { get; set; }
}

public class MechanicAvailabilityDto
{
    public Guid Id { get; set; }
    public string MechanicId { get; set; } = string.Empty;
    public string MechanicNombre { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
