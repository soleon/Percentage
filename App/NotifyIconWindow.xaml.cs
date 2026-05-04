using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Percentage.App.Extensions;
using Percentage.App.Pages;
using Percentage.App.Resources;
using Percentage.App.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using static Percentage.App.Properties.Settings;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using NotifyIcon = Wpf.Ui.Tray.Controls.NotifyIcon;
using TextBlock = System.Windows.Controls.TextBlock;

namespace Percentage.App;

#pragma warning disable CA1001 // Owns _batteryStatusUpdateSubject; this is the app's tray-icon window with WPF Application lifetime.
public partial class NotifyIconWindow
#pragma warning restore CA1001
{
    private static readonly TimeSpan _debounceTimeSpan = TimeSpan.FromMilliseconds(500);
    private readonly Subject<bool> _batteryStatusUpdateSubject = new();
    private readonly DispatcherTimer _refreshTimer;
    private readonly TextBlock _trayTextBlock = new();

    private (ToastNotificationExtensions.NotificationType Type, DateTime DateTime) _lastNotification =
        (default, default);

    private string? _notificationText;
    private string? _notificationTitle;

    // Loaded can fire more than once if the window's content is re-rooted; the actual
    // Rx Subscribe / timer start below must run only once per process.
    private bool _initialised;

    public NotifyIconWindow()
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();

        // Set up the timer to update the tray icon.
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Default.RefreshSeconds) };
        _refreshTimer.Tick += (_, _) => _batteryStatusUpdateSubject.OnNext(false);

        // SystemEvents are static publishers that root their handler lambdas for the
        // process lifetime; attach them once in the constructor (not OnLoaded) so a
        // re-Loaded never stacks duplicate handlers. The actual subscriber (Subject
        // .Subscribe below) lives in OnLoaded for a different reason — see there.
        SystemEvents.PowerModeChanged += (_, _) => _batteryStatusUpdateSubject.OnNext(false);
        SystemEvents.DisplaySettingsChanged += (_, _) => _batteryStatusUpdateSubject.OnNext(false);
        SystemEvents.UserPreferenceChanged += (_, _) => _batteryStatusUpdateSubject.OnNext(false);
    }

    private static async Task ShutDownAsync()
    {
        if (!Default.ShutDownWithoutConfirmation)
        {
            var result = await new MessageBox
            {
                Title = Strings.Shutdown_DialogTitle,
                Content = Strings.Shutdown_DialogContent,
                PrimaryButtonAppearance = ControlAppearance.Caution,
                PrimaryButtonText = Strings.Shutdown_DialogYes,
                SecondaryButtonAppearance = ControlAppearance.Caution,
                SecondaryButtonText = Strings.Shutdown_DialogAlwaysYes,
                CloseButtonText = Strings.Shutdown_DialogNo
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

    private void OnAboutMenuItemClick(object sender, RoutedEventArgs e) =>
        Application.Current.ActivateMainWindow().NavigateToPage<AboutPage>();

    private void OnAppSettingsMenuItemClick(object sender, RoutedEventArgs e) =>
        Application.Current.ActivateMainWindow().NavigateToPage<SettingsPage>();

    private void OnDetailsMenuItemClick(object sender, RoutedEventArgs e) =>
        Application.Current.ActivateMainWindow().NavigateToPage<DetailsPage>();

    private void OnExitMenuItemClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        Visibility = Visibility.Collapsed;

        if (_initialised)
        {
            // Loaded fires per re-rooting; only do the one-time wiring + initial
            // tick / timer start the first time.
            return;
        }
        _initialised = true;

        if (!Default.HideAtStartup)
        {
            Application.Current.ActivateMainWindow().NavigateToPage<DetailsPage>();
        }

        // The Subject.Subscribe calls deliberately live here — not in the constructor —
        // so that the very first UpdateBatteryStatus, and therefore the first
        // notifyIcon.Icon = bitmap assignment, runs against an already-Registered
        // NotifyIcon. WPF-UI's TrayManager.ModifyIcon early-returns when
        // !IsRegistered (Wpf.Ui.Tray 4.2.1, TrayManager.cs:345-353) and IsRegistered
        // is only flipped true from inside NotifyIcon.OnRender (NotifyIcon.cs:1713-1721,
        // which calls Register() the first time it paints). Loaded fires after the
        // initial render pass, so subscribing here guarantees we never push an icon
        // before Register() has captured the HICON via Hicon.FromSource. SystemEvents
        // emissions that arrive earlier are intentionally dropped (Subject<T> doesn't
        // replay) — the OS rarely fires them before the visual tree is ready, and the
        // explicit OnNext below covers the cold-start refresh.
        //
        // Each subscriber wraps in try/catch routing to SetAppError so a transient OS
        // failure (e.g. WMI / Battery.AggregateBattery glitch) does not terminate the
        // Rx chain — an unhandled OnNext exception ends the subscription, which would
        // silently disable future tray refreshes.
        _batteryStatusUpdateSubject
            .Throttle(_debounceTimeSpan)
            .ObserveOn(AsyncOperationManager.SynchronizationContext)
            .Subscribe(_ =>
            {
                try
                {
                    UpdateBatteryStatus();
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    App.SetAppError(ex);
                }
            });

        Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => Default.PropertyChanged += handler,
                handler => Default.PropertyChanged -= handler)
            .Throttle(_debounceTimeSpan)
            .ObserveOn(AsyncOperationManager.SynchronizationContext)
            .Subscribe(pattern =>
            {
                try
                {
                    OnUserSettingsPropertyChanged(pattern.EventArgs.PropertyName);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    App.SetAppError(ex);
                }
            });

        // Initial update.
        _batteryStatusUpdateSubject.OnNext(false);

        // Kick off timer to update the tray icon.
        _refreshTimer.Start();
    }

    private void OnShutDownMenuItemClick(object sender, RoutedEventArgs e) => _ = ShutDownAsync();

    private void OnSleepMenuItemClick(object sender, RoutedEventArgs e) => ExternalProcessExtensions.SleepDevice();

    private void OnSystemSettingsMenuItemClick(object sender, RoutedEventArgs e) =>
        ExternalProcessExtensions.OpenPowerSettings();

    private void OnUserSettingsPropertyChanged(string? propertyName)
    {
        // Always save settings change immediately in case the app crashes, losing all changes.
        // ApplicationSettingsBase is wrapped via Synchronized() in the generated Settings class,
        // so background-thread Save() is safe; offloading keeps user.config disk I/O off the
        // dispatcher. A failing Save() (disk full, ACL, antivirus quarantine) surfaces through
        // SetAppError instead of the dispatcher's MessageBox path so a transient I/O hiccup
        // doesn't interrupt the user with a modal dialog.
        _ = Task.Run(static () =>
        {
            try
            {
                Default.Save();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                App.SetAppError(ex);
            }
        });

        switch (propertyName)
        {
            case nameof(Default.RefreshSeconds):
                _refreshTimer.Interval = TimeSpan.FromSeconds(Default.RefreshSeconds);
                break;
            case nameof(Default.BatteryCriticalNotificationValue):
                if (Default.BatteryLowNotificationValue < Default.BatteryCriticalNotificationValue)
                {
                    Default.BatteryLowNotificationValue = Default.BatteryCriticalNotificationValue;
                }

                if (Default.BatteryHighNotificationValue < Default.BatteryCriticalNotificationValue)
                {
                    Default.BatteryHighNotificationValue = Default.BatteryCriticalNotificationValue;
                }

                break;
            case nameof(Default.BatteryLowNotificationValue):
                if (Default.BatteryCriticalNotificationValue > Default.BatteryLowNotificationValue)
                {
                    Default.BatteryCriticalNotificationValue = Default.BatteryLowNotificationValue;
                }

                if (Default.BatteryHighNotificationValue < Default.BatteryLowNotificationValue)
                {
                    Default.BatteryHighNotificationValue = Default.BatteryLowNotificationValue;
                }

                break;
            case nameof(Default.BatteryHighNotificationValue):
                if (Default.BatteryCriticalNotificationValue > Default.BatteryHighNotificationValue)
                {
                    Default.BatteryCriticalNotificationValue = Default.BatteryHighNotificationValue;
                }

                if (Default.BatteryLowNotificationValue > Default.BatteryHighNotificationValue)
                {
                    Default.BatteryLowNotificationValue = Default.BatteryHighNotificationValue;
                }

                break;
        }

        _batteryStatusUpdateSubject.OnNext(false);
    }

    public void RequestBatteryStatusUpdate() => _batteryStatusUpdateSubject.OnNext(false);

    private void SetNotifyIconText(string text, Brush foreground, Brush? background)
    {
        try
        {
            _trayTextBlock.Text = text;
            _trayTextBlock.Foreground = foreground;
            _trayTextBlock.FontSize = Default.TrayIconFontSize;
            _trayTextBlock.FontFamily = Default.TrayIconFontFamily ?? App.DefaultTrayIconFontFamily;
            _trayTextBlock.FontWeight = Default.TrayIconFontBold ? FontWeights.Bold : FontWeights.Normal;
            _trayTextBlock.TextDecorations = Default.TrayIconFontUnderline ? TextDecorations.Underline : null;

            NotifyIcon.SetIcon(_trayTextBlock, background);
        }
        catch (Exception e)
        {
            App.SetAppError(e);
        }
    }

    private void UpdateBatteryStatus()
    {
        var readings = SystemBatterySource.Instance.Read();
        BatteryEvaluator.Thresholds thresholds = new(
            Default.BatteryCriticalNotificationValue,
            Default.BatteryLowNotificationValue,
            Default.BatteryHighNotificationValue,
            Default.BatteryFullNotification,
            Default.BatteryHighNotification,
            Default.BatteryLowNotification,
            Default.BatteryCriticalNotification);

        var decision = BatteryEvaluator.Evaluate(readings, thresholds, CultureInfo.CurrentCulture);

        _notificationTitle = decision.TooltipTitle;
        _notificationText = decision.TooltipBody;

        // Legacy parity: the Full case never prefixes the tooltip with the title even when the
        // toast title is set. The toast itself receives the title separately via _notificationTitle.
        NotifyIcon.TooltipText = decision.TooltipTitle is null || decision.Situation == BatterySituation.Full
            ? decision.TooltipBody
            : decision.TooltipTitle + Environment.NewLine + decision.TooltipBody;

        if (decision is { Situation: BatterySituation.Full, TrayIconText: null })
        {
            NotifyIcon.SetBatteryFullIcon();
        }
        else if (decision.TrayIconText is { } trayText)
        {
            var foreground = decision.VisualCategory switch
            {
                BatteryVisualCategory.Charging => BrushExtensions.GetBatteryChargingBrush(),
                BatteryVisualCategory.Low => BrushExtensions.GetBatteryLowBrush(),
                BatteryVisualCategory.Critical => BrushExtensions.GetBatteryCriticalBrush(),
                _ => BrushExtensions.GetBatteryNormalBrush()
            };
            var background = decision.VisualCategory switch
            {
                BatteryVisualCategory.Charging => BrushExtensions.GetBatteryChargingBackgroundBrush(),
                BatteryVisualCategory.Low => BrushExtensions.GetBatteryLowBackgroundBrush(),
                BatteryVisualCategory.Critical => BrushExtensions.GetBatteryCriticalBackgroundBrush(),
                _ => BrushExtensions.GetBatteryNormalBackgroundBrush()
            };
            SetNotifyIconText(trayText, foreground, background);
        }

        if (decision.Notification != BatteryNotificationCategory.None)
        {
            CheckAndSendNotification(decision.Notification);
        }

        return;

        void CheckAndSendNotification(BatteryNotificationCategory category)
        {
            var utcNow = DateTime.UtcNow;
            var type = ToType(category);

            // Reminder types (Critical/Low) repeat every 5 minutes while the condition holds, so
            // the user keeps being prodded to plug in. Milestone types (High/Full) fire once per
            // type-change and stay quiet until the situation changes again - repeating them on a
            // timer would just be noise once the user has seen "you can unplug now". The previous
            // implementation updated the timestamp unconditionally, which reset the 5-minute
            // window on every tick and silently suppressed all repeat reminders.
            var typeChanged = _lastNotification.Type != type;
            var isReminderType = type is ToastNotificationExtensions.NotificationType.Critical
                or ToastNotificationExtensions.NotificationType.Low;
            var dueForRepeat = isReminderType
                               && utcNow - _lastNotification.DateTime > TimeSpan.FromMinutes(5);

            if (typeChanged || dueForRepeat)
            {
                ToastNotificationExtensions.ShowToastNotification(_notificationTitle, _notificationText, type);
                _lastNotification = (type, utcNow);
            }
        }

        static ToastNotificationExtensions.NotificationType ToType(BatteryNotificationCategory c)
        {
            return c switch
            {
                BatteryNotificationCategory.Critical => ToastNotificationExtensions.NotificationType.Critical,
                BatteryNotificationCategory.Low => ToastNotificationExtensions.NotificationType.Low,
                BatteryNotificationCategory.High => ToastNotificationExtensions.NotificationType.High,
                BatteryNotificationCategory.Full => ToastNotificationExtensions.NotificationType.Full,
                _ => ToastNotificationExtensions.NotificationType.None
            };
        }
    }

#pragma warning disable CA1822 // XAML-wired (LeftClick="...") event handlers must be instance members.
    private void OnNotifyIconLeftClick(NotifyIcon sender, RoutedEventArgs e)
    {
        if (!Default.DoubleClickActivation)
        {
            Application.Current.ActivateMainWindow().NavigateToPage<DetailsPage>();
        }
    }

    private void OnNotifyIconLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e)
    {
        if (Default.DoubleClickActivation)
        {
            Application.Current.ActivateMainWindow().NavigateToPage<DetailsPage>();
        }
    }
#pragma warning restore CA1822
}
