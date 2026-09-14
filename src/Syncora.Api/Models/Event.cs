using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class Event
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CalendarId { get; set; }
        public Guid CreatorId { get; set; }

        public Guid? MeetingId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        public bool IsAllDay { get; set; }

        public Guid? RecurrenceId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(CalendarId))]
        public Calendar? Calendar { get; set; }

        [ForeignKey(nameof(CreatorId))]
        public User? Creator { get; set; }

        [ForeignKey(nameof(MeetingId))]
        public Meeting? Meeting { get; set; }

        [ForeignKey(nameof(RecurrenceId))]
        public Recurrence? Recurrence { get; set; }
    }
}
