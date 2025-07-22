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
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public TodoStatus Status { get; set; }
        public int Priority { get; set; }
        public string Tags { get; set; } // Comma-separated tags
        public int UserId { get; set; }
        public User User { get; set; }
    }
} 