using Ecom.Reporting.Application.DTOs;

namespace Ecom.Reporting.Application.Interfaces;

public interface IReportExportService
{
    Task<byte[]> ExportAsync(ReportExportRequestDto request);
}
