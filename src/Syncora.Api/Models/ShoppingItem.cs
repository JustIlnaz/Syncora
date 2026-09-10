using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class ShoppingItem
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ShoppingListId { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? Quantity { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(ShoppingListId))]
        public ShoppingList? ShoppingList { get; set; }
    }
}
