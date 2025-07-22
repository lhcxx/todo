namespace TodoApi.DTOs
{
    public class TodoReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Tags { get; set; } = string.Empty;
        public bool IsShared { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class TodoCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int Priority { get; set; }
        public string Tags { get; set; } = string.Empty;
        public int? TeamId { get; set; }
    }

    public class TodoUpdateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Status { get; set; }
        public int? Priority { get; set; }
        public string? Tags { get; set; }
    }
} 