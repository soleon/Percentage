using System;
using System.Windows;
using System.Windows.Media;
using Percentage.App.Properties;
using Wpf.Ui.Markup;

namespace Percentage.App.Extensions;

using static Settings;

internal static class BrushExtensions
{
    internal static Brush GetBatteryChargingBrush()
    {
        return GetTargetBrush(Default.IsAutoBatteryChargingColour, Default.BatteryChargingColour,
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(App.DefaultBatteryChargingColour)!));
    }

    internal static Brush GetBatteryCriticalBrush()
    {
        return GetTargetBrush(Default.IsAutoBatteryCriticalColour, Default.BatteryCriticalColour,
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(App.DefaultBatteryCriticalColour)!));
    }

    internal static Brush GetBatteryLowBrush()
    {
        return GetTargetBrush(Default.IsAutoBatteryLowColour, Default.BatteryLowColour,
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(App.DefaultBatteryLowColour)!));
    }

    internal static Brush GetBatteryNormalBrush()
    {
        return GetTargetBrush(Default.IsAutoBatteryNormalColour, Default.BatteryNormalColour,
            (Brush)Application.Current.FindResource(nameof(ThemeResource.TextFillColorPrimaryBrush))!);
    }


    private static Brush GetBrushFromColourHexString(string hexString, Brush fallbackBrush)
    {
        object? colour;
        try
        {
            colour = ColorConverter.ConvertFromString(hexString);
        }
        catch (FormatException)
        {
            return fallbackBrush;
        }

        return colour == null ? fallbackBrush : new SolidColorBrush((Color)colour);
    }

    private static Brush GetTargetBrush(bool isUsingAutoColour, string targetColour, Brush fallbackBrush)
    {
        return isUsingAutoColour
            ? new SolidColorBrush((Color)Application.Current.FindResource(nameof(ThemeResource.TextFillColorPrimary))!)
            : GetBrushFromColourHexString(targetColour, fallbackBrush);
    }
}