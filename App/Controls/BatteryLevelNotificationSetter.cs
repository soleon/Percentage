using System.Windows;
using System.Windows.Controls;

namespace Percentage.App.Controls;

/// <summary>
///     One row in the Settings notification list: a label, a percent-threshold spinner, and a
///     toggle that turns the corresponding notification on or off. Used for Critical / Low / High
///     thresholds; the Full notification has no threshold and uses a plain toggle instead.
/// </summary>
public class BatteryLevelNotificationSetter : Control
{
    /// <summary>Identifies the <see cref="IsChecked" /> dependency property.</summary>
    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
        nameof(IsChecked), typeof(bool), typeof(BatteryLevelNotificationSetter));

    /// <summary>Identifies the <see cref="StatusName" /> dependency property.</summary>
    public static readonly DependencyProperty StatusNameProperty = DependencyProperty.Register(
        nameof(StatusName), typeof(string), typeof(BatteryLevelNotificationSetter));

    /// <summary>Identifies the <see cref="Value" /> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(BatteryLevelNotificationSetter));

    static BatteryLevelNotificationSetter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BatteryLevelNotificationSetter),
            new FrameworkPropertyMetadata(typeof(BatteryLevelNotificationSetter)));
    }

    /// <summary>True when this level's notification is enabled.</summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Localised level name shown to the user (e.g. "Critical", "Low", "High").</summary>
    public string StatusName
    {
        get => (string)GetValue(StatusNameProperty);
        set => SetValue(StatusNameProperty, value);
    }

    /// <summary>Battery-percentage threshold at which the notification fires.</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
