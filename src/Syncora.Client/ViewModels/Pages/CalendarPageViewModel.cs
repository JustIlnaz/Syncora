using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Syncora.Client.Messages;
using Syncora.Client.Models.Calendar;
using Syncora.Client.Models.Event;
using Syncora.Client.Services;
using Syncora.Client.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels.Pages;

public partial class CalendarPageViewModel : ViewModelBase
{
    private readonly CalendarService _calendarService;
    private readonly EventService _eventService;
    private readonly UserProfileService _userProfileService;

    private const int DayStartHour = 7;
    private const int DayEndHour = 22;
    private const double HourHeight = 64.0;

    private Guid _currentUserId;

    [ObservableProperty]
    private DateTime currentWeekStart;

    [ObservableProperty]
    private string monthTitle = string.Empty;

    [ObservableProperty]
    private string weekRangeText = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string successMessage = string.Empty;

    // ---- Редактор события ----
    [ObservableProperty]
    private bool isEditorOpen;

    [ObservableProperty]
    private bool isEditorBusy;

    /// <summary>Может ли текущий пользователь редактировать/удалять открытое событие.</summary>
    [ObservableProperty]
    private bool canEditEditingEvent;

    [ObservableProperty]
    private Guid editingEventId;

    [ObservableProperty]
    private bool isEditingExisting;

    [ObservableProperty]
    private string editTitle = string.Empty;

    [ObservableProperty]
    private string? editDescription;

    [ObservableProperty]
    private DateTime? editDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan editStartTime = new(10, 0, 0);

    [ObservableProperty]
    private TimeSpan editEndTime = new(11, 0, 0);

    [ObservableProperty]
    private CalendarDto? editCalendar;

    [ObservableProperty]
    private string selectedEventColor = "#8B70FB";

    [ObservableProperty]
    private bool isCreateCalendarDialogOpen;

    private CreateCalendarDialogViewModel? _createCalendarDialog;

    public CreateCalendarDialogViewModel? CreateCalendarDialog
    {
        get => _createCalendarDialog;
        private set => SetProperty(ref _createCalendarDialog, value);
    }
    /// <summary>Имена участников встречи, принявших приглашение.</summary>
    public bool HasAcceptedParticipants => EditAcceptedParticipants.Count > 0;

    private List<string> editAcceptedParticipants = new();
    public List<string> EditAcceptedParticipants
    {
        get => editAcceptedParticipants;
        set
        {
            if (SetProperty(ref editAcceptedParticipants, value))
                OnPropertyChanged(nameof(HasAcceptedParticipants));
        }
    }

    public ObservableCollection<CalendarDto> Calendars { get; } = new();
    /// <summary>Календари, в которых текущий пользователь может создавать события.</summary>
    public ObservableCollection<CalendarDto> EditableCalendars { get; } = new();
    public ObservableCollection<CalendarItemViewModel> CalendarItems { get; } = new();
    public ObservableCollection<EventColorOptionViewModel> EventColors { get; } = new();
    public ObservableCollection<WeekDayColumnViewModel> Days { get; } = new();
    public ObservableCollection<string> HourLabels { get; } = new();

    // ---- Выбранный календарь / управление ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedCalendarGroup))]
    [NotifyPropertyChangedFor(nameof(IsSelectedCalendarPersonal))]
    [NotifyPropertyChangedFor(nameof(IsSelectedCalendarOwner))]
    [NotifyPropertyChangedFor(nameof(CanManageSelectedCalendar))]
    [NotifyPropertyChangedFor(nameof(CanDeleteSelectedCalendar))]
    [NotifyPropertyChangedFor(nameof(SelectedCalendarMembersCountText))]
    [NotifyPropertyChangedFor(nameof(SelectedCalendarHasMembers))]
    private CalendarItemViewModel? selectedCalendar;

    [ObservableProperty]
    private bool isCalendarEditOpen;

    [ObservableProperty]
    private string editCalendarName = string.Empty;

    [ObservableProperty]
    private string editCalendarColor = "#A78BFA";

