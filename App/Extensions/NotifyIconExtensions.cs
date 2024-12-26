﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Tray.Controls;

namespace Percentage.App.Extensions;

internal static class NotifyIconExtensions
{
    private const double DefaultNotifyIconSize = 16;

    internal static void SetIcon(this NotifyIcon notifyIcon, FrameworkElement textBlock)
    {
        // Measure the size of the element first.
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
     
        // Use the desired size to work out the appropriate margin so that the element can be centre aligned in the
        // tray icon's 16-by-16 region.
        textBlock.Margin = new Thickness((DefaultNotifyIconSize - textBlock.DesiredSize.Width) / 2,
            (DefaultNotifyIconSize - textBlock.DesiredSize.Height) / 2, 0, 0);
        
        // Measure again for the correct desired size with the margin.
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        textBlock.Arrange(new Rect(textBlock.DesiredSize));

        // Render the element with the correct DPI scale.
        var dpiScale = VisualTreeHelper.GetDpi(textBlock);
        var renderTargetBitmap = new RenderTargetBitmap(
            (int)Math.Round(DefaultNotifyIconSize * dpiScale.DpiScaleX, MidpointRounding.AwayFromZero),
            (int)Math.Round(DefaultNotifyIconSize * dpiScale.DpiScaleY, MidpointRounding.AwayFromZero),
            dpiScale.PixelsPerInchX,
            dpiScale.PixelsPerInchY,
            PixelFormats.Default);
        renderTargetBitmap.Render(textBlock);

        // There's a chance that some native exception may be thrown when setting the icon's image.
        // Catch any exception here and retry a few times then fail silently with logs.
        for (var i = 0; i < 5; i++)
            try
            {
                notifyIcon.Icon = renderTargetBitmap;
                App.SetTrayIconUpdateError(null);
                break;
            }
            catch (Exception e)
            {
                if (i == 4)
                    // Retried maximum number of times.
                    // Log error and continue.
                    App.SetTrayIconUpdateError(e);
            }
    }

    internal static void SetBatteryFullIcon(this NotifyIcon notifyIcon)
    {
        notifyIcon.SetIcon(new TextBlock
        {
            Text = "\uf5fc",
            Foreground = BrushExtensions.GetBatteryNormalBrush(),
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 16
        });
    }
}