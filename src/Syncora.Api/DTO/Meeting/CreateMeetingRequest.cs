using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Meeting
{
    /// <summary>
    /// Создание встречи на выбранном слоте (ТЗ §18.3).
    /// </summary>
    public class CreateMeetingRequest
    {
        [Required(ErrorMessage = "Название обязательно")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Укажите хотя бы одного участника")]
        public List<Guid> ParticipantIds { get; set; } = new();

        [Required]
        public DateTime Start { get; set; }

        [Required]
        public DateTime End { get; set; }
    }
}
