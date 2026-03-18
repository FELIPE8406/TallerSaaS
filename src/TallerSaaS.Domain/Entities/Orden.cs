using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Domain.Entities;

public class Orden
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public EstadoOrden Estado { get; set; } = EstadoOrden.Recibido;
    public DateTime FechaEntrada { get; set; } = DateTime.UtcNow;
    public DateTime? FechaSalida { get; set; }
    public string? DiagnosticoInicial { get; set; }
    public string? TrabajoRealizado { get; set; }
    public string? Observaciones { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal IVA { get; set; }
    public bool AplicarRetencion { get; set; } = false;
    public decimal PorcentajeRetencion { get; set; }
    public decimal MontoRetencion { get; set; }
    public decimal Total { get; set; }
    public bool Pagada { get; set; } = false;
    public bool Bloqueada { get; set; } = false;
    public Guid? FacturaId { get; set; }
    public Factura? Factura { get; set; }
    
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public ICollection<ItemOrden> Items { get; set; } = new List<ItemOrden>();

    public string EstadoTexto => Estado switch
    {
        EstadoOrden.Recibido            => "Recibido",
        EstadoOrden.EnReparacion        => "En Reparación",
        EstadoOrden.Terminado           => "Terminado",
        EstadoOrden.Entregado           => "Entregado",
        EstadoOrden.Facturada           => "Facturada",
        EstadoOrden.EntregadoYFacturado => "Entregado y Facturado",
        _                               => "Desconocido"
    };

    public string EstadoClase => Estado switch
    {
        EstadoOrden.Recibido            => "badge-recibido",
        EstadoOrden.EnReparacion        => "badge-reparacion",
        EstadoOrden.Terminado           => "badge-terminado",
        EstadoOrden.Entregado           => "badge-entregado",
        EstadoOrden.Facturada           => "badge-facturada",
        EstadoOrden.EntregadoYFacturado => "badge-entregado-facturado",
        _                               => "bg-secondary"
    };
}
