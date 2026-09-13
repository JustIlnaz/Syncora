using System.Collections.Generic;

namespace Syncora.DTO.Meeting
{
    /// <summary>
    /// Ответ поиска общего свободного времени (ТЗ §18.2).
    /// </summary>
    public class MeetingSearchResponse
    {
        public List<MeetingSlotDto> Slots { get; set; } = new();
    }
}
