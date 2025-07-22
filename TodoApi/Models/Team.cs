using System.Collections.Generic;

namespace TodoApi.Models
{
    public enum TeamRole
    {
        Owner,
        Admin,
        Member,
        Viewer
    }

    public class Team
    {
        public Team()
        {
            Members = new List<TeamMember>();
            SharedTodos = new List<Todo>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public ICollection<TeamMember> Members { get; set; }
        public ICollection<Todo> SharedTodos { get; set; }
    }

    public class TeamMember
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public int UserId { get; set; }
        public TeamRole Role { get; set; }
        public Team Team { get; set; } = null!;
        public User User { get; set; } = null!;
    }
} 