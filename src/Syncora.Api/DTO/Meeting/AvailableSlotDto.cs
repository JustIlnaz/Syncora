namespace Syncora.DTO.Meeting
{
    public class AvailableSlotDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int DurationMinutes { get; set; }
    }
}
