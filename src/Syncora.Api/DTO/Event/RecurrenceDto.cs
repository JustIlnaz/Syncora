namespace Syncora.DTO.Event
{
    public class RecurrenceDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string? Frequency { get; set; }
        public int? Interval { get; set; }
        public string? DayOfWeek { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? Count { get; set; }
    }
}
