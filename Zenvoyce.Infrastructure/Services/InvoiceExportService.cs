using ClosedXML.Excel;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Invoices.DTOs;

namespace Zenvoyce.Infrastructure.Services;

public class InvoiceExportService : IInvoiceExportService
{
    private const string InvoiceListSheetName = "Danh sách hóa đơn";
    private const string LineItemsSheetName = "Chi tiết hàng hóa";
    private const string SalesReportSheetName = "Báo cáo doanh số";

    public async Task<ExportResultDto> GenerateInvoiceListExcelAsync(
        IReadOnlyCollection<InvoiceForExportDto> invoices,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();

        // Create invoice list sheet
        var invoiceSheet = workbook.Worksheets.Add(InvoiceListSheetName);
        CreateInvoiceListSheet(invoiceSheet, invoices);

        // Create line items sheet
        var lineItemsSheet = workbook.Worksheets.Add(LineItemsSheetName);
        CreateLineItemsSheet(lineItemsSheet, invoices);

        // Generate file
        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var excelBytes = stream.ToArray();

        var filename = $"Invoices_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return new ExportResultDto
        {
            ExcelBytes = excelBytes,
            Filename = filename
        };
    }

    public async Task<ExportResultDto> GenerateSalesReportExcelAsync(
        IReadOnlyCollection<SalesReportRow> salesData,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add(SalesReportSheetName);
        CreateSalesReportSheet(sheet, salesData);

        // Generate file
        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var excelBytes = stream.ToArray();

        var filename = $"SalesReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return new ExportResultDto
        {
            ExcelBytes = excelBytes,
            Filename = filename
        };
    }

    private void CreateInvoiceListSheet(IXLWorksheet sheet, IReadOnlyCollection<InvoiceForExportDto> invoices)
    {
        // Set column widths
        sheet.Column(1).Width = 12;  // Số hóa đơn
        sheet.Column(2).Width = 12;  // Ký hiệu
        sheet.Column(3).Width = 15;  // Ngày lập
        sheet.Column(4).Width = 25;  // Khách hàng
        sheet.Column(5).Width = 15;  // MST Khách
        sheet.Column(6).Width = 12;  // Tổng tiền
        sheet.Column(7).Width = 12;  // Tiền thuế
        sheet.Column(8).Width = 12;  // Tổng thanh toán
        sheet.Column(9).Width = 15;  // Trạng thái
        sheet.Column(10).Width = 20; // Đơn vị

        // Create header row
        var headerRow = sheet.Row(1);
        headerRow.Cell(1).Value = "Số hóa đơn";
        headerRow.Cell(2).Value = "Ký hiệu";
        headerRow.Cell(3).Value = "Ngày lập";
        headerRow.Cell(4).Value = "Khách hàng";
        headerRow.Cell(5).Value = "MST Khách";
        headerRow.Cell(6).Value = "Tổng tiền";
        headerRow.Cell(7).Value = "Tiền thuế";
        headerRow.Cell(8).Value = "Tổng thanh toán";
        headerRow.Cell(9).Value = "Trạng thái";
        headerRow.Cell(10).Value = "Đơn vị";

        // Format header row
        var headerStyle = headerRow.Style;
        headerStyle.Font.Bold = true;
        headerStyle.Fill.BackgroundColor = XLColor.LightGray;
        headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Add data rows
        var rowNumber = 2;
        foreach (var invoice in invoices)
        {
            sheet.Cell(rowNumber, 1).Value = invoice.Sohoadon ?? string.Empty;
            sheet.Cell(rowNumber, 2).Value = invoice.Kyhieu ?? string.Empty;
            sheet.Cell(rowNumber, 3).Value = invoice.Ngaylap;
            sheet.Cell(rowNumber, 4).Value = invoice.TenKhachhang;
            sheet.Cell(rowNumber, 5).Value = invoice.MaSoThueKhachhang ?? string.Empty;
            sheet.Cell(rowNumber, 6).Value = invoice.Tongtien;
            sheet.Cell(rowNumber, 7).Value = invoice.Tienthue;
            sheet.Cell(rowNumber, 8).Value = invoice.Tongthanhtoan;
            sheet.Cell(rowNumber, 9).Value = invoice.Trangthai;
            sheet.Cell(rowNumber, 10).Value = invoice.TenDonvi;

            // Format data row
            for (var col = 1; col <= 10; col++)
            {
                var cell = sheet.Cell(rowNumber, col);
                cell.Style.Alignment.Horizontal = col >= 6 && col <= 8 
                    ? XLAlignmentHorizontalValues.Right 
                    : XLAlignmentHorizontalValues.Left;
            }

            // Format number cells
            sheet.Cell(rowNumber, 6).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(rowNumber, 7).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(rowNumber, 8).Style.NumberFormat.Format = "#,##0.00";

            rowNumber++;
        }

        // Freeze header row
        sheet.SheetView.FreezeRows(1);
    }

