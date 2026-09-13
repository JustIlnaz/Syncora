using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Helpers;
using Syncora.Client.Messages;
using Syncora.Client.Models.User;
using Syncora.Client.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels.Pages;

public partial class ProfilePageViewModel : ViewModelBase
{
    private readonly UserProfileService _profileService;
    private readonly AuthSessionStore _sessionStore;
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserInitials))]
    [NotifyPropertyChangedFor(nameof(HasAvatar))]
    [NotifyPropertyChangedFor(nameof(AvatarDisplayUrl))]
    private string? avatarUrl;

    [ObservableProperty]
    private Bitmap? avatarImage;

    [ObservableProperty]
    private int avatarVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserInitials))]
    private string name = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string timezone = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string successMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isAvatarBusy;

    [ObservableProperty]
    private bool isDeleting;

    [ObservableProperty]
    private bool showDeleteConfirmation;

    public string UserInitials => GetInitials(Name);
    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public string? AvatarDisplayUrl => HasAvatar
        ? $"{AvatarUrlHelper.ToAbsolute(AvatarUrl)}?v={AvatarVersion}"
        : null;

    public ProfilePageViewModel(
        string userName,
        string email,
        UserProfileService profileService,
        AuthSessionStore sessionStore,
        ApiClient apiClient)
    {
        Name = userName;
        Email = email;
        Timezone = TimeZoneInfo.Local.Id;
        _profileService = profileService;
        _sessionStore = sessionStore;
        _apiClient = apiClient;

        _ = LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            ApplyProfile(await _profileService.GetProfileAsync());
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось загрузить профиль.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task UploadAvatarAsync(Stream stream, string fileName)
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsAvatarBusy = true;

        try
        {
            var updated = await _profileService.UploadAvatarAsync(
                stream,
                fileName,
                GetContentType(fileName));

            ApplyProfile(updated);
            AvatarVersion++;
            AvatarImageLoader.Invalidate(updated.AvatarUrl);
            await RefreshAvatarImageAsync();
            SuccessMessage = "Аватар обновлён.";
            NotifyProfileUpdated();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось загрузить аватар.";
        }
        finally
        {
            IsAvatarBusy = false;
        }
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
            ApplyProfile(updated);
            AvatarVersion++;
            await RefreshAvatarImageAsync();
            SuccessMessage = "Аватар удалён.";
            NotifyProfileUpdated();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось удалить аватар.";
        }
        finally
        {
            IsAvatarBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Имя не может быть пустым.";
            return;
        }

        IsLoading = true;

        try
        {
            var updated = await _profileService.UpdateProfileAsync(new UpdateUserProfileRequest
            {
                Name = Name.Trim(),
                Timezone = string.IsNullOrWhiteSpace(Timezone) ? null : Timezone.Trim()
            });

            ApplyProfile(updated);
            SuccessMessage = "Профиль успешно обновлён.";
            NotifyProfileUpdated();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось сохранить профиль.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        LogoutLocally();
    }

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
            LogoutLocally();
        }
        catch (ApiException ex)
        {
            ShowDeleteConfirmation = false;
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ShowDeleteConfirmation = false;
            ErrorMessage = "Не удалось удалить профиль.";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    private void ApplyProfile(UserProfileDto profile)
    {
        Name = profile.Name;
        Email = profile.Email;
        Timezone = profile.Timezone ?? TimeZoneInfo.Local.Id;
        AvatarUrl = profile.AvatarUrl;
        _ = RefreshAvatarImageAsync();
    }

    private async Task RefreshAvatarImageAsync()
    {
        var image = await AvatarImageLoader.LoadAsync(AvatarUrl, AvatarVersion);
        AvatarImage = image;
    }

    private void NotifyProfileUpdated()
    {
        UpdateSavedSession(Name, Email);
        WeakReferenceMessenger.Default.Send(new ProfileUpdatedMessage(Name, Email, AvatarUrl));
    }

    private void UpdateSavedSession(string userName, string email)
    {
        var session = _sessionStore.Load();
        if (session == null || !session.RememberMe)
            return;

        session.UserName = userName;
        session.Email = email;
        _sessionStore.Save(session);
    }

    private void LogoutLocally()
    {
        _sessionStore.Clear();
        _apiClient.ClearToken();
        WeakReferenceMessenger.Default.Send(new LogoutMessage());
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
