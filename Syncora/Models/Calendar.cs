namespace Syncora.Models
{
    public class Calendar
    {
        public Guid Id { get; set; }

        public Guid OwnerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = "#7C5CFC";

        public string Type { get; set; } = "personal";

        public string? Description { get; set; }

        public string Timezone { get; set; } = "UTC";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
