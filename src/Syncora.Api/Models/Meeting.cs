using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class Meeting
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CreatorId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? DurationMinutes { get; set; }

        public DateTime? SearchStart { get; set; }
        public DateTime? SearchEnd { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        public DateTime? SelectedSlotStart { get; set; }
        public DateTime? SelectedSlotEnd { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(CreatorId))]
        public User? Creator { get; set; }

        public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();

    }
}
