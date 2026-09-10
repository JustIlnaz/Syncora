namespace Syncora.Client.Services;

public class CalendarService
{
    private readonly ApiClient _apiClient;

    public CalendarService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
}
