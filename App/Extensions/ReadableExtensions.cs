using System;

namespace Percentage.App.Extensions;

internal static class ReadableExtensions
{
    internal static string GetReadableTimeSpan(TimeSpan timeSpan)
    {
        var hours = timeSpan.Hours;
        var minutes = timeSpan.Minutes;
        return timeSpan.TotalSeconds < 60
            ? "less than 1 minute"
            : (hours > 0
                  ? hours > 1 ? hours + " hours " : "1 hour "
                  : null) +
              (minutes > 0
                  ? minutes > 1 ? minutes + " minutes" : "1 minute"
                  : null);
    }
}