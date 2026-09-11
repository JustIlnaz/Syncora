using Avalonia;
using Avalonia.Controls;
using Syncora.Client.ViewModels;
using System;
using System.ComponentModel;

namespace Syncora.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Opened += (_, _) => CenterOnScreen();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyWindowSize(viewModel.CurrentView);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainWindowViewModel viewModel
            && e.PropertyName == nameof(MainWindowViewModel.CurrentView))
        {
            ApplyWindowSize(viewModel.CurrentView);
        }
    }

    private void ApplyWindowSize(object? currentView)
    {
        switch (currentView)
        {
            case AuthViewModel:
                Width = 520;
                Height = 900;
                MinWidth = 480;
                MinHeight = 860;
                break;

            case DashboardViewModel:
                Width = 520;
                Height = 420;
                MinWidth = 420;
                MinHeight = 380;
                break;

            case AppShellViewModel:
                Width = 1280;
                Height = 820;
                MinWidth = 720;
                MinHeight = 600;
                break;

            default:
                Width = 1000;
                Height = 650;
                MinWidth = 800;
                MinHeight = 600;
                break;
        }

        CenterOnScreen();
    }

    private void CenterOnScreen()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen == null)
            return;

        var area = screen.WorkingArea;
        var x = area.X + Math.Max(0, (area.Width - (int)Width) / 2);
        var y = area.Y + Math.Max(0, (area.Height - (int)Height) / 2);
        Position = new PixelPoint(x, y);
    }
}
