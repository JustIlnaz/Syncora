namespace Syncora.Client.Services;

public class EventService
{
    private readonly ApiClient _apiClient;

    public EventService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
}
