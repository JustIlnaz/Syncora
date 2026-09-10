using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class MeetingParticipant
    {
        [Key]
        public Guid Id { get; set; }

        public Guid MeetingId { get; set; }
        public Guid UserId { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(MeetingId))]
        public Meeting? Meeting { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
