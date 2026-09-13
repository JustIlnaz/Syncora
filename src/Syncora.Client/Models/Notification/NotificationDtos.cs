using System;

namespace Syncora.Client.Models.Notification;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
