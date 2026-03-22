using TallerSaaS.Domain.Entities;

namespace TallerSaaS.Application.Interfaces;

public interface IEmpleadoContratoService
{
    Task<EmpleadoContrato?> GetByUserIdAsync(string userId);
    Task<List<EmpleadoContrato>> GetAllActiveAsync();
    Task<bool> SaveContratoAsync(string userId, decimal salarioBase, decimal comision, bool activo, string tipoEmpleado, string? urlPdf);
}
