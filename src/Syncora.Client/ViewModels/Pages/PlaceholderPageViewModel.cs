using Syncora.Client.ViewModels;

namespace Syncora.Client.ViewModels.Pages;

public class PlaceholderPageViewModel : ViewModelBase
{
    public string Title { get; }
    public string Description { get; }

    public PlaceholderPageViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }
}
