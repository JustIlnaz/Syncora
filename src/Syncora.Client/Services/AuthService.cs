using System;
using System.Threading.Tasks;
using Syncora.Client.Models.Auth;

namespace Syncora.Client.Services;

public class AuthService
{
    private readonly ApiClient _apiClient;

    public AuthService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await _apiClient.PostAsync<LoginRequest, LoginResponse>(
            "/api/auth/login", request);

        _apiClient.SetToken(response.Token);
        return response;
    }

    public async Task<LoginResponse> RegisterAsync(string name, string email, string password)
    {
        var request = new RegisterRequest
        {
            Name = name,
            Email = email,
            Password = password,
            Timezone = TimeZoneInfo.Local.Id
        };

        var response = await _apiClient.PostAsync<RegisterRequest, LoginResponse>(
            "/api/auth/register", request);

        _apiClient.SetToken(response.Token);
        return response;
    }
}
