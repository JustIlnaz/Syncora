namespace Syncora.Models
{
    public class UserWorkingHours
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public short DayOfWeek { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public bool IsWorkingDay { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
