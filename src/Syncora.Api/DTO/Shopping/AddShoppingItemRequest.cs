using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Shopping
{
    public class AddShoppingItemRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? Quantity { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }
    }
}
