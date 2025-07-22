using AutoMapper;
using TodoApi.Models;
using TodoApi.DTOs;

namespace TodoApi.Profiles
{
    public class TodoProfile : Profile
    {
            public TodoProfile()
    {
        CreateMap<Todo, TodoReadDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team != null ? src.Team.Name : null))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.User != null ? src.User.Username : "Unknown"));
        CreateMap<TodoCreateDto, Todo>();
        CreateMap<TodoUpdateDto, Todo>();
    }
    }
} 