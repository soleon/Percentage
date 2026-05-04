using System.Runtime.InteropServices;
using Microsoft.Toolkit.Uwp.Notifications;
using Percentage.App.Resources;

namespace Percentage.App.Extensions;

/// <summary>
///     Builds and displays the app's tray toast notifications and round-trips their action
///     arguments. Notifications carry a <see cref="NotificationType" /> argument so the activator
///     can offer to disable the specific notification that fired.
/// </summary>
internal static class ToastNotificationExtensions
{
    private const string ActionArgumentKey = "action";
    private const string NotificationTypeArgumentKey = "notificationType";

    /// <summary>
    ///     Shows a toast for the given <paramref name="notificationType" /> with View-Details and
    ///     Disable-this-notification action buttons. Routes any <see cref="COMException" /> from the
    ///     toast subsystem (the documented <c>0x803E0105</c> "service not available" on portable /
    ///     non-MSIX installations, plus transient RPC / activator failures) through
    ///     <c>App.SetAppError</c> instead of letting them escape into the dispatcher unhandled-
    ///     exception MessageBox.
    /// </summary>
    /// <param name="header">Toast first line.</param>
    /// <param name="body">Toast second line.</param>
    /// <param name="notificationType">Which notification this is used by the Disable button.</param>
    internal static void ShowToastNotification(string? header, string? body, NotificationType notificationType)
    {
        if (notificationType == NotificationType.None)
        {
            throw new NotSupportedException($"Notification type {notificationType} is not supported.");
        }

        try
        {
            new ToastContentBuilder()
                .AddText(header)
                .AddText(body)
                .AddButton(new ToastButton().SetContent(Strings.Toast_ButtonDetails)
                    .AddArgument(ActionArgumentKey, Action.ViewDetails))
                .AddButton(new ToastButton().SetContent(Strings.Toast_ButtonDisable)
                    .AddArgument(ActionArgumentKey, Action.DisableBatteryNotification)
                    .AddArgument(NotificationTypeArgumentKey, notificationType))
                .AddButton(new ToastButtonDismiss())
                .Show();
        }
        catch (COMException e)
        {
            App.SetAppError(e);
        }
    }

    extension(ToastArguments arguments)
    {
        /// <summary>Pulls the <see cref="Action" /> argument off a clicked toast.</summary>
        internal bool TryGetActionArgument(out Action action) => arguments.TryGetValue(ActionArgumentKey, out action);

        /// <summary>Pulls the <see cref="NotificationType" /> argument off a clicked toast.</summary>
        internal bool TryGetNotificationTypeArgument(out NotificationType notificationType) =>
            arguments.TryGetValue(NotificationTypeArgumentKey, out notificationType);
    }

    /// <summary>Action argument carried on a toast button click.</summary>
    internal enum Action
    {
        /// <summary>Open the Details page.</summary>
        ViewDetails = 0,

        /// <summary>Disable the battery notification that produced this toast.</summary>
        DisableBatteryNotification
    }

    /// <summary>Which battery notification produced a toast, used by the Disable button.</summary>
    internal enum NotificationType
    {
        /// <summary>Sentinel; never used as an actual toast type.</summary>
        None = 0,

        /// <summary>Critical-threshold toast.</summary>
        Critical,

        /// <summary>Low-threshold toast.</summary>
        Low,

        /// <summary>High-threshold (charging-up) toast.</summary>
        High,

        /// <summary>Full (100%) toast.</summary>
        Full
    }
}
