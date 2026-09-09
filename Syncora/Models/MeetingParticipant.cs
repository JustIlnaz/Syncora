namespace Syncora.Models
{
    public class MeetingParticipant
    {
        public Guid Id { get; set; }

        public Guid MeetingId { get; set; }

        public Guid UserId { get; set; }

        public string Status { get; set; } = "pending";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
