using System;
using System.Globalization;
using System.Windows.Data;

namespace Percentage.App.Localization;

/// <summary>
///   Multi-value converter that formats a value through a localized format string. The first
///   binding supplies the format (e.g. <c>{0} seconds</c>); the remaining bindings supply the
///   arguments. Lets XAML pull format strings out of <see cref="LocalizationManager"/> while
///   the value to format comes from the data context.
/// </summary>
public sealed class LocalizedFormatConverter : IMultiValueConverter
{
    public static LocalizedFormatConverter Instance { get; } = new();

    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Length == 0 || values[0] is not string format)
            return null;
        if (values.Length == 1) return format;

        var args = new object?[values.Length - 1];
        Array.Copy(values, 1, args, 0, args.Length);
        return string.Format(culture, format, args);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
