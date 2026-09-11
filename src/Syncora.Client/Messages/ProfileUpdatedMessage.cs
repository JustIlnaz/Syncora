namespace Syncora.Client.Messages;

public sealed class ProfileUpdatedMessage
{
    public string UserName { get; }
    public string Email { get; }
    public string? AvatarUrl { get; }

    public ProfileUpdatedMessage(string userName, string email, string? avatarUrl = null)
    {
        UserName = userName;
        Email = email;
        AvatarUrl = avatarUrl;
    }
}
