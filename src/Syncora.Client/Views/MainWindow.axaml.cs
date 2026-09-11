using System;
using System.ComponentModel;
using Avalonia.Controls;
using Syncora.Client.ViewModels;

namespace Syncora.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
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
        if (currentView is AuthViewModel)
        {
            Width = 520;
            Height = 900;
            MinWidth = 480;
            MinHeight = 860;
        }
        else
        {
            Width = 1000;
            Height = 650;
            MinWidth = 800;
            MinHeight = 600;
        }
    }
}
