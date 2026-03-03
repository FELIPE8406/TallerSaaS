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

    // ─── PDF Apple-Style: Factura por OrdenId (legado compatible) ────────────
    public async Task<byte[]> GenerarFacturaPdfAsync(Guid ordenId, string tenantNombre, string? tenantLogo = null)
    {
        var orden = await _db.Ordenes
            .Include(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == ordenId)
            ?? throw new Exception("Orden no encontrada");

        return GenerarPdfAppleStyle(
            tenantNombre, tenantNombre,
            orden.NumeroOrden,
            orden.FechaEntrada,
            orden.Vehiculo?.Cliente?.NombreCompleto ?? "",
            orden.Vehiculo?.Cliente?.Telefono ?? "",
            orden.Vehiculo?.Cliente?.Email ?? "",
            orden.Vehiculo?.Descripcion ?? "",
            orden.Vehiculo?.Placa ?? "N/A",
            orden.Vehiculo?.VIN ?? "N/A",
            orden.DiagnosticoInicial,
            orden.Items.Select(i => (i.Descripcion, i.Cantidad, i.PrecioUnitario)).ToList(),
            orden.Subtotal, orden.Descuento, orden.IVA, orden.Total,
            orden.Observaciones,
            qrCode: $"TALLERSAAS|{orden.NumeroOrden}|{DateTime.Now:yyyyMMddHHmm}");
    }

    // ─── PDF Apple-Style: Factura por FacturaId (nueva lógica multi-orden) ────
    public async Task<byte[]> GenerarFacturaPdfPorFacturaAsync(Guid facturaId, string tenantNombre)
    {
        var factura = await _db.Facturas
            .Include(f => f.Ordenes).ThenInclude(o => o.Vehiculo).ThenInclude(v => v!.Cliente)
            .Include(f => f.Ordenes).ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(f => f.Id == facturaId)
            ?? throw new Exception("Factura no encontrada");

        // Aggregate all items from all orders
        var allItems = factura.Ordenes
            .SelectMany(o => o.Items)
            .Select(i => (i.Descripcion, i.Cantidad, i.PrecioUnitario))
            .ToList();

        var primerOrden = factura.Ordenes.FirstOrDefault();
        var clienteNombre  = primerOrden?.Vehiculo?.Cliente?.NombreCompleto ?? "";
        var clienteTel     = primerOrden?.Vehiculo?.Cliente?.Telefono ?? "";
        var clienteEmail   = primerOrden?.Vehiculo?.Cliente?.Email ?? "";
        var ordenes        = string.Join(", ", factura.Ordenes.Select(o => o.NumeroOrden));

        return GenerarPdfAppleStyle(
            tenantNombre, tenantNombre,
            factura.NumeroFactura,
            factura.FechaEmision,
            clienteNombre, clienteTel, clienteEmail,
            $"Órdenes: {ordenes}", "", "",
            null,
            allItems,
            factura.Subtotal, factura.Descuento, factura.IVA, factura.Total,
            factura.Observaciones,
            qrCode: factura.CodigoQR ?? $"TALLERSAAS|{factura.NumeroFactura}");
    }

    // ─── Core PDF Generation (Apple-Style) ───────────────────────────────────
    private static byte[] GenerarPdfAppleStyle(
        string tenantNombre, string tenantSubtitulo,
        string numero, DateTime fecha,
        string clienteNombre, string clienteTel, string clienteEmail,
        string vehiculoDesc, string placa, string vin,
        string? diagnostico,
        List<(string Descripcion, decimal Cantidad, decimal PrecioUnitario)> items,
        decimal subtotal, decimal descuento, decimal iva, decimal total,
        string? observaciones,
        string qrCode = "")
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(3, Unit.Centimetre);
                page.MarginVertical(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(10).FontColor("#1a1a1a"));

                // ── Header ─────────────────────────────────────────────────
                page.Header().PaddingBottom(20).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // Taller info
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(tenantNombre)
                                .Bold().FontSize(22).FontColor("#0A0A0A");
                            c.Item().Text("Servicio Automotriz Profesional")
                                .FontSize(10).FontColor("#888888").Italic();
                        });
                        // Invoice badge
                        row.ConstantItem(140).AlignRight().Column(c =>
                        {
                            c.Item().Background("#0A84FF").Padding(8).AlignRight().Column(inner =>
                            {
                                inner.Item().Text("FACTURA").Bold().FontSize(11)
                                    .FontColor(Colors.White).AlignRight();
                                inner.Item().Text($"#{numero}").FontSize(10)
                                    .FontColor("#CCE5FF").AlignRight();
                            });
                            c.Item().PaddingTop(4).Text($"Fecha: {fecha:dd MMM yyyy}")
                                .FontSize(9).FontColor("#888").AlignRight();
                        });
                    });
                    col.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor("#E5E5EA");
                });

                // ── Content ────────────────────────────────────────────────
                page.Content().Column(col =>
                {
                    // Client & Vehicle info - two columns with breathing room
                    col.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("CLIENTE").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            c.Item().PaddingTop(4).Text(clienteNombre).Bold().FontSize(13);
                            if (!string.IsNullOrWhiteSpace(clienteTel))
                                c.Item().PaddingTop(2).Text(clienteTel).FontSize(10).FontColor("#555");
                            if (!string.IsNullOrWhiteSpace(clienteEmail))
                                c.Item().Text(clienteEmail).FontSize(10).FontColor("#555");
                        });

                        row.ConstantItem(20);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("VEHÍCULO").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            c.Item().PaddingTop(4).Text(vehiculoDesc).Bold().FontSize(13);
                            if (!string.IsNullOrWhiteSpace(placa) && placa != "N/A")
                                c.Item().PaddingTop(2).Text($"Placa: {placa}").FontSize(10).FontColor("#555");
                            if (!string.IsNullOrWhiteSpace(vin) && vin != "N/A")
                                c.Item().Text($"VIN: {vin}").FontSize(10).FontColor("#555");
                        });
                    });

                    // Diagnostic
                    if (!string.IsNullOrWhiteSpace(diagnostico))
                    {
                        col.Item().PaddingTop(20).Column(c =>
                        {
                            c.Item().Text("DIAGNÓSTICO").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            c.Item().PaddingTop(4).Background("#F9F9FB").Padding(10)
                                .Text(diagnostico).FontSize(10).FontColor("#3a3a3c");
                        });
                    }

                    // Items table
                    col.Item().PaddingTop(24).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(5);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(1.8f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor("#E5E5EA")
                                .PaddingBottom(8).Text("DESCRIPCIÓN").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            header.Cell().BorderBottom(1).BorderColor("#E5E5EA")
                                .PaddingBottom(8).AlignCenter().Text("CANT.").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            header.Cell().BorderBottom(1).BorderColor("#E5E5EA")
                                .PaddingBottom(8).AlignRight().Text("PRECIO").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            header.Cell().BorderBottom(1).BorderColor("#E5E5EA")
                                .PaddingBottom(8).AlignRight().Text("SUBTOTAL").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                        });

                        foreach (var (descripcion, cantidad, precioUnitario) in items)
                        {
                            table.Cell().PaddingVertical(8).Text(descripcion).FontSize(10);
                            table.Cell().PaddingVertical(8).AlignCenter()
                                .Text(cantidad.ToString("N0")).FontSize(10);
                            table.Cell().PaddingVertical(8).AlignRight()
                                .Text($"${precioUnitario:N0}").FontSize(10);
                            table.Cell().PaddingVertical(8).AlignRight()
                                .Text($"${cantidad * precioUnitario:N0}").FontSize(10);

                            table.Cell().ColumnSpan(4).BorderBottom(0.3f).BorderColor("#F2F2F7");
                        }
                    });

                    // Totals — right-aligned, clean
                    col.Item().PaddingTop(16).AlignRight().Column(c =>
                    {
                        void TotalRow(string label, string value, bool isBold = false, string color = "#1a1a1a") =>
                            c.Item().PaddingBottom(4).Row(r =>
                            {
                                 r.ConstantItem(120).AlignRight()
                                    .Text(label).FontSize(10).FontColor("#8E8E93");
                                 var valText = r.ConstantItem(110).AlignRight()
                                    .Text(value).FontSize(10).FontColor(color);
                                 if (isBold) valText.Bold();
                             });

                        TotalRow("Subtotal", $"${subtotal:N0}");
                        if (descuento > 0) TotalRow("Descuento", $"-${descuento:N0}", color: "#34C759");
                        TotalRow("IVA (19%)", $"${iva:N0}");

                        c.Item().PaddingTop(8).PaddingBottom(8)
                            .LineHorizontal(0.5f).LineColor("#E5E5EA");
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(120).AlignRight()
                                .Text("TOTAL").Bold().FontSize(14).FontColor("#0a0a0a");
                            r.ConstantItem(110).AlignRight()
                                .Text($"${total:N0}").Bold().FontSize(14).FontColor("#0A84FF");
                        });
                    });

                    // Observaciones
                    if (!string.IsNullOrWhiteSpace(observaciones))
                    {
                        col.Item().PaddingTop(24).Column(c =>
                        {
                            c.Item().Text("OBSERVACIONES").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            c.Item().PaddingTop(4).Background("#F9F9FB").Padding(10)
                                .Text(observaciones).FontSize(10).FontColor("#3a3a3c");
                        });
                    }

                    // ── Firma Digital + QR ────────────────────────────────────
                    col.Item().PaddingTop(36).Row(row =>
                    {
                        // Firma digital
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FIRMA DEL CLIENTE").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            c.Item().PaddingTop(8).Height(60).Border(0.5f).BorderColor("#E5E5EA")
                                .Background("#FAFAFA");
                            c.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor("#8E8E93");
                            c.Item().PaddingTop(4).Text("Nombre y firma del cliente")
                                .FontSize(8).FontColor("#AEAEB2");
                        });

                        row.ConstantItem(30);

                        // QR validación
                        row.ConstantItem(100).Column(c =>
                        {
                            c.Item().Text("VALIDACIÓN").Bold().FontSize(8)
                                .FontColor("#8E8E93").LetterSpacing(0.1f);
                            c.Item().PaddingTop(4).Height(80).Width(80)
                                .Background("#F2F2F7").AlignCenter().AlignMiddle()
                                .Text("QR").FontSize(9).FontColor("#8E8E93");
                            c.Item().PaddingTop(4).Text(qrCode.Length > 30
                                ? qrCode[..30] + "…"
                                : qrCode).FontSize(6).FontColor("#AEAEB2");
                        });
                    });
                });

                // ── Footer ──────────────────────────────────────────────────
                page.Footer().PaddingTop(12).Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor("#E5E5EA");
                    footer.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("Gracias por su confianza")
                            .FontSize(8).FontColor("#AEAEB2").Italic();
                        row.ConstantItem(160).AlignRight()
                            .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor("#AEAEB2");
                    });
                });
            });
        });

        return pdf.GeneratePdf();
    }

    // ─── Excel Pro: Clientes ──────────────────────────────────────────────────
    public async Task<byte[]> ExportarClientesExcelAsync()
    {
        var clientes = await _db.Clientes
            .Include(c => c.Vehiculos)
            .Where(c => c.Activo)
            .OrderBy(c => c.NombreCompleto)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Clientes");

        AplicarEstiloTituloSheet(ws, "REPORTE DE CLIENTES", 7);

        var headers = new[] { "Nombre Completo", "Cédula/NIT", "Email", "Teléfono", "Dirección", "Vehículos", "Fecha Registro" };
        AplicarHeaders(ws, headers, 2);

        int row = 3;
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
                ws.Row(row).Cells(1, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");
            row++;
        }

        AplicarFilaTotales(ws, row, new[] { 6 }, headers.Length);
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ─── Excel Pro: Ventas ────────────────────────────────────────────────────
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
        int totalCols = 12;

        AplicarEstiloTituloSheet(ws, $"REPORTE DE VENTAS — {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}", totalCols);

        var headers = new[] { "No. Orden", "Cliente", "Vehículo", "Placa", "Estado", "Fecha Entrada", "Fecha Salida", "Subtotal", "Descuento", "IVA", "Total", "Pagada" };
        AplicarHeaders(ws, headers, 2);

        int row = 3;
        foreach (var o in ordenes)
        {
            ws.Cell(row, 1).Value = o.NumeroOrden;
            ws.Cell(row, 2).Value = o.Vehiculo?.Cliente?.NombreCompleto ?? "";
            ws.Cell(row, 3).Value = o.Vehiculo?.Descripcion ?? "";
            ws.Cell(row, 4).Value = o.Vehiculo?.Placa ?? "";
            ws.Cell(row, 5).Value = o.EstadoTexto;
            ws.Cell(row, 6).Value = o.FechaEntrada.ToString("dd/MM/yyyy");
            ws.Cell(row, 7).Value = o.FechaSalida?.ToString("dd/MM/yyyy") ?? "";

            void ColMoneda(int col, decimal val)
            {
                ws.Cell(row, col).Value = val;
                ws.Cell(row, col).Style.NumberFormat.Format = "$#,##0";
            }
            ColMoneda(8, o.Subtotal);
            ColMoneda(9, o.Descuento);
            ColMoneda(10, o.IVA);
            ColMoneda(11, o.Total);
            ws.Cell(row, 12).Value = o.Pagada ? "Sí" : "No";

            if (row % 2 == 0)
                ws.Row(row).Cells(1, totalCols).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");
            row++;
        }

        // Totals row with SUM formulas — columns Subtotal(8), Descuento(9), IVA(10), Total(11)
        AplicarFilaTotales(ws, row, new[] { 8, 9, 10, 11 }, totalCols,
            startDataRow: 3, useFormulas: true);

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ─── Excel Pro: Facturas ──────────────────────────────────────────────────
    public async Task<byte[]> ExportarFacturasExcelAsync()
    {
        var facturas = await _db.Facturas
            .Include(f => f.Ordenes)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Facturas");
        int totalCols = 8;

        AplicarEstiloTituloSheet(ws, "REPORTE DE FACTURAS", totalCols);

        var headers = new[] { "No. Factura", "Fecha Emisión", "Órdenes Incluidas", "Subtotal", "Descuento", "IVA", "Total", "Firmada" };
        AplicarHeaders(ws, headers, 2);

        int row = 3;
        foreach (var f in facturas)
        {
            ws.Cell(row, 1).Value = f.NumeroFactura;
            ws.Cell(row, 2).Value = f.FechaEmision.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = f.Ordenes.Count;

            void ColMoneda(int col, decimal val)
            {
                ws.Cell(row, col).Value = val;
                ws.Cell(row, col).Style.NumberFormat.Format = "$#,##0";
            }
            ColMoneda(4, f.Subtotal);
            ColMoneda(5, f.Descuento);
            ColMoneda(6, f.IVA);
            ColMoneda(7, f.Total);
            ws.Cell(row, 8).Value = f.FirmadaDigitalmente ? "Sí" : "No";

            if (row % 2 == 0)
                ws.Row(row).Cells(1, totalCols).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");
            row++;
        }

        AplicarFilaTotales(ws, row, new[] { 4, 5, 6, 7 }, totalCols,
            startDataRow: 3, useFormulas: true);

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ─── Helpers Excel ────────────────────────────────────────────────────────
    private static void AplicarEstiloTituloSheet(IXLWorksheet ws, string titulo, int totalCols)
    {
        ws.Cell(1, 1).Value = titulo;
        var titleRange = ws.Range(1, 1, 1, totalCols);
        titleRange.Merge();
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 14;
        titleRange.Style.Font.FontColor = XLColor.FromHtml("#0A84FF");
        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F7FF");
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 28;
    }

    private static void AplicarHeaders(IXLWorksheet ws, string[] headers, int fila)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(fila, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1C1C1E");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(fila).Height = 20;
    }

    private static void AplicarFilaTotales(IXLWorksheet ws, int row, int[] columnasSuma,
        int totalCols, int startDataRow = 2, bool useFormulas = false)
    {
        ws.Cell(row, columnasSuma[0] - 1).Value = "TOTAL";
        ws.Cell(row, columnasSuma[0] - 1).Style.Font.Bold = true;
        ws.Cell(row, columnasSuma[0] - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        foreach (var col in columnasSuma)
        {
            var colLetter = ((char)('A' + col - 1)).ToString();
            var cell = ws.Cell(row, col);
            if (useFormulas)
                cell.FormulaA1 = $"=SUM({colLetter}{startDataRow}:{colLetter}{row - 1})";
            else
                cell.Value = 0; // placeholder
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.FromHtml("#0A84FF");
            cell.Style.NumberFormat.Format = "$#,##0";
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F7FF");
        }

        ws.Range(row, 1, row, totalCols).Style.Border.TopBorder = XLBorderStyleValues.Medium;
        ws.Range(row, 1, row, totalCols).Style.Border.TopBorderColor = XLColor.FromHtml("#0A84FF");
        ws.Row(row).Height = 22;
    }
}
