using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TallerSaaS.Application.DTOs;
using TallerSaaS.Application.Interfaces;
using TallerSaaS.Domain.Entities;

namespace TallerSaaS.Application.Services;

public class ReporteService
{
    private readonly IApplicationDbContext _db;

    public ReporteService(IApplicationDbContext db) => _db = db;

    // ─── PDF: Factura / Presupuesto ───────────────────────────────────────────
    public async Task<byte[]> GenerarFacturaPdfAsync(Guid ordenId, string tenantNombre, string? tenantLogo = null)
    {
        var orden = await _db.Ordenes
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == ordenId)
            ?? throw new Exception("Orden no encontrada");

        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(tenantNombre).Bold().FontSize(18).FontColor("#0066CC");
                            c.Item().Text("Taller Automotriz").FontSize(11).FontColor("#666");
                        });
                        row.ConstantItem(120).Column(c =>
                        {
                            c.Item().Text("FACTURA").Bold().FontSize(16).AlignRight();
                            c.Item().Text($"#{orden.NumeroOrden}").FontSize(11).AlignRight().FontColor("#666");
                            c.Item().Text($"Fecha: {orden.FechaEntrada:dd/MM/yyyy}").FontSize(9).AlignRight();
                        });
                    });
                    col.Item().PaddingTop(5).BorderBottom(1).BorderColor("#e0e0e0");
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    // Client info
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("CLIENTE").Bold().FontSize(9).FontColor("#888");
                            c.Item().Text(orden.Vehiculo?.Cliente?.NombreCompleto ?? "").Bold().FontSize(12);
                            c.Item().Text(orden.Vehiculo?.Cliente?.Telefono ?? "").FontSize(9);
                            c.Item().Text(orden.Vehiculo?.Cliente?.Email ?? "").FontSize(9);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("VEHÍCULO").Bold().FontSize(9).FontColor("#888");
                            c.Item().Text(orden.Vehiculo?.Descripcion ?? "").Bold();
                            c.Item().Text($"Placa: {orden.Vehiculo?.Placa ?? "N/A"}").FontSize(9);
                            c.Item().Text($"VIN: {orden.Vehiculo?.VIN ?? "N/A"}").FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(15).Text("DIAGNÓSTICO").Bold().FontSize(9).FontColor("#888");
                    col.Item().Text(orden.DiagnosticoInicial ?? "Sin diagnóstico").FontSize(10);

                    // Items table
                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background("#0066CC").Padding(6).Text("Descripción").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background("#0066CC").Padding(6).Text("Cant.").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background("#0066CC").Padding(6).Text("Precio Unit.").Bold().FontColor(Colors.White).FontSize(9);
                            header.Cell().Background("#0066CC").Padding(6).Text("Subtotal").Bold().FontColor(Colors.White).FontSize(9);
                        });

                        bool alternado = false;
                        foreach (var item in orden.Items)
                        {
                            var bg = alternado ? "#f5f5f5" : Colors.White.ToString();
                            table.Cell().Background(bg).Padding(5).Text(item.Descripcion).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(item.Cantidad.ToString("N0")).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text($"${item.PrecioUnitario:N2}").FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text($"${item.Cantidad * item.PrecioUnitario:N2}").FontSize(9);
                            alternado = !alternado;
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(10).AlignRight().Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Subtotal:").AlignRight().FontSize(10);
                            r.ConstantItem(100).Text($"${orden.Subtotal:N2}").AlignRight().FontSize(10);
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Descuento:").AlignRight().FontSize(10);
                            r.ConstantItem(100).Text($"-${orden.Descuento:N2}").AlignRight().FontSize(10);
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("IVA (16%):").AlignRight().FontSize(10);
                            r.ConstantItem(100).Text($"${orden.IVA:N2}").AlignRight().FontSize(10);
                        });
                        c.Item().BorderTop(1).BorderColor("#1a73e8").PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL:").AlignRight().Bold().FontSize(13);
                            r.ConstantItem(100).Text($"${orden.Total:N2}").AlignRight().Bold().FontSize(13).FontColor("#1a73e8");
                        });
                    });

                    if (!string.IsNullOrEmpty(orden.Observaciones))
                    {
                        col.Item().PaddingTop(20).Text("OBSERVACIONES").Bold().FontSize(9).FontColor("#888");
                        col.Item().Text(orden.Observaciones).FontSize(10);
                    }
                });

                page.Footer().BorderTop(1).BorderColor("#e0e0e0").PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("Gracias por su preferencia").FontSize(9).FontColor("#888");
                    row.ConstantItem(120).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).AlignRight().FontColor("#aaa");
                });
            });
        });

        return pdf.GeneratePdf();
    }

    // ─── Excel: Clientes ──────────────────────────────────────────────────────
    public async Task<byte[]> ExportarClientesExcelAsync()
    {
        var clientes = await _db.Clientes
            .Include(c => c.Vehiculos)
            .Where(c => c.Activo)
            .OrderBy(c => c.NombreCompleto)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Clientes");

        // Header styling
        var headers = new[] { "Nombre Completo", "Cédula/NIT", "Email", "Teléfono", "Dirección", "Vehículos", "Fecha Registro" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0066CC");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var c in clientes)
        {
            ws.Cell(row, 1).Value = c.NombreCompleto;
            ws.Cell(row, 2).Value = c.Cedula ?? "";
            ws.Cell(row, 3).Value = c.Email ?? "";
            ws.Cell(row, 4).Value = c.Telefono ?? "";
            ws.Cell(row, 5).Value = c.Direccion ?? "";
            ws.Cell(row, 6).Value = c.Vehiculos.Count;
            ws.Cell(row, 7).Value = c.FechaRegistro.ToString("dd/MM/yyyy");
            if (row % 2 == 0)
                ws.Row(row).Cells().Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ─── Excel: Ventas ────────────────────────────────────────────────────────
    public async Task<byte[]> ExportarVentasExcelAsync(DateTime? desde = null, DateTime? hasta = null)
    {
        desde ??= DateTime.UtcNow.AddMonths(-1);
        hasta ??= DateTime.UtcNow;

        var ordenes = await _db.Ordenes
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Where(o => o.FechaEntrada >= desde && o.FechaEntrada <= hasta)
            .OrderByDescending(o => o.FechaEntrada)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Ventas");

        var headers = new[] { "No. Orden", "Cliente", "Vehículo", "Placa", "Estado", "Fecha Entrada", "Fecha Salida", "Subtotal", "Descuento", "IVA", "Total", "Pagada" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0066CC");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var o in ordenes)
        {
            ws.Cell(row, 1).Value = o.NumeroOrden;
            ws.Cell(row, 2).Value = o.Vehiculo?.Cliente?.NombreCompleto ?? "";
            ws.Cell(row, 3).Value = o.Vehiculo?.Descripcion ?? "";
            ws.Cell(row, 4).Value = o.Vehiculo?.Placa ?? "";
            ws.Cell(row, 5).Value = o.EstadoTexto;
            ws.Cell(row, 6).Value = o.FechaEntrada.ToString("dd/MM/yyyy");
            ws.Cell(row, 7).Value = o.FechaSalida?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(row, 8).Value = o.Subtotal;    ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 9).Value = o.Descuento;   ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 10).Value = o.IVA;        ws.Cell(row, 10).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 11).Value = o.Total;      ws.Cell(row, 11).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 12).Value = o.Pagada ? "Sí" : "No";
            if (row % 2 == 0)
                ws.Row(row).Cells().Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
            row++;
        }

        // Totals row
        ws.Cell(row, 10).Value = "TOTAL:"; ws.Cell(row, 10).Style.Font.Bold = true;
        ws.Cell(row, 11).FormulaA1 = $"=SUM(K2:K{row-1})";
        ws.Cell(row, 11).Style.Font.Bold = true;
        ws.Cell(row, 11).Style.NumberFormat.Format = "$#,##0.00";

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
