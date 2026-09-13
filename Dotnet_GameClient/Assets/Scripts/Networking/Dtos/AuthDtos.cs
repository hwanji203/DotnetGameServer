using System;

namespace Networking.Dtos
{
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Nickname { get; set; }
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserResponse User { get; set; }
    }
    
    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public long Gold { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}