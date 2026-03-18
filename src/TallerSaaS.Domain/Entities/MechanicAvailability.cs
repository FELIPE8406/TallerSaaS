using System.ComponentModel.DataAnnotations;

namespace TallerSaaS.Domain.Entities;

public class MechanicAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string MechanicId { get; set; } = string.Empty;
    
    public int DayOfWeek { get; set; } // 0-6 (Sunday-Saturday)
    
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}
