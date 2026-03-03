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
    public decimal Total { get; set; }
    public bool Pagada { get; set; }
    public bool Bloqueada { get; set; }
    public Guid? FacturaId { get; set; }
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
    public string? CodigoQR { get; set; }
    public bool FirmadaDigitalmente { get; set; }
    public string? Observaciones { get; set; }
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
}

public class DashboardDto
{
    public int TotalClientes { get; set; }
    public int TotalVehiculos { get; set; }
    public int OrdenesAbiertas { get; set; }
    public decimal VentasMes { get; set; }
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
