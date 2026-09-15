using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncora.Client.Models.Calendar;
using Syncora.Client.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Syncora.Client.ViewModels.Dialogs;

public partial class CreateCalendarDialogViewModel : ViewModelBase
{
    private readonly CalendarService _calendarService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string name = string.Empty;

    [ObservableProperty]
    private string? selectedColor;

    [ObservableProperty]
    private CalendarTypeOption selectedType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private bool isCreating;

    partial void OnSelectedColorChanged(string? value)
    {
        if (value != null)
        {
            // Additional logic if needed
        }
    }

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public ObservableCollection<string> CalendarColors { get; } = new()
    {
        "#A78BFA", // Фиолетовый (по умолчанию)
        "#34D399", // Зелёный
        "#60A5FA", // Синий
        "#F472B6", // Розовый
        "#FBBF24", // Жёлтый
        "#F87171", // Красный
        "#A78BFA", // Лиловый
        "#818CF8", // Индиго
    };

    public ObservableCollection<CalendarTypeOption> CalendarTypes { get; } = new()
    {
        new() { Value = "work", DisplayName = "Рабочий" },
        new() { Value = "group", DisplayName = "Групповой" }
    };

    public bool CanCreate => !string.IsNullOrWhiteSpace(Name) && !IsCreating;

    public CreateCalendarDialogViewModel(CalendarService calendarService)
    {
        _calendarService = calendarService;
        SelectedColor = CalendarColors[0];
        SelectedType = CalendarTypes.First();
    }

    [RelayCommand]
    private void SelectColor(string color)
    {
        SelectedColor = color;
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Введите название календаря";
            return;
        }

        try
        {
            IsCreating = true;
            ErrorMessage = string.Empty;

            var request = new CreateCalendarRequest
            {
                Name = Name,
                Color = SelectedColor,
                Type = SelectedType.Value
            };

            await _calendarService.CreateCalendarAsync(request);
            
            // Успешное создание - закрываем диалог
            OnDialogClosed?.Invoke(true);
        }
        catch
        {
            ErrorMessage = "Не удалось создать календарь. Попробуйте позже.";
        }
        finally
        {
            IsCreating = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        // Закрытие диалога будет обрабатываться родительским ViewModel
        OnDialogClosed?.Invoke(false);
    }

    public Action<bool>? OnDialogClosed { get; set; }
}