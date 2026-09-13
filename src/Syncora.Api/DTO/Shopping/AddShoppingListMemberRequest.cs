using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Shopping
{
    public class AddShoppingListMemberRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Role { get; set; } = "member";
    }
}
