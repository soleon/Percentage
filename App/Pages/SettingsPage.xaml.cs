using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Windows.ApplicationModel;
using Percentage.App.Extensions;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using static Percentage.App.Properties.Settings;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace Percentage.App.Pages;

public partial class SettingsPage
{
    private StartupTask? _startupTask;

    public SettingsPage()
    {
        InitializeComponent();

        new Func<Task>(async () =>
        {
            try
            {
                AutoStartToggleSwitch.IsChecked = (_startupTask = await StartupTask.GetAsync(App.Id)).State ==
                                                  StartupTaskState.Enabled;
            }
            catch
            {
                AutoStartDisabledInfoBar.Visibility = Visibility.Visible;
                return;
            }

            RegisterAutoStartEventHandling();
            AutoStartToggleSwitch.IsEnabled = true;
        })();
    }

    private async Task EnableAutoStart()
    {
        if (_startupTask == null) return;

        AutoStartToggleSwitch.IsEnabled = false;
        var state = await _startupTask.RequestEnableAsync();
        UnRegisterAutoStarEventHandling();
        switch (state)
        {
            case StartupTaskState.Disabled:
                AutoStartToggleSwitch.IsChecked = false;
                await new MessageBox
                {
                    Title = "Auto start disabled",
                    Content = "Auto start for this app has been disabled.\r\n\r\n" +
                              "To re-enable it, please go to \"Settings > Apps > Startup\" to enable \"Battery Percentage Icon\"."
                }.ShowDialogAsync();
                break;
            case StartupTaskState.DisabledByUser:
                AutoStartToggleSwitch.IsChecked = false;
                await new MessageBox
                {
                    Title = "Auto start disabled by user",
                    Content = "Auto start for this app has been disabled manually in system settings.\r\n\r\n" +
                              "To re-enable it, please go to \"Settings > Apps > Startup\" to enable \"Battery Percentage Icon\"."
                }.ShowDialogAsync();
                break;
            case StartupTaskState.DisabledByPolicy:
                AutoStartToggleSwitch.IsChecked = false;
                await new MessageBox
                {
                    Title = "Auto start disabled by policy",
                    Content = "Auto start for this app has been disabled manually by system policy.\r\n\r\n" +
                              "To re-enable it, remove any system policies that restrict auto start, and go to \"Settings > Apps > Startup\" to enable \"Battery Percentage Icon\"."
                }.ShowDialogAsync();
                break;
            case StartupTaskState.Enabled:
            case StartupTaskState.EnabledByPolicy:
                AutoStartToggleSwitch.IsChecked = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        RegisterAutoStartEventHandling();
        AutoStartToggleSwitch.IsEnabled = true;
    }

    private void OnAutoStartChecked(object sender, RoutedEventArgs e)
    {
        _ = EnableAutoStart();
    }

    private void OnAutoStartUnchecked(object sender, RoutedEventArgs e)
    {
        _startupTask?.Disable();
    }

    private void OnResetButtonClick(object sender, RoutedEventArgs e)
    {
        var result = new MessageBox
        {
            Title = "Reset All Settings",
            Content = "Are you sure you want to reset all settings to their default values?",
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = "Reset",
            PrimaryButtonAppearance = ControlAppearance.Caution,
            CloseButtonText = "Cancel"
        }.ShowDialogAsync().GetAwaiter().GetResult();

        if (result != MessageBoxResult.Primary) return;

        Default.BatteryCriticalColour = App.DefaultBatteryCriticalColour;
        Default.BatteryLowColour = App.DefaultBatteryLowColour;
        Default.BatteryChargingColour = App.DefaultBatteryChargingColour;
        Default.BatteryNormalColour = ((Brush)FindResource(nameof(ThemeResource.TextFillColorPrimaryBrush))).ToString();
        Default.TrayIconFontFamily = App.DefaultTrayIconFontFamily;
        Default.TrayIconFontBold = App.DefaultTrayIconFontBold;
        Default.TrayIconFontUnderline = App.DefaultTrayIconFontUnderline;
        Default.TrayIconFontSize = App.DefaultTrayIconFontSize;
        Default.BatteryCriticalNotificationValue = App.DefaultBatteryCriticalNotificationValue;
        Default.BatteryLowNotificationValue = App.DefaultBatteryLowNotificationValue;
        Default.BatteryHighNotificationValue = App.DefaultBatteryHighNotificationValue;
        Default.RefreshSeconds = App.DefaultRefreshSeconds;
        Default.BatteryFullNotification = App.DefaultBatteryFullNotification;
        Default.BatteryLowNotification = App.DefaultBatteryLowNotification;
        Default.BatteryHighNotification = App.DefaultBatteryHighNotification;
        Default.BatteryCriticalNotification = App.DefaultBatteryCriticalNotification;
        Default.HideAtStartup = App.DefaultHideAtStartup;
        Default.IsAutoBatteryNormalColour = App.DefaultIsAutoBatteryNormalColour;
        Default.IsAutoBatteryChargingColour = App.DefaultIsAutoBatteryChargingColour;
        Default.IsAutoBatteryLowColour = App.DefaultIsAutoBatteryLowColour;
        Default.IsAutoBatteryCriticalColour = App.DefaultIsAutoBatteryCriticalColour;
        Default.DoubleClickActivation = App.DefaultDoubleClickActivation;
        Default.ShutDownWithoutConfirmation = App.DefaultShutDownWithoutConfirmation;

        _ = EnableAutoStart();
    }

    private void OnSystemPowerSettingsClick(object sender, RoutedEventArgs e)
    {
        ExternalProcessExtensions.OpenPowerSettings();
    }

    private void RegisterAutoStartEventHandling()
    {
        AutoStartToggleSwitch.Checked += OnAutoStartChecked;
        AutoStartToggleSwitch.Unchecked += OnAutoStartUnchecked;
    }

    private void UnRegisterAutoStarEventHandling()
    {
        AutoStartToggleSwitch.Checked -= OnAutoStartChecked;
        AutoStartToggleSwitch.Unchecked -= OnAutoStartUnchecked;
    }
}