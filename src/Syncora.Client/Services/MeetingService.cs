namespace Syncora.Client.Services;

public class MeetingService
{
    private readonly ApiClient _apiClient;

    public MeetingService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
}
