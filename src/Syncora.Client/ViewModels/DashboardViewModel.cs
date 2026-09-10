using CommunityToolkit.Mvvm.ComponentModel;

namespace Syncora.Client.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    public string UserName { get; }
    public string WelcomeMessage => $"Добро пожаловать, {UserName}!";

    public DashboardViewModel(string userName)
    {
        UserName = userName;
    }
}

