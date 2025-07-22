namespace TodoApi.DTOs
{
    public class TodoReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public int Priority { get; set; }
        public string Tags { get; set; }
    }

    public class TodoCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public int Priority { get; set; }
        public string Tags { get; set; }
    }

    public class TodoUpdateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public int Priority { get; set; }
        public string Tags { get; set; }
    }
} 