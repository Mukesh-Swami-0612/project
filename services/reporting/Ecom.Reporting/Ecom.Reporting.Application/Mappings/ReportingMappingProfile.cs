using AutoMapper;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Domain.Entities;

namespace Ecom.Reporting.Application.Mappings;

public class ReportingMappingProfile : Profile
{
    public ReportingMappingProfile()
    {
        CreateMap<AuditLog, AuditLogDto>().ReverseMap();
    }
}
