using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Messages;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly string _userName;
    private readonly string _email;
    private readonly string _token;

    public string UserName => _userName;
    public string WelcomeMessage => $"Добро пожаловать, {_userName}!";

    [ObservableProperty]
    private string loadingMessage = "Подготавливаем ваше расписание...";

    [ObservableProperty]
    private double loadingProgress;

    public DashboardViewModel(string userName, string email, string token)
    {
        _userName = userName;
        _email = email;
        _token = token;
        _ = RunLoadingAsync();
    }

    private async Task RunLoadingAsync()
    {
        LoadingProgress = 0;

        LoadingMessage = "Подготавливаем ваше расписание...";
        await AnimateProgressAsync(0, 35, 600);

        LoadingMessage = "Загружаем календарь...";
        await AnimateProgressAsync(35, 70, 700);

        LoadingMessage = "Синхронизируем события...";
        await AnimateProgressAsync(70, 100, 500);

        await Task.Delay(300);

        WeakReferenceMessenger.Default.Send(
            new NavigateToCalendarMessage(_userName, _email, _token));
    }

    private async Task AnimateProgressAsync(double from, double to, int durationMs)
    {
        const int steps = 20;
        var stepDelay = durationMs / steps;
        var stepSize = (to - from) / steps;

        for (var i = 1; i <= steps; i++)
        {
            LoadingProgress = from + stepSize * i;
            await Task.Delay(stepDelay);
        }

        LoadingProgress = to;
    }
}
