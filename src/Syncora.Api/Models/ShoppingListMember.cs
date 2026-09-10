using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Syncora.Models
{
    public class ShoppingListMember
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ShoppingListId { get; set; }
        public Guid UserId { get; set; }

        [MaxLength(20)]
        public string? Role { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(ShoppingListId))]
        public ShoppingList? ShoppingList { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
