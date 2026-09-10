using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Calendar
{
    public class UpdateCalendarMemberRequest
    {
        [MaxLength(20)]
        public string? Role { get; set; }

        [MaxLength(20)]
        public string? AccessLevel { get; set; }
    }
}
