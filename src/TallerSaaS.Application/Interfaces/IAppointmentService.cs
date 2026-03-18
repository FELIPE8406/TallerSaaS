using TallerSaaS.Application.DTOs;

namespace TallerSaaS.Application.Interfaces;

public interface IAppointmentService
{
    Task<List<AppointmentDto>> GetAppointmentsAsync(DateTime start, DateTime end);
    Task<AppointmentDto> GetAppointmentByIdAsync(Guid id);
    Task<Guid> CreateAppointmentAsync(AppointmentDto dto);
    Task UpdateAppointmentAsync(AppointmentDto dto);
    Task DeleteAppointmentAsync(Guid id);
    Task UpdateAppointmentStatusAsync(Guid id, int status);
    Task<Guid> ConvertToServiceOrderAsync(Guid appointmentId);
    
    // Availability
    Task<List<MechanicAvailabilityDto>> GetMechanicAvailabilityAsync(string mechanicId);
    Task UpdateMechanicAvailabilityAsync(string mechanicId, List<MechanicAvailabilityDto> availability);
    Task<bool> IsMechanicAvailableAsync(string mechanicId, DateTime start, DateTime end);
    
    // Notifications
    Task SendWhatsappReminderAsync(Guid appointmentId);
}
