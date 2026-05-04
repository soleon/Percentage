namespace Percentage.App.Services;

/// <summary>The high-level battery situation reported by <see cref="BatteryEvaluator" />.</summary>
internal enum BatterySituation
{
    /// <summary>No battery present.</summary>
    NoBattery,

    /// <summary>Battery present but state cannot be read.</summary>
    Unknown,

    /// <summary>Battery at 100% (Full).</summary>
    Full,

    /// <summary>Battery is charging (any percentage below 100).</summary>
    Charging,

    /// <summary>Battery is discharging.</summary>
    Discharging
}
