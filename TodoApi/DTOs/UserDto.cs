namespace TodoApi.DTOs
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class UserRegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserLoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
} 