using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Settings
{
    public class UpdateAccessSettingsRequest
    {
        [Required]
        public Guid CalendarId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PrivacyLevel { get; set; } = "private";
    }
}
