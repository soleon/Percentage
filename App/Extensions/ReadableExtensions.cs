using System;
using System.Globalization;
using Percentage.App.Resources;

namespace Percentage.App.Extensions;

internal static class ReadableExtensions
{
    internal static string GetReadableTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 60)
            return Strings.Readable_LessThanOneMinute;

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
