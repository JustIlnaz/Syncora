using CommunityToolkit.Mvvm.ComponentModel;
using Syncora.Client.ViewModels;

namespace Syncora.Client.ViewModels.Navigation;

public partial class NavItemViewModel : ViewModelBase
{
    public AppSection Section { get; }
    public string Title { get; }
    public string IconData { get; }

    [ObservableProperty]
    private bool isActive;

    public NavItemViewModel(AppSection section, string title, string iconData, bool isActive = false)
    {
        Section = section;
        Title = title;
        IconData = iconData;
        IsActive = isActive;
    }
}
