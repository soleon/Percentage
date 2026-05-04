namespace Percentage.App.Services;

/// <summary>The notification category that the evaluator decided fits the current readings.</summary>
internal enum BatteryNotificationCategory
{
    /// <summary>No notification fires.</summary>
    None = 0,

    /// <summary>Critical-threshold notification.</summary>
    Critical,

    /// <summary>Low-threshold notification.</summary>
    Low,

    /// <summary>High-threshold (charging-up) notification.</summary>
    High,

    /// <summary>Full (100%) notification.</summary>
    Full
}
