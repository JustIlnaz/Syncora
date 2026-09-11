using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.User
{
    public class UpdateWorkingHoursRequest
    {
        [Required]
        public List<WorkingHoursItem> Items { get; set; } = new();
    }

    public class WorkingHoursItem
    {
        [Required]
        [Range(1, 7)]
        public short DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public bool IsWorkingDay { get; set; }
    }
}
