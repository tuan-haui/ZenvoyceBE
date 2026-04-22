using AutoMapper;
using Zenvoyce.Application.Features.Users.DTOs;
using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Features.Users;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<Nguoidung, UserDto>();
        CreateMap<Nguoidung, LoginUserInfoDto>();
    }
}
