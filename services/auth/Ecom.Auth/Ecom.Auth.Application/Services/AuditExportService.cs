using ClosedXML.Excel;
using Ecom.Auth.Application.DTOs;

namespace Ecom.Auth.Application.Services;

public class AuditExportService
{
    private const int MaxExportRecords = 10000;

    public byte[] GenerateExcel(IEnumerable<AuditLogDto> logs, AuditLogFilterDto filter)
    {
        var logList = logs.ToList();

        // Enforce export limit to prevent memory issues
        if (logList.Count > MaxExportRecords)
        {
            throw new InvalidOperationException($"Export limit exceeded. Maximum {MaxExportRecords} records allowed.");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Audit Logs");

        // Report metadata (professional context)
        worksheet.Cell(1, 1).Value = "Audit Logs Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        worksheet.Cell(2, 1).Value = $"Generated At: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        worksheet.Cell(3, 1).Value = $"Total Records: {logList.Count}";

        // Filter summary (if filters applied)
        var filterSummary = BuildFilterSummary(filter);
        if (!string.IsNullOrEmpty(filterSummary))
        {
            worksheet.Cell(4, 1).Value = $"Filters Applied: {filterSummary}";
        }

        // Headers start at row 6
        int headerRow = 6;
        worksheet.Cell(headerRow, 1).Value = "User Email";
        worksheet.Cell(headerRow, 2).Value = "Action Performed";
        worksheet.Cell(headerRow, 3).Value = "Status";
        worksheet.Cell(headerRow, 4).Value = "IP Address";
        worksheet.Cell(headerRow, 5).Value = "Timestamp (UTC)";

        // Style headers
        var headerRange = worksheet.Range(headerRow, 1, headerRow, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data starts at row 7
        int row = headerRow + 1;
        foreach (var log in logList)
        {
            worksheet.Cell(row, 1).Value = SanitizeExcelValue(log.Email ?? "N/A");
            worksheet.Cell(row, 2).Value = SanitizeExcelValue(log.Action);
            worksheet.Cell(row, 3).Value = SanitizeExcelValue(log.Status);
            worksheet.Cell(row, 4).Value = SanitizeExcelValue(log.IpAddress ?? "N/A");
            worksheet.Cell(row, 5).Value = log.CreatedAt;
            row++;
        }

        // Format timestamp column as datetime
        worksheet.Column(5).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
        worksheet.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Builds a human-readable filter summary
    /// </summary>
    private string BuildFilterSummary(AuditLogFilterDto filter)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(filter.Email))
            parts.Add($"Email={filter.Email}");

        if (!string.IsNullOrEmpty(filter.Action))
            parts.Add($"Action={filter.Action}");

        if (filter.FromDate.HasValue)
            parts.Add($"From={filter.FromDate.Value:yyyy-MM-dd}");

        if (filter.ToDate.HasValue)
            parts.Add($"To={filter.ToDate.Value:yyyy-MM-dd}");

        return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
    }

    /// <summary>
    /// Sanitizes Excel values to prevent formula injection attacks
    /// </summary>
    private string SanitizeExcelValue(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Prevent Excel formula injection by escaping dangerous characters
        if (input.StartsWith("=") || input.StartsWith("+") || 
            input.StartsWith("-") || input.StartsWith("@"))
        {
            return "'" + input;
        }

        return input;
    }
}
