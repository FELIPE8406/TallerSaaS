using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Domain.Entities;

public class EventoTrazabilidad
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    public TipoEvento Tipo { get; set; }

    /// <summary>Descripción legible del evento, e.g. "Orden #ORD-202603-0001 creada".</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>ID del objeto relacionado (Orden, Factura, ProductoInventario).</summary>
    public Guid ReferenciaId { get; set; }

    public DateTime FechaEvento { get; set; } = DateTime.UtcNow;

    // ── Display helpers ───────────────────────────────────────────────────────
    public string TipoIcono => Tipo switch
    {
        TipoEvento.OrdenCreada          => "📋",
        TipoEvento.OrdenAdicionalCreada => "➕",
        TipoEvento.FacturaGenerada      => "🧾",
        TipoEvento.StockDescontado      => "📦",
        TipoEvento.EstadoCambiado       => "🔄",
        _                               => "📌"
    };

    public string TipoClase => Tipo switch
    {
        TipoEvento.OrdenCreada          => "timeline-orden",
        TipoEvento.OrdenAdicionalCreada => "timeline-adicional",
        TipoEvento.FacturaGenerada      => "timeline-factura",
        TipoEvento.StockDescontado      => "timeline-stock",
        TipoEvento.EstadoCambiado       => "timeline-estado",
        _                               => "timeline-default"
    };
}
