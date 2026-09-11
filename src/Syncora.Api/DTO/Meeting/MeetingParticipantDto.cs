namespace Syncora.DTO.Meeting
{
    public class MeetingParticipantDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Status { get; set; }
    }
}
