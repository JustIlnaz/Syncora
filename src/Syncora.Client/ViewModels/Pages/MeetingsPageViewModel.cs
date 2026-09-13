using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncora.Client.Models.Meeting;
using Syncora.Client.Models.User;
using Syncora.Client.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels.Pages;

public partial class MeetingsPageViewModel : ViewModelBase
{
    private readonly MeetingService _meetingService;
    private readonly UserProfileService _userProfileService;

    // ── Список встреч ──────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<MeetingDto> meetings = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string successMessage = string.Empty;

    [ObservableProperty]
    private string selectedTab = "upcoming"; // upcoming | past | all

    // ── Правая панель «Сформировать встречу» ───────────────────────
    [ObservableProperty]
    private bool isSearchPanelOpen;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private string searchTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ParticipantItemViewModel> availableContacts = new();

    [ObservableProperty]
    private ObservableCollection<ParticipantItemViewModel> selectedParticipants = new();

    [ObservableProperty]
    private int selectedDurationMinutes = 60;

    [ObservableProperty]
    private string selectedDurationText = "1 час";

    [ObservableProperty]
    private DateTime? searchDateFrom = DateTime.Today;

    [ObservableProperty]
    private DateTime? searchDateTo = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private TimeSpan searchTimeFrom = new(9, 0, 0);

    [ObservableProperty]
    private TimeSpan searchTimeTo = new(21, 0, 0);

    [ObservableProperty]
    private ObservableCollection<MeetingSlotViewModel> foundSlots = new();

    [ObservableProperty]
    private MeetingSlotViewModel? selectedSlot;

    // Пресеты длительности (ТЗ §9.2)
    public List<DurationPreset> DurationPresets { get; } = new()
    {
        new(15, "15 минут"),
        new(30, "30 минут"),
        new(45, "45 минут"),
        new(60, "1 час"),
        new(90, "1,5 часа"),
        new(120, "2 часа"),
        new(180, "3 часа"),
        new(240, "4 часа"),
    };

    public MeetingsPageViewModel(MeetingService meetingService, UserProfileService userProfileService)
    {
        _meetingService = meetingService;
        _userProfileService = userProfileService;
        _ = LoadMeetingsAsync();
        _ = LoadContactsAsync();
    }

