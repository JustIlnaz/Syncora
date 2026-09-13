using Syncora.Client.Models.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Syncora.Client.Services;

public class UserProfileService
{
    private readonly ApiClient _apiClient;

    public UserProfileService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<UserProfileDto> GetProfileAsync()
        => _apiClient.GetAsync<UserProfileDto>("/api/users/me");

    public Task<UserProfileDto> UpdateProfileAsync(UpdateUserProfileRequest request)
        => _apiClient.PutAsync<UpdateUserProfileRequest, UserProfileDto>("/api/users/me", request);

    public Task<UserProfileDto> UploadAvatarAsync(Stream stream, string fileName, string contentType)
        => _apiClient.PostMultipartAsync<UserProfileDto>("/api/users/me/avatar", stream, fileName, contentType);

    public Task<UserProfileDto> DeleteAvatarAsync()
        => _apiClient.DeleteWithBodyAsync<UserProfileDto>("/api/users/me/avatar");

    public Task DeleteProfileAsync()
        => _apiClient.DeleteAsync("/api/users/me");

    public Task<List<ContactDto>> GetContactsAsync()
        => _apiClient.GetAsync<List<ContactDto>>("/api/users/me/contacts");

    public Task<ContactDto> AddContactAsync(AddContactRequest request)
        => _apiClient.PostAsync<AddContactRequest, ContactDto>("/api/users/me/contacts", request);

    public Task RemoveContactAsync(Guid contactId)
        => _apiClient.DeleteAsync($"/api/users/me/contacts/{contactId}");

    public Task<List<WorkingHoursDto>> GetWorkingHoursAsync()
        => _apiClient.GetAsync<List<WorkingHoursDto>>("/api/users/me/working-hours");

    public Task<List<WorkingHoursDto>> UpdateWorkingHoursAsync(UpdateWorkingHoursRequest request)
        => _apiClient.PutAsync<UpdateWorkingHoursRequest, List<WorkingHoursDto>>(
            "/api/users/me/working-hours", request);

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<UserSearchResultDto>();

        var encoded = WebUtility.UrlEncode(query.Trim());
        return await _apiClient.GetAsync<List<UserSearchResultDto>>(
            $"/api/users/search?query={encoded}");
    }
}
