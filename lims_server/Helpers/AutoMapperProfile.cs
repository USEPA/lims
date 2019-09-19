using AutoMapper;
using LimsServer.Dtos;
using LimsServer.Entities;

namespace LimsServer.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}