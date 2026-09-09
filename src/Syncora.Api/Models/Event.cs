namespace Syncora.Models
{
    public class Event
    {
        public Guid Id { get; set; }

        public Guid CalendarId { get; set; }

        public Guid CreatorId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public string? Location { get; set; }

        public bool IsAllDay { get; set; }

        public Guid? RecurrenceId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
