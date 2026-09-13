using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Syncora.Client.Converters;

/// <summary>True → акцентный фиолетовый фон, False → прозрачный.</summary>
public class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance = new();

    private static readonly SolidColorBrush Accent = SolidColorBrush.Parse("#8B70FB");
    private static readonly SolidColorBrush Transparent = new(Colors.Transparent);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Accent : Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>True → белый текст (на акцентном фоне), False → основной цвет текста.</summary>
public class BoolToForegroundConverter : IValueConverter
{
    public static readonly BoolToForegroundConverter Instance = new();

    private static readonly SolidColorBrush White = SolidColorBrush.Parse("#FFFFFF");
    private static readonly SolidColorBrush Primary = SolidColorBrush.Parse("#30264A");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? White : Primary;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>True → SemiBold, False → Normal.</summary>
public class BoolToFontWeightConverter : IValueConverter
{
    public static readonly BoolToFontWeightConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontWeight.SemiBold : FontWeight.Normal;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Not null → true.</summary>
public class NotNullConverter : IValueConverter
{
    public static readonly NotNullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Null → true.</summary>
public class NullConverter : IValueConverter
{
    public static readonly NullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
