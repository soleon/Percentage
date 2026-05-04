using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Toolkit.Uwp.Notifications;
using Percentage.App.Extensions;
using Percentage.App.Localization;
using Percentage.App.Pages;
using Percentage.App.Resources;
using Wpf.Ui;
using Wpf.Ui.Markup;
using static Percentage.App.Properties.Settings;

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

// Required so the source generator behind LibraryImport can produce zero-stub marshalling for
// SafeNotifyIcon's Shell_NotifyIconW call (the runtime cannot auto-marshal a struct that contains
// InlineArray fixed buffers). The project's only direct P/Invokes - DestroyIcon and
// Shell_NotifyIconW - are both fully blittable / explicitly attributed, so disabling runtime
// marshalling is safe and gives us the modern source-generated path AGENTS.md prefers.
[assembly: DisableRuntimeMarshalling]

namespace Percentage.App;

#pragma warning disable CA1001 // _appMutex disposal is handled by OnExit; the WPF Application owns its own lifecycle.
public partial class App
#pragma warning restore CA1001
{
    internal const string DefaultBatteryChargingColour = "#FF10893E";
    internal const string DefaultBatteryCriticalColour = "#FFE81123";
    internal const bool DefaultBatteryCriticalNotification = true;
    internal const int DefaultBatteryCriticalNotificationValue = 10;
    internal const bool DefaultBatteryFullNotification = true;
    internal const bool DefaultBatteryHighNotification = true;
    internal const int DefaultBatteryHighNotificationValue = 80;
    internal const string DefaultBatteryLowColour = "#FFCA5010";
    internal const bool DefaultBatteryLowNotification = true;
    internal const int DefaultBatteryLowNotificationValue = 20;
    internal const bool DefaultDoubleClickActivation = false;
    internal const bool DefaultHideAtStartup = false;
    internal const bool DefaultIsAutoBatteryChargingBackgroundColour = true;
    internal const bool DefaultIsAutoBatteryChargingColour = false;
    internal const bool DefaultIsAutoBatteryCriticalBackgroundColour = true;
    internal const bool DefaultIsAutoBatteryCriticalColour = false;
    internal const bool DefaultIsAutoBatteryLowBackgroundColour = true;
    internal const bool DefaultIsAutoBatteryLowColour = false;
    internal const bool DefaultIsAutoBatteryNormalBackgroundColour = true;
    internal const bool DefaultIsAutoBatteryNormalColour = true;
    internal const int DefaultRefreshSeconds = 60;
    internal const bool DefaultShutDownWithoutConfirmation = false;
    internal const bool DefaultTrayIconFontBold = false;
    internal const int DefaultTrayIconFontSize = 15;
    internal const bool DefaultTrayIconFontUnderline = false;
    internal const string Id = "f05f920a-c997-4817-84bd-c54d87e40625";
    private static Exception? _appError;
    internal static readonly FontFamily DefaultTrayIconFontFamily = new("Microsoft Sans Serif");
    internal static readonly ISnackbarService SnackBarService = new SnackbarService();

    private readonly Mutex _appMutex;

    public App()
    {
        // Set ShutdownMode before any branch that may call Shutdown(): the second-instance
        // path below short-circuits the rest of the constructor, so doing this later would
        // leave the second instance with the framework default and rely on a window opening.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _appMutex = new Mutex(true, Id, out var isNewInstance);

        if (!isNewInstance)
        {
            Shutdown(1);
            return;
        }

        // Must run before any Settings access (incl. InitializeComponent and MigrateUserSettings).
        TryMigratePreviousUserConfig();

        DispatcherUnhandledException += (_, e) =>
        {
            HandleException(e.Exception);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) => HandleException(e.ExceptionObject);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            HandleException(e.Exception);
            e.SetObserved();
        };

        InitializeComponent();

        // User settings migration for backward compatibility.
        MigrateUserSettings();

        // Apply the user's chosen language (or fall back to OS UI culture) before the first
        // window loads, so every binding resolves against the right culture from the start.
        LocalizationManager.Instance.ApplyFromSettings();

        // Subscribe to toast notification activations.
        ToastNotificationManagerCompat.OnActivated += OnToastNotificationActivated;
    }

    internal static event Action<Exception>? AppErrorSet;

    internal static Exception? GetAppError() => _appError;

    private static void HandleException(object exception)
    {
        // AppDomain.UnhandledException and TaskScheduler.UnobservedTaskException can fire on
        // arbitrary threads; MessageBox.Show on a non-UI thread is fragile and can re-enter the
        // dispatcher's unhandled-exception handler. Marshal to the dispatcher when one is alive,
        // and fall through to a direct call only when the dispatcher has shut down.
        var dispatcher = Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.HasShutdownStarted && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => HandleException(exception));
            return;
        }

        var version = VersionExtensions.GetAppVersion();

        if (exception is OutOfMemoryException)
        {
            MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.App_OutOfMemoryBody, version),
                Strings.App_OutOfMemoryTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        else
        {
            var details = exception is Exception exp
                ? exp.ToString()
                : $"Error type: {exception.GetType().FullName}\r\n{exception}";
            MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.App_GeneralErrorBody, version, details),
                Strings.App_GeneralErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    internal static void SetAppError(Exception e)
    {
        _appError = e;
        AppErrorSet?.Invoke(e);
    }

    // MSIX Store updates (and EXE relocation) put the new user.config in a sibling
    // hash folder; Settings.Default.Upgrade() can't see across hash folders, so copy
    // the most recent sibling user.config forward before Settings first loads.
    private static void TryMigratePreviousUserConfig()
    {
        string currentPath;
        try
        {
            currentPath = ConfigurationManager
                .OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal)
                .FilePath;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Surface the cause through SetAppError so the About page can show it; the app
            // still boots with default settings rather than crashing on a bad config file.
            SetAppError(ex);
            return;
        }

        if (string.IsNullOrEmpty(currentPath) || File.Exists(currentPath))
        {
            return;
        }

        var versionDir = Path.GetDirectoryName(currentPath);
        var hashDir = Path.GetDirectoryName(versionDir);
        var companyDir = Path.GetDirectoryName(hashDir);
        if (versionDir is null || hashDir is null || companyDir is null || !Directory.Exists(companyDir))
        {
            return;
        }

        // Hash folders look like "Percentage.App_Url_<hash>" or "Percentage.App_Path_<hash>".
        // Match siblings by assembly-name prefix, ignoring an evidence type.
        var prefix = Path.GetFileName(hashDir).Split('_').FirstOrDefault();
        if (string.IsNullOrEmpty(prefix))
        {
            return;
        }

        var mostRecent = Directory
            .EnumerateDirectories(companyDir, prefix + "_*")
            .Where(d => !string.Equals(d, hashDir, StringComparison.OrdinalIgnoreCase))
            .SelectMany(d => Directory.EnumerateFiles(d, "user.config", SearchOption.AllDirectories))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (mostRecent is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(versionDir);
            File.Copy(mostRecent, currentPath);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Best-effort: on failure the app falls back to defaults, but surface the cause so
            // the user can report a disk-full / ACL / antivirus issue from the About page.
            SetAppError(ex);
        }
    }

    private void MigrateUserSettings()
    {
        if (Default.RequiresUpgrade)
        {
            Default.Upgrade();
            Default.RequiresUpgrade = false;
            Default.Save();
        }

        if (Default.RefreshSeconds < 5)
        {
            Default.RefreshSeconds = 5;
        }

        // The setting's stored default is an empty string (see Settings.settings), so a
        // simple ??= would not trigger; check explicitly for null OR empty.
        if (string.IsNullOrEmpty(Default.BatteryNormalColour))
        {
            Default.BatteryNormalColour =
                ((Brush)FindResource(nameof(ThemeResource.TextFillColorPrimaryBrush))!).ToString(CultureInfo
                    .InvariantCulture);
        }

        if (Default.BatteryLowColour is { Length: 7 } lowColourHexValue)
        {
            Default.BatteryLowColour = lowColourHexValue.Insert(1, "FF");
        }

        if (Default.BatteryCriticalColour is { Length: 7 } criticalColourHexValue)
        {
            Default.BatteryCriticalColour = criticalColourHexValue.Insert(1, "FF");
        }

        if (Default.BatteryChargingColour is { Length: 7 } chargingColourHexValue)
        {
            Default.BatteryChargingColour = chargingColourHexValue.Insert(1, "FF");
        }

        Default.Save();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Save user settings when exiting the app.
        _appMutex.Dispose();
        base.OnExit(e);
    }

    private void OnToastNotificationActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        var arguments = ToastArguments.Parse(toastArgs.Argument);
        if (!arguments.TryGetActionArgument(out var action))
            // When there's no action from toast notification activation, this is most likely triggered by users
            // clicking the entire notification instead of an individual button.
            // Show the details view in this case.
        {
            _ = Dispatcher.InvokeAsync(() => this.ActivateMainWindow().NavigateToPage<DetailsPage>());
            return;
        }

        switch (action)
        {
            case ToastNotificationExtensions.Action.ViewDetails:
                _ = Dispatcher.InvokeAsync(() => this.ActivateMainWindow().NavigateToPage<DetailsPage>());
                break;
            case ToastNotificationExtensions.Action.DisableBatteryNotification:
                if (!arguments.TryGetNotificationTypeArgument(out var type))
                {
                    break;
                }

                switch (type)
                {
                    case ToastNotificationExtensions.NotificationType.Critical:
                        Default.BatteryCriticalNotification = false;
                        break;
                    case ToastNotificationExtensions.NotificationType.Low:
                        Default.BatteryLowNotification = false;
                        break;
                    case ToastNotificationExtensions.NotificationType.High:
                        Default.BatteryHighNotification = false;
                        break;
                    case ToastNotificationExtensions.NotificationType.Full:
                        Default.BatteryFullNotification = false;
                        break;
                    case ToastNotificationExtensions.NotificationType.None:
                    default:
                        throw new InvalidEnumArgumentException(nameof(type), (int)type,
                            typeof(ToastNotificationExtensions.NotificationType));
                }

                break;
            default:
                throw new InvalidEnumArgumentException(nameof(action), (int)action,
                    typeof(ToastNotificationExtensions.Action));
        }
    }
}
