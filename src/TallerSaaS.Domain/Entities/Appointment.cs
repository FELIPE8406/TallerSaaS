using System.ComponentModel.DataAnnotations;
using TallerSaaS.Domain.Enums;

namespace TallerSaaS.Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    
    [Required]
    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    
    [Required]
    public Guid VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }
    
    [Required]
    public string MechanicId { get; set; } = string.Empty;
    
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    
    public int EstimatedDuration { get; set; } // minutes
    
    [MaxLength(200)]
    public string ServiceType { get; set; } = string.Empty;
    
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    
    public bool WhatsappReminderSent { get; set; } = false;
    
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
