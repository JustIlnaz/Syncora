using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Event
{
    public class CreateEventRequest
    {
        [Required]
        public Guid CalendarId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        public bool IsAllDay { get; set; } = false;
    }
}
