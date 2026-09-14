namespace Syncora.DTO.Event
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public Guid CalendarId { get; set; }
        public string CalendarName { get; set; } = string.Empty;
        public string? CalendarColor { get; set; }
        public Guid CreatorId { get; set; }
        public Guid? MeetingId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string? Location { get; set; }
        public bool IsAllDay { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Имена участников связанной встречи, принявших приглашение.</summary>
        public List<string> AcceptedParticipants { get; set; } = new();

        public RecurrenceDto? Recurrence { get; set; }
    }
}
