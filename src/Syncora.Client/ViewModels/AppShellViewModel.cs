using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Helpers;
using Syncora.Client.Messages;
using Syncora.Client.Services;
using Syncora.Client.ViewModels.Navigation;
using Syncora.Client.ViewModels.Pages;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels;

public partial class AppShellViewModel : ViewModelBase,
    IRecipient<ProfileUpdatedMessage>
{
    private readonly UserProfileService _profileService;
    private readonly AuthSessionStore _sessionStore;
    private readonly ApiClient _apiClient;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserInitials))]
    private string userName;

    [ObservableProperty]
    private string userEmail;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAvatar))]
    [NotifyPropertyChangedFor(nameof(AvatarDisplayUrl))]
    private string? avatarUrl;

    [ObservableProperty]
    private int avatarVersion;

    public string UserInitials => GetInitials(UserName);
    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public string? AvatarDisplayUrl => HasAvatar
        ? $"{AvatarUrlHelper.ToAbsolute(AvatarUrl)}?v={AvatarVersion}"
        : null;

    public ObservableCollection<NavItemViewModel> NavigationItems { get; } = new();

    [ObservableProperty]
    private ViewModelBase currentPage;

    [ObservableProperty]
    private AppSection activeSection = AppSection.Calendar;

    [ObservableProperty]
    private bool isSidebarCompact;

    [ObservableProperty]
    private double sidebarWidth = 240;

    [ObservableProperty]
    private double sidebarMinWidth = 220;

    [ObservableProperty]
    private double sidebarMaxWidth = 260;

    [ObservableProperty]
    private bool isProfilePageOpen;

    public bool IsSidebarExpanded => !IsSidebarCompact;

    public AppShellViewModel(
        string userName,
        string email,
        UserProfileService profileService,
        AuthSessionStore sessionStore,
        ApiClient apiClient)
    {
        UserName = userName;
        UserEmail = email;
        _profileService = profileService;
        _sessionStore = sessionStore;
        _apiClient = apiClient;

        WeakReferenceMessenger.Default.Register(this);

        NavigationItems.Add(new NavItemViewModel(AppSection.Calendar,
            "Календарь",
            "M19 3H5C3.9 3 3 3.9 3 5V19C3 20.1 3.9 21 5 21H19C20.1 21 21 20.1 21 19V5C21 3.9 20.1 3 19 3ZM19 19H5V8H19V19Z",
            true));
        NavigationItems.Add(new NavItemViewModel(AppSection.Meetings,
            "Встречи",
            "M16 11C17.66 11 19 9.66 19 8C19 6.34 17.66 5 16 5C14.34 5 13 6.34 13 8C13 9.66 14.34 11 16 11Z"));
        NavigationItems.Add(new NavItemViewModel(AppSection.ShoppingLists,
            "Списки покупок",
            "M7 18C8.1 18 9 17.1 9 16C9 14.9 8.1 14 7 14C5.9 14 5 14.9 5 16C5 17.1 5.9 18 7 18ZM1 2V4H3L6.6 11.59L5.25 14.04C5.09 14.32 5 14.65 5 15C5 16.1 5.9 17 7 17H19V15H7.42C7.28 15 7.17 14.89 7.17 14.75L7.2 14.63L8.1 13H15.55C16.3 13 16.96 12.59 17.3 11.97L20.88 5.48C20.96 5.34 21 5.17 21 5C21 4.45 20.55 4 20 4H5.21L4.27 2H1Z"));
        NavigationItems.Add(new NavItemViewModel(AppSection.People,
            "Люди",
            "M12 12C14.21 12 16 10.21 16 8C16 5.79 14.21 4 12 4C9.79 4 8 5.79 8 8C8 10.21 9.79 12 12 12ZM12 14C9.33 14 4 15.34 4 18V20H20V18C20 15.34 14.67 14 12 14Z"));
        NavigationItems.Add(new NavItemViewModel(AppSection.Settings,
            "Настройки",
            "M19.14 12.94C19.18 12.64 19.2 12.33 19.2 12C19.2 11.67 19.18 11.36 19.14 11.06L21.16 9.37C21.34 9.22 21.39 8.95 21.28 8.73L19.28 5.27C19.17 5.05 18.92 4.96 18.7 5.03L16.25 5.88C15.75 5.49 15.19 5.17 14.58 4.94L14.22 2.34C14.18 2.11 13.98 1.94 13.75 1.94H10.25C10.02 1.94 9.82 2.11 9.78 2.34L9.42 4.94C8.81 5.17 8.25 5.49 7.75 5.88L5.3 5.03C5.08 4.96 4.83 5.05 4.72 5.27L2.72 8.73C2.61 8.95 2.66 9.22 2.84 9.37L4.86 11.06C4.82 11.36 4.8 11.67 4.8 12C4.8 12.33 4.82 12.64 4.86 12.94L2.84 14.63C2.66 14.78 2.61 15.05 2.72 15.27L4.72 18.73C4.83 18.95 5.08 19.04 5.3 18.97L7.75 18.12C8.25 18.51 8.81 18.83 9.42 19.06L9.78 21.66C9.82 21.89 10.02 22.06 10.25 22.06H13.75C13.98 22.06 14.18 21.89 14.22 21.66L14.58 19.06C15.19 18.83 15.75 18.51 16.25 18.12L18.7 18.97C18.92 19.04 19.17 18.95 19.28 18.73L21.28 15.27C21.39 15.05 21.34 14.78 21.16 14.63L19.14 12.94Z"));

        CurrentPage = CreatePage(AppSection.Calendar);
        _ = LoadSidebarProfileAsync();
    }

    private async Task LoadSidebarProfileAsync()
    {
        try
        {
            var profile = await _profileService.GetProfileAsync();
            UserName = profile.Name;
            UserEmail = profile.Email;
            AvatarUrl = profile.AvatarUrl;
            AvatarVersion++;
        }
        catch
        {
            // Sidebar can still work with session data.
        }
    }

    [RelayCommand]
    private void SelectNav(AppSection section)
    {
        if (ActiveSection == section && !IsProfilePageOpen)
            return;

        ActiveSection = section;
        IsProfilePageOpen = false;

        foreach (var item in NavigationItems)
            item.IsActive = item.Section == section;

        CurrentPage = CreatePage(section);
    }

    [RelayCommand]
    private void OpenProfile()
    {
        IsProfilePageOpen = true;

        foreach (var item in NavigationItems)
            item.IsActive = false;

        CurrentPage = new ProfilePageViewModel(
            UserName,
            UserEmail,
            _profileService,
            _sessionStore,
            _apiClient);
    }

    public void Receive(ProfileUpdatedMessage message)
    {
        UserName = message.UserName;
        UserEmail = message.Email;
        AvatarUrl = message.AvatarUrl;
        AvatarVersion++;
    }

    partial void OnIsSidebarCompactChanged(bool value)
    {
        SidebarWidth = value ? 72 : 240;
        SidebarMinWidth = value ? 72 : 220;
        SidebarMaxWidth = value ? 72 : 260;
        OnPropertyChanged(nameof(IsSidebarExpanded));
    }

    public void UpdateLayoutForWidth(double width)
    {
        IsSidebarCompact = width < 960;
    }

    private static PlaceholderPageViewModel CreatePage(AppSection section) => section switch
    {
        AppSection.Calendar => new PlaceholderPageViewModel(
            "Календарь",
            "Здесь будет ваше расписание, события и недельный вид."),
        AppSection.Meetings => new PlaceholderPageViewModel(
            "Встречи",
            "Здесь будут ваши встречи и созвоны."),
        AppSection.ShoppingLists => new PlaceholderPageViewModel(
            "Списки покупок",
            "Здесь будут общие и личные списки покупок."),
        AppSection.People => new PlaceholderPageViewModel(
            "Люди",
            "Здесь будет список контактов и участников."),
        AppSection.Settings => new PlaceholderPageViewModel(
            "Настройки",
            "Здесь будут настройки профиля и приложения."),
        _ => new PlaceholderPageViewModel("Syncora", "Раздел в разработке.")
    };

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
            : name[..System.Math.Min(2, name.Length)].ToUpperInvariant();
    }
}
