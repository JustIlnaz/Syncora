using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Meeting
{
    public class FindMeetingTimeRequest
    {
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
