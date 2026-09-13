using Syncora.Client.Models.Notification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Syncora.Client.Services;

public class NotificationService
{
    private readonly ApiClient _apiClient;

    public NotificationService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<List<NotificationDto>> GetNotificationsAsync(bool onlyUnread = false)
        => _apiClient.GetAsync<List<NotificationDto>>(
            $"/api/notifications?onlyUnread={onlyUnread.ToString().ToLowerInvariant()}");

    public async Task<int> GetUnreadCountAsync()
    {
        var result = await _apiClient.GetAsync<UnreadCountResponse>("/api/notifications/unread-count");
        return result.Count;
    }

    public Task MarkAsReadAsync(Guid id)
        => _apiClient.PutNoContentAsync($"/api/notifications/{id}/read");

    public Task MarkAllAsReadAsync()
        => _apiClient.PutNoContentAsync("/api/notifications/read-all");

    public Task DeleteAsync(Guid id)
        => _apiClient.DeleteAsync($"/api/notifications/{id}");

    private class UnreadCountResponse
    {
        public int Count { get; set; }
    }
}
