using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Windows.Devices.Power;
using Windows.System.Power;
using Percentage.App.Extensions;
using Percentage.App.Properties;
using Wpf.Ui.Controls;
using PowerLineStatus = System.Windows.Forms.PowerLineStatus;

namespace Percentage.App.Controls;

public partial class BatteryInformation : KeyValueItemsControl
{
    private readonly BatteryInformationObservableValue _batteryHealth =
        new(SymbolRegular.BatterySaver20, "Battery Health");

    private readonly BatteryInformationObservableValue _batteryLifePercent =
        new(SymbolRegular.Battery520, "Battery Capacity");

    private readonly BatteryInformationObservableValue _batteryStatus =
        new(SymbolRegular.CommentLightning20, "Battery Status");

    private readonly BatteryInformationObservableValue _batteryTime =
        new(SymbolRegular.Clock20, "Battery Time");

    private readonly BatteryInformationObservableValue _chargeRate =
        new(SymbolRegular.BatteryCharge20, "Charge Rate");

    private readonly BatteryInformationObservableValue _designCapacity =
        new(SymbolRegular.BatteryCheckmark20, "Design Capacity");

    private readonly BatteryInformationObservableValue _fullChargeCapacity =
        new(SymbolRegular.Battery1020, "Full Charge Capacity");

    private readonly BatteryInformationObservableValue _powerLineStatus =
        new(SymbolRegular.LineStyle20, "Power Line Status");

    private readonly BatteryInformationObservableValue _remainingChargeCapacity =
        new(SymbolRegular.Battery020, "Remaining Charge Capacity");
    
    private readonly Subject<bool> _updateSubject = new();

    static BatteryInformation()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BatteryInformation),
            new FrameworkPropertyMetadata(typeof(BatteryInformation)));
    }

    public BatteryInformation()
    {
        ItemsSource = new[]
        {
            _batteryLifePercent,
            _chargeRate,
            _powerLineStatus,
            _batteryTime,
            _designCapacity,
            _fullChargeCapacity,
            _remainingChargeCapacity,
            _batteryStatus,
            _batteryHealth
        }.ToDictionary(x => x.Name, object (x) => x);

        IDisposable? updateSubscription = null;
        Loaded += (_, _) =>
        {
            // This is a hack to get around a strange timing bug.
            // When the control is loaded, the CardControl in the item containers may have a null style.
            // This loop finds any CardControl in the item container that has a null style and sets the correct default
            // style to it.
            for (var i = 0; i < ItemContainerGenerator.Items.Count; i++)
                if (ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter presenter &&
                    VisualTreeHelper.GetChildrenCount(presenter) > 0 &&
                    VisualTreeHelper.GetChild(presenter, 0) is CardControl { Style: null } card &&
                    FindResource(typeof(CardControl)) is Style style)
                    card.Style = style;

            _updateSubject.Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(AsyncOperationManager.SynchronizationContext)
                .Subscribe(_ => Update());

            _updateSubject.OnNext(false);

            SetupUpdateSubscription();

            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    handler => Settings.Default.PropertyChanged += handler,
                    handler => Settings.Default.PropertyChanged -= handler)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ => SetupUpdateSubscription());

            return;

            void SetupUpdateSubscription()
            {
                updateSubscription?.Dispose();
                updateSubscription = Observable.Interval(TimeSpan.FromSeconds(Settings.Default.RefreshSeconds))
                    .ObserveOn(AsyncOperationManager.SynchronizationContext).Subscribe(_ => _updateSubject.OnNext(false));
            }
        };

        Unloaded += (_, _) => { updateSubscription?.Dispose(); };
    }

    [GeneratedRegex(@"\B[A-Z]")]
    private static partial Regex WordStartLetterRegex();

    private void Update()
    {
        var report = Battery.AggregateBattery.GetReport();
        var powerStatus = SystemInformation.PowerStatus;
        _batteryLifePercent.Value = report.Status == BatteryStatus.NotPresent
            ? "Unknown"
            : powerStatus.BatteryLifePercent.ToString("P");
        var chargeRateInMilliWatts = report.ChargeRateInMilliwatts;
        var fullChargeCapacityInMilliWattHours = report.FullChargeCapacityInMilliwattHours;
        var remainingCapacityInMilliWattHours = report.RemainingCapacityInMilliwattHours;
        switch (chargeRateInMilliWatts)
        {
            case null:
                _batteryTime.Value = _chargeRate.Value = "Unknown";
                break;
            case 0:
                _batteryTime.Value = "Unknown";
                _chargeRate.Value = "Not Charging";
                break;
            default:
                if (chargeRateInMilliWatts > 0)
                {
                    if (fullChargeCapacityInMilliWattHours.HasValue && remainingCapacityInMilliWattHours.HasValue)
                        _batteryTime.Value =
                            $"{ReadableExtensions.GetReadableTimeSpan(TimeSpan.FromHours((fullChargeCapacityInMilliWattHours.Value - remainingCapacityInMilliWattHours.Value) / (double)chargeRateInMilliWatts.Value))} until full";
                    else
                        _batteryTime.Value = "Unknown";
                }
                else
                {
                    _batteryTime.Value =
                        $"{ReadableExtensions.GetReadableTimeSpan(TimeSpan.FromSeconds(powerStatus.BatteryLifeRemaining))} remaining";
                }

                _chargeRate.Value = chargeRateInMilliWatts + " mW";
                break;
        }

        _powerLineStatus.Value = powerStatus.PowerLineStatus switch
        {
            PowerLineStatus.Online => "Connected",
            PowerLineStatus.Offline => "Disconnected",
            _ => "Unknown"
        };
        var designCapacity = report.DesignCapacityInMilliwattHours;
        _designCapacity.Value = designCapacity == null
            ? "Unknown"
            : designCapacity + " mWh";
        _fullChargeCapacity.Value = fullChargeCapacityInMilliWattHours == null
            ? "Unknown"
            : fullChargeCapacityInMilliWattHours + " mWh";
        _remainingChargeCapacity.Value = remainingCapacityInMilliWattHours == null
            ? "Unknown"
            : remainingCapacityInMilliWattHours + " mWh";
        if (designCapacity != null && fullChargeCapacityInMilliWattHours != null)
        {
            var health = (double)fullChargeCapacityInMilliWattHours.Value / designCapacity.Value;
            _batteryHealth.Value = (health > 1 ? 1 : health).ToString("P");
        }
        else
        {
            _batteryHealth.Value = "Unknown";
        }

        // Inserts a space between each word in battery status.
        _batteryStatus.Value = WordStartLetterRegex().Replace(report.Status.ToString(), " $0");
    }

    private sealed class BatteryInformationObservableValue(SymbolRegular icon, string name)
        : SymbolIconObservableValue<string>(icon)
    {
        internal string Name => name;
    }

    private class SymbolIconObservableValue<T>(SymbolRegular symbol) : ObservableValue<T>
    {
        public SymbolIcon SymbolIcon { get; } = new(symbol);

        public override string? ToString()
        {
            return Value?.ToString();
        }
    }

    internal void RequestUpdate()
    {
        _updateSubject.OnNext(false);
    }
}