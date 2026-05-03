using System.Text;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;

namespace Ecom.Reporting.Application.Services;

public class ReportExportService : IReportExportService
{
    private readonly IAuditRepository _repository;

    public ReportExportService(IAuditRepository repository) => _repository = repository;

    public async Task<byte[]> ExportAsync(ReportExportRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ReportType))
            throw new InvalidOperationException("Report type is required.");

        // Use repository method with filtering and limit
        var logs = await _repository.GetFilteredAsync(
            request.FromDate, 
            request.ToDate, 
            request.ReportType, 
            10000);

        // Build CSV
        var sb = new StringBuilder();
        sb.AppendLine("Id,EntityName,EntityId,Action,EventType,SourceService,CorrelationId,CreatedAt");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id},{log.EntityName},{log.EntityId},{log.Action},{log.EventType},{log.SourceService},{log.CorrelationId},{log.CreatedAt:O}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