    // ── Загрузка данных ────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadMeetingsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var list = await _meetingService.GetMyMeetingsAsync();
            Meetings = new ObservableCollection<MeetingDto>(
                list.OrderByDescending(m => m.CreatedAt));
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось загрузить встречи."; }
        finally { IsLoading = false; }
    }

    private async Task LoadContactsAsync()
    {
        try
        {
            var contacts = await _userProfileService.GetContactsAsync();
            AvailableContacts = new ObservableCollection<ParticipantItemViewModel>(
                contacts.Select(c => new ParticipantItemViewModel
                {
                    UserId = c.UserId,
                    Name = c.Name,
                    Email = c.Email,
                    AvatarUrl = c.AvatarUrl,
                    IsSelected = false
                }));
        }
        catch { /* контакты могут быть пустыми */ }
    }

    // ── Панель поиска ─────────────────────────────────────────────

    [RelayCommand]
    private void OpenSearchPanel()
    {
        IsSearchPanelOpen = true;
        SearchTitle = string.Empty;
        FoundSlots.Clear();
        SelectedSlot = null;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private void CloseSearchPanel()
    {
        IsSearchPanelOpen = false;
    }

    [RelayCommand]
    private void ToggleParticipant(ParticipantItemViewModel? item)
    {
        if (item == null) return;
        item.IsSelected = !item.IsSelected;

        if (item.IsSelected)
            SelectedParticipants.Add(item);
        else
            SelectedParticipants.Remove(item);
    }

    [RelayCommand]
    private void SelectDuration(DurationPreset? preset)
    {
        if (preset == null) return;
        SelectedDurationMinutes = preset.Minutes;
        SelectedDurationText = preset.Label;
    }

    [RelayCommand]
    private async Task SearchSlotsAsync()
    {
        if (SelectedParticipants.Count < 1)
        {
            ErrorMessage = "Выберите хотя бы одного участника (кроме себя).";
            return;
        }

        if (SelectedParticipants.Count > 9)
        {
            ErrorMessage = "Максимум 10 участников (включая вас).";
            return;
        }

        IsSearching = true;
        ErrorMessage = string.Empty;
        FoundSlots.Clear();
        SelectedSlot = null;

        try
        {
            var fromDate = (SearchDateFrom ?? DateTime.Today).Date;
            var toDate = (SearchDateTo ?? fromDate.AddDays(1)).Date;
            var fromLocal = fromDate + SearchTimeFrom;
            var toLocal = toDate + SearchTimeTo;

            var request = new MeetingSearchRequest
            {
                ParticipantIds = SelectedParticipants.Select(p => p.UserId).ToList(),
                DurationMinutes = SelectedDurationMinutes,
                From = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime(),
                To = DateTime.SpecifyKind(toLocal, DateTimeKind.Local).ToUniversalTime()
            };

            var result = await _meetingService.SearchSlotsAsync(request);

            var slots = result.Slots
                .Select(s => new MeetingSlotViewModel
                {
                    Start = s.Start.Kind == DateTimeKind.Utc ? s.Start.ToLocalTime() : s.Start,
                    End = s.End.Kind == DateTimeKind.Utc ? s.End.ToLocalTime() : s.End
                })
                .Take(5)
                .ToList();

            foreach (var s in slots)
                FoundSlots.Add(s);

            if (slots.Count == 0)
                ErrorMessage = "Не найдено подходящих слотов. Попробуйте изменить параметры.";
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Ошибка поиска. Проверьте подключение."; }
        finally { IsSearching = false; }
    }

    [RelayCommand]
    private void SelectSlot(MeetingSlotViewModel? slot)
    {
        if (slot == null) return;
        foreach (var s in FoundSlots)
            s.IsSelected = false;
        slot.IsSelected = true;
        SelectedSlot = slot;
    }

    [RelayCommand]
    private async Task ConfirmMeetingAsync()
    {
        if (SelectedSlot == null)
        {
            ErrorMessage = "Выберите слот для встречи.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchTitle))
        {
            ErrorMessage = "Введите название встречи.";
            return;
        }

        IsSearching = true;
        ErrorMessage = string.Empty;

        try
        {
            await _meetingService.CreateMeetingAsync(new CreateMeetingRequest
            {
                Title = SearchTitle.Trim(),
                ParticipantIds = SelectedParticipants.Select(p => p.UserId).ToList(),
                Start = DateTime.SpecifyKind(SelectedSlot.Start, DateTimeKind.Local).ToUniversalTime(),
                End = DateTime.SpecifyKind(SelectedSlot.End, DateTimeKind.Local).ToUniversalTime()
            });

            SuccessMessage = "Встреча создана!";
            IsSearchPanelOpen = false;
            await LoadMeetingsAsync();
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось создать встречу."; }
        finally { IsSearching = false; }
    }

    // ── Действия с встречами ───────────────────────────────────────

    [RelayCommand]
    private async Task AcceptMeetingAsync(MeetingDto? meeting)
    {
        if (meeting == null) return;
        try
        {
            await _meetingService.RespondAsync(meeting.Id, "accepted");
            await LoadMeetingsAsync();
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task DeclineMeetingAsync(MeetingDto? meeting)
    {
        if (meeting == null) return;
        try
        {
            await _meetingService.RespondAsync(meeting.Id, "declined");
            await LoadMeetingsAsync();
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task DeleteMeetingAsync(MeetingDto? meeting)
    {
        if (meeting == null) return;
        try
        {
            await _meetingService.DeleteMeetingAsync(meeting.Id);
            SuccessMessage = "Встреча удалена.";
            await LoadMeetingsAsync();
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }
}

// ── Вспомогательные VM ────────────────────────────────────────────

public partial class ParticipantItemViewModel : ObservableObject
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    [ObservableProperty]
    private bool isSelected;
}

public partial class MeetingSlotViewModel : ObservableObject
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string TimeText => $"{Start:HH:mm} – {End:HH:mm}";
    public string DateText => Start.ToString("d MMMM, dddd", new CultureInfo("ru-RU"));

    [ObservableProperty]
    private bool isSelected;
}

public record DurationPreset(int Minutes, string Label);
