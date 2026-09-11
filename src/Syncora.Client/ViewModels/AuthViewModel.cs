using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Messages;
using Syncora.Client.Models.Auth;
using Syncora.Client.Services;
using System;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels
{
    public partial class AuthViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly AuthSessionStore _sessionStore;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HeaderTitle))]
        [NotifyPropertyChangedFor(nameof(Subtitle))]
        [NotifyPropertyChangedFor(nameof(SubmitButtonText))]
        [NotifyPropertyChangedFor(nameof(ToggleModeText))]
        private bool isLoginMode = true;

        public string HeaderTitle => IsLoginMode ? "Добро пожаловать!" : "Создайте аккаунт";
        public string Subtitle => IsLoginMode
            ? "Войдите в свой аккаунт, чтобы продолжить работу с расписанием, списками и планами."
            : "Это займёт меньше минуты.";

        public string SubmitButtonText => IsLoginMode ? "Войти" : "Зарегистрироваться";
        public string ToggleModeText => IsLoginMode
            ? "Нет аккаунта? Зарегистрироваться"
            : "Уже есть аккаунт? Войти";

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool rememberMe;

        public AuthViewModel(AuthService authService, AuthSessionStore sessionStore)
        {
            _authService = authService;
            _sessionStore = sessionStore;
            RestoreSavedSession();
        }

        private void RestoreSavedSession()
        {
            var session = _sessionStore.Load();
            if (session == null)
                return;

            Email = session.Email;
            RememberMe = session.RememberMe;
        }

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

            if (!IsLoginMode)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    ErrorMessage = "Имя обязательно.";
                    return;
                }

                if (Password.Length < 6)
                {
                    ErrorMessage = "Пароль должен быть минимум 6 символов.";
                    return;
                }

                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "Пароли не совпадают.";
                    return;
                }
            }

            IsLoading = true;

            try
            {
                LoginResponse response;

                if (IsLoginMode)
                {
                    response = await _authService.LoginAsync(Email, Password);
                    PersistSession(response);
                }
                else
                {
                    response = await _authService.RegisterAsync(Name, Email, Password);
                    _sessionStore.Clear();
                }

                WeakReferenceMessenger.Default.Send(
                    new NavigateToMainMessage(response.Name, response.Email, response.Token));
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "Не удалось подключиться к серверу.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PersistSession(LoginResponse response)
        {
            if (RememberMe)
            {
                _sessionStore.Save(new SavedSession
                {
                    RememberMe = true,
                    Email = response.Email,
                    UserName = response.Name,
                    Token = response.Token
                });
            }
            else
            {
                _sessionStore.Clear();
            }
        }
    }
}
