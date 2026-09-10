namespace Syncora.Client.Messages;

public sealed class NavigateToMainMessage
{
    public string UserName { get; }
    public string Token { get; }

    public NavigateToMainMessage(string userName, string token)
    {
        UserName = userName;
        Token = token;
    }
}

