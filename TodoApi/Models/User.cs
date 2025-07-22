using System.Collections.Generic;

namespace TodoApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public ICollection<Todo> Todos { get; set; }
    }
} 