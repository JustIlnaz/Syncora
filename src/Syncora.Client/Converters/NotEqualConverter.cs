using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Syncora.Client.Converters;

public class NotEqualMultiConverter : IMultiValueConverter
{
    public static readonly NotEqualMultiConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return true;
        return !Equals(values[0], values[1]);
    }
}
