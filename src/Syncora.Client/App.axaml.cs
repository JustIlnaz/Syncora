using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Net.Http;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Syncora.Client.Services;
using Syncora.Client.ViewModels;
using Syncora.Client.ViewModels.Pages;
using Syncora.Client.Views;

namespace Syncora.Client;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            var services = new ServiceCollection();

            services.AddSingleton<HttpClient>();
            services.AddSingleton<ApiClient>();
            services.AddSingleton<AuthSessionStore>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<UserProfileService>();
            services.AddSingleton<CalendarService>();
            services.AddSingleton<EventService>();
            services.AddSingleton<MeetingService>();
            services.AddSingleton<ShoppingService>();
            services.AddSingleton<NotificationService>();

            services.AddTransient<AuthViewModel>();
            services.AddSingleton<MainWindowViewModel>(sp =>
                new MainWindowViewModel(
                    sp.GetRequiredService<AuthViewModel>(),
                    sp.GetRequiredService<AuthSessionStore>(),
                    sp.GetRequiredService<ApiClient>(),
                    sp));

            // Page ViewModels
            services.AddTransient<CalendarPageViewModel>();
            services.AddTransient<MeetingsPageViewModel>();
            services.AddTransient<ShoppingListsPageViewModel>();
            services.AddTransient<PeoplePageViewModel>();
            services.AddTransient<SettingsPageViewModel>();
            services.AddTransient<ProfilePageViewModel>();

            var provider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);

            desktop.MainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}