    [ObservableProperty]
    private bool isCalendarBusy;

    // ---- Участники ----
    [ObservableProperty]
    private string newMemberEmail = string.Empty;

    [ObservableProperty]
    private AccessLevelOption? newMemberAccessLevel;

    public ObservableCollection<AccessLevelOption> AccessLevels { get; } = new()
    {
        new() { Value = "view", DisplayName = "Просмотр" },
        new() { Value = "edit", DisplayName = "Редактирование" },
        new() { Value = "full", DisplayName = "Полный доступ" },
        new() { Value = "free-busy", DisplayName = "Занят/свободен" },
    };

    public bool IsSelectedCalendarGroup =>
        string.Equals(SelectedCalendar?.Type, "group", StringComparison.OrdinalIgnoreCase);

    public bool IsSelectedCalendarPersonal =>
        string.Equals(SelectedCalendar?.Type, "personal", StringComparison.OrdinalIgnoreCase);

    public bool IsSelectedCalendarOwner =>
        SelectedCalendar != null && SelectedCalendar.OwnerId == _currentUserId;

    public bool CanManageSelectedCalendar => IsSelectedCalendarOwner;

    /// <summary>Личный календарь удалять нельзя.</summary>
    public bool CanDeleteSelectedCalendar => IsSelectedCalendarOwner && !IsSelectedCalendarPersonal;

    public string SelectedCalendarMembersCountText
    {
        get
        {
            var count = SelectedCalendar?.Members.Count ?? 0;
            return count == 1 ? "1 участник" : $"{count} участников";
        }
    }

    public bool SelectedCalendarHasMembers => (SelectedCalendar?.Members.Count ?? 0) > 0;

    public CalendarPageViewModel(
        CalendarService calendarService,
        EventService eventService,
        UserProfileService userProfileService)
    {
        _calendarService = calendarService;
        _eventService = eventService;
        _userProfileService = userProfileService;

        var today = DateTime.Today;
        int diff = ((int)today.DayOfWeek + 6) % 7; // понедельник = первый день
        CurrentWeekStart = today.AddDays(-diff);

        for (int h = DayStartHour; h <= DayEndHour; h++)
            HourLabels.Add($"{h:00}:00");

        foreach (var color in EventColorOptionViewModel.DefaultColors())
            EventColors.Add(color);

        _ = LoadAsync();
    }

