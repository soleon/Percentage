namespace Percentage.App.Services;

/// <summary>
///     Pure data describing the next tray-icon refresh: what to draw, what tooltip to show,
///     and whether to fire a toast notification. Produced by <see cref="BatteryEvaluator.Evaluate" />.
/// </summary>
/// <param name="Situation">High-level situation classification.</param>
/// <param name="Percent">Battery percentage 0-100; -1 when not applicable (NoBattery / Unknown).</param>
/// <param name="VisualCategory">The category callers map to a brush.</param>
/// <param name="Notification">Notification category to fire (or <see cref="BatteryNotificationCategory.None" />).</param>
/// <param name="TooltipTitle">First line of the tooltip (maybe null when the body is the only line).</param>
/// <param name="TooltipBody">Second line of the tooltip / body of the notification.</param>
/// <param name="TrayIconText">
///     Text to render in the tray icon. Null when the caller should call
///     <c>SetBatteryFullIcon</c> instead.
/// </param>
internal readonly record struct BatteryDecision(
    BatterySituation Situation,
    int Percent,
    BatteryVisualCategory VisualCategory,
    BatteryNotificationCategory Notification,
    string? TooltipTitle,
    string TooltipBody,
    string? TrayIconText);
