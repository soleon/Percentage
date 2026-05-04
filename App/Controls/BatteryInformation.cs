using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Windows.Devices.Power;
using Percentage.App.Extensions;
using Percentage.App.Localization;
using Percentage.App.Properties;
using Percentage.App.Resources;
using Percentage.App.Services;
using Wpf.Ui.Controls;
using PowerLineStatus = System.Windows.Forms.PowerLineStatus;

namespace Percentage.App.Controls;

/// <summary>
///     Details-page control that renders the live battery readout (capacity, charge rate, time
///     remaining, design / full / remaining capacity, status, health) as a list of
///     <c>KeyValuePair</c> rows. Refreshes on a debounced timer and on language switches; no
///     dependency properties because every value is computed from
///     <see cref="SystemBatterySource" /> + <c>Battery.AggregateBattery</c>.
/// </summary>
#pragma warning disable CA1001 // _updateSubject is a Subject<bool> whose only Dispose effect is OnCompleted; this control's lifetime is bounded by the host page (DetailsPage).
public partial class BatteryInformation : KeyValueItemsControl
#pragma warning restore CA1001
{
    private readonly BatteryInformationObservableValue _batteryHealth =
        new(SymbolRegular.BatterySaver20, () => Strings.Battery_Health);

    private readonly BatteryInformationObservableValue _batteryLifePercent =
        new(SymbolRegular.Battery520, () => Strings.Battery_Capacity);

    private readonly BatteryInformationObservableValue _batteryStatus =
        new(SymbolRegular.CommentLightning20, () => Strings.Battery_Status);

    private readonly BatteryInformationObservableValue _batteryTime =
        new(SymbolRegular.Clock20, () => Strings.Battery_Time);

    private readonly BatteryInformationObservableValue _chargeRate =
        new(SymbolRegular.BatteryCharge20, () => Strings.Battery_ChargeRate);

    private readonly BatteryInformationObservableValue _designCapacity =
        new(SymbolRegular.BatteryCheckmark20, () => Strings.Battery_DesignCapacity);

    private readonly BatteryInformationObservableValue _fullChargeCapacity =
        new(SymbolRegular.Battery1020, () => Strings.Battery_FullChargeCapacity);

    private readonly BatteryInformationObservableValue _powerLineStatus =
        new(SymbolRegular.LineStyle20, () => Strings.Battery_PowerLineStatus);

    private readonly BatteryInformationObservableValue _remainingChargeCapacity =
        new(SymbolRegular.Battery020, () => Strings.Battery_RemainingChargeCapacity);

    private readonly Subject<bool> _updateSubject = new();

    static BatteryInformation()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BatteryInformation),
            new FrameworkPropertyMetadata(typeof(BatteryInformation)));
    }

    /// <summary>
    ///     Wires the control to a debounced 500ms refresh subject driven by an interval timer
    ///     (<c>Settings.RefreshSeconds</c>) plus language-change events, and unsubscribes on
    ///     <see cref="FrameworkElement.Unloaded" />.
    /// </summary>
    public BatteryInformation()
    {
        RebuildItemsSource();

        IDisposable? updateSubscription = null;
        Loaded += (_, _) =>
        {
            // This is a hack to get around a strange timing bug.
            // When the control is loaded, the CardControl in the item containers may have a null style.
            // This loop finds any CardControl in the item container that has a null style and sets the correct default
            // style to it.
            for (var i = 0; i < ItemContainerGenerator.Items.Count; i++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter presenter &&
                    VisualTreeHelper.GetChildrenCount(presenter) > 0 &&
                    VisualTreeHelper.GetChild(presenter, 0) is CardControl { Style: null } card &&
                    FindResource(typeof(CardControl)) is Style style)
                {
                    card.Style = style;
                }
            }

            // try/catch so a transient battery/WMI failure surfaces through SetAppError
            // instead of terminating the Rx chain and silently freezing this control.
            _updateSubject.Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(AsyncOperationManager.SynchronizationContext)
                .Subscribe(_ =>
                {
                    try
                    {
                        Update();
                    }
                    catch (Exception ex) when (ex is not OutOfMemoryException)
                    {
                        App.SetAppError(ex);
                    }
                });

            _updateSubject.OnNext(false);

            SetupUpdateSubscription();

            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    handler => Settings.Default.PropertyChanged += handler,
                    handler => Settings.Default.PropertyChanged -= handler)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ => SetupUpdateSubscription());

            // Rebuild row labels and re-run Update() on language switches so both the keys
            // (rendered as headers) and the values (e.g. "Unknown", "Connected") refresh live.
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;

            // The DetailsPage uses NavigationCacheMode="Enabled", so this control may have been
            // unloaded (and unsubscribed) when the user changed the language on another page.
            // Rebuild now to pick up any culture change that happened while we were detached.
            RebuildItemsSource();

            return;

            void SetupUpdateSubscription()
            {
                updateSubscription?.Dispose();
                updateSubscription = Observable.Interval(TimeSpan.FromSeconds(Settings.Default.RefreshSeconds))
                    .ObserveOn(AsyncOperationManager.SynchronizationContext)
                    .Subscribe(_ => _updateSubject.OnNext(false));
            }
        };

        Unloaded += (_, _) =>
        {
            updateSubscription?.Dispose();
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
        };
    }

    [GeneratedRegex(@"\B[A-Z]")]
    private static partial Regex WordStartLetterRegex();

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            RebuildItemsSource();
            _updateSubject.OnNext(false);
        });
    }

    private void RebuildItemsSource()
    {
        BatteryInformationObservableValue[] ordered =
        [
            _batteryLifePercent,
            _chargeRate,
            _powerLineStatus,
            _batteryTime,
            _designCapacity,
            _fullChargeCapacity,
            _remainingChargeCapacity,
            _batteryStatus,
            _batteryHealth
        ];

        Dictionary<string, object> dict = new(ordered.Length);
        foreach (var entry in ordered)
        {
            dict[entry.Name] = entry;
        }

        ItemsSource = dict;
    }

    /// <summary>
    ///     Pushes through the debounce so callers (e.g. <c>DetailsPage</c>) can force the next
    ///     visible refresh after an external event.
    /// </summary>
    internal void RequestUpdate() => _updateSubject.OnNext(false);

    private void Update()
    {
        var readings = SystemBatterySource.Instance.Read();

        _batteryLifePercent.Value = readings.ChargeStatus == BatteryChargeStatus.NoSystemBattery
            ? Strings.Battery_ValueUnknown
            : readings.BatteryLifePercent.ToString("P", CultureInfo.CurrentCulture);

        var rate = readings.ChargeRateInMilliwatts;
        var full = readings.FullChargeCapacityInMilliwattHours;
        var remaining = readings.RemainingCapacityInMilliwattHours;

        switch (rate)
        {
            case null:
                _batteryTime.Value = _chargeRate.Value = Strings.Battery_ValueUnknown;
                break;
            case 0:
                _batteryTime.Value = Strings.Battery_ValueUnknown;
                _chargeRate.Value = Strings.Battery_ValueNotCharging;
                break;
            default:
                if (rate > 0)
                {
                    _batteryTime.Value = full is { } f && remaining is { } r
                        ? string.Format(CultureInfo.CurrentCulture, Strings.Battery_TimeUntilFull,
                            ReadableExtensions.GetReadableTimeSpan(TimeSpan.FromHours((f - r) / (double)rate.Value)))
                        : Strings.Battery_ValueUnknown;
                }
                else
                {
                    _batteryTime.Value = string.Format(CultureInfo.CurrentCulture, Strings.Battery_TimeRemaining,
                        ReadableExtensions.GetReadableTimeSpan(
                            TimeSpan.FromSeconds(readings.BatteryLifeRemainingSeconds)));
                }

                _chargeRate.Value = rate + " mW";
                break;
        }

        _powerLineStatus.Value = readings.LineStatus switch
        {
            PowerLineStatus.Online => Strings.Battery_ValueConnected,
            PowerLineStatus.Offline => Strings.Battery_ValueDisconnected,
            _ => Strings.Battery_ValueUnknown
        };

        _designCapacity.Value = readings.DesignCapacityInMilliwattHours is { } designCapacity
            ? designCapacity + " mWh"
            : Strings.Battery_ValueUnknown;
        _fullChargeCapacity.Value = full is { } fc ? fc + " mWh" : Strings.Battery_ValueUnknown;
        _remainingChargeCapacity.Value = remaining is { } rc ? rc + " mWh" : Strings.Battery_ValueUnknown;

        if (readings.DesignCapacityInMilliwattHours is { } design && full is { } ff)
        {
            var health = (double)ff / design;
            _batteryHealth.Value = (health > 1 ? 1 : health).ToString("P", CultureInfo.CurrentCulture);
        }
        else
        {
            _batteryHealth.Value = Strings.Battery_ValueUnknown;
        }

        // BatteryReport.Status (Windows.System.Power.BatteryStatus enum) is the friendly
        // status string ("Charging", "Discharging", etc.). Not exposed via BatteryReadings
        // because the rest of the evaluator uses BatteryChargeStatus instead; this single
        // direct call is cheap.
        var aggregateReport = Battery.AggregateBattery.GetReport();
        _batteryStatus.Value = WordStartLetterRegex().Replace(aggregateReport.Status.ToString(), " $0");
    }

    private sealed class BatteryInformationObservableValue(SymbolRegular icon, Func<string> nameProvider)
        : SymbolIconObservableValue<string>(icon)
    {
        internal string Name => nameProvider();
    }

    private class SymbolIconObservableValue<T>(SymbolRegular symbol) : ObservableValue<T>
    {
        public SymbolIcon SymbolIcon { get; } = new(symbol);

        public override string? ToString() => Value?.ToString();
    }
}
