using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Syncora.Client.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:5131");
    }

    public HttpClient Http => _httpClient;
    public void SetToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(url, request, JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var messageProp))
                {
                    throw new ApiException(response.StatusCode,
                        messageProp.GetString() ?? "Ошибка сервера");
                }
            }
            catch (JsonException) { }

            throw new ApiException(response.StatusCode,
                $"Ошибка сервера: {(int)response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        return result ?? throw new ApiException(response.StatusCode, "Пустой ответ сервера");
    }

    public async Task<T> GetAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return result ?? throw new ApiException(response.StatusCode, "Пустой ответ сервера");
    }

    public async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync(url, request, JsonOptions);
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        return result ?? throw new ApiException(response.StatusCode, "Пустой ответ сервера");
    }

    public async Task DeleteAsync(string url)
    {
        var response = await _httpClient.DeleteAsync(url);
        await EnsureSuccessAsync(response);
    }

    public async Task<TResponse> PostMultipartAsync<TResponse>(
        string url,
        Stream stream,
        string fileName,
        string contentType)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync(url, content);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        return result ?? throw new ApiException(response.StatusCode, "Пустой ответ сервера");
    }

    public async Task<TResponse> DeleteWithBodyAsync<TResponse>(string url)
    {
        var response = await _httpClient.DeleteAsync(url);
        await EnsureSuccessAsync(response);

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        return result ?? throw new ApiException(response.StatusCode, "Пустой ответ сервера");
    }

    public async Task PutNoContentAsync(string url)
    {
        var response = await _httpClient.PutAsync(url, content: null);
        await EnsureSuccessAsync(response);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url)
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage(method, url));
        await EnsureSuccessAsync(response);
        return response;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorContent = await response.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(errorContent);
            if (doc.RootElement.TryGetProperty("message", out var messageProp))
            {
                throw new ApiException(response.StatusCode,
                    messageProp.GetString() ?? "Ошибка сервера");
            }
        }
        catch (JsonException) { }

        throw new ApiException(response.StatusCode,
            $"Ошибка сервера: {(int)response.StatusCode}");
    }
}

public class ApiException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }

    public ApiException(System.Net.HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
