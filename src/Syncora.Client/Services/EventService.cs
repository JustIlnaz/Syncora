using Syncora.Client.Models.Event;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Syncora.Client.Services;

public class EventService
{
    private readonly ApiClient _apiClient;

    public EventService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<List<EventDto>> GetEventsInRangeAsync(DateTime startUtc, DateTime endUtc)
    {
        var url = $"/api/events?start={startUtc:yyyy-MM-ddTHH:mm:ss}Z&end={endUtc:yyyy-MM-ddTHH:mm:ss}Z";
        return _apiClient.GetAsync<List<EventDto>>(url);
    }

    public async Task<EventDto?> GetEventAsync(Guid id)
    {
        try
        {
            return await _apiClient.GetAsync<EventDto>($"/api/events/{id}");
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public Task<EventDto> CreateEventAsync(CreateEventRequest request)
        => _apiClient.PostAsync<CreateEventRequest, EventDto>("/api/events", request);

    public Task<EventDto> UpdateEventAsync(Guid id, UpdateEventRequest request)
        => _apiClient.PutAsync<UpdateEventRequest, EventDto>($"/api/events/{id}", request);

    public Task DeleteEventAsync(Guid id)
        => _apiClient.DeleteAsync($"/api/events/{id}");
}
