using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Tray.Controls;

namespace Percentage.App.Extensions;

internal static class NotifyIconExtensions
{
    private const double DefaultNotifyIconSize = 16;

    extension(NotifyIcon notifyIcon)
    {
        internal void SetBatteryFullIcon()
        {
            notifyIcon.SetIcon(new TextBlock
            {
                Text = "\uf5fc",
                Foreground = BrushExtensions.GetBatteryNormalBrush(),
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 16
            });
        }

        internal void SetIcon(FrameworkElement textBlock)
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

            // There's a known issue that keeps creating RenderTargetBitmap in a WPF app, the COMException: MILERR_WIN32ERROR
            // may happen.
            // This is reported as https://github.com/dotnet/wpf/issues/3067.
            // Manually forcing a GC seems to help.
            var renderTargetBitmap =
                new RenderTargetBitmap(
                    (int)Math.Round(DefaultNotifyIconSize * dpiScale.DpiScaleX, MidpointRounding.AwayFromZero),
                    (int)Math.Round(DefaultNotifyIconSize * dpiScale.DpiScaleY, MidpointRounding.AwayFromZero),
                    dpiScale.PixelsPerInchX,
                    dpiScale.PixelsPerInchY,
                    PixelFormats.Default);
            renderTargetBitmap.Render(textBlock);

            // Force GC after each RenderTargetBitmap creation to avoid running into COMException: MILERR_WIN32ERROR.
            GC.Collect();
            GC.WaitForPendingFinalizers();
        
            notifyIcon.Icon = renderTargetBitmap;
        }
    }
}