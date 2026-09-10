using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class ShoppingList
    {
        [Key]
        public Guid Id { get; set; }

        public Guid OwnerId { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsShared { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public User? Owner { get; set; }

        public ICollection<ShoppingListMember> Members { get; set; } = new List<ShoppingListMember>();
        public ICollection<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();
    }
}
