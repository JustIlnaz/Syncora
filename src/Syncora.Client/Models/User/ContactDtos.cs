using System;
using System.Collections.Generic;

namespace Syncora.Client.Models.User;

public class ContactDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Timezone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddContactRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
}

public class WorkingHoursDto
{
    public Guid Id { get; set; }
    public short DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkingDay { get; set; }
}

public class UpdateWorkingHoursRequest
{
    public List<WorkingHoursItemDto> Items { get; set; } = new();
}

public class WorkingHoursItemDto
{
    public short DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkingDay { get; set; }
}

public class UserSearchResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
