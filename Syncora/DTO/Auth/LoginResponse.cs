namespace Syncora.DTO.Auth
{
    public class LoginResponse
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
