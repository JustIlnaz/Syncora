using System;

namespace Syncora.Client.Messages;

/// <summary>
/// Запрос открыть редактор встречи (например, по клику на событие-встречу в календаре).
/// </summary>
public sealed class OpenMeetingEditMessage
{
    public Guid MeetingId { get; }

    public OpenMeetingEditMessage(Guid meetingId)
    {
        MeetingId = meetingId;
    }
}
