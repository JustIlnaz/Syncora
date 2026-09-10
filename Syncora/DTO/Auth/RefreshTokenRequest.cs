using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
