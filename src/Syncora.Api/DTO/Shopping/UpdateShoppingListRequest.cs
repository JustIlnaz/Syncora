using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Shopping
{
    public class UpdateShoppingListRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        public bool? IsShared { get; set; }
    }
}
