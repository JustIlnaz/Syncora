using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Helpers;
using Syncora.Client.Messages;
using Syncora.Client.Models.User;
using Syncora.Client.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels.Pages;

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly UserProfileService _profileService;
    private readonly AuthSessionStore _sessionStore;
    private readonly ApiClient _apiClient;

    // ── Профиль ────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserInitials))]
    private string userName = string.Empty;

    [ObservableProperty]
    private string userEmail = string.Empty;

    [ObservableProperty]
    private string? timezone;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAvatar))]
    [NotifyPropertyChangedFor(nameof(UserInitials))]
    private string? avatarUrl;

    [ObservableProperty]
    private Bitmap? avatarImage;

    [ObservableProperty]
    private int avatarVersion;

    [ObservableProperty]
    private bool isAvatarBusy;

    [ObservableProperty]
    private bool isDeleting;

    [ObservableProperty]
    private bool showDeleteConfirmation;

    public string UserInitials => GetInitials(UserName);
    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);

    // ── Рабочие часы ───────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<WorkingHoursItemViewModel> workingHours = new();

    // ── Статус ─────────────────────────────────────────────────────
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string successMessage = string.Empty;

    // Часовые пояса (основные)
    public List<string> AvailableTimezones { get; } = new()
    {
        "Europe/Kaliningrad", "Europe/Moscow", "Europe/Samara",
        "Asia/Yekaterinburg", "Asia/Omsk", "Asia/Krasnoyarsk",
        "Asia/Irkutsk", "Asia/Yakutsk", "Asia/Vladivostok",
        "Asia/Magadan", "Asia/Kamchatka", "UTC"
    };

    public SettingsPageViewModel(
        UserProfileService profileService,
        AuthSessionStore sessionStore,
        ApiClient apiClient)
    {
        _profileService = profileService;
        _sessionStore = sessionStore;
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var profile = await _profileService.GetProfileAsync();
            UserName = profile.Name;
            UserEmail = profile.Email;
            AvatarUrl = profile.AvatarUrl;
            AvatarVersion++;
            await RefreshAvatarImageAsync();
            Timezone = profile.Timezone ?? "Europe/Moscow";

            var hours = await _profileService.GetWorkingHoursAsync();
            WorkingHours = new ObservableCollection<WorkingHoursItemViewModel>(
                hours.Select(h => new WorkingHoursItemViewModel
                {
                    DayOfWeek = h.DayOfWeek,
                    DayName = h.DayName,
                    StartTime = h.StartTime,
                    EndTime = h.EndTime,
                    IsWorkingDay = h.IsWorkingDay
                }));
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось загрузить настройки."; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            ErrorMessage = "Имя не может быть пустым.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var updated = await _profileService.UpdateProfileAsync(new UpdateUserProfileRequest
            {
                Name = UserName.Trim(),
                Timezone = Timezone
            });

            SuccessMessage = "Профиль сохранён.";
            WeakReferenceMessenger.Default.Send(new ProfileUpdatedMessage(
                updated.Name, updated.Email, updated.AvatarUrl));
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось сохранить профиль."; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveWorkingHoursAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            await _profileService.UpdateWorkingHoursAsync(new UpdateWorkingHoursRequest
            {
                Items = WorkingHours.Select(h => new WorkingHoursItemDto
                {
                    DayOfWeek = h.DayOfWeek,
                    StartTime = h.StartTime,
                    EndTime = h.EndTime,
                    IsWorkingDay = h.IsWorkingDay
                }).ToList()
            });
            SuccessMessage = "Рабочие часы сохранены.";
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось сохранить рабочие часы."; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Logout()
    {
        _sessionStore.Clear();
        _apiClient.ClearToken();
        WeakReferenceMessenger.Default.Send(new LogoutMessage());
    }

    // ── Аватар ─────────────────────────────────────────────────────

    public async Task UploadAvatarAsync(Stream stream, string fileName)
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsAvatarBusy = true;

        try
        {
            var updated = await _profileService.UploadAvatarAsync(
                stream, fileName, GetContentType(fileName));

            AvatarUrl = updated.AvatarUrl;
            AvatarVersion++;
            AvatarImageLoader.Invalidate(updated.AvatarUrl);
            await RefreshAvatarImageAsync();
            SuccessMessage = "Аватар обновлён.";
            NotifyProfileUpdated(updated);
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось загрузить аватар."; }
        finally { IsAvatarBusy = false; }
    }

    [RelayCommand]
    private async Task RemoveAvatarAsync()
    {
        if (!HasAvatar)
            return;

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsAvatarBusy = true;

        try
        {
            var updated = await _profileService.DeleteAvatarAsync();
            AvatarUrl = updated.AvatarUrl;
            AvatarVersion++;
            await RefreshAvatarImageAsync();
            SuccessMessage = "Аватар удалён.";
            NotifyProfileUpdated(updated);
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось удалить аватар."; }
        finally { IsAvatarBusy = false; }
    }

    // ── Удаление профиля ───────────────────────────────────────────

    [RelayCommand]
    private void RequestDelete()
    {
        ShowDeleteConfirmation = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        ShowDeleteConfirmation = false;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsDeleting = true;

        try
        {
            await _profileService.DeleteProfileAsync();
            _sessionStore.Clear();
            _apiClient.ClearToken();
            WeakReferenceMessenger.Default.Send(new LogoutMessage());
        }
        catch (ApiException ex)
        {
            ShowDeleteConfirmation = false;
            ErrorMessage = ex.Message;
        }
        catch
        {
            ShowDeleteConfirmation = false;
            ErrorMessage = "Не удалось удалить профиль.";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    // ── Вспомогательные ────────────────────────────────────────────

    private async Task RefreshAvatarImageAsync()
    {
        AvatarImage = await AvatarImageLoader.LoadAsync(AvatarUrl, AvatarVersion);
    }

    private void NotifyProfileUpdated(UserProfileDto profile)
    {
        var session = _sessionStore.Load();
        if (session != null && session.RememberMe)
        {
            session.UserName = profile.Name;
            session.Email = profile.Email;
            _sessionStore.Save(session);
        }

        WeakReferenceMessenger.Default.Send(new ProfileUpdatedMessage(
            profile.Name, profile.Email, profile.AvatarUrl));
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
            : name[..Math.Min(2, name.Length)].ToUpperInvariant();
    }
}

public partial class WorkingHoursItemViewModel : ObservableObject
{
    public short DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;

    [ObservableProperty]
    private TimeSpan startTime = new(9, 0, 0);

    [ObservableProperty]
    private TimeSpan endTime = new(18, 0, 0);

    [ObservableProperty]
    private bool isWorkingDay = true;
}
