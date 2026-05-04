namespace Percentage.App.Services;

/// <summary>
///     Abstraction over the OS battery APIs. The default implementation
///     (<see cref="SystemBatterySource" />) reads from <c>SystemInformation.PowerStatus</c> and
///     <c>Battery.AggregateBattery</c>; tests substitute their own readings.
/// </summary>
internal interface IBatterySource
{
    /// <summary>Returns a single synchronised snapshot of the OS battery state.</summary>
    BatteryReadings Read();
}
