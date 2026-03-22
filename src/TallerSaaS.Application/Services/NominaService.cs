using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;
using TallerSaaS.Domain.Enums;
using TallerSaaS.Domain.Interfaces;
using TallerSaaS.Application.Models;
using TallerSaaS.Application.DTOs;

namespace TallerSaaS.Application.Services;

public class NominaService : INominaService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUserProvider _userProvider;

    public NominaService(IApplicationDbContext db, ICurrentTenantService tenantService, IUserProvider userProvider)
    {
        _db = db;
        _tenantService = tenantService;
        _userProvider = userProvider;
    }

    public async Task<PagedResult<NominaRegistro>> GetPagedAsync(int page, int pageSize, string period, NominaStatus? status, string mechanicId)
    {
        var tenantId = _tenantService.TenantId;
        var query = _db.NominaRegistros
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId);

        // Default to current month if no period provided
        if (string.IsNullOrEmpty(period))
        {
            period = DateTime.Now.ToString("yyyy-MM");
        }

        query = query.Where(n => n.Periodo == period);

        if (status.HasValue)
        {
            query = query.Where(n => n.Estado == status.Value);
        }

        if (!string.IsNullOrEmpty(mechanicId))
        {
            query = query.Where(n => n.UserId == mechanicId);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.Periodo)
            .ThenBy(n => n.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<NominaRegistro>
        {
            Data = items,
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetCountAsync(string? search) => await _db.NominaRegistros.AsNoTracking().CountAsync(n => n.TenantId == _tenantService.TenantId);

    public async Task<NominaRegistro?> GetByIdAsync(Guid id)
    {
        return await _db.NominaRegistros
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == _tenantService.TenantId);
    }

    public async Task GenerateBatchAsync(int month, int year)
    {
        var tenantId = _tenantService.TenantId;
        if (tenantId == null) return;

        var periodo = $"{year}-{month:D2}";
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Remove existing Drafts for this period to allow regeneration
        var existingDrafts = _db.NominaRegistros.Where(n => n.TenantId == tenantId && n.Periodo == periodo && n.Estado == NominaStatus.Draft);
        _db.NominaRegistros.RemoveRange(existingDrafts);

        var empleados = await _db.EmpleadoContratos
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.Activo)
            .ToListAsync();

        if (!empleados.Any()) return;

        // ── PERFORMANCE: Fetch ALL relevant orders for ALL employees in ONE query (Avoid N+1) ──
        var allServiceOrders = await _db.Ordenes
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Appointment)
            .Where(o => o.TenantId == tenantId 
                     && (o.Estado == EstadoOrden.Facturada || o.Estado == EstadoOrden.EntregadoYFacturado)
                     && o.FechaSalida >= startDate && o.FechaSalida <= endDate
                     && o.Appointment != null)
            .ToListAsync();

        // Group orders by MechanicId in memory for fast lookup
        var ordersByMechanic = allServiceOrders
            .Where(o => o.Appointment?.MechanicId != null)
            .GroupBy(o => o.Appointment!.MechanicId)
            .ToDictionary(g => g.Key!, g => g.ToList());

        foreach (var emp in empleados)
        {
            // Lookup pre-fetched orders instead of querying the DB in a loop
            ordersByMechanic.TryGetValue(emp.UserId, out var mechanicOrders);
            
            decimal laborRevenue = 0;
            if (mechanicOrders != null)
            {
                laborRevenue = mechanicOrders
                    .SelectMany(o => o.Items)
                    .Where(i => i.Tipo == "Servicio" || i.Tipo == "Mano de Obra")
                    .Sum(i => i.Cantidad * i.PrecioUnitario);
            }

            decimal baseSalary = emp.SalarioBase;
            decimal commissions = laborRevenue * (emp.PorcentajeComision / 100m);
            decimal deductions = 0;
            
            var registro = new NominaRegistro
            {
                UserId = emp.UserId,
                TenantId = tenantId.Value,
                Periodo = periodo,
                SalarioBase = baseSalary,
                Comisiones = commissions,
                Deducciones = deductions,
                IngresosGenerados = laborRevenue,
                Estado = NominaStatus.Draft
            };
            
            _db.NominaRegistros.Add(registro);
        }

        await _db.SaveChangesAsync();
    }

    public async Task GenerateBatchAsync(string period)
    {
        var parts = period.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out int year) && int.TryParse(parts[1], out int month))
        {
            await GenerateBatchAsync(month, year);
        }
        else
        {
            throw new ArgumentException("Formato de período inválido. Use YYYY-MM.");
        }
    }

    public async Task RecalculateAsync(Guid id)
    {
        var registro = await _db.NominaRegistros
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == _tenantService.TenantId);
        if (registro == null || registro.Estado != NominaStatus.Draft) return;

        var parts = registro.Periodo.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out int year) && int.TryParse(parts[1], out int month))
        {
            var emp = await _db.EmpleadoContratos.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == registro.UserId);
            if (emp == null) return;

            var (baseSalary, commissions, deductions, ingresos) = await CalculateTotalsAsync(emp, month, year);
            registro.SalarioBase = baseSalary;
            registro.Comisiones = commissions;
            registro.Deducciones = deductions;
            registro.IngresosGenerados = ingresos;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<NominaKpiSummary> GetKpiSummaryAsync(string period, NominaStatus? status, string mechanicId)
    {
        var tenantId = _tenantService.TenantId;
        var query = _db.NominaRegistros.AsNoTracking().Where(n => n.TenantId == tenantId);

        if (string.IsNullOrEmpty(period))
        {
            period = DateTime.Now.ToString("yyyy-MM");
        }

        query = query.Where(n => n.Periodo == period);

        if (status.HasValue)
        {
            query = query.Where(n => n.Estado == status.Value);
        }

        if (!string.IsNullOrEmpty(mechanicId))
        {
            query = query.Where(n => n.UserId == mechanicId);
        }

        var data = await query.ToListAsync();

        return new NominaKpiSummary
        {
            TotalNomina = data.Sum(n => n.TotalNeto),
            TotalComisiones = data.Sum(n => n.Comisiones),
            PendientesDIAN = data.Count(n => n.Estado == NominaStatus.Draft),
            RentabilidadPromedio = data.Any() ? data.Average(n => n.IngresosGenerados - n.TotalCosto) : 0,
            MecanicosNoRentables = data.Count(n => !n.EsRentable)
        };
    }

    public async Task<bool> EnviarNominaDIANAsync(Guid id)
    {
        var registro = await GetByIdAsync(id);
        if (registro == null || registro.Estado == NominaStatus.Reported) return false;

        // Security: Lock check is handled by the Estado check above and UI logic.
        // Integration placeholder
        await Task.Delay(300); 

        registro.Estado = NominaStatus.Reported;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<(decimal baseSalary, decimal commissions, decimal deductions, decimal ingresos)> CalculateTotalsAsync(EmpleadoContrato emp, int month, int year)
    {
        decimal baseSalary = emp.SalarioBase;
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get matching orders assigned to the employee
        var orders = await _db.Ordenes
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.TenantId == _tenantService.TenantId 
                     && (o.Estado == EstadoOrden.Facturada || o.Estado == EstadoOrden.EntregadoYFacturado)
                     && o.FechaSalida >= startDate && o.FechaSalida <= endDate
                     && o.Appointment != null && o.Appointment.MechanicId == emp.UserId)
            .ToListAsync();

        // Revenue from labor (Mano de Obra)
        decimal laborRevenue = orders
            .SelectMany(o => o.Items)
            .Where(i => i.Tipo == "Servicio" || i.Tipo == "Mano de Obra") // Support both labels
            .Sum(i => i.Cantidad * i.PrecioUnitario);

        decimal commissions = laborRevenue * (emp.PorcentajeComision / 100m);
        decimal deductions = 0;

        return (baseSalary, commissions, deductions, laborRevenue);
    }

}
