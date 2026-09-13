using System;
using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Meeting
{
    /// <summary>
    /// Изменение встречи (ТЗ §11: изменение встречи → уведомление участникам).
    /// </summary>
    public class UpdateMeetingRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime Start { get; set; }

        [Required]
        public DateTime End { get; set; }
    }
}
