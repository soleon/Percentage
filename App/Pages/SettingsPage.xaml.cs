using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Windows.ApplicationModel;
using Percentage.App.Extensions;
using Percentage.App.Localization;
using Percentage.App.Resources;
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

        LanguageComboBox.ItemsSource = SupportedLanguages.All;

        Loaded += OnLoadedAsync;
    }

    private async Task EnableAutoStartAsync()
    {
        if (_startupTask is null)
        {
            return;
        }

        AutoStartToggleSwitch.IsEnabled = false;
        var state = await _startupTask.RequestEnableAsync();
        UnRegisterAutoStarEventHandling();

        // The two policy states require Windows 10 versions newer than the manifest's
        // 10.0.14393 (1607) baseline: DisabledByPolicy requires 10.0.16299 (1709) and
        // EnabledByPolicy requires 10.0.17134 (1803). The OS itself never returns these
        // enum values on older builds, but CA1416 sees the bare reference and cannot
        // recognise a `when` guard on a `case` label, so we lift the policy arms out of
        // the switch into version-guarded branches (per Microsoft Learn's CA1416 docs).
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134) && state == StartupTaskState.EnabledByPolicy)
        {
            AutoStartToggleSwitch.IsChecked = true;
        }
        else if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 16299) && state == StartupTaskState.DisabledByPolicy)
        {
            AutoStartToggleSwitch.IsChecked = false;
            await new MessageBox
            {
                Title = Strings.Settings_AutoStartDisabledByPolicyDialogTitle,
                Content = Strings.Settings_AutoStartDisabledByPolicyDialogContent
            }.ShowDialogAsync();
        }
        else
        {
            switch (state)
            {
                case StartupTaskState.Disabled:
                    AutoStartToggleSwitch.IsChecked = false;
                    await new MessageBox
                    {
                        Title = Strings.Settings_AutoStartDisabledDialogTitle,
                        Content = Strings.Settings_AutoStartDisabledDialogContent
                    }.ShowDialogAsync();
                    break;
                case StartupTaskState.DisabledByUser:
                    AutoStartToggleSwitch.IsChecked = false;
                    await new MessageBox
                    {
                        Title = Strings.Settings_AutoStartDisabledByUserDialogTitle,
                        Content = Strings.Settings_AutoStartDisabledByUserDialogContent
                    }.ShowDialogAsync();
                    break;
                case StartupTaskState.Enabled:
                    AutoStartToggleSwitch.IsChecked = true;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(state), (int)state, typeof(StartupTaskState));
            }
        }

        RegisterAutoStartEventHandling();
        AutoStartToggleSwitch.IsEnabled = true;
    }

    private void OnAutoStartChecked(object sender, RoutedEventArgs e) => _ = EnableAutoStartAsync();

    private void OnAutoStartUnchecked(object sender, RoutedEventArgs e) => _startupTask?.Disable();

#pragma warning disable VSTHRD100, VSTHRD200 // Loaded-event handler must return void; the Async suffix follows the established convention for async event handlers.
    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100, VSTHRD200
    {
        Loaded -= OnLoadedAsync;

        try
        {
            _startupTask = await StartupTask.GetAsync(App.Id);
            AutoStartToggleSwitch.IsChecked = _startupTask.State == StartupTaskState.Enabled;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            AutoStartDisabledInfoBar.Visibility = Visibility.Visible;
            App.SetAppError(ex);
            return;
        }

        RegisterAutoStartEventHandling();
        AutoStartToggleSwitch.IsEnabled = true;
    }

#pragma warning disable VSTHRD100 // Click-event handler must return void; exceptions are funnelled through App.SetAppError via the await-chain.
    private async void OnResetButtonClick(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100
    {
        var result = await new MessageBox
        {
            Title = Strings.Settings_ResetDialogTitle,
            Content = Strings.Settings_ResetDialogContent,
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = Strings.Settings_ResetDialogReset,
            PrimaryButtonAppearance = ControlAppearance.Caution,
            CloseButtonText = Strings.Settings_ResetDialogCancel
        }.ShowDialogAsync();

        if (result != MessageBoxResult.Primary)
        {
            return;
        }

        Default.BatteryCriticalColour = App.DefaultBatteryCriticalColour;
        Default.BatteryLowColour = App.DefaultBatteryLowColour;
        Default.BatteryChargingColour = App.DefaultBatteryChargingColour;
        Default.BatteryNormalColour =
            ((Brush)FindResource(nameof(ThemeResource.TextFillColorPrimaryBrush))).ToString(
                CultureInfo.InvariantCulture);
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
        Default.BatteryCriticalBackgroundColour = string.Empty;
        Default.BatteryLowBackgroundColour = string.Empty;
        Default.BatteryChargingBackgroundColour = string.Empty;
        Default.BatteryNormalBackgroundColour = string.Empty;
        Default.IsAutoBatteryCriticalBackgroundColour = App.DefaultIsAutoBatteryCriticalBackgroundColour;
        Default.IsAutoBatteryLowBackgroundColour = App.DefaultIsAutoBatteryLowBackgroundColour;
        Default.IsAutoBatteryChargingBackgroundColour = App.DefaultIsAutoBatteryChargingBackgroundColour;
        Default.IsAutoBatteryNormalBackgroundColour = App.DefaultIsAutoBatteryNormalBackgroundColour;
        Default.DoubleClickActivation = App.DefaultDoubleClickActivation;
        Default.ShutDownWithoutConfirmation = App.DefaultShutDownWithoutConfirmation;
        Default.Language = SupportedLanguages.SystemCultureName;

        await EnableAutoStartAsync();
    }

    private void OnSystemPowerSettingsClick(object sender, RoutedEventArgs e) =>
        ExternalProcessExtensions.OpenPowerSettings();

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
