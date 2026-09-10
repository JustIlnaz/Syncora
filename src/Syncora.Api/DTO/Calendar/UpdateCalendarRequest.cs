using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Calendar
{
    public class UpdateCalendarRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Timezone { get; set; }
    }
}
