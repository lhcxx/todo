using System;
using System.Collections.Generic;

namespace TodoApi.Models
{
    public enum TodoStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    public class Todo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public TodoStatus Status { get; set; }
        public int Priority { get; set; }
        public string Tags { get; set; } = string.Empty; // Comma-separated tags
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int? TeamId { get; set; }
        public Team? Team { get; set; }
        public bool IsShared { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 