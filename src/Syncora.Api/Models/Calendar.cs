using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class Calendar
    {
        [Key]
        public Guid Id { get; set; }

        public Guid OwnerId { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Color { get; set; }

        public Syncora.Models.Enums.CalendarType Type { get; set; } = Syncora.Models.Enums.CalendarType.Personal;

        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Timezone { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public User? Owner { get; set; }

        public ICollection<CalendarMember> Members { get; set; } = new List<CalendarMember>();
        public ICollection<UserCalendarSettings> UserSettings { get; set; } = new List<UserCalendarSettings>();
        public ICollection<UserAccessSettings> AccessSettings { get; set; } = new List<UserAccessSettings>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
