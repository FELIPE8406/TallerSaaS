namespace TallerSaaS.Application.Models;

public class NominaKpiSummary
{
    public decimal TotalNomina { get; set; }
    public decimal TotalComisiones { get; set; }
    public int PendientesDIAN { get; set; }
    public decimal RentabilidadPromedio { get; set; }
    public int MecanicosNoRentables { get; set; }
}
