using Syncora.Client.Models.Meeting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Syncora.Client.Services;

public class MeetingService
{
    private readonly ApiClient _apiClient;

    public MeetingService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<List<MeetingDto>> GetMyMeetingsAsync()
        => _apiClient.GetAsync<List<MeetingDto>>("/api/meetings");

    public Task<MeetingSearchResponse> SearchSlotsAsync(MeetingSearchRequest request)
        => _apiClient.PostAsync<MeetingSearchRequest, MeetingSearchResponse>(
            "/api/meetings/search", request);

    public Task<MeetingDto> CreateMeetingAsync(CreateMeetingRequest request)
        => _apiClient.PostAsync<CreateMeetingRequest, MeetingDto>("/api/meetings", request);

    public Task<MeetingDto> UpdateMeetingAsync(Guid id, UpdateMeetingRequest request)
        => _apiClient.PutAsync<UpdateMeetingRequest, MeetingDto>($"/api/meetings/{id}", request);

    public Task DeleteMeetingAsync(Guid id)
        => _apiClient.DeleteAsync($"/api/meetings/{id}");

    public Task RespondAsync(Guid meetingId, string status)
        => _apiClient.PutNoContentAsync($"/api/meetings/{meetingId}/respond?status={status}");
}
