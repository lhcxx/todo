namespace TodoApi.DTOs
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
    }

    public class UserRegisterDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserLoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
} 