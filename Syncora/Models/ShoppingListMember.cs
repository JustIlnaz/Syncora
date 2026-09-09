namespace Syncora.Models
{
    public class ShoppingListMember
    {
        public Guid Id { get; set; }

        public Guid ShoppingListId { get; set; }

        public Guid UserId { get; set; }

        public string Role { get; set; } = "member";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
