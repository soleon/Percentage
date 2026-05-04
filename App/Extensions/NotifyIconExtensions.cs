using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Tray.Controls;

namespace Percentage.App.Extensions;

/// <summary>
///     Helpers for rendering a <see cref="FrameworkElement" /> child (a <see cref="TextBlock" />)
///     into the 16x16 tray-icon bitmap WPF-UI hands to Windows. Owns a thread-static container
///     <see cref="Grid" /> that survives across refreshes to avoid allocating one per tick.
/// </summary>
internal static class NotifyIconExtensions
{
    private const double DefaultNotifyIconSize = 16;

    [ThreadStatic] private static Grid? _container;

    extension(NotifyIcon notifyIcon)
    {
        /// <summary>
        ///     Replaces the tray icon with the Segoe Fluent Icons full-battery glyph (U+F5FC). Used by
        ///     <see cref="NotifyIconWindow" /> when the evaluator decides the device is at 100%.
        /// </summary>
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

        /// <summary>
        ///     Renders <paramref name="child" /> centred in a 16x16 DPI-aware bitmap and assigns it as
        ///     the tray icon. Reuses a thread-static <see cref="Grid" /> to avoid per-refresh allocation.
        /// </summary>
        /// <param name="child">Element to render (typically a tray <see cref="TextBlock" />).</param>
        /// <param name="background">Optional background brush; null leaves the tray-region transparent.</param>
        internal void SetIcon(FrameworkElement child, Brush? background)
        {
            // Measure the child first so we can centre it within the 16x16 tray region.
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            child.Margin = new Thickness(
                (DefaultNotifyIconSize - child.DesiredSize.Width) / 2,
                (DefaultNotifyIconSize - child.DesiredSize.Height) / 2,
                0, 0);

            // Reuse a [ThreadStatic] container per refresh. Owning the Grid avoids one allocation
            // per refresh tick. Detach the previous child so a new caller can attach without
            // throwing "child already has a parent".
            var container = _container ??= new Grid
            {
                Width = DefaultNotifyIconSize,
                Height = DefaultNotifyIconSize
            };
            container.Children.Clear();
            container.Background = background;
            container.Children.Add(child);
            container.Measure(new Size(DefaultNotifyIconSize, DefaultNotifyIconSize));
            container.Arrange(new Rect(0, 0, DefaultNotifyIconSize, DefaultNotifyIconSize));

            var dpiScale = VisualTreeHelper.GetDpi(child);

            // RenderTargetBitmap retains unmanaged MIL bitmap memory until GC; without a periodic
            // reclaim, repeated tray refreshes eventually trip MILERR_WIN32ERROR
            // (see https://github.com/dotnet/wpf/issues/3067). The previous implementation paid
            // a blocking full-process GC plus WaitForPendingFinalizers on every refresh, which
            // stalled the dispatcher for tens of milliseconds per tick. The non-blocking
            // optimised variant lets the runtime amortise the work across refreshes without
            // pausing the tray refresh path.
            RenderTargetBitmap renderTargetBitmap =
                new(
                    (int)Math.Round(DefaultNotifyIconSize * dpiScale.DpiScaleX, MidpointRounding.AwayFromZero),
                    (int)Math.Round(DefaultNotifyIconSize * dpiScale.DpiScaleY, MidpointRounding.AwayFromZero),
                    dpiScale.PixelsPerInchX,
                    dpiScale.PixelsPerInchY,
                    PixelFormats.Default);
            renderTargetBitmap.Render(container);

            // Detach the child before assigning the icon so the caller may re-parent it next refresh.
            container.Children.Clear();

            notifyIcon.Icon = renderTargetBitmap;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: false, compacting: false);
        }
    }
}
