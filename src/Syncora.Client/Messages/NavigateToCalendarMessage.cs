namespace Syncora.Client.Messages;

public sealed class NavigateToCalendarMessage
{
    public string UserName { get; }
    public string Email { get; }
    public string Token { get; }

    public NavigateToCalendarMessage(string userName, string email, string token)
    {
        UserName = userName;
        Email = email;
        Token = token;
    }
}
