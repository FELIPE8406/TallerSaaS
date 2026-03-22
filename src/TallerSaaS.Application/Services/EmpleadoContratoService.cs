using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Application.Services;

public class EmpleadoContratoService : IEmpleadoContratoService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;

    public EmpleadoContratoService(IApplicationDbContext db, ICurrentTenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
    }

    public async Task<EmpleadoContrato?> GetByUserIdAsync(string userId)
    {
        return await _db.EmpleadoContratos
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<List<EmpleadoContrato>> GetAllActiveAsync()
    {
        return await _db.EmpleadoContratos
            .Where(c => c.Activo)
            .ToListAsync();
    }

    public async Task<bool> SaveContratoAsync(string userId, decimal salarioBase, decimal comision, bool activo, string tipoEmpleado, string? urlPdf)
    {
        var tenantId = _tenantService.TenantId;
        if (!tenantId.HasValue) return false;

        var contrato = await _db.EmpleadoContratos.FirstOrDefaultAsync(c => c.UserId == userId);
        
        if (contrato == null)
        {
            contrato = new EmpleadoContrato
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenantId.Value,
                SalarioBase = salarioBase,
                PorcentajeComision = comision,
                Activo = activo,
                TipoEmpleado = tipoEmpleado,
                URLContratoPDF = urlPdf,
                FechaIngreso = DateTime.UtcNow
            };
            _db.EmpleadoContratos.Add(contrato);
        }
        else
        {
            contrato.SalarioBase = salarioBase;
            contrato.PorcentajeComision = comision;
            contrato.Activo = activo;
            contrato.TipoEmpleado = tipoEmpleado;
            if (urlPdf != null) contrato.URLContratoPDF = urlPdf;
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
