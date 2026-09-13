using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Syncora.DTO.Meeting
{
    /// <summary>
    /// Запрос поиска общего свободного времени (ТЗ §18.2).
    /// </summary>
    public class MeetingSearchRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "Укажите хотя бы одного участника")]
        public List<Guid> ParticipantIds { get; set; } = new();

        [Required]
        [Range(15, 1440, ErrorMessage = "Длительность должна быть от 15 минут до 24 часов")]
        public int DurationMinutes { get; set; } = 60;

        [Required]
        public DateTime From { get; set; }

        [Required]
        public DateTime To { get; set; }
    }
}
