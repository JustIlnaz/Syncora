using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Shopping
{
    public class CreateShoppingListRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsShared { get; set; } = false;

        public List<string> MemberEmails { get; set; } = new();
    }
}
