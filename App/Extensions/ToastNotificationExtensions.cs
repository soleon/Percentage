using System;
using System.Runtime.InteropServices;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Percentage.App.Extensions;

internal static class ToastNotificationExtensions
{
    private const string ActionArgumentKey = "action";
    private const string NotificationTypeArgumentKey = "notificationType";

    extension(ToastArguments arguments)
    {
        internal bool TryGetActionArgument(out Action action)
        {
            return arguments.TryGetValue(ActionArgumentKey, out action);
        }

        internal bool TryGetNotificationTypeArgument(out NotificationType notificationType)
        {
            return arguments.TryGetValue(NotificationTypeArgumentKey, out notificationType);
        }
    }

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
                .AddButton(new ToastButton().SetContent("Details")
                    .AddArgument(ActionArgumentKey, Action.ViewDetails))
                .AddButton(new ToastButton().SetContent("Disable")
                    .AddArgument(ActionArgumentKey, Action.DisableBatteryNotification)
                    .AddArgument(NotificationTypeArgumentKey, notificationType))
                .AddButton(new ToastButtonDismiss())
                .Show();
        }
        catch (COMException e) when ((uint)e.HResult == 0x803E0105)
        {
            App.SetAppError(e);
        }
    }

    internal enum Action
    {
        ViewDetails = 0,
        DisableBatteryNotification
    }

    internal enum NotificationType
    {
        None = 0,
        Critical,
        Low,
        High,
        Full
    }
}