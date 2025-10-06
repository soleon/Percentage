using System;
using System.Windows;
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

        // There's a known issue that keep creating RenderTargetBitmap in a WPF app, the COMException: MILERR_WIN32ERROR
        // may happen.
        // This is reported as https://github.com/dotnet/wpf/issues/3067.
        // Manually forcing a GC seems to help. But for now due to a memory leak in WPF-UI framework we can't hit 
        // this limit and crash with a GDI+ exception long before.
        // See https://github.com/lepoco/wpfui/issues/1313.
        // So let's not force expensive GC for now as tests show basically no difference.
        var renderTargetBitmap =
            new RenderTargetBitmap(
            (int)Math.Round(DefaultNotifyIconSize * dpiScale.DpiScaleX, MidpointRounding.AwayFromZero),
            (int)Math.Round(DefaultNotifyIconSize * dpiScale.DpiScaleY, MidpointRounding.AwayFromZero),
            dpiScale.PixelsPerInchX,
            dpiScale.PixelsPerInchY,
            PixelFormats.Default);
        renderTargetBitmap.Render(textBlock);

        notifyIcon.Icon = renderTargetBitmap;
    }
}