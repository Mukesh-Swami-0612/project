using ClosedXML.Excel;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Infrastructure.Services;

/// <summary>
/// Service for exporting workflow data to various formats
/// </summary>
public class WorkflowExportService : IWorkflowExportService
{
    /// <summary>
    /// Export workflow audit logs to Excel format
    /// </summary>
    public byte[] ExportLogsToExcel(List<WorkflowAuditLog> logs, Guid workflowId)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Workflow Logs");

        // Header row with styling
        worksheet.Cell(1, 1).Value = "Username";
        worksheet.Cell(1, 2).Value = "Action";
        worksheet.Cell(1, 3).Value = "Message";
        worksheet.Cell(1, 4).Value = "Date Time";

        // Style header
        var headerRange = worksheet.Range(1, 1, 1, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data rows
        int row = 2;
        foreach (var log in logs)
        {
            worksheet.Cell(row, 1).Value = log.Username;
            worksheet.Cell(row, 2).Value = log.Action;
            worksheet.Cell(row, 3).Value = log.Message;
            worksheet.Cell(row, 4).Value = log.LoggedAt.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Add metadata
        worksheet.Cell(row + 1, 1).Value = $"Workflow ID: {workflowId}";
        worksheet.Cell(row + 2, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        worksheet.Cell(row + 3, 1).Value = $"Total Records: {logs.Count}";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
