namespace Syncora.Client.Services;

public class ShoppingService
{
    private readonly ApiClient _apiClient;

    public ShoppingService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
}
