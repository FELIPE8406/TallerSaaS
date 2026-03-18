using TallerSaaS.Domain.Entities;

namespace TallerSaaS.Web.Models;

public class TransaccionesViewModel
{
    public List<Pago> Transacciones { get; set; } = new();
    
    // KPIs
    public decimal RecaudoMes { get; set; }
    public decimal IngresosMesAnterior { get; set; }
    public decimal PagosPendientes { get; set; }
    public double TasaRenovacion { get; set; }

    // Dropdown para registro manual
    public List<Tenant> Talleres { get; set; } = new();
}

public class PagoManualViewModel
{
    public Guid TenantId { get; set; }
    public decimal Monto { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = "Transferencia";
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
