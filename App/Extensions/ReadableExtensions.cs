using System.Globalization;
using Percentage.App.Resources;

namespace Percentage.App.Extensions;

/// <summary>Localised humanisation helpers for tooltip / notification body text.</summary>
internal static class ReadableExtensions
{
    /// <summary>
    ///     Formats a <see cref="TimeSpan" /> as a localised "X hours Y minutes" string. Used by both
    ///     the battery evaluator and the Details page so they speak the same vocabulary.
    /// </summary>
    internal static string GetReadableTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 60)
        {
            return Strings.Readable_LessThanOneMinute;
        }

        var hours = timeSpan.Hours;
        var minutes = timeSpan.Minutes;
        var culture = CultureInfo.CurrentCulture;

        var hoursPart = hours switch
        {
            0 => null,
            1 => Strings.Readable_HourSingular,
            _ => string.Format(culture, Strings.Readable_HourPlural, hours)
        };

        var minutesPart = minutes switch
        {
            0 => null,
            1 => Strings.Readable_MinuteSingular,
            _ => string.Format(culture, Strings.Readable_MinutePlural, minutes)
        };

        return (hoursPart, minutesPart) switch
        {
            (null, null) => Strings.Readable_LessThanOneMinute,
            (not null, null) => hoursPart,
            (null, not null) => minutesPart,
            _ => $"{hoursPart} {minutesPart}"
        };
    }
}
