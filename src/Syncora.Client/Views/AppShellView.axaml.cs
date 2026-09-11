using Avalonia;
using Avalonia.Controls;
using Syncora.Client.ViewModels;

namespace Syncora.Client.Views;

public partial class AppShellView : UserControl
{
    public AppShellView()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if (DataContext is AppShellViewModel viewModel)
            viewModel.UpdateLayoutForWidth(e.NewSize.Width);
    }
}
