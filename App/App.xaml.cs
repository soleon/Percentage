using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui;
using static Percentage.App.Properties.Settings;

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

namespace Percentage.App;

public partial class App
{
    internal const string DefaultBatteryChargingColour = "#10893E";
    internal const string DefaultBatteryCriticalColour = "#E81123";
    internal const bool DefaultBatteryCriticalNotification = true;
    internal const int DefaultBatteryCriticalNotificationValue = 10;
    internal const bool DefaultBatteryFullNotification = true;
    internal const bool DefaultBatteryHighNotification = true;
    internal const int DefaultBatteryHighNotificationValue = 80;
    internal const string DefaultBatteryLowColour = "#CA5010";
    internal const bool DefaultBatteryLowNotification = true;
    internal const int DefaultBatteryLowNotificationValue = 20;
    internal const string DefaultBatteryNormalColour = null;
    internal const bool DefaultHideAtStartup = false;
    internal const int DefaultRefreshSeconds = 60;
    internal const bool DefaultTrayIconFontBold = false;
    internal const bool DefaultTrayIconFontUnderline = false;
    internal const string Id = "f05f920a-c997-4817-84bd-c54d87e40625";

    internal static readonly ISnackbarService SnackbarService = new SnackbarService();
    internal static readonly FontFamily TrayIconFontFamily = new("Microsoft Sans Serif");

    private readonly Mutex _appMutex;

    public App()
    {
        _appMutex = new Mutex(true, Id, out var isNewInstance);

        DispatcherUnhandledException += (_, e) => HandleException(e.Exception);

        AppDomain.CurrentDomain.UnhandledException += (_, e) => HandleException(e.ExceptionObject);

        TaskScheduler.UnobservedTaskException += (_, e) => HandleException(e.Exception);

        InitializeComponent();

        if (!isNewInstance) Shutdown(1);
    }

    private static void HandleException(object exception)
    {
        if (exception is OutOfMemoryException)
        {
            MessageBox.Show("Battery Percentage Icon did not have enough memory to perform some work.\r\n" +
                            "Please consider closing some running applications or background services to free up some memory.",
                "Your system memory is running low",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        else
        {
            const string title = "You Found An Error";
            var message = "Battery Percentage Icon has run into an error. You can help to fix this by:\r\n" +
                          "1. press Ctrl+C on this message\r\n" +
                          "2. paste it in an email\r\n" +
                          "3. send it to soleon@live.com\r\n\r\n" +
                          (exception is Exception exp
                              ? exp.ToString()
                              : $"Error type: {exception.GetType().FullName}\r\n{exception}");
            try
            {
                new Wpf.Ui.Controls.MessageBox
                {
                    Title = title,
                    Content = message
                }.ShowDialogAsync().GetAwaiter().GetResult();
            }
            catch
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Save user settings when exiting the app.
        Default.Save();
        _appMutex.Dispose();
        base.OnExit(e);
    }
}