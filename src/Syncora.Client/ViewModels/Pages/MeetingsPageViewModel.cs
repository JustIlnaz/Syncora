using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Messages;
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

public partial class MeetingsPageViewModel : ViewModelBase,
    IRecipient<OpenMeetingEditMessage>
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

    private Guid currentUserId;

    public Guid CurrentUserId
    {
        get => currentUserId;
        private set
        {
            if (currentUserId == value) return;
            currentUserId = value;
            UpdateMeetingPermissions();
            OnPropertyChanged();
        }
    }

    private bool isEditMeetingOpen;
    public bool IsEditMeetingOpen
    {
        get => isEditMeetingOpen;
        set => SetProperty(ref isEditMeetingOpen, value);
    }

    private MeetingDto? meetingBeingEdited;
    public MeetingDto? MeetingBeingEdited
    {
        get => meetingBeingEdited;
        set => SetProperty(ref meetingBeingEdited, value);
    }

    private string editMeetingTitle = string.Empty;
    public string EditMeetingTitle
    {
        get => editMeetingTitle;
        set => SetProperty(ref editMeetingTitle, value);
    }

    private string editMeetingDescription = string.Empty;
    public string EditMeetingDescription
    {
        get => editMeetingDescription;
        set => SetProperty(ref editMeetingDescription, value);
    }

    // ── Правая панель «Сформировать встречу» ──────────────────
    [ObservableProperty]
    private bool isSearchPanelOpen;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private string searchTitle = string.Empty;

    private string searchDescription = string.Empty;
    public string SearchDescription
    {
        get => searchDescription;
        set => SetProperty(ref searchDescription, value);
    }

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
        WeakReferenceMessenger.Default.Register<OpenMeetingEditMessage>(this);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Сначала профиль — чтобы права (CanRespond/IsCurrentUserCreator) были корректны,
        // затем параллельно встречи и контакты.
        await LoadCurrentUserAsync();
        await Task.WhenAll(LoadMeetingsAsync(), LoadContactsAsync());
    }

    private async Task LoadCurrentUserAsync()
    {
        try
        {
            var profile = await _userProfileService.GetProfileAsync();
            CurrentUserId = profile.Id;
        }
        catch { /* кнопки останутся доступны, если профиль не загрузился */ }
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
            UpdateMeetingPermissions();
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch { ErrorMessage = "Не удалось загрузить встречи."; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
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

            SelectedParticipants = new ObservableCollection<ParticipantItemViewModel>(
                AvailableContacts.Where(c => SelectedParticipants.Any(p => p.UserId == c.UserId)));
            foreach (var selected in SelectedParticipants)
                selected.IsSelected = true;
        }
        catch { /* контакты могут быть пустыми */ }
    }

    // ── Панель поиска ─────────────────────────────────────────────

    [RelayCommand]
    private void OpenSearchPanel()
    {
        IsSearchPanelOpen = true;
        SearchTitle = string.Empty;
        SearchDescription = string.Empty;
        FoundSlots.Clear();
        SelectedSlot = null;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        foreach (var participant in SelectedParticipants)
            participant.IsSelected = false;
        SelectedParticipants.Clear();
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

        var validParticipants = AvailableContacts
            .Where(c => SelectedParticipants.Any(p => p.UserId == c.UserId))
            .ToList();
        if (validParticipants.Count != SelectedParticipants.Count)
            SelectedParticipants = new ObservableCollection<ParticipantItemViewModel>(validParticipants);

        if (SelectedParticipants.Count < 1)
        {
            IsSearching = false;
            ErrorMessage = "Выбранные участники больше не существуют. Выберите участников ещё раз.";
            return;
        }

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
                Description = string.IsNullOrWhiteSpace(SearchDescription) ? null : SearchDescription.Trim(),
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
            SuccessMessage = "Приглашение принято. Встреча появилась в вашем календаре.";
            await LoadMeetingsAsync();
        }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void OpenEditMeeting(MeetingDto? meeting)
    {
        if (meeting == null || !meeting.IsCurrentUserCreator) return;

        MeetingBeingEdited = meeting;
        EditMeetingTitle = meeting.Title;
        EditMeetingDescription = meeting.Description ?? string.Empty;
        IsEditMeetingOpen = true;
    }

    [RelayCommand]
    private void CloseEditMeeting() => IsEditMeetingOpen = false;

    [RelayCommand]
    private async Task SaveMeetingAsync()
    {
        var meeting = MeetingBeingEdited;
        if (meeting == null || !meeting.IsCurrentUserCreator) return;
        if (string.IsNullOrWhiteSpace(EditMeetingTitle))
        {
            ErrorMessage = "Введите название встречи.";
            return;
        }

        try
        {
            await _meetingService.UpdateMeetingAsync(meeting.Id, new UpdateMeetingRequest
            {
                Title = EditMeetingTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(EditMeetingDescription)
                    ? null
                    : EditMeetingDescription.Trim(),
                Start = meeting.SelectedSlotStart!.Value,
                End = meeting.SelectedSlotEnd!.Value
            });

            IsEditMeetingOpen = false;
            SuccessMessage = "Встреча изменена.";
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
            SuccessMessage = "Приглашение отклонено.";
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

    private void UpdateMeetingPermissions()
    {
        foreach (var meeting in Meetings)
        {
            meeting.IsCurrentUserCreator = meeting.CreatorId == CurrentUserId;
            var participant = meeting.Participants.FirstOrDefault(p => p.UserId == CurrentUserId);
            meeting.CanRespond = !meeting.IsCurrentUserCreator && participant?.Status == "pending";
        }
    }

    public async void Receive(OpenMeetingEditMessage message)
    {
        var meeting = Meetings.FirstOrDefault(m => m.Id == message.MeetingId);
        if (meeting == null)
        {
            // Список мог ещё не загрузиться — пробуем обновить и найти снова.
            await LoadMeetingsAsync();
            meeting = Meetings.FirstOrDefault(m => m.Id == message.MeetingId);
        }

        if (meeting != null)
            OpenEditMeeting(meeting);
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
