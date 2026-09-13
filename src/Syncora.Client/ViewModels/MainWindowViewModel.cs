using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Syncora.Client.Messages;
using Syncora.Client.Services;
using System;

namespace Syncora.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase,
    IRecipient<NavigateToMainMessage>,
    IRecipient<NavigateToCalendarMessage>,
    IRecipient<LogoutMessage>
{
    private readonly AuthViewModel _authViewModel;
    private readonly AuthSessionStore _sessionStore;
    private readonly ApiClient _apiClient;
    private readonly IServiceProvider _serviceProvider;

    public string Greeting { get; } = "Syncora";

    [ObservableProperty]
    private ObservableObject currentView;

    public MainWindowViewModel(
        AuthViewModel authViewModel,
        AuthSessionStore sessionStore,
        ApiClient apiClient,
        IServiceProvider serviceProvider)
    {
        _authViewModel = authViewModel;
        _sessionStore = sessionStore;
        _apiClient = apiClient;
        _serviceProvider = serviceProvider;

        WeakReferenceMessenger.Default.RegisterAll(this);

        var session = sessionStore.Load();
        if (session?.RememberMe == true && !string.IsNullOrWhiteSpace(session.Token))
        {
            apiClient.SetToken(session.Token);
            CurrentView = CreateAppShell(session.UserName, session.Email);
        }
        else
        {
            CurrentView = authViewModel;
        }
    }

    private AppShellViewModel CreateAppShell(string userName, string email)
    {
        return new AppShellViewModel(
            userName,
            email,
            _serviceProvider.GetRequiredService<UserProfileService>(),
            _sessionStore,
            _apiClient);
    }

    public void Receive(NavigateToMainMessage message)
    {
        CurrentView = CreateAppShell(message.UserName, message.Email);
    }

    public void Receive(NavigateToCalendarMessage message)
    {
        CurrentView = CreateAppShell(message.UserName, message.Email);
    }

    public void Receive(LogoutMessage message)
    {
        _sessionStore.Clear();
        _apiClient.ClearToken();
        CurrentView = _authViewModel;
    }
}
