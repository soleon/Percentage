namespace Percentage.App.Services;

/// <summary>
///     Visual category used by callers to pick brushes; abstracts the brush layer out of the evaluator
///     so the evaluator stays pure (no <see cref="System.Windows.Media.Brush" /> dependency).
/// </summary>
internal enum BatteryVisualCategory
{
    /// <summary>Default state; callers pick the Normal brush.</summary>
    Normal,

    /// <summary>Battery is charging (any percentage below 100).</summary>
    Charging,

    /// <summary>Battery has crossed the Low threshold.</summary>
    Low,

    /// <summary>Battery has crossed the Critical threshold.</summary>
    Critical
}
