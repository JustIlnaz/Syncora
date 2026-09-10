using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Messages;

namespace Syncora.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IRecipient<NavigateToMainMessage>
{
    public string Greeting { get; } = "Syncora";

    [ObservableProperty]
    private ObservableObject currentView;

    public MainWindowViewModel(AuthViewModel authViewModel)
    {
        CurrentView = authViewModel;
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(NavigateToMainMessage message)
    {
        CurrentView = new DashboardViewModel(message.UserName);
    }
}
