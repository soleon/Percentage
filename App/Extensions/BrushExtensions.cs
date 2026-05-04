using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Markup;
using static Percentage.App.Properties.Settings;

namespace Percentage.App.Extensions;

/// <summary>
///     Brush factory used by <see cref="NotifyIconWindow" /> on every tray refresh. Resolves the
///     "auto vs custom" decision per battery state, parses hex colours through a bounded cache,
///     and freezes the resulting <see cref="SolidColorBrush" /> so WPF can skip change tracking.
/// </summary>
internal static class BrushExtensions
{
    // Bounded by the small set of hex strings users pick from AccentColourPicker plus the
    // four DefaultBattery*Colour constants. Concurrent because tray refresh runs on the UI
    // dispatcher but theme-change callbacks may post from a worker; cheap to be safe.
    private static readonly ConcurrentDictionary<string, Color?> ParsedColours = new(StringComparer.OrdinalIgnoreCase);

    private static SolidColorBrush Freeze(SolidColorBrush brush)
    {
        // Freezing transitions the brush to a thread-safe, immutable representation and
        // lets WPF skip change-tracking - material in a refresh hot path.
        if (brush.CanFreeze && !brush.IsFrozen)
        {
            brush.Freeze();
        }

        return brush;
    }

    /// <summary>Charging-state tray-icon background brush, or null when the user chose "auto" / transparent.</summary>
    internal static Brush? GetBatteryChargingBackgroundBrush()
    {
        return GetTargetBackgroundBrush(Default.IsAutoBatteryChargingBackgroundColour,
            Default.BatteryChargingBackgroundColour);
    }

    /// <summary>Charging-state tray-icon foreground brush; falls back to the system accent / default.</summary>
    internal static Brush GetBatteryChargingBrush()
    {
        return GetTargetBrush(Default.IsAutoBatteryChargingColour, Default.BatteryChargingColour,
            App.DefaultBatteryChargingColour);
    }

    /// <summary>Critical-state tray-icon background brush, or null when the user chose "auto" / transparent.</summary>
    internal static Brush? GetBatteryCriticalBackgroundBrush()
    {
        return GetTargetBackgroundBrush(Default.IsAutoBatteryCriticalBackgroundColour,
            Default.BatteryCriticalBackgroundColour);
    }

    /// <summary>Critical-state tray-icon foreground brush; falls back to the system accent / default.</summary>
    internal static Brush GetBatteryCriticalBrush()
    {
        return GetTargetBrush(Default.IsAutoBatteryCriticalColour, Default.BatteryCriticalColour,
            App.DefaultBatteryCriticalColour);
    }

    /// <summary>Low-state tray-icon background brush, or null when the user chose "auto" / transparent.</summary>
    internal static Brush? GetBatteryLowBackgroundBrush() =>
        GetTargetBackgroundBrush(Default.IsAutoBatteryLowBackgroundColour, Default.BatteryLowBackgroundColour);

    /// <summary>Low-state tray-icon foreground brush; falls back to the system accent / default.</summary>
    internal static Brush GetBatteryLowBrush()
    {
        return GetTargetBrush(Default.IsAutoBatteryLowColour, Default.BatteryLowColour,
            App.DefaultBatteryLowColour);
    }

    /// <summary>Normal-state tray-icon background brush, or null when the user chose "auto" / transparent.</summary>
    internal static Brush? GetBatteryNormalBackgroundBrush()
    {
        return GetTargetBackgroundBrush(Default.IsAutoBatteryNormalBackgroundColour,
            Default.BatteryNormalBackgroundColour);
    }

    /// <summary>
    ///     Normal-state tray-icon foreground brush. No baked-in fallback hex, so the auto path returns
    ///     the WPF <c>TextFillColorPrimaryBrush</c> resource directly.
    /// </summary>
    internal static Brush GetBatteryNormalBrush() =>
        GetTargetBrush(Default.IsAutoBatteryNormalColour, Default.BatteryNormalColour, null);

    private static SolidColorBrush? GetTargetBackgroundBrush(bool isUsingAutoColour, string? targetColour)
    {
        if (isUsingAutoColour || string.IsNullOrEmpty(targetColour))
        {
            return null;
        }

        var parsed = TryParseColour(targetColour);
        return parsed is null ? null : Freeze(new SolidColorBrush(parsed.Value));
    }

    private static Brush GetTargetBrush(bool isUsingAutoColour, string? targetColour, string? fallbackHex)
    {
        if (isUsingAutoColour)
        {
            // The "auto" path always pulls a freshly-frozen WPF resource; FindResource is O(1) cached.
            return (Brush)Application.Current.FindResource(nameof(ThemeResource.TextFillColorPrimaryBrush))!;
        }

        var parsed = TryParseColour(targetColour);
        if (parsed is { } colour)
        {
            return Freeze(new SolidColorBrush(colour));
        }

        if (fallbackHex is not null && TryParseColour(fallbackHex) is { } fallbackColour)
        {
            return Freeze(new SolidColorBrush(fallbackColour));
        }

        return (Brush)Application.Current.FindResource(nameof(ThemeResource.TextFillColorPrimaryBrush))!;
    }

    private static Color? TryParseColour(string? hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return null;
        }

        return ParsedColours.GetOrAdd(hex, static value =>
        {
            try
            {
                return ColorConverter.ConvertFromString(value) is Color c ? c : null;
            }
            catch (Exception e) when (e is FormatException or InvalidOperationException)
            {
                return null;
            }
        });
    }
}
