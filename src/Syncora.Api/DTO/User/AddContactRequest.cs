using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.User
{
    public class AddContactRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Nickname { get; set; }
    }
}
