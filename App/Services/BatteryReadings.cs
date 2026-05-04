using System.Windows.Forms;

namespace Percentage.App.Services;

/// <summary>
///     Plain-data snapshot of the operating-system battery state at a single instant. Decoupled from
///     <see cref="System.Windows.Forms.SystemInformation.PowerStatus" /> and
///     <c>Windows.Devices.Power.Battery.AggregateBattery</c> so the evaluator can be unit tested
///     without a live OS battery.
/// </summary>
/// <param name="ChargeStatus">Charge status flags from <c>SystemInformation.PowerStatus</c>.</param>
/// <param name="LineStatus">AC vs battery from <c>SystemInformation.PowerStatus</c>.</param>
/// <param name="BatteryLifePercent">0.0 - 1.0 fraction of remaining capacity.</param>
/// <param name="BatteryLifeRemainingSeconds">Seconds remaining as reported by Windows; 0 / -1 when unknown.</param>
/// <param name="ChargeRateInMilliwatts">
///     Aggregate charge rate (positive = charging, 0 = idle, &lt;0 = discharging); null
///     when not reported.
/// </param>
/// <param name="FullChargeCapacityInMilliwattHours">Full charge capacity; null when not reported.</param>
/// <param name="RemainingCapacityInMilliwattHours">Remaining charge capacity; null when not reported.</param>
/// <param name="DesignCapacityInMilliwattHours">Battery design capacity; null when not reported.</param>
internal readonly record struct BatteryReadings(
    BatteryChargeStatus ChargeStatus,
    PowerLineStatus LineStatus,
    float BatteryLifePercent,
    int BatteryLifeRemainingSeconds,
    int? ChargeRateInMilliwatts,
    int? FullChargeCapacityInMilliwattHours,
    int? RemainingCapacityInMilliwattHours,
    int? DesignCapacityInMilliwattHours);
