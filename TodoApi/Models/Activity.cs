using System;

namespace TodoApi.Models
{
    public enum ActivityType
    {
        TodoCreated,
        TodoUpdated,
        TodoCompleted,
        TodoDeleted,
        MemberJoined,
        MemberLeft,
        TeamCreated
    }

    public class Activity
    {
        public int Id { get; set; }
        public ActivityType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int? TeamId { get; set; }
        public Team? Team { get; set; }
        public int? TodoId { get; set; }
        public Todo? Todo { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 