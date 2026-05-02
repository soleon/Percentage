using System;
using System.Collections.Generic;
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
using Windows.System.Power;
using Percentage.App.Extensions;
using Percentage.App.Localization;
using Percentage.App.Properties;
using Percentage.App.Resources;
using Wpf.Ui.Controls;
using PowerLineStatus = System.Windows.Forms.PowerLineStatus;

namespace Percentage.App.Controls;

public partial class BatteryInformation : KeyValueItemsControl
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
                    .ObserveOn(AsyncOperationManager.SynchronizationContext).Subscribe(_ => _updateSubject.OnNext(false));
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
        Dispatcher.InvokeAsync(() =>
        {
            RebuildItemsSource();
            _updateSubject.OnNext(false);
        });
    }

    private void RebuildItemsSource()
    {
        var ordered = new[]
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
        };

        var dict = new Dictionary<string, object>(ordered.Length);
        foreach (var entry in ordered)
            dict[entry.Name] = entry;
        ItemsSource = dict;
    }

    private void Update()
    {
        var report = Battery.AggregateBattery.GetReport();
        var powerStatus = SystemInformation.PowerStatus;
        _batteryLifePercent.Value = report.Status == BatteryStatus.NotPresent
            ? Strings.Battery_ValueUnknown
            : powerStatus.BatteryLifePercent.ToString("P", CultureInfo.CurrentCulture);
        var chargeRateInMilliWatts = report.ChargeRateInMilliwatts;
        var fullChargeCapacityInMilliWattHours = report.FullChargeCapacityInMilliwattHours;
        var remainingCapacityInMilliWattHours = report.RemainingCapacityInMilliwattHours;
        switch (chargeRateInMilliWatts)
        {
            case null:
                _batteryTime.Value = _chargeRate.Value = Strings.Battery_ValueUnknown;
                break;
            case 0:
                _batteryTime.Value = Strings.Battery_ValueUnknown;
                _chargeRate.Value = Strings.Battery_ValueNotCharging;
                break;
            default:
                if (chargeRateInMilliWatts > 0)
                {
                    if (fullChargeCapacityInMilliWattHours.HasValue && remainingCapacityInMilliWattHours.HasValue)
                        _batteryTime.Value = string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.Battery_TimeUntilFull,
                            ReadableExtensions.GetReadableTimeSpan(TimeSpan.FromHours(
                                (fullChargeCapacityInMilliWattHours.Value - remainingCapacityInMilliWattHours.Value)
                                / (double)chargeRateInMilliWatts.Value)));
                    else
                        _batteryTime.Value = Strings.Battery_ValueUnknown;
                }
                else
                {
                    _batteryTime.Value = string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.Battery_TimeRemaining,
                        ReadableExtensions.GetReadableTimeSpan(TimeSpan.FromSeconds(powerStatus.BatteryLifeRemaining)));
                }

                _chargeRate.Value = chargeRateInMilliWatts + " mW";
                break;
        }

        _powerLineStatus.Value = powerStatus.PowerLineStatus switch
        {
            PowerLineStatus.Online => Strings.Battery_ValueConnected,
            PowerLineStatus.Offline => Strings.Battery_ValueDisconnected,
            _ => Strings.Battery_ValueUnknown
        };
        var designCapacity = report.DesignCapacityInMilliwattHours;
        _designCapacity.Value = designCapacity == null
            ? Strings.Battery_ValueUnknown
            : designCapacity + " mWh";
        _fullChargeCapacity.Value = fullChargeCapacityInMilliWattHours == null
            ? Strings.Battery_ValueUnknown
            : fullChargeCapacityInMilliWattHours + " mWh";
        _remainingChargeCapacity.Value = remainingCapacityInMilliWattHours == null
            ? Strings.Battery_ValueUnknown
            : remainingCapacityInMilliWattHours + " mWh";
        if (designCapacity != null && fullChargeCapacityInMilliWattHours != null)
        {
            var health = (double)fullChargeCapacityInMilliWattHours.Value / designCapacity.Value;
            _batteryHealth.Value = (health > 1 ? 1 : health).ToString("P", CultureInfo.CurrentCulture);
        }
        else
        {
            _batteryHealth.Value = Strings.Battery_ValueUnknown;
        }

        // Inserts a space between each word in battery status. Note: the source enum names are
        // English; spacing them is a presentation tweak, not a translation.
        _batteryStatus.Value = WordStartLetterRegex().Replace(report.Status.ToString(), " $0");
    }

    private sealed class BatteryInformationObservableValue(SymbolRegular icon, Func<string> nameProvider)
        : SymbolIconObservableValue<string>(icon)
    {
        internal string Name => nameProvider();
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
