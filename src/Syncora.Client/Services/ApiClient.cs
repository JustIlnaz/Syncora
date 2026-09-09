using System.Net.Http;

namespace Syncora.Client.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public HttpClient Http => _httpClient;
}
