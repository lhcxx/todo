using System.Collections.Generic;

namespace TodoApi.Models
{
    public class User
    {
        public User()
        {
            Todos = new List<Todo>();
            OwnedTeams = new List<Team>();
            TeamMemberships = new List<TeamMember>();
            Activities = new List<Activity>();
        }

        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public ICollection<Todo> Todos { get; set; }
        public ICollection<Team> OwnedTeams { get; set; }
        public ICollection<TeamMember> TeamMemberships { get; set; }
        public ICollection<Activity> Activities { get; set; }
    }
} 