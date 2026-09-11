namespace Syncora.DTO.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Timezone { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<WorkingHoursDto> WorkingHours { get; set; } = new();
    }
}
