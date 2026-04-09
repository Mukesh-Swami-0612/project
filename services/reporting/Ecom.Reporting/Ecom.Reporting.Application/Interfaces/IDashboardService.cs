using Ecom.Reporting.Application.DTOs;

namespace Ecom.Reporting.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}
