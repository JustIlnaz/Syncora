namespace Syncora.Models
{
    public class Contact
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid ContactUserId { get; set; }

        public string? Nickname { get; set; }

        public string? Timezone { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