    private async Task LoadCurrentUserAsync()
    {
        try
        {
            var profile = await _userProfileService.GetProfileAsync();
            _currentUserId = profile.Id;
        }
        catch { /* права на редактирование просто не будут выставлены */ }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await LoadCurrentUserAsync();
            await LoadCalendarsAsync();
            await LoadWeekEventsSafeAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось загрузить календарь. Проверьте подключение к серверу.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenCreateCalendarDialog()
    {
        CreateCalendarDialog = new CreateCalendarDialogViewModel(_calendarService);
        CreateCalendarDialog.OnDialogClosed = async (success) =>
        {
            if (success)
            {
                await LoadCalendarsAsync();
            }
            IsCreateCalendarDialogOpen = false;
        };
        IsCreateCalendarDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCreateCalendarDialog()
    {
        IsCreateCalendarDialogOpen = false;
    }

    // ---- CRUD календаря ----

    [RelayCommand]
    private void SelectCalendar(CalendarItemViewModel? item)
    {
        if (item == null) return;
        SelectedCalendar = item;
        EditCalendarName = item.Name;
        EditCalendarColor = item.Color;
    }

    [RelayCommand]
    private void OpenCalendarEdit()
    {
        if (SelectedCalendar == null) return;
        EditCalendarName = SelectedCalendar.Name;
        EditCalendarColor = SelectedCalendar.Color;
        IsCalendarEditOpen = true;
    }

    [RelayCommand]
    private void CloseCalendarEdit()
    {
        IsCalendarEditOpen = false;
    }

    [RelayCommand]
    private async Task SaveCalendarAsync()
    {
        if (SelectedCalendar == null || !CanManageSelectedCalendar) return;
        if (string.IsNullOrWhiteSpace(EditCalendarName))
        {
            ErrorMessage = "Введите название календаря.";
            return;
        }

        IsCalendarBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await _calendarService.UpdateCalendarAsync(SelectedCalendar.Id, new UpdateCalendarRequest
            {
                Name = EditCalendarName.Trim(),
                Color = EditCalendarColor
            });
            SuccessMessage = "Календарь обновлён.";
            IsCalendarEditOpen = false;
            await LoadCalendarsAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось обновить календарь.";
        }
        finally
        {
            IsCalendarBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCalendarAsync()
    {
        if (SelectedCalendar == null || !CanManageSelectedCalendar) return;

        if (!CanDeleteSelectedCalendar)
        {
            ErrorMessage = "Личный календарь нельзя удалить.";
            return;
        }

        IsCalendarBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await _calendarService.DeleteCalendarAsync(SelectedCalendar.Id);
            SuccessMessage = "Календарь удалён.";
            IsCalendarEditOpen = false;
            SelectedCalendar = null;
            await LoadCalendarsAsync();
            await LoadWeekEventsSafeAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось удалить календарь.";
        }
        finally
        {
            IsCalendarBusy = false;
        }
    }

    [RelayCommand]
    private void SelectCalendarColor(EventColorOptionViewModel? color)
    {
        if (color == null) return;
        EditCalendarColor = color.Hex;
    }

    // ---- Участники календаря ----

    [RelayCommand]
    private async Task AddMemberAsync()
    {
        if (SelectedCalendar == null || !CanManageSelectedCalendar) return;
        if (string.IsNullOrWhiteSpace(NewMemberEmail))
        {
            ErrorMessage = "Введите email пользователя.";
            return;
        }

        IsCalendarBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await _calendarService.AddMemberAsync(SelectedCalendar.Id, new AddCalendarMemberRequest
            {
                Email = NewMemberEmail.Trim(),
                Role = "member",
                AccessLevel = NewMemberAccessLevel?.Value ?? "edit"
            });
            SuccessMessage = "Участник добавлен.";
            NewMemberEmail = string.Empty;
            NewMemberAccessLevel = null;
            await LoadCalendarsAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось добавить участника.";
        }
        finally
        {
            IsCalendarBusy = false;
        }
    }

    [RelayCommand]
    private async Task UpdateMemberAccessAsync(CalendarMemberDto? member)
    {
        if (member == null || SelectedCalendar == null || !CanManageSelectedCalendar) return;

        IsCalendarBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await _calendarService.UpdateMemberAsync(SelectedCalendar.Id, member.UserId,
                new UpdateCalendarMemberRequest { AccessLevel = member.AccessLevel });
            SuccessMessage = "Права обновлены.";
            await LoadCalendarsAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось обновить права.";
        }
        finally
        {
            IsCalendarBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveMemberAsync(CalendarMemberDto? member)
    {
        if (member == null || SelectedCalendar == null || !CanManageSelectedCalendar) return;

        IsCalendarBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            await _calendarService.RemoveMemberAsync(SelectedCalendar.Id, member.UserId);
            SuccessMessage = "Участник удалён.";
            await LoadCalendarsAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось удалить участника.";
        }
        finally
        {
            IsCalendarBusy = false;
        }
    }

    private async Task LoadCalendarsAsync()
    {
        var calendars = await _calendarService.GetCalendarsAsync();

        var selectedId = EditCalendar?.Id;
        var selectedItemId = SelectedCalendar?.Id;
        var previousVisibility = CalendarItems.ToDictionary(c => c.Id, c => c.IsVisible);

        Calendars.Clear();
        CalendarItems.Clear();
        foreach (var c in calendars)
        {
            Calendars.Add(c);
            var item = new CalendarItemViewModel(c, _currentUserId);

            // Сохраняем выбор видимости пользователя между перезагрузками списка
            if (previousVisibility.TryGetValue(item.Id, out var wasVisible))
                item.IsVisible = wasVisible;

            // При переключении галочки сразу перестраиваем события на сетке
            item.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(CalendarItemViewModel.IsVisible))
                    await LoadWeekEventsSafeAsync();
            };

            CalendarItems.Add(item);
        }

