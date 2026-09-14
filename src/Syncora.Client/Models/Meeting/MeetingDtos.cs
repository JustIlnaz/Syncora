using System;
using System.Collections.Generic;

namespace Syncora.Client.Models.Meeting;

public class MeetingDto
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? SearchStart { get; set; }
    public DateTime? SearchEnd { get; set; }
    public string? Status { get; set; }
    public DateTime? SelectedSlotStart { get; set; }
    public DateTime? SelectedSlotEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MeetingParticipantDto> Participants { get; set; } = new();

    public bool IsCurrentUserCreator { get; set; }
    public bool CanRespond { get; set; }
}

public class MeetingParticipantDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Status { get; set; }

    public string StatusText => Status switch
    {
        "pending" => "Ожидает",
        "accepted" => "Принял",
        "declined" => "Отклонил",
        _ => Status ?? string.Empty
    };

    public bool IsAccepted => Status == "accepted";
    public bool IsDeclined => Status == "declined";

    public string StatusIcon => Status switch
    {
        "accepted" => "✓ ",
        "declined" => "✕ ",
        _ => "⏳ "
    };
}

public class MeetingSearchRequest
{
    public List<Guid> ParticipantIds { get; set; } = new();
    public int DurationMinutes { get; set; } = 60;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class MeetingSlotDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class MeetingSearchResponse
{
    public List<MeetingSlotDto> Slots { get; set; } = new();
}

public class CreateMeetingRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class UpdateMeetingRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
