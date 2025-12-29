using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Windows.Devices.Power;
using Microsoft.Win32;
using Percentage.App.Extensions;
using Percentage.App.Pages;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using PowerLineStatus = System.Windows.Forms.PowerLineStatus;
using TimeSpan = System.TimeSpan;
using static Percentage.App.Properties.Settings;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using NotifyIcon = Wpf.Ui.Tray.Controls.NotifyIcon;
using TextBlock = System.Windows.Controls.TextBlock;

namespace Percentage.App;

public partial class NotifyIconWindow
{
    private static readonly TimeSpan DebounceTimeSpan = TimeSpan.FromMilliseconds(500);
    private readonly Subject<bool> _batteryStatusUpdateSubject = new();
    private readonly DispatcherTimer _refreshTimer;

    private (ToastNotificationExtensions.NotificationType Type, DateTime DateTime) _lastNotification =
        (default, default);

    private string? _notificationText;
    private string? _notificationTitle;

    public NotifyIconWindow()
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();

        // Set up the timer to update the tray icon.
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Default.RefreshSeconds) };
        _refreshTimer.Tick += (_, _) => _batteryStatusUpdateSubject.OnNext(false);
    }

    private static async Task ShutDownAsync()
    {
        if (!Default.ShutDownWithoutConfirmation)
        {
            var result = await new MessageBox
            {
                Title = "Shut Down",
                Content = "Are you sure you want to shut down your device?",
                PrimaryButtonAppearance = ControlAppearance.Caution,
                PrimaryButtonText = "Yes",
                SecondaryButtonAppearance = ControlAppearance.Caution,
                SecondaryButtonText = "Always Yes",
                CloseButtonText = "No"
            }.ShowDialogAsync().ConfigureAwait(false);

            switch (result)
            {
                case MessageBoxResult.None:
                    return;
                case MessageBoxResult.Primary:
                    break;
                case MessageBoxResult.Secondary:
                    Default.ShutDownWithoutConfirmation = true;
                    Default.Save();
                    break;
                default:
                    throw new InvalidEnumArgumentException($"{result} is not a supported enum value.");
            }
        }

        ExternalProcessExtensions.ShutDownDevice();
    }

    private void OnAboutMenuItemClick(object sender, RoutedEventArgs e)
    {
        Application.Current.ActivateMainWindow().NavigateToPage<AboutPage>();
    }

    private void OnAppSettingsMenuItemClick(object sender, RoutedEventArgs e)
    {
        Application.Current.ActivateMainWindow().NavigateToPage<SettingsPage>();
    }

    private void OnDetailsMenuItemClick(object sender, RoutedEventArgs e)
    {
        Application.Current.ActivateMainWindow().NavigateToPage<DetailsPage>();
    }

    private void OnExitMenuItemClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        Visibility = Visibility.Collapsed;

        if (!Default.HideAtStartup) Application.Current.ActivateMainWindow().NavigateToPage<DetailsPage>();

        // Debounce all calls to update battery status.
        // This should be the only place that calls the UpdateBatteryStatus method.
        _batteryStatusUpdateSubject.Throttle(DebounceTimeSpan).ObserveOn(AsyncOperationManager.SynchronizationContext)
            .Subscribe(_ => UpdateBatteryStatus());

        // Update battery status when the computer resumes or when the power status changes with debouncing.
        SystemEvents.PowerModeChanged += (_, _) => _batteryStatusUpdateSubject.OnNext(false);

        // Update battery status when the display settings change with debouncing.
        // This will redraw the tray icon to ensure optimal icon resolution under the current display settings.
        SystemEvents.DisplaySettingsChanged += (_, _) => _batteryStatusUpdateSubject.OnNext(false);

        // This event can be triggered multiple times when Windows changes between dark and light theme.
        // Update tray icon colour when user preference changes settled down.
        SystemEvents.UserPreferenceChanged += (_, _) => _batteryStatusUpdateSubject.OnNext(false);

        // Handle user settings change with debouncing.
        Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => Default.PropertyChanged += handler,
                handler => Default.PropertyChanged -= handler)
            .Throttle(DebounceTimeSpan)
            .ObserveOn(AsyncOperationManager.SynchronizationContext)
            .Subscribe(pattern => OnUserSettingsPropertyChanged(pattern.EventArgs.PropertyName));

        // Initial update.
        _batteryStatusUpdateSubject.OnNext(false);

        // Kick off timer to update the tray icon.
        _refreshTimer.Start();
    }

    private void OnNotifyIconLeftClick(NotifyIcon sender, RoutedEventArgs e)
    {
        if (!Default.DoubleClickActivation)
            Application.Current.ActivateMainWindow().NavigateToPage<DetailsPage>();
    }

    private void OnNotifyIconLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e)
    {
        if (Default.DoubleClickActivation)
            Application.Current.ActivateMainWindow().NavigateToPage<DetailsPage>();
    }

    private void OnShutDownMenuItemClick(object sender, RoutedEventArgs e)
    {
        _ = ShutDownAsync();
    }

    private void OnSleepMenuItemClick(object sender, RoutedEventArgs e)
    {
        ExternalProcessExtensions.SleepDevice();
    }

    private void OnSystemSettingsMenuItemClick(object sender, RoutedEventArgs e)
    {
        ExternalProcessExtensions.OpenPowerSettings();
    }

    private void OnUserSettingsPropertyChanged(string? propertyName)
    {
        // Always save settings change immediately in case the app crashes, losing all changes.
        Default.Save();

        switch (propertyName)
        {
            case nameof(Default.RefreshSeconds):
                _refreshTimer.Interval = TimeSpan.FromSeconds(Default.RefreshSeconds);
                break;
            case nameof(Default.BatteryCriticalNotificationValue):
                if (Default.BatteryLowNotificationValue < Default.BatteryCriticalNotificationValue)
                    Default.BatteryLowNotificationValue = Default.BatteryCriticalNotificationValue;
                if (Default.BatteryHighNotificationValue < Default.BatteryCriticalNotificationValue)
                    Default.BatteryHighNotificationValue = Default.BatteryCriticalNotificationValue;
                break;
            case nameof(Default.BatteryLowNotificationValue):
                if (Default.BatteryCriticalNotificationValue > Default.BatteryLowNotificationValue)
                    Default.BatteryCriticalNotificationValue = Default.BatteryLowNotificationValue;
                if (Default.BatteryHighNotificationValue < Default.BatteryLowNotificationValue)
                    Default.BatteryHighNotificationValue = Default.BatteryLowNotificationValue;
                break;
            case nameof(Default.BatteryHighNotificationValue):
                if (Default.BatteryCriticalNotificationValue > Default.BatteryHighNotificationValue)
                    Default.BatteryCriticalNotificationValue = Default.BatteryHighNotificationValue;
                if (Default.BatteryLowNotificationValue > Default.BatteryHighNotificationValue)
                    Default.BatteryLowNotificationValue = Default.BatteryHighNotificationValue;
                break;
        }

        _batteryStatusUpdateSubject.OnNext(false);
    }

    public void RequestBatteryStatusUpdate()
    {
        _batteryStatusUpdateSubject.OnNext(false);
    }

    private void SetNotifyIconText(string text, Brush foreground)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = foreground,
            FontSize = Default.TrayIconFontSize
        };

        if (Default.TrayIconFontFamily != null) textBlock.FontFamily = Default.TrayIconFontFamily;

        if (Default.TrayIconFontBold) textBlock.FontWeight = FontWeights.Bold;

        if (Default.TrayIconFontUnderline) textBlock.TextDecorations = TextDecorations.Underline;

        NotifyIcon.SetIcon(textBlock);
    }

    private void UpdateBatteryStatus()
    {
        var powerStatus = SystemInformation.PowerStatus;
        var batteryChargeStatus = powerStatus.BatteryChargeStatus;
        var percent = (int)Math.Round(powerStatus.BatteryLifePercent * 100);
        var notificationType = ToastNotificationExtensions.NotificationType.None;
        Brush brush;
        string trayIconText;
        switch (batteryChargeStatus)
        {
            case BatteryChargeStatus.NoSystemBattery:
                // When no battery detected.
                trayIconText = "❌";
                brush = BrushExtensions.GetBatteryNormalBrush();
                _notificationTitle = null;
                _notificationText = "No battery detected";
                break;
            case BatteryChargeStatus.Unknown:
                // When battery status is unknown.
                trayIconText = "❓";
                brush = BrushExtensions.GetBatteryNormalBrush();
                break;
            case BatteryChargeStatus.High:
            case BatteryChargeStatus.Low:
            case BatteryChargeStatus.Critical:
            case BatteryChargeStatus.Charging:
            default:
            {
                if (percent == 100)
                {
                    NotifyIcon.SetBatteryFullIcon();

                    var powerLineText = powerStatus.PowerLineStatus == PowerLineStatus.Online
                        ? " and connected to power"
                        : null;

                    NotifyIcon.TooltipText = _notificationText = "Your battery is fully charged" + powerLineText;

                    // If we don't need to show a fully charged notification, we can return straight away.
                    if (!Default.BatteryFullNotification) return;

                    // Show fully charged notification.

                    _notificationTitle = "Fully charged" + powerLineText;
                    notificationType = ToastNotificationExtensions.NotificationType.Full;
                    CheckAndSendNotification();

                    return;
                }

                // When battery status is normal, display percentage in tray icon.
                trayIconText = percent.ToString();
                if (batteryChargeStatus.HasFlag(BatteryChargeStatus.Charging))
                {
                    // When the battery is charging.
                    brush = BrushExtensions.GetBatteryChargingBrush();
                    var report = Battery.AggregateBattery.GetReport();
                    var chargeRateInMilliWatts = report.ChargeRateInMilliwatts;
                    if (chargeRateInMilliWatts > 0)
                    {
                        _notificationTitle = percent + "% charging";

                        var fullChargeCapacityInMilliWattHours = report.FullChargeCapacityInMilliwattHours;
                        var remainingCapacityInMilliWattHours = report.RemainingCapacityInMilliwattHours;
                        if (fullChargeCapacityInMilliWattHours.HasValue &&
                            remainingCapacityInMilliWattHours.HasValue)
                            _notificationText = ReadableExtensions.GetReadableTimeSpan(TimeSpan.FromHours(
                                (fullChargeCapacityInMilliWattHours.Value -
                                 remainingCapacityInMilliWattHours.Value) /
                                (double)chargeRateInMilliWatts.Value)) + " until fully charged";
                    }
                    else
                    {
                        _notificationTitle = null;
                        _notificationText = percent + "% charging";
                    }

                    SetHighOrFullNotification();
                }
                else
                {
                    // When battery is not charging.
                    if (percent <= Default.BatteryCriticalNotificationValue)
                    {
                        // When battery capacity is critical.
                        brush = BrushExtensions.GetBatteryCriticalBrush();
                        if (Default.BatteryCriticalNotification)
                            notificationType = ToastNotificationExtensions.NotificationType.Critical;
                    }
                    else if (percent <= Default.BatteryLowNotificationValue)
                    {
                        // When battery capacity is low.
                        brush = BrushExtensions.GetBatteryLowBrush();
                        if (Default.BatteryLowNotification)
                            notificationType = ToastNotificationExtensions.NotificationType.Low;
                    }
                    else
                    {
                        // When battery capacity is normal.
                        brush = BrushExtensions.GetBatteryNormalBrush();
                        SetHighOrFullNotification();
                    }

                    if (powerStatus.BatteryLifeRemaining > 0)
                    {
                        _notificationTitle = $"{percent}% {(powerStatus.PowerLineStatus == PowerLineStatus.Online
                            ? "connected (not charging)"
                            : "on battery")}";
                        _notificationText =
                            ReadableExtensions.GetReadableTimeSpan(
                                TimeSpan.FromSeconds(powerStatus.BatteryLifeRemaining)) +
                            " remaining";
                    }
                    else
                    {
                        _notificationTitle = null;
                        _notificationText =
                            $"{percent}% {(powerStatus.PowerLineStatus == PowerLineStatus.Online
                                ? "connected (not charging)"
                                : "on battery")}";
                    }
                }

                break;

                void SetHighOrFullNotification()
                {
                    if (percent == Default.BatteryHighNotificationValue && Default.BatteryHighNotification)
                        notificationType = ToastNotificationExtensions.NotificationType.High;
                    else if (percent == 100 && Default.BatteryFullNotification)
                        notificationType = ToastNotificationExtensions.NotificationType.Full;
                }
            }
        }

        // Set the tray icon tool tip based on the balloon notification texts.
        NotifyIcon.TooltipText = _notificationTitle == null
            ? _notificationText ?? string.Empty
            : _notificationTitle + Environment.NewLine + _notificationText;

        SetNotifyIconText(trayIconText, brush);

        CheckAndSendNotification();
        return;

        // Check and send a notification.
        void CheckAndSendNotification()
        {
            if (notificationType == ToastNotificationExtensions.NotificationType.None)
                // No notification required.
                return;

            var utcNow = DateTime.UtcNow;
            if (_lastNotification.Type != notificationType ||
                utcNow - _lastNotification.DateTime > TimeSpan.FromMinutes(5))
                // Notification is required if the existing notification type is different from the previous one or
                // battery status is the same, but it has been more than 5 minutes since the last notification was shown.
                ToastNotificationExtensions.ShowToastNotification(_notificationTitle, _notificationText,
                    notificationType);

            _lastNotification = (notificationType, utcNow);
        }
    }
}