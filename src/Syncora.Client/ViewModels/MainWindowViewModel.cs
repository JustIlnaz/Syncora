using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Messages;
using Syncora.Client.Services;

namespace Syncora.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase,
    IRecipient<NavigateToMainMessage>,
    IRecipient<NavigateToCalendarMessage>,
    IRecipient<LogoutMessage>
{
    private readonly AuthViewModel _authViewModel;
    private readonly AuthSessionStore _sessionStore;
    private readonly ApiClient _apiClient;

    public string Greeting { get; } = "Syncora";

    [ObservableProperty]
    private ObservableObject currentView;

    public MainWindowViewModel(
        AuthViewModel authViewModel,
        AuthSessionStore sessionStore,
        ApiClient apiClient)
    {
        _authViewModel = authViewModel;
        _sessionStore = sessionStore;
        _apiClient = apiClient;

        WeakReferenceMessenger.Default.RegisterAll(this);

        var session = sessionStore.Load();
        if (session?.RememberMe == true && !string.IsNullOrWhiteSpace(session.Token))
        {
            apiClient.SetToken(session.Token);
            CurrentView = new DashboardViewModel(
                session.UserName,
                session.Email,
                session.Token);
        }
        else
        {
            CurrentView = authViewModel;
        }
    }

    public void Receive(NavigateToMainMessage message)
    {
        CurrentView = new DashboardViewModel(message.UserName, message.Email, message.Token);
    }

    public void Receive(NavigateToCalendarMessage message)
    {
        CurrentView = new AppShellViewModel(
            message.UserName,
            message.Email,
            Ioc.Default.GetRequiredService<UserProfileService>(),
            _sessionStore,
            _apiClient);
    }

    public void Receive(LogoutMessage message)
    {
        _sessionStore.Clear();
        _apiClient.ClearToken();
        CurrentView = _authViewModel;
    }
}
