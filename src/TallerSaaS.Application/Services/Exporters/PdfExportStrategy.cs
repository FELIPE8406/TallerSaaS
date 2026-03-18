using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;

namespace TallerSaaS.Application.Services.Exporters;

/// <summary>
/// Exporta datos en PDF usando QuestPDF.
/// Implementa IExportStrategy del Patrón Estrategia de reportes.
/// </summary>
public class PdfExportStrategy : IExportStrategy
{
    private readonly IApplicationDbContext _db;
    public string ContentType => "application/pdf";
    public string FileExtension => "pdf";

    public PdfExportStrategy(IApplicationDbContext db) => _db = db;

    // ── ÓRDENES ───────────────────────────────────────────────────────────────
    public async Task<byte[]> ExportarOrdenesAsync(ReporteFilter filtro)
    {
        var ordenes = await _db.Ordenes
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Where(o => o.FechaEntrada >= filtro.Desde && o.FechaEntrada <= filtro.Hasta)
            .OrderByDescending(o => o.FechaEntrada)
            .ToListAsync();

        var totales = ordenes.Sum(o => o.Total);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Element(HeaderCell("REPORTE DE ÓRDENES DE TRABAJO", filtro));
                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(2);
                        c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                    });
                    string[] headers = ["No. Orden", "Cliente", "Estado", "Fecha Entrada", "Descuento", "Total COP"];
                    t.Header(h =>
                    {
                        foreach (var header in headers)
                            h.Cell().Background("#1C1C1E").Padding(5)
                             .Text(header).FontColor("#FFFFFF").Bold().FontSize(8);
                    });

                    bool alt = false;
                    foreach (var o in ordenes)
                    {
                        var bg = alt ? "#F2F2F7" : "#FFFFFF"; alt = !alt;
                        t.Cell().Background(bg).Padding(4).Text(o.NumeroOrden).FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text(o.Vehiculo?.Cliente?.NombreCompleto ?? "—").FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text(o.EstadoTexto).FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text(o.FechaEntrada.ToString("dd/MM/yyyy")).FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text($"${o.Descuento:N0}").FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text($"${o.Total:N0}").Bold().FontSize(8);
                    }
                });
                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text($"Total registros: {ordenes.Count}   |   Total: ${totales:N0} COP")
                        .FontSize(9).Bold();
                    r.ConstantItem(80).AlignRight().Text(x =>
                    {
                        x.CurrentPageNumber().FontSize(9);
                        x.Span(" / ").FontSize(9);
                        x.TotalPages().FontSize(9);
                    });
                });
            });
        }).GeneratePdf();
    }

    // ── FACTURAS ──────────────────────────────────────────────────────────────
    public async Task<byte[]> ExportarFacturasAsync(ReporteFilter filtro)
    {
        var facturas = await _db.Facturas
            .Include(f => f.Ordenes)
            .Where(f => f.FechaEmision >= filtro.Desde && f.FechaEmision <= filtro.Hasta)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();

        var totalFacturado = facturas.Sum(f => f.Total);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Element(HeaderCell("REPORTE DE FACTURAS", filtro));
                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(1);
                        c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                    });
                    string[] headers = ["No. Factura", "Fecha Emisión", "Órdenes", "Subtotal", "IVA", "Total COP"];
                    t.Header(h =>
                    {
                        foreach (var header in headers)
                            h.Cell().Background("#1C1C1E").Padding(5)
                             .Text(header).FontColor("#FFFFFF").Bold().FontSize(8);
                    });

                    bool alt = false;
                    foreach (var f in facturas)
                    {
                        var bg = alt ? "#F2F2F7" : "#FFFFFF"; alt = !alt;
                        t.Cell().Background(bg).Padding(4).Text(f.NumeroFactura).FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text(f.FechaEmision.ToString("dd/MM/yyyy")).FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text(f.Ordenes.Count.ToString()).FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text($"${f.Subtotal:N0}").FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text($"${f.IVA:N0}").FontSize(8);
                        t.Cell().Background(bg).Padding(4).Text($"${f.Total:N0}").Bold().FontSize(8);
                    }
                });
                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text($"Total facturas: {facturas.Count}   |   Total facturado: ${totalFacturado:N0} COP")
                        .FontSize(9).Bold();
                    r.ConstantItem(80).AlignRight().Text(x =>
                    {
                        x.CurrentPageNumber().FontSize(9);
                        x.Span(" / ").FontSize(9);
                        x.TotalPages().FontSize(9);
                    });
                });
            });
        }).GeneratePdf();
    }

    // ── CLIENTES Y VEHÍCULOS ──────────────────────────────────────────────────
    public async Task<byte[]> ExportarClientesVehiculosAsync(ReporteFilter filtro)
    {
        var clientes = await _db.Clientes
            .Include(c => c.Vehiculos)
            .Where(c => c.Activo && c.FechaRegistro >= filtro.Desde && c.FechaRegistro <= filtro.Hasta)
            .OrderBy(c => c.NombreCompleto)
            .ToListAsync();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Element(HeaderCell("RELACIÓN CLIENTES Y VEHÍCULOS", filtro));
                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); c.RelativeColumn(2);
                        c.RelativeColumn(3); c.RelativeColumn(2);
                    });
                    string[] headers = ["Cliente", "Teléfono", "Vehículo", "Placa"];
                    t.Header(h =>
                    {
                        foreach (var header in headers)
                            h.Cell().Background("#1C1C1E").Padding(5)
                             .Text(header).FontColor("#FFFFFF").Bold().FontSize(8);
                    });

                    bool alt = false;
                    foreach (var c in clientes)
                    {
                        if (!c.Vehiculos.Any())
                        {
                            var bg = alt ? "#F2F2F7" : "#FFFFFF"; alt = !alt;
                            t.Cell().Background(bg).Padding(4).Text(c.NombreCompleto).FontSize(8);
                            t.Cell().Background(bg).Padding(4).Text(c.Telefono ?? "—").FontSize(8);
                            t.Cell().Background(bg).Padding(4).Text("Sin vehículo").FontSize(8);
                            t.Cell().Background(bg).Padding(4).Text("—").FontSize(8);
                        }
                        else
                        {
                            foreach (var v in c.Vehiculos)
                            {
                                var bg = alt ? "#F2F2F7" : "#FFFFFF"; alt = !alt;
                                t.Cell().Background(bg).Padding(4).Text(c.NombreCompleto).FontSize(8);
                                t.Cell().Background(bg).Padding(4).Text(c.Telefono ?? "—").FontSize(8);
                                t.Cell().Background(bg).Padding(4).Text($"{v.Anio} {v.Marca} {v.Modelo}").FontSize(8);
                                t.Cell().Background(bg).Padding(4).Text(v.Placa ?? "—").FontSize(8);
                            }
                        }
                    }
                });
                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text($"Total clientes: {clientes.Count}").FontSize(9).Bold();
                    r.ConstantItem(80).AlignRight().Text(x =>
                    {
                        x.CurrentPageNumber().FontSize(9);
                        x.Span(" / ").FontSize(9);
                        x.TotalPages().FontSize(9);
                    });
                });
            });
        }).GeneratePdf();
    }

    // ── Helper: bloque de encabezado reutilizable ─────────────────────────────
    private static Action<IContainer> HeaderCell(string titulo, ReporteFilter filtro) =>
        c => c.Column(col =>
        {
            col.Item().Text(titulo).FontSize(16).Bold().FontColor("#1C1C1E");
            col.Item().Text($"Período: {filtro.Desde:dd/MM/yyyy} — {filtro.Hasta:dd/MM/yyyy}")
                .FontSize(10).FontColor("#636366");
            col.Item().PaddingBottom(12).BorderBottom(1).BorderColor("#E5E5EA");
        });
}
