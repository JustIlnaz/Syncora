using System;
using System.Collections.Generic;

namespace Syncora.Client.Models.Calendar;

public class CalendarDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
    public string? Timezone { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CalendarMemberDto> Members { get; set; } = new();
}

public class CalendarMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Role { get; set; }
    public string? AccessLevel { get; set; }
}

public class CreateCalendarRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
    public string? Timezone { get; set; }
}

public class UpdateCalendarRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
    public string? Timezone { get; set; }
}

public class AddCalendarMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; } = "member";
    public string? AccessLevel { get; set; } = "edit";
}

public class UpdateCalendarMemberRequest
{
    public string? Role { get; set; }
    public string? AccessLevel { get; set; }
}
