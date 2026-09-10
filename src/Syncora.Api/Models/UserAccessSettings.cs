using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class UserAccessSettings
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public Guid CalendarId { get; set; }

        [MaxLength(20)]
        public string? PrivacyLevel { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(CalendarId))]
        public Calendar? Calendar { get; set; }
    }
}
