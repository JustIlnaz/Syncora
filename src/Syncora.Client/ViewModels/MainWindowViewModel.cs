using CommunityToolkit.Mvvm.ComponentModel;

namespace Syncora.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Syncora";
    [ObservableProperty]
    private ObservableObject currentView;

    public MainWindowViewModel()
    {
        CurrentView = new AuthViewModel();
    }
}