        // Календари, в которые можно создавать события (владелец, full или edit)
        EditableCalendars.Clear();
        foreach (var item in CalendarItems.Where(i => i.CanEdit))
        {
            var dto = Calendars.First(c => c.Id == item.Id);
            EditableCalendars.Add(dto);
        }

        EditCalendar = EditableCalendars.FirstOrDefault(c => c.Id == selectedId)
            ?? EditableCalendars.FirstOrDefault();

        SelectedCalendar = CalendarItems.FirstOrDefault(c => c.Id == selectedItemId)
            ?? CalendarItems.FirstOrDefault();
    }

    partial void OnEditCalendarChanged(CalendarDto? value)
    {
        if (!IsEditingExisting)
            SelectedEventColor = NormalizeColor(value?.Color);
        UpdateColorSelection();
    }

    partial void OnSelectedEventColorChanged(string value)
    {
        UpdateColorSelection();
    }

    private async Task LoadWeekEventsAsync()
    {
        var weekEnd = CurrentWeekStart.AddDays(7);
        var events = await _eventService.GetEventsInRangeAsync(
            DateTime.SpecifyKind(CurrentWeekStart, DateTimeKind.Local).ToUniversalTime(),
            DateTime.SpecifyKind(weekEnd, DateTimeKind.Local).ToUniversalTime());

        UpdateHeaders();
        BuildDays(events);
    }

    private void UpdateHeaders()
    {
        var culture = new CultureInfo("ru-RU");
        var weekEnd = CurrentWeekStart.AddDays(6);

        MonthTitle = culture.TextInfo.ToTitleCase(
            weekEnd.ToString("MMMM yyyy", culture));

        WeekRangeText = $"{CurrentWeekStart:dd MMM} — {weekEnd:dd MMM}";
    }

    private void BuildDays(List<EventDto> events)
    {
        Days.Clear();

        var visibleIds = CalendarItems
            .Where(c => c.IsVisible)
            .Select(c => c.Id)
            .ToHashSet();

        var filtered = CalendarItems.Count == 0
            ? events
            : events.Where(e => visibleIds.Contains(e.CalendarId)).ToList();

        for (int i = 0; i < 7; i++)
        {
            var date = CurrentWeekStart.AddDays(i);
            var column = new WeekDayColumnViewModel
            {
                Date = date,
                IsToday = date == DateTime.Today
            };

            var dayEvents = filtered
                .Select(e => (evt: e, local: ToLocalInterval(e)))
                .Where(x => x.local.Start.Date == date && !x.evt.IsAllDay)
                .OrderBy(x => x.local.Start)
                .ThenBy(x => x.local.End)
                .ToList();

            // Группы взаимно пересекающихся событий (кластеры).
            // Внутри кластера раскладываем по колонкам «жадно».
            var cluster = new List<(EventDto Evt, DateTime Start, DateTime End)>();
            DateTime clusterEnd = DateTime.MinValue;

            void FlushCluster()
            {
                if (cluster.Count == 0)
                    return;

                var columns = new List<DateTime>(); // время конца по каждой колонке
                var added = new List<EventBlockViewModel>();

                foreach (var item in cluster)
                {
                    int col = -1;
                    for (int c = 0; c < columns.Count; c++)
                    {
                        if (columns[c] <= item.Start)
                        {
                            col = c;
                            break;
                        }
                    }
                    if (col < 0)
                    {
                        col = columns.Count;
                        columns.Add(item.End);
                    }
                    else
                    {
                        columns[col] = item.End;
                    }

                    double startMinutes = Math.Max((item.Start - date).TotalMinutes - DayStartHour * 60, 0);
                    double endMinutes = Math.Min((item.End - date).TotalMinutes - DayStartHour * 60,
                        (DayEndHour - DayStartHour + 1) * 60);
                    if (endMinutes <= startMinutes)
                        continue;

                    var block = new EventBlockViewModel
                    {
                        Id = item.Evt.Id,
                        Title = item.Evt.Title,
                        CalendarName = item.Evt.CalendarName,
                        CreatorName = item.Evt.CreatorName,
                        TimeText = $"{item.Start:HH:mm} – {item.End:HH:mm}",
                        ColorHex = NormalizeColor(item.Evt.Color ?? item.Evt.CalendarColor),
                        MeetingId = item.Evt.MeetingId,
                        CreatorId = item.Evt.CreatorId,
                        IsCurrentUserCreator = item.Evt.CreatorId == _currentUserId,
                        Description = item.Evt.Description,
                        AcceptedParticipants = item.Evt.AcceptedParticipants ?? new(),
                        Top = startMinutes / 60.0 * HourHeight,
                        Height = Math.Max((endMinutes - startMinutes) / 60.0 * HourHeight, 22),
                        Column = col,
                        StartMinutes = startMinutes,
                        EndMinutes = endMinutes
                    };
                    column.Events.Add(block);
                    added.Add(block);
                }

                int total = Math.Max(1, columns.Count);
                foreach (var b in added)
                    b.ColumnCount = total;

                cluster.Clear();
            }

            foreach (var (evt, local) in dayEvents)
            {
                if (cluster.Count > 0 && local.Start >= clusterEnd)
                    FlushCluster();

                cluster.Add((evt, local.Start, local.End));
                if (local.End > clusterEnd)
                    clusterEnd = local.End;
            }
            FlushCluster();

            Days.Add(column);
        }
    }

    private static (DateTime Start, DateTime End) ToLocalInterval(EventDto e)
    {
        var start = e.StartAt.Kind == DateTimeKind.Utc ? e.StartAt.ToLocalTime() : e.StartAt;
        var end = e.EndAt.Kind == DateTimeKind.Utc ? e.EndAt.ToLocalTime() : e.EndAt;
        return (start, end);
    }

    [RelayCommand]
    private async Task PreviousWeekAsync()
    {
        CurrentWeekStart = CurrentWeekStart.AddDays(-7);
        await LoadWeekEventsSafeAsync();
    }

    [RelayCommand]
    private async Task NextWeekAsync()
    {
        CurrentWeekStart = CurrentWeekStart.AddDays(7);
        await LoadWeekEventsSafeAsync();
    }

    [RelayCommand]
    private async Task TodayAsync()
    {
        var today = DateTime.Today;
        int diff = ((int)today.DayOfWeek + 6) % 7;
        CurrentWeekStart = today.AddDays(-diff);
        await LoadWeekEventsSafeAsync();
    }

    private async Task LoadWeekEventsSafeAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            await LoadWeekEventsAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось загрузить события недели.";
        }
    }

    // ---- Редактор ----

    [RelayCommand]
    private void OpenCreateEditor()
    {
        if (EditableCalendars.Count == 0)
        {
            ErrorMessage = "Нет календаря, в который вы можете добавлять события.";
            return;
        }

        IsEditingExisting = false;
        CanEditEditingEvent = true;
        EditingEventId = Guid.Empty;
        EditTitle = string.Empty;
        EditDescription = null;
        EditAcceptedParticipants = new();
        SelectedEventColor = NormalizeColor(EditCalendar?.Color);
        EditDate = DateTime.Today;
        EditStartTime = new TimeSpan(10, 0, 0);
        EditEndTime = new TimeSpan(11, 0, 0);
        EditCalendar ??= EditableCalendars.FirstOrDefault();
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void OpenEditEditor(EventBlockViewModel? block)
    {
        if (block == null)
            return;

        // Событие-встреча: открываем редактор встречи (доступен создателю)
        if (block.MeetingId.HasValue)
        {
            WeakReferenceMessenger.Default.Send(new OpenMeetingEditMessage(block.MeetingId.Value));
            return;
        }

        OpenEditEditorById(block.Id);
    }

    private async void OpenEditEditorById(Guid eventId)
    {
        try
        {
            var evt = await _eventService.GetEventAsync(eventId);
            if (evt == null)
                return;

            IsEditingExisting = true;
            EditingEventId = evt.Id;
            EditTitle = evt.Title;
            EditDescription = evt.Description;
            EditAcceptedParticipants = evt.AcceptedParticipants ?? new();
            SelectedEventColor = NormalizeColor(evt.Color ?? evt.CalendarColor);

            var (start, end) = ToLocalInterval(evt);
            EditDate = start.Date;
            EditStartTime = start.TimeOfDay;
            EditEndTime = end.TimeOfDay;
            EditCalendar = Calendars.FirstOrDefault(c => c.Id == evt.CalendarId)
                ?? Calendars.FirstOrDefault();

            // Редактировать можно, если есть доступ на запись к календарю
            // или текущий пользователь — создатель события
            var calendarItem = CalendarItems.FirstOrDefault(c => c.Id == evt.CalendarId);
            CanEditEditingEvent = calendarItem?.CanEdit == true || evt.CreatorId == _currentUserId;

            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            IsEditorOpen = true;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось открыть событие.";
        }
    }

    [RelayCommand]
    private void CloseEditor()
    {
        IsEditorOpen = false;
    }

    [RelayCommand]
    private async Task SaveEventAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (!CanEditEditingEvent)
        {
            ErrorMessage = "У вас нет прав на изменение этого события.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditTitle))
        {
            ErrorMessage = "Укажите название события.";
            return;
        }

        if (EditCalendar == null)
        {
            ErrorMessage = "Выберите календарь.";
            return;
        }

        if (EditEndTime <= EditStartTime)
        {
            ErrorMessage = "Время окончания должно быть позже времени начала.";
            return;
        }

        var editDateValue = (EditDate ?? DateTime.Today).Date;
        var localStart = editDateValue + EditStartTime;
        var localEnd = editDateValue + EditEndTime;

        IsEditorBusy = true;
        try
        {
            if (IsEditingExisting)
            {
                await _eventService.UpdateEventAsync(EditingEventId, new UpdateEventRequest
                {
                    Title = EditTitle.Trim(),
                    Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                    Color = SelectedEventColor,
                    StartAt = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime(),
                    EndAt = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime()
                });
                SuccessMessage = "Событие обновлено.";
            }
            else
            {
                await _eventService.CreateEventAsync(new CreateEventRequest
                {
                    CalendarId = EditCalendar.Id,
                    Title = EditTitle.Trim(),
                    Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                    Color = SelectedEventColor,
                    StartAt = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime(),
                    EndAt = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime()
                });
                SuccessMessage = "Событие создано.";
            }

            IsEditorOpen = false;
            await LoadWeekEventsAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось сохранить событие.";
        }
        finally
        {
            IsEditorBusy = false;
        }
    }

    [RelayCommand]
    private void SelectEventColor(EventColorOptionViewModel? color)
    {
        if (color == null)
            return;

        SelectedEventColor = color.Hex;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private void UpdateColorSelection()
    {
        foreach (var color in EventColors)
            color.IsSelected = string.Equals(color.Hex, SelectedEventColor, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeColor(string? color)
        => string.IsNullOrWhiteSpace(color) ? "#8B70FB" : color;

    [RelayCommand]
    private async Task DeleteEventAsync()
    {
        if (!IsEditingExisting || EditingEventId == Guid.Empty)
            return;

        IsEditorBusy = true;
        try
        {
            await _eventService.DeleteEventAsync(EditingEventId);
            SuccessMessage = "Событие удалено.";
            IsEditorOpen = false;
            await LoadWeekEventsAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось удалить событие.";
        }
        finally
        {
            IsEditorBusy = false;
        }
    }
}

public partial class WeekDayColumnViewModel : ObservableObject
{
    private static readonly string[] DayNames =
        { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

    [ObservableProperty]
    private DateTime date;

    [ObservableProperty]
    private bool isToday;

    public string DayName => DayNames[(int)Date.DayOfWeek == 0 ? 6 : (int)Date.DayOfWeek - 1];
    public string DayNumber => Date.Day.ToString();

    public ObservableCollection<EventBlockViewModel> Events { get; } = new();
}

public partial class EventBlockViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CalendarName { get; set; } = string.Empty;

    /// <summary>Имя создателя события (отображается в групповых календарях).</summary>
    public string CreatorName { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#8B70FB";

    /// <summary>Если задано — событие является событием-встречей.</summary>
    public Guid? MeetingId { get; set; }

    public Guid CreatorId { get; set; }

    /// <summary>Текущий пользователь — создатель события/встречи (может редактировать).</summary>
    public bool IsCurrentUserCreator { get; set; }

    public string? Description { get; set; }

    public bool IsMeeting => MeetingId.HasValue;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    /// <summary>Имена участников встречи, принявших приглашение.</summary>
    public List<string> AcceptedParticipants { get; set; } = new();

    public string AcceptedParticipantsText => "✓ " + string.Join(", ", AcceptedParticipants);

    [ObservableProperty]
    private double top;

    [ObservableProperty]
    private double height;

    [ObservableProperty]
    private int column;

    [ObservableProperty]
    private int columnCount = 1;

    public double StartMinutes { get; set; }
    public double EndMinutes { get; set; }
}

public partial class EventColorOptionViewModel : ObservableObject
{
    public string Hex { get; init; } = "#8B70FB";
    public string Name { get; init; } = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    public static IEnumerable<EventColorOptionViewModel> DefaultColors()
    {
        yield return new EventColorOptionViewModel { Hex = "#8B70FB", Name = "Лавандовый" };
        yield return new EventColorOptionViewModel { Hex = "#6EA8FE", Name = "Голубой" };
        yield return new EventColorOptionViewModel { Hex = "#58C4B8", Name = "Мятный" };
        yield return new EventColorOptionViewModel { Hex = "#75C878", Name = "Зеленый" };
        yield return new EventColorOptionViewModel { Hex = "#F4B860", Name = "Янтарный" };
        yield return new EventColorOptionViewModel { Hex = "#F1849E", Name = "Розовый" };
        yield return new EventColorOptionViewModel { Hex = "#C084FC", Name = "Фиолетовый" };
        yield return new EventColorOptionViewModel { Hex = "#8E99F3", Name = "Индиго" };
    }
}

public partial class CalendarItemViewModel : ObservableObject
{
    public CalendarItemViewModel(CalendarDto dto, Guid currentUserId)
    {
        Id = dto.Id;
        OwnerId = dto.OwnerId;
        Name = dto.Name;
        Color = dto.Color ?? "#A78BFA";
        Type = dto.Type ?? "personal";
        Description = dto.Description;
        Members = dto.Members;
        IsOwner = dto.OwnerId == currentUserId;
        MyAccessLevel = dto.Members
            .FirstOrDefault(m => m.UserId == currentUserId)?.AccessLevel;
        IsVisible = true;
    }

    public Guid Id { get; }
    public Guid OwnerId { get; }
    public string Name { get; }
    public string Color { get; }
    public string Type { get; }
    public string? Description { get; }
    public List<CalendarMemberDto> Members { get; }
    public bool IsOwner { get; }

    /// <summary>Уровень доступа текущего пользователя к этому календарю (full/edit/view/free-busy).</summary>
    public string? MyAccessLevel { get; }

    /// <summary>Может ли текущий пользователь создавать/изменять события в этом календаре.</summary>
    public bool CanEdit => IsOwner
        || string.Equals(MyAccessLevel, "full", StringComparison.OrdinalIgnoreCase)
        || string.Equals(MyAccessLevel, "edit", StringComparison.OrdinalIgnoreCase);

    [ObservableProperty]
    private bool isVisible;

    public string TypeDisplayName => Type switch
    {
        "personal" => "Личный",
        "work" => "Рабочий",
        "group" => "Групповой",
        _ => Type
    };
}

public class AccessLevelOption
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