    private void CreateLineItemsSheet(IXLWorksheet sheet, IReadOnlyCollection<InvoiceForExportDto> invoices)
    {
        // Set column widths
        sheet.Column(1).Width = 12;  // Số hóa đơn
        sheet.Column(2).Width = 25;  // Tên hàng hóa
        sheet.Column(3).Width = 12;  // Số lượng
        sheet.Column(4).Width = 12;  // Đơn giá
        sheet.Column(5).Width = 12;  // Thuế suất
        sheet.Column(6).Width = 12;  // Thành tiền

        // Create header row
        var headerRow = sheet.Row(1);
        headerRow.Cell(1).Value = "Số hóa đơn";
        headerRow.Cell(2).Value = "Tên hàng hóa";
        headerRow.Cell(3).Value = "Số lượng";
        headerRow.Cell(4).Value = "Đơn giá";
        headerRow.Cell(5).Value = "Thuế suất";
        headerRow.Cell(6).Value = "Thành tiền";

        // Format header row
        var headerStyle = headerRow.Style;
        headerStyle.Font.Bold = true;
        headerStyle.Fill.BackgroundColor = XLColor.LightGray;
        headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Add data rows
        var rowNumber = 2;
        foreach (var invoice in invoices)
        {
            foreach (var lineItem in invoice.LineItems)
            {
                sheet.Cell(rowNumber, 1).Value = invoice.Sohoadon ?? string.Empty;
                sheet.Cell(rowNumber, 2).Value = lineItem.TenHanghoa;
                sheet.Cell(rowNumber, 3).Value = lineItem.Soluong;
                sheet.Cell(rowNumber, 4).Value = lineItem.Dongia;
                sheet.Cell(rowNumber, 5).Value = lineItem.Thuesuat;
                sheet.Cell(rowNumber, 6).Value = lineItem.Thanhtien;

                // Format data row
                for (var col = 1; col <= 6; col++)
                {
                    var cell = sheet.Cell(rowNumber, col);
                    cell.Style.Alignment.Horizontal = col >= 3 && col <= 6 
                        ? XLAlignmentHorizontalValues.Right 
                        : XLAlignmentHorizontalValues.Left;
                }

                // Format number cells
                sheet.Cell(rowNumber, 3).Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(rowNumber, 4).Style.NumberFormat.Format = "#,##0.00";
                sheet.Cell(rowNumber, 5).Style.NumberFormat.Format = "0.00%";
                sheet.Cell(rowNumber, 6).Style.NumberFormat.Format = "#,##0.00";

                rowNumber++;
            }
        }

        // Freeze header row
        sheet.SheetView.FreezeRows(1);
    }

    private void CreateSalesReportSheet(IXLWorksheet sheet, IReadOnlyCollection<SalesReportRow> salesData)
    {
        // Set column widths
        sheet.Column(1).Width = 25;  // Tên khách hàng
        sheet.Column(2).Width = 12;  // Số hóa đơn
        sheet.Column(3).Width = 15;  // Tổng tiền hàng
        sheet.Column(4).Width = 12;  // Tiền thuế
        sheet.Column(5).Width = 15;  // Tổng thanh toán

        // Create header row
        var headerRow = sheet.Row(1);
        headerRow.Cell(1).Value = "Tên khách hàng";
        headerRow.Cell(2).Value = "Số hóa đơn";
        headerRow.Cell(3).Value = "Tổng tiền hàng";
        headerRow.Cell(4).Value = "Tiền thuế";
        headerRow.Cell(5).Value = "Tổng thanh toán";

        // Format header row
        var headerStyle = headerRow.Style;
        headerStyle.Font.Bold = true;
        headerStyle.Fill.BackgroundColor = XLColor.LightGray;
        headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Add data rows
        var rowNumber = 2;
        decimal totalTienHang = 0;
        decimal totalTienThue = 0;
        decimal totalThanhToan = 0;

        foreach (var row in salesData)
        {
            sheet.Cell(rowNumber, 1).Value = row.TenKhachHang;
            sheet.Cell(rowNumber, 2).Value = row.SoHoaDon;
            sheet.Cell(rowNumber, 3).Value = row.TongTienHang;
            sheet.Cell(rowNumber, 4).Value = row.TienThue;
            sheet.Cell(rowNumber, 5).Value = row.TongThanhToan;

            // Format data row
            for (var col = 1; col <= 5; col++)
            {
                var cell = sheet.Cell(rowNumber, col);
                cell.Style.Alignment.Horizontal = col >= 2 
                    ? XLAlignmentHorizontalValues.Right 
                    : XLAlignmentHorizontalValues.Left;
            }

            // Format number cells
            sheet.Cell(rowNumber, 2).Style.NumberFormat.Format = "0";
            sheet.Cell(rowNumber, 3).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(rowNumber, 4).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(rowNumber, 5).Style.NumberFormat.Format = "#,##0.00";

            // Accumulate totals
            totalTienHang += row.TongTienHang;
            totalTienThue += row.TienThue;
            totalThanhToan += row.TongThanhToan;

            rowNumber++;
        }

        // Add totals row
        sheet.Cell(rowNumber, 1).Value = "TỔNG CỘNG";
        sheet.Cell(rowNumber, 1).Style.Font.Bold = true;
        sheet.Cell(rowNumber, 2).Value = salesData.Sum(x => x.SoHoaDon);
        sheet.Cell(rowNumber, 3).Value = totalTienHang;
        sheet.Cell(rowNumber, 4).Value = totalTienThue;
        sheet.Cell(rowNumber, 5).Value = totalThanhToan;

        // Format totals row
        var totalsStyle = sheet.Row(rowNumber).Style;
        totalsStyle.Font.Bold = true;
        totalsStyle.Fill.BackgroundColor = XLColor.LightYellow;

        sheet.Cell(rowNumber, 2).Style.NumberFormat.Format = "0";
        sheet.Cell(rowNumber, 3).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(rowNumber, 4).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(rowNumber, 5).Style.NumberFormat.Format = "#,##0.00";

        // Freeze header row
        sheet.SheetView.FreezeRows(1);
    }
}
