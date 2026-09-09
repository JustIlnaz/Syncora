namespace Syncora.Models
{
    public class CalendarMember
    {
        public Guid Id { get; set; }

        public Guid CalendarId { get; set; }

        public Guid UserId { get; set; }

        public string Role { get; set; } = "member";

        public string AccessLevel { get; set; } = "free_busy";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
