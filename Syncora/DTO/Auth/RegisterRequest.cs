using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Auth
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен быть минимум 6 символов")]
        public string Password { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Timezone { get; set; }
    }
}
