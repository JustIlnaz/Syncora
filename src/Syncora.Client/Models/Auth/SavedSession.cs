namespace Syncora.Client.Models.Auth;

public sealed class SavedSession
{
    public bool RememberMe { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
