using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Event
{
    public class UpdateEventRequest
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        public bool? IsAllDay { get; set; }
    }
}
