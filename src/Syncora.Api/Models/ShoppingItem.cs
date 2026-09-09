namespace Syncora.Models
{
    public class ShoppingItem
    {
        public Guid Id { get; set; }

        public Guid ShoppingListId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string? Unit { get; set; }

        public string? Category { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
