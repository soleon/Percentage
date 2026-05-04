using System.Windows;

namespace Percentage.App.Extensions;

/// <summary>
///     <see cref="Application" /> extension members for finding the app's two top-level windows
///     (<see cref="MainWindow" /> and <see cref="NotifyIconWindow" />). Used by tray-menu clicks
///     and toast activations to surface UI without touching the WPF window list directly.
/// </summary>
internal static class ApplicationExtensions
{
    extension(Application app)
    {
        /// <summary>
        ///     Returns the existing <see cref="MainWindow" /> (activating it) or shows a new one. Single
        ///     entry point used by tray-menu clicks and toast activations.
        /// </summary>
        internal MainWindow ActivateMainWindow()
        {
            var window = app.Windows.OfType<MainWindow>().FirstOrDefault();
            if (window is not null)
            {
                window.Activate();
                return window;
            }

            window = new MainWindow();
            window.Show();
            return window;
        }

        /// <summary>
        ///     Returns the running <see cref="NotifyIconWindow" />. Throws if startup failed to create
        ///     it, since the tray icon is the app's primary surface and absence is unrecoverable.
        /// </summary>
        internal NotifyIconWindow GetNotifyIconWindow()
        {
            return app.Windows.OfType<NotifyIconWindow>().FirstOrDefault() ??
                   throw new InvalidOperationException("NotifyIconWindow not found");
        }
    }
}
