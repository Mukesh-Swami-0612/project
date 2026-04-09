using AutoMapper;
using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Domain.Entities;

namespace Ecom.Auth.Application.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName));
    }
}
