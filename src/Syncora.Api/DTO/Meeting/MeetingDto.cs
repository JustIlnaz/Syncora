namespace Syncora.DTO.Meeting
{
    public class MeetingDto
    {
        public Guid Id { get; set; }
        public Guid CreatorId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? DurationMinutes { get; set; }
        public DateTime? SearchStart { get; set; }
        public DateTime? SearchEnd { get; set; }
        public string? Status { get; set; }
        public DateTime? SelectedSlotStart { get; set; }
        public DateTime? SelectedSlotEnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<MeetingParticipantDto> Participants { get; set; } = new();
    }
}
