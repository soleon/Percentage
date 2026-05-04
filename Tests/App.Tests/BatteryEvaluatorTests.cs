using System.Globalization;

namespace Percentage.App.Tests;

public sealed class BatteryEvaluatorTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static BatteryEvaluator.Thresholds DefaultThresholds()
    {
        return new BatteryEvaluator.Thresholds(10,
            20,
            80,
            true,
            true,
            true,
            true);
    }

    private static BatteryReadings Readings(
        BatteryChargeStatus status = BatteryChargeStatus.High,
        PowerLineStatus line = PowerLineStatus.Offline,
        float percent = 0.5f,
        int seconds = 3600,
        int? rateMilliwatts = null,
        int? fullMwh = null,
        int? remainingMwh = null,
        int? designMwh = null) =>
        new(status, line, percent, seconds, rateMilliwatts, fullMwh, remainingMwh, designMwh);

    [Fact]
    public void Charging_at_high_threshold_returns_High_notification()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.Charging, PowerLineStatus.Online, 0.80f,
                rateMilliwatts: 5000, fullMwh: 50000, remainingMwh: 40000),
            DefaultThresholds(), Invariant);

        Assert.Equal(BatterySituation.Charging, d.Situation);
        Assert.Equal(BatteryVisualCategory.Charging, d.VisualCategory);
        Assert.Equal(BatteryNotificationCategory.High, d.Notification);
        Assert.Equal("80", d.TrayIconText);
    }

    [Fact]
    public void Charging_when_rate_is_negative_falls_back_to_no_eta_body()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.Charging, PowerLineStatus.Online,
                rateMilliwatts: -100, fullMwh: 50000, remainingMwh: 25000),
            DefaultThresholds(), Invariant);

        Assert.Equal(BatterySituation.Charging, d.Situation);
        Assert.Null(d.TooltipTitle);
    }

    [Fact]
    public void Discharging_above_low_threshold_at_high_threshold_returns_High_notification()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.High, PowerLineStatus.Offline, 0.80f),
            DefaultThresholds(), Invariant);

        Assert.Equal(BatteryVisualCategory.Normal, d.VisualCategory);
        Assert.Equal(BatteryNotificationCategory.High, d.Notification);
    }

    [Fact]
    public void Discharging_below_critical_threshold_returns_Critical_visual_and_notification()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.Critical, PowerLineStatus.Offline, 0.05f),
            DefaultThresholds(), Invariant);

        Assert.Equal(BatterySituation.Discharging, d.Situation);
        Assert.Equal(5, d.Percent);
        Assert.Equal(BatteryVisualCategory.Critical, d.VisualCategory);
        Assert.Equal(BatteryNotificationCategory.Critical, d.Notification);
        Assert.Equal("5", d.TrayIconText);
    }

    [Fact]
    public void Discharging_below_low_threshold_returns_Low_visual_and_notification()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.Low, PowerLineStatus.Offline, 0.15f),
            DefaultThresholds(), Invariant);

        Assert.Equal(BatteryVisualCategory.Low, d.VisualCategory);
        Assert.Equal(BatteryNotificationCategory.Low, d.Notification);
    }

    [Fact]
    public void Discharging_with_zero_remaining_seconds_returns_only_body_no_eta()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.High, PowerLineStatus.Offline, 0.50f, 0),
            DefaultThresholds(), Invariant);

        Assert.Null(d.TooltipTitle);
        Assert.NotEmpty(d.TooltipBody);
    }

    [Fact]
    public void Hundred_percent_full_notification_disabled_returns_None()
    {
        var thresholds = DefaultThresholds() with { FullNotificationEnabled = false };
        var d = BatteryEvaluator.Evaluate(
            Readings(percent: 1f),
            thresholds, Invariant);

        Assert.Equal(BatteryNotificationCategory.None, d.Notification);
    }

    [Fact]
    public void Hundred_percent_unplugged_returns_Full_situation_and_full_notification_when_enabled()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.High, PowerLineStatus.Offline, 1f),
            DefaultThresholds(), Invariant);

        Assert.Equal(BatterySituation.Full, d.Situation);
        Assert.Equal(100, d.Percent);
        Assert.Null(d.TrayIconText);
        Assert.Equal(BatteryNotificationCategory.Full, d.Notification);
    }

    [Fact]
    public void NoSystemBattery_returns_NoBattery_situation_with_red_X_glyph()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.NoSystemBattery),
            DefaultThresholds(), Invariant);

        Assert.Equal(BatterySituation.NoBattery, d.Situation);
        Assert.Equal("❌", d.TrayIconText);
        Assert.Equal(BatteryNotificationCategory.None, d.Notification);
    }

    [Fact]
    public void Unknown_charge_status_returns_question_mark_glyph()
    {
        var d = BatteryEvaluator.Evaluate(
            Readings(BatteryChargeStatus.Unknown),
            DefaultThresholds(), Invariant);

        Assert.Equal(BatterySituation.Unknown, d.Situation);
        Assert.Equal("❓", d.TrayIconText);
    }
}
