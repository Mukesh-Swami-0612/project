using Ecom.Reporting.Application.DTOs;
using FluentValidation;

namespace Ecom.Reporting.Application.Validators;

public class ReportExportRequestValidator : AbstractValidator<ReportExportRequestDto>
{
    private static readonly string[] ValidReportTypes = { "catalog-quality", "low-stock", "price-changes", "all-audit" };

    public ReportExportRequestValidator()
    {
        RuleFor(x => x.ReportType)
            .NotEmpty()
            .WithMessage("Report type is required")
            .Must(type => ValidReportTypes.Contains(type.ToLower()))
            .WithMessage($"Invalid report type. Valid types: {string.Join(", ", ValidReportTypes)}");

        RuleFor(x => x.Format)
            .Must(f => f == "csv" || f == "excel")
            .WithMessage("Format must be 'csv' or 'excel'");

        RuleFor(x => x.FromDate)
            .LessThan(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("FromDate must be before ToDate");
    }
}
