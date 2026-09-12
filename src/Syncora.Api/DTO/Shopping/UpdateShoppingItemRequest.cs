using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Shopping
{
    public class UpdateShoppingItemRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        public int? Quantity { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        public bool? IsCompleted { get; set; }
    }
}
