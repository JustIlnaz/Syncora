using Syncora.Client.Models.User;
using System.IO;
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
}
