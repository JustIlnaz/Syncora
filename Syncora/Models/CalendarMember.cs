using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class CalendarMember
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CalendarId { get; set; }
        public Guid UserId { get; set; }

        [MaxLength(20)]
        public string? Role { get; set; }

        [MaxLength(20)]
        public string? AccessLevel { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(CalendarId))]
        public Calendar? Calendar { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
