using AutoMapper;
using TodoApi.Models;
using TodoApi.DTOs;

namespace TodoApi.Profiles
{
    public class TeamProfile : Profile
    {
        public TeamProfile()
        {
            CreateMap<Team, TeamReadDto>()
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.Owner.Username))
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count));
            
            CreateMap<TeamCreateDto, Team>();
            CreateMap<TeamUpdateDto, Team>();
            
            CreateMap<TeamMember, TeamMemberDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
        }
    }
} 