using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Meeting
{
    public class CreateMeetingRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(15, 480)]
        public int DurationMinutes { get; set; } = 60;

        [Required]
        public DateTime SearchStart { get; set; }

        [Required]
        public DateTime SearchEnd { get; set; }

        [Required]
        public List<string> ParticipantEmails { get; set; } = new();
    }
}
