namespace Syncora.Client.Models.User;

public class UpdateUserProfileRequest
{
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Timezone { get; set; }
}
