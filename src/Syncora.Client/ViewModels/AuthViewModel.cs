using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels
{
    public partial class AuthViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HeaderTitle))]
        [NotifyPropertyChangedFor(nameof(SubmitButtonText))]
        [NotifyPropertyChangedFor(nameof(ToggleModeText))]
        private bool isLoginMode = true;

        public string HeaderTitle => IsLoginMode ? "Вход" : "Регистрация";
        public string SubmitButtonText => IsLoginMode ? "Войти" : "Зарегистрироваться";
        public string ToggleModeText => IsLoginMode ? "Нет аккаунта? Зарегистрироваться" : "Уже есть аккаунт? Войти";

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [RelayCommand]
        private void ToggleMode()
        {
            IsLoginMode = !IsLoginMode;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task SubmitAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Email и пароль обязательны.";
                return;
            }

            if (!IsLoginMode && Password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают.";
                return;
            }

            // TODO: Implement actual authentication via REST API Service
            await Task.Delay(500); // Simulate network call

            // Simulate navigation to main app for now
        }
    }
}
