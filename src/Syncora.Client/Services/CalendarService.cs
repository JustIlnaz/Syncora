using Syncora.Client.Models.Calendar;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Syncora.Client.Services;

public class CalendarService
{
    private readonly ApiClient _apiClient;

    public CalendarService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<List<CalendarDto>> GetCalendarsAsync()
        => _apiClient.GetAsync<List<CalendarDto>>("/api/calendars");

    public Task<CalendarDto> GetCalendarAsync(Guid id)
        => _apiClient.GetAsync<CalendarDto>($"/api/calendars/{id}");

    public Task<CalendarDto> CreateCalendarAsync(CreateCalendarRequest request)
        => _apiClient.PostAsync<CreateCalendarRequest, CalendarDto>("/api/calendars", request);

    public Task<CalendarDto> UpdateCalendarAsync(Guid id, UpdateCalendarRequest request)
        => _apiClient.PutAsync<UpdateCalendarRequest, CalendarDto>($"/api/calendars/{id}", request);

    public Task DeleteCalendarAsync(Guid id)
        => _apiClient.DeleteAsync($"/api/calendars/{id}");

    public Task<CalendarDto> AddMemberAsync(Guid calendarId, AddCalendarMemberRequest request)
        => _apiClient.PostAsync<AddCalendarMemberRequest, CalendarDto>(
            $"/api/calendars/{calendarId}/members", request);

    public Task<CalendarMemberDto> UpdateMemberAsync(Guid calendarId, Guid userId, UpdateCalendarMemberRequest request)
        => _apiClient.PutAsync<UpdateCalendarMemberRequest, CalendarMemberDto>(
            $"/api/calendars/{calendarId}/members/{userId}", request);

    public Task RemoveMemberAsync(Guid calendarId, Guid userId)
        => _apiClient.DeleteAsync($"/api/calendars/{calendarId}/members/{userId}");
}
