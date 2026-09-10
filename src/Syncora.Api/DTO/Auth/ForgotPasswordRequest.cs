using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Auth
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
