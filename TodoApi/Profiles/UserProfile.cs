using AutoMapper;
using TodoApi.Models;
using TodoApi.DTOs;

namespace TodoApi.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserReadDto>();
        }
    }
} 