using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncora.Client.Models.Calendar;
using Syncora.Client.Models.User;
using Syncora.Client.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels.Pages;

public partial class PeoplePageViewModel : ViewModelBase
{
    private readonly UserProfileService _userProfileService;
    private readonly CalendarService _calendarService;

    // ── Контакты ───────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<ContactDto> contacts = new();

    [ObservableProperty]
    private bool isLoadingContacts;

    // ── Поиск пользователей ────────────────────────────────────────
    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<UserSearchResultDto> searchResults = new();

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private string searchError = string.Empty;

    // ── Общие сообщения ────────────────────────────────────────────
    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string successMessage = string.Empty;

    // ── Добавление в календарь ─────────────────────────────────────
    [ObservableProperty]
    private bool isAddToCalendarOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddToCalendarTitle))]
    private UserSearchResultDto? selectedUser;

    [ObservableProperty]
    private ObservableCollection<CalendarDto> calendars = new();

    [ObservableProperty]
    private CalendarDto? selectedCalendar;

    [ObservableProperty]
    private string selectedAccessLevel = "edit";

    public List<string> AccessLevels { get; } = new() { "view", "edit", "free-busy" };

    public string AddToCalendarTitle => SelectedUser == null
        ? "Добавить в календарь"
        : $"Добавить {SelectedUser.Name} в календарь";

    // ── Добавление контакта ────────────────────────────────────────
    [ObservableProperty]
    private bool isAddContactOpen;

    [ObservableProperty]
    private string newContactEmail = string.Empty;

    [ObservableProperty]
    private string newContactNickname = string.Empty;

    public PeoplePageViewModel(UserProfileService userProfileService, CalendarService calendarService)
    {
        _userProfileService = userProfileService;
        _calendarService = calendarService;
        _ = LoadContactsAsync();
        _ = LoadCalendarsAsync();
    }

    // ── Загрузка ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadContactsAsync()
    {
        IsLoadingContacts = true;
        try
        {
            var result = await _userProfileService.GetContactsAsync();
            Contacts = new ObservableCollection<ContactDto>(result);
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось загрузить контакты."; }
        finally { IsLoadingContacts = false; }
    }

    private async Task LoadCalendarsAsync()
    {
        try
        {
            var result = await _calendarService.GetCalendarsAsync();
            Calendars = new ObservableCollection<CalendarDto>(
                result.Where(c => c.Type == "group" || c.Type == "GROUP"));
        }
        catch { /* игнорируем */ }
    }

    // ── Поиск ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
        {
            SearchResults.Clear();
            return;
        }

        IsSearching = true;
        SearchError = string.Empty;
        try
        {
            var result = await _userProfileService.SearchUsersAsync(SearchQuery.Trim());
            SearchResults = new ObservableCollection<UserSearchResultDto>(result);
        }
        catch (ApiException ex) { SearchError = ex.Message; }
        catch { SearchError = "Ошибка поиска."; }
        finally { IsSearching = false; }
    }

    // ── Добавление контакта ────────────────────────────────────────

    [RelayCommand]
    private void OpenAddContact()
    {
        NewContactEmail = string.Empty;
        NewContactNickname = string.Empty;
        IsAddContactOpen = true;
    }

    [RelayCommand]
    private void CloseAddContact() => IsAddContactOpen = false;

    [RelayCommand]
    private async Task AddContactAsync()
    {
        if (string.IsNullOrWhiteSpace(NewContactEmail))
        {
            ErrorMessage = "Введите email.";
            return;
        }

        try
        {
            await _userProfileService.AddContactAsync(new AddContactRequest
            {
                Email = NewContactEmail.Trim(),
                Nickname = string.IsNullOrWhiteSpace(NewContactNickname) ? null : NewContactNickname.Trim()
            });
            IsAddContactOpen = false;
            SuccessMessage = "Контакт добавлен.";
            await LoadContactsAsync();
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось добавить контакт."; }
    }

    // ── Добавление в календарь ─────────────────────────────────────

    [RelayCommand]
    private void OpenAddToCalendar(UserSearchResultDto? user)
    {
        if (user == null) return;
        SelectedUser = user;
        SelectedCalendar = Calendars.FirstOrDefault();
        SelectedAccessLevel = "edit";
        IsAddToCalendarOpen = true;
    }

    [RelayCommand]
    private void CloseAddToCalendar() => IsAddToCalendarOpen = false;

    [RelayCommand]
    private async Task AddToCalendarAsync()
    {
        if (SelectedUser == null || SelectedCalendar == null) return;

        try
        {
            await _calendarService.AddMemberAsync(SelectedCalendar.Id, new AddCalendarMemberRequest
            {
                Email = SelectedUser.Email,
                Role = "member",
                AccessLevel = SelectedAccessLevel
            });
            IsAddToCalendarOpen = false;
            SuccessMessage = $"{SelectedUser.Name} добавлен в календарь «{SelectedCalendar.Name}».";
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось добавить участника в календарь."; }
    }

    // ── Удаление контакта ──────────────────────────────────────────

    [RelayCommand]
    private async Task RemoveContactAsync(ContactDto? contact)
    {
        if (contact == null) return;
        try
        {
            await _userProfileService.RemoveContactAsync(contact.Id);
            Contacts.Remove(contact);
            SuccessMessage = "Контакт удалён.";
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }
}
