using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Models;

namespace TallerSaaS.Application.Interfaces;

public interface INominaService
{
    Task<PagedResult<NominaRegistro>> GetPagedAsync(int page, int pageSize, string period, NominaStatus? status, string mechanicId);
    Task<int> GetCountAsync(string? search);
    Task<NominaRegistro?> GetByIdAsync(Guid id);
    Task GenerateBatchAsync(int month, int year);
    Task GenerateBatchAsync(string period);
    Task<NominaKpiSummary> GetKpiSummaryAsync(string period, NominaStatus? status, string mechanicId);
    Task RecalculateAsync(Guid id);
    Task<bool> EnviarNominaDIANAsync(Guid id);
}
