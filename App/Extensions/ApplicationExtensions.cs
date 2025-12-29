using System;
using System.Linq;
using System.Windows;

namespace Percentage.App.Extensions;

internal static class ApplicationExtensions
{
    extension(Application app)
    {
        internal MainWindow ActivateMainWindow()
        {
            var window = app.Windows.OfType<MainWindow>().FirstOrDefault();
            if (window != null)
            {
                window.Activate();
                return window;
            }

            window = new MainWindow();
            window.Show();
            return window;
        }

        internal NotifyIconWindow GetNotifyIconWindow()
        {
            return app.Windows.OfType<NotifyIconWindow>().FirstOrDefault() ??
                   throw new InvalidOperationException("NotifyIconWindow not found");
        }
    }
}