namespace Syncora.Client.Services;

public class NotificationService
{
    private readonly ApiClient _apiClient;

    public NotificationService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
}
