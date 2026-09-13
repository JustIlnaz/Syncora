using System;

namespace Syncora.DTO.Meeting
{
    /// <summary>
    /// Свободный интервал, подходящий всем участникам (ТЗ §18.2).
    /// </summary>
    public class MeetingSlotDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
