using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Calendar
{
    public class AddCalendarMemberRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Role { get; set; } = "member";

        [MaxLength(20)]
        public string? AccessLevel { get; set; } = "edit";
    }
}
