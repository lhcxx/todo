using TodoApi.Models;

namespace TodoApi.DTOs
{
    public class ActivityReadDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int? TodoId { get; set; }
        public string TodoName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ActivityFilterDto
    {
        public int? TeamId { get; set; }
        public int? UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class ActivityCreateDto
    {
        public ActivityType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public int? TodoId { get; set; }
    }
} 