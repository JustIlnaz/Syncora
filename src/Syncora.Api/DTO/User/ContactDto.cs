namespace Syncora.DTO.User
{
    public class ContactDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Timezone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
