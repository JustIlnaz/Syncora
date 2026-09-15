using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Syncora.Client.ViewModels.Pages;
using System;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Syncora.Client.Views.Pages;

/// <summary>
/// Колонка одного дня недели: линии часов + абсолютно позиционированные блоки событий.
/// Позиционирование выполняется кодом, т.к. Canvas.Left/Top зависят от ширины колонки.
/// </summary>
public class DayColumnView : UserControl
{
    public static readonly StyledProperty<WeekDayColumnViewModel?> DayColumnProperty =
        AvaloniaProperty.Register<DayColumnView, WeekDayColumnViewModel?>(nameof(DayColumn));

    public static readonly StyledProperty<ICommand?> OpenEventCommandProperty =
        AvaloniaProperty.Register<DayColumnView, ICommand?>(nameof(OpenEventCommand));

    public WeekDayColumnViewModel? DayColumn
    {
        get => GetValue(DayColumnProperty);
        set => SetValue(DayColumnProperty, value);
    }

    public ICommand? OpenEventCommand
    {
        get => GetValue(OpenEventCommandProperty);
        set => SetValue(OpenEventCommandProperty, value);
    }

    private const double HourHeight = 64.0;
    private const int HourCount = 16; // 07:00–22:00
    private Canvas? _canvas;
    private WeekDayColumnViewModel? _subscribed;

    public DayColumnView()
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#E7E3F2")),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Background = Brushes.White
        };

        _canvas = new Canvas
        {
            Height = HourCount * HourHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ClipToBounds = true
        };

        border.Child = _canvas;
        Content = border;

        _canvas.SizeChanged += (_, _) => RenderEvents();

        DrawHourLines();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DayColumnProperty)
        {
            if (_subscribed != null)
            {
                _subscribed.Events.CollectionChanged -= OnEventsChanged;
            }

            _subscribed = DayColumn;

            if (_subscribed != null)
            {
                _subscribed.Events.CollectionChanged += OnEventsChanged;
            }

            RenderEvents();
            UpdateTodayHighlight();
        }
    }

    private void OnEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RenderEvents();

    private void UpdateTodayHighlight()
    {
        if (Content is Border border)
        {
            border.Background = DayColumn?.IsToday == true
                ? new SolidColorBrush(Color.Parse("#10EAE4FF"))
                : Brushes.Transparent;
        }
    }

    private void DrawHourLines()
    {
        if (_canvas == null)
            return;

        _canvas.Children.Clear();

        for (int i = 1; i <= HourCount; i++)
        {
            var line = new Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.Parse("#F0EDF8")),
                Opacity = i % 2 == 0 ? 0.95 : 0.62
            };
            Canvas.SetTop(line, i * HourHeight);
            line.SetValue(Canvas.LeftProperty, 8.0);
            line.HorizontalAlignment = HorizontalAlignment.Stretch;
            // Ширину ставим при рендере событий (SizeChanged)
            _canvas.Children.Add(line);
        }
    }

    private void RenderEvents()
    {
        if (_canvas == null)
            return;

        DrawHourLines();

        double width = _canvas.Bounds.Width;
        if (width <= 0)
            width = 120; // до первого layout

        // Обновим ширину линий
        foreach (var child in _canvas.Children)
        {
            if (child is Rectangle r)
                r.Width = Math.Max(width - 16, 0);
        }

        if (DayColumn == null)
            return;

        foreach (var ev in DayColumn.Events)
        {
            double colWidth = (width - 4) / Math.Max(1, ev.ColumnCount);
            double left = 2 + colWidth * ev.Column;

            var brush = TryBrush(ev.ColorHex, 0.25);
            var accent = TryBrush(ev.ColorHex, 1.0);

            var textStack = new StackPanel { Spacing = 1 };

            // Заголовок: для встреч добавляем маркер участников
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            if (ev.IsMeeting)
            {
                titlePanel.Children.Add(new TextBlock
                {
                    Text = "\uD83D\uDC65", // 👥
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            titlePanel.Children.Add(new TextBlock
            {
                Text = ev.Title,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#30264A")),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            textStack.Children.Add(titlePanel);

            textStack.Children.Add(new TextBlock
            {
                Text = ev.TimeText,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#756D88"))
            });

            // Создатель события (чтобы в групповых календарях было видно, чьё событие)
            if (!string.IsNullOrWhiteSpace(ev.CreatorName))
            {
                textStack.Children.Add(new TextBlock
                {
                    Text = ev.CreatorName,
                    FontSize = 10,
                    FontStyle = FontStyle.Italic,
                    Foreground = new SolidColorBrush(Color.Parse("#5A4E7C")),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxLines = 1
                });
            }

            // Описание — только если блок достаточно высокий
            if (ev.HasDescription && ev.Height >= 56)
            {
                textStack.Children.Add(new TextBlock
                {
                    Text = ev.Description,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#9B94AB")),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxLines = 2,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            // Принявшие встречу — только если блок достаточно высокий
            if (ev.AcceptedParticipants.Count > 0 && ev.Height >= 48)
            {
                textStack.Children.Add(new TextBlock
                {
                    Text = ev.AcceptedParticipantsText,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#2E7D32")),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxLines = 1
                });
            }

            var block = new Border
            {
                Width = Math.Max(colWidth - 4, 30),
                Height = ev.Height,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 6, 4, 4),
                Background = brush,
                ClipToBounds = true,
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = textStack
            };

            // Подсказка: описание и тип
            var tipText = ev.IsMeeting ? $"Встреча\n{ev.Title}" : ev.Title;
            tipText += $"\nКалендарь: {ev.CalendarName}";
            if (!string.IsNullOrWhiteSpace(ev.CreatorName))
                tipText += $"\nСоздатель: {ev.CreatorName}";
            if (ev.HasDescription)
                tipText += $"\n{ev.Description}";
            if (ev.AcceptedParticipants.Count > 0)
                tipText += $"\nПриняли: {string.Join(", ", ev.AcceptedParticipants)}";
            ToolTip.SetTip(block, tipText);

            // Акцентная полоска слева
            var stripe = new Border
            {
                Width = 3,
                Height = ev.Height,
                CornerRadius = new CornerRadius(2),
                Background = accent
            };
            Canvas.SetLeft(stripe, left);
            Canvas.SetTop(stripe, ev.Top);
            _canvas.Children.Add(stripe);

            Canvas.SetLeft(block, left + 3);
            Canvas.SetTop(block, ev.Top);
            block.PointerPressed += (_, e) =>
            {
                if (OpenEventCommand?.CanExecute(ev) == true)
                {
                    OpenEventCommand.Execute(ev);
                    e.Handled = true;
                }
            };
            _canvas.Children.Add(block);
        }
    }

    private static IBrush TryBrush(string hex, double opacity)
    {
        if (Color.TryParse(hex, out var color))
        {
            var c = Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B);
            return new SolidColorBrush(c);
        }
        return new SolidColorBrush(Color.Parse("#8B70FB"));
    }
}
