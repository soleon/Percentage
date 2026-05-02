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
            }, BrushExtensions.GetBatteryNormalBackgroundBrush());
        }

        internal void SetIcon(FrameworkElement textBlock, Brush? background)
        {
            // Measure the size of the element first.
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            // Use the desired size to work out the appropriate margin so that the element can be centre aligned in the
            // tray icon's 16-by-16 region.
            textBlock.Margin = new Thickness((DefaultNotifyIconSize - textBlock.DesiredSize.Width) / 2,
                (DefaultNotifyIconSize - textBlock.DesiredSize.Height) / 2, 0, 0);

            // Wrap the text in a fixed 16x16 container so the user-chosen background fills the whole icon. Leave
            // ClipToBounds at its default (false) so oversized text still extends past the container and is clipped
            // only at the bitmap edge, matching pre-background behaviour.
            var container = new Grid
            {
                Width = DefaultNotifyIconSize,
                Height = DefaultNotifyIconSize,
                Background = background
            };
            container.Children.Add(textBlock);
            container.Measure(new Size(DefaultNotifyIconSize, DefaultNotifyIconSize));
            container.Arrange(new Rect(0, 0, DefaultNotifyIconSize, DefaultNotifyIconSize));

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
            renderTargetBitmap.Render(container);

            // Force GC after each RenderTargetBitmap creation to avoid running into COMException: MILERR_WIN32ERROR.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            notifyIcon.Icon = renderTargetBitmap;
        }
    }
}