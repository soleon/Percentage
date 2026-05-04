using System.Globalization;
using System.Windows.Forms;
using Percentage.App.Extensions;
using Percentage.App.Resources;

namespace Percentage.App.Services;

/// <summary>
///     Pure function that turns <see cref="BatteryReadings" /> plus the user's notification
///     thresholds into a <see cref="BatteryDecision" />. No Brush, no DispatcherTimer, no Toast -
///     suitable for unit testing without WPF or Windows Forms power APIs running.
/// </summary>
/// <remarks>
///     Behaviour is intended to be byte-for-byte identical to the legacy
///     <c>NotifyIconWindow.UpdateBatteryStatus</c> path; tests in
///     <c>Tests/App.Tests/BatteryEvaluatorTests.cs</c> lock that down.
/// </remarks>
internal static class BatteryEvaluator
{
    /// <summary>
    ///     Maps <paramref name="readings" /> + the user's <paramref name="thresholds" /> to a
    ///     <see cref="BatteryDecision" />, formatted in <paramref name="culture" />. Pure function;
    ///     locked down by <c>BatteryEvaluatorTests</c>.
    /// </summary>
    public static BatteryDecision Evaluate(BatteryReadings readings, Thresholds thresholds, CultureInfo culture)
    {
        switch (readings.ChargeStatus)
        {
            case BatteryChargeStatus.NoSystemBattery:
                return new BatteryDecision(
                    BatterySituation.NoBattery, -1, BatteryVisualCategory.Normal,
                    BatteryNotificationCategory.None,
                    null, Strings.Tray_NoBattery, "❌");

            case BatteryChargeStatus.Unknown:
                return new BatteryDecision(
                    BatterySituation.Unknown, -1, BatteryVisualCategory.Normal,
                    BatteryNotificationCategory.None,
                    null, string.Empty, "❓");
        }

        var percent = (int)Math.Round(readings.BatteryLifePercent * 100);
        var isPlugged = readings.LineStatus == PowerLineStatus.Online;

        if (percent == 100)
        {
            var fullTitle = isPlugged ? Strings.Tray_FullyChargedAndPluggedTitle : Strings.Tray_FullyChargedTitle;
            var fullTooltip =
                isPlugged ? Strings.Tray_FullyChargedAndPluggedTooltip : Strings.Tray_FullyChargedTooltip;
            var notify = thresholds.FullNotificationEnabled
                ? BatteryNotificationCategory.Full
                : BatteryNotificationCategory.None;
            return new BatteryDecision(
                BatterySituation.Full, percent, BatteryVisualCategory.Normal, notify,
                notify == BatteryNotificationCategory.None ? null : fullTitle,
                fullTooltip,
                null);
        }

        var charging = readings.ChargeStatus.HasFlag(BatteryChargeStatus.Charging);
        BatteryVisualCategory visual;
        var notification = BatteryNotificationCategory.None;
        string? title;
        string body;

        if (charging)
        {
            visual = BatteryVisualCategory.Charging;
            title = string.Format(culture, Strings.Tray_ChargingTitle, percent);

            if (readings.ChargeRateInMilliwatts is { } rate and > 0
                && readings is
                    { FullChargeCapacityInMilliwattHours: { } full, RemainingCapacityInMilliwattHours: { } remaining })
            {
                body = string.Format(culture, Strings.Tray_ChargingBodyEta,
                    ReadableExtensions.GetReadableTimeSpan(TimeSpan.FromHours((full - remaining) / (double)rate)));
            }
            else
            {
                title = null;
                body = string.Format(culture, Strings.Tray_ChargingTitle, percent);
            }

            // Use >= rather than == so a charging battery that jumps past the threshold
            // between two ticks still triggers the notification. Repeats are suppressed
            // by NotifyIconWindow.CheckAndSendNotification's milestone-once-per-type rule.
            if (percent >= thresholds.HighThresholdPercent && thresholds.HighNotificationEnabled)
            {
                notification = BatteryNotificationCategory.High;
            }
        }
        else
        {
            if (percent <= thresholds.CriticalThresholdPercent)
            {
                visual = BatteryVisualCategory.Critical;
                if (thresholds.CriticalNotificationEnabled)
                {
                    notification = BatteryNotificationCategory.Critical;
                }
            }
            else if (percent <= thresholds.LowThresholdPercent)
            {
                visual = BatteryVisualCategory.Low;
                if (thresholds.LowNotificationEnabled)
                {
                    notification = BatteryNotificationCategory.Low;
                }
            }
            else
            {
                visual = BatteryVisualCategory.Normal;
                if (percent == thresholds.HighThresholdPercent && thresholds.HighNotificationEnabled)
                {
                    notification = BatteryNotificationCategory.High;
                }
            }

            var dischargingTitleFormat = isPlugged
                ? Strings.Tray_DischargingTitlePlugged
                : Strings.Tray_DischargingTitleOnBattery;

            if (readings.BatteryLifeRemainingSeconds > 0)
            {
                title = string.Format(culture, dischargingTitleFormat, percent);
                body = string.Format(culture, Strings.Tray_DischargingBodyTimeRemaining,
                    ReadableExtensions.GetReadableTimeSpan(TimeSpan.FromSeconds(readings.BatteryLifeRemainingSeconds)));
            }
            else
            {
                title = null;
                body = string.Format(culture, dischargingTitleFormat, percent);
            }
        }

        return new BatteryDecision(
            charging ? BatterySituation.Charging : BatterySituation.Discharging,
            percent, visual, notification, title, body,
            percent.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     The notification thresholds the user has configured. Passed in explicitly so the
    ///     evaluator can stay decoupled from <see cref="Properties.Settings.Default" />.
    /// </summary>
    /// <param name="CriticalThresholdPercent">Battery percentage at or below which a Critical notification fires.</param>
    /// <param name="LowThresholdPercent">Battery percentage at or below which a Low notification fires.</param>
    /// <param name="HighThresholdPercent">Battery percentage at or above which a High notification fires.</param>
    /// <param name="FullNotificationEnabled">Whether the Full (100%) notification fires.</param>
    /// <param name="HighNotificationEnabled">Whether the High notification fires.</param>
    /// <param name="LowNotificationEnabled">Whether the Low notification fires.</param>
    /// <param name="CriticalNotificationEnabled">Whether the Critical notification fires.</param>
    public readonly record struct Thresholds(
        int CriticalThresholdPercent,
        int LowThresholdPercent,
        int HighThresholdPercent,
        bool FullNotificationEnabled,
        bool HighNotificationEnabled,
        bool LowNotificationEnabled,
        bool CriticalNotificationEnabled);
}
