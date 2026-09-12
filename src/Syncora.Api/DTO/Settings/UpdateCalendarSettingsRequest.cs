using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Settings
{
    public class UpdateCalendarSettingsRequest
    {
        [Required]
        public Guid CalendarId { get; set; }

        [Required]
        [MaxLength(20)]
        public string DefaultView { get; set; } = "week";
    }
}
