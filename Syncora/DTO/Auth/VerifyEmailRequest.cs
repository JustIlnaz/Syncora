using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Auth
{
    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
