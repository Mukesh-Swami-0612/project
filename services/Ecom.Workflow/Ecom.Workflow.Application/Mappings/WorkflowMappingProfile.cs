using AutoMapper;
using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Application.Mappings;

public class WorkflowMappingProfile : Profile
{
    public WorkflowMappingProfile()
    {
        CreateMap<Price, PricingDto>().ReverseMap();
        CreateMap<Inventory, InventoryDto>().ReverseMap();
        CreateMap<Approval, ApprovalDto>().ReverseMap();
    }
}
