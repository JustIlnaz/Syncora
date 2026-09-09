namespace Syncora.Models
{
    public class Recurrence
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public string Frequency { get; set; } = string.Empty;

        public int Interval { get; set; } = 1;

        public string? DayOfWeek { get; set; }

        public DateOnly? EndDate { get; set; }

        public int? Count { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
