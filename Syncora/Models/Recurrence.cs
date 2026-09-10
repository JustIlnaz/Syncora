using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class Recurrence
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        [MaxLength(20)]
        public string? Frequency { get; set; }

        public int? Interval { get; set; }

        [MaxLength(20)]
        public string? DayOfWeek { get; set; }

        public DateOnly? EndDate { get; set; }

        public int? Count { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
