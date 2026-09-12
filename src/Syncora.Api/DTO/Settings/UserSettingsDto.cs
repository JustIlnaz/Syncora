using Syncora.DTO.User;

namespace Syncora.DTO.Settings
{
    public class UserSettingsDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Timezone { get; set; }

        public List<WorkingHoursDto> WorkingHours { get; set; } = new();
        public List<CalendarSettingsItemDto> CalendarSettings { get; set; } = new();
    }

    public class CalendarSettingsItemDto
    {
        public Guid CalendarId { get; set; }
        public string CalendarName { get; set; } = string.Empty;
        public string? DefaultView { get; set; }     // day / week / month
        public string? PrivacyLevel { get; set; }    // public / private / custom
    }
}
