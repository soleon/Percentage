using System.Windows.Forms;
using Windows.Devices.Power;

namespace Percentage.App.Services;

/// <summary>
///     Default <see cref="IBatterySource" />: pulls a snapshot from
///     <see cref="SystemInformation.PowerStatus" /> together with
///     <see cref="Battery.AggregateBattery" />'s <see cref="BatteryReport" /> so both legs read in
///     lockstep on each refresh.
/// </summary>
internal sealed class SystemBatterySource : IBatterySource
{
    private SystemBatterySource()
    {
    }

    /// <summary>Singleton instance; the source is stateless so a single shared reader is enough.</summary>
    public static SystemBatterySource Instance { get; } = new();

    /// <summary>Reads one synchronised <see cref="BatteryReadings" /> snapshot.</summary>
    public BatteryReadings Read()
    {
        var powerStatus = SystemInformation.PowerStatus;
        var report = Battery.AggregateBattery.GetReport();

        return new BatteryReadings(
            powerStatus.BatteryChargeStatus,
            powerStatus.PowerLineStatus,
            powerStatus.BatteryLifePercent,
            powerStatus.BatteryLifeRemaining,
            report.ChargeRateInMilliwatts,
            report.FullChargeCapacityInMilliwattHours,
            report.RemainingCapacityInMilliwattHours,
            report.DesignCapacityInMilliwattHours);
    }
}
