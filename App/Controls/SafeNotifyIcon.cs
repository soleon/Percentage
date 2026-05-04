using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using Wpf.Ui.Tray.Controls;

namespace Percentage.App.Controls;

/// <summary>
///     A version of <see cref="NotifyIcon" /> that ensures icons are properly destroyed to prevent memory leaks
///     and that tooltip text containing non-ASCII characters renders correctly.
/// </summary>
public partial class SafeNotifyIcon : NotifyIcon
{
    private const uint NifTip = 0x00000004;

    private const uint NimModify = 0x00000001;

    /// <summary>
    ///     Reflection field for the internal notification icon manager in Wpf.Ui.
    /// </summary>
    private static readonly FieldInfo? InternalNotifyIconManagerField;

    /// <summary>
    ///     Reflection field for the hIcon field in Shell32.NOTIFYICONDATA.
    /// </summary>
    private static readonly FieldInfo? Shell32NotifyIconDataHIconField;

    /// <summary>
    ///     Reflection field for the hWnd field in Shell32.NOTIFYICONDATA.
    /// </summary>
    private static readonly FieldInfo? Shell32NotifyIconDataHWndField;

    /// <summary>
    ///     Reflection field for the uID field in Shell32.NOTIFYICONDATA.
    /// </summary>
    private static readonly FieldInfo? Shell32NotifyIconDataUIDField;

    /// <summary>
    ///     Reflection property for ShellIconData in INotifyIcon.
    /// </summary>
    private static readonly PropertyInfo? ShellIconDataProperty;

    /// <summary>
    ///     Initialises static members of the <see cref="SafeNotifyIcon" /> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if reflection fails to find required members.</exception>
    static SafeNotifyIcon()
    {
        var notifyIconType = typeof(NotifyIcon);

        var wpfUiTrayAssembly = notifyIconType.Assembly;

        InternalNotifyIconManagerField =
            notifyIconType.GetField("internalNotifyIconManager", BindingFlags.NonPublic | BindingFlags.Instance) ??
            throw new InvalidOperationException(
                "Unable to get Wpf.Ui.Tray.Controls.NotifyIcon.internalNotifyIconManager field.");

        ShellIconDataProperty =
            wpfUiTrayAssembly.GetType("Wpf.Ui.Tray.INotifyIcon")
                ?.GetProperty("ShellIconData", BindingFlags.Public | BindingFlags.Instance) ??
            throw new InvalidOperationException("Unable to get Wpf.Ui.Tray.INotifyIcon.ShellIconData property.");

        var shell32Type = wpfUiTrayAssembly.GetType("Wpf.Ui.Tray.Interop.Shell32") ??
                          throw new InvalidOperationException("Unable to get Wpf.Ui.Tray.Interop.Shell32 type.");

        var shell32NotifyIconDataType =
            shell32Type.GetNestedType("NOTIFYICONDATA", BindingFlags.Public | BindingFlags.NonPublic) ??
            throw new InvalidOperationException(
                "Unable to get Wpf.Ui.Tray.Interop.Shell32.NOTIFYICONDATA nested type.");

        Shell32NotifyIconDataHIconField =
            shell32NotifyIconDataType.GetField("hIcon", BindingFlags.Public | BindingFlags.Instance) ??
            throw new InvalidOperationException(
                "Unable to get Wpf.Ui.Tray.Interop.Shell32.NOTIFYICONDATA.hIcon field.");

        Shell32NotifyIconDataHWndField =
            shell32NotifyIconDataType.GetField("hWnd", BindingFlags.Public | BindingFlags.Instance) ??
            throw new InvalidOperationException(
                "Unable to get Wpf.Ui.Tray.Interop.Shell32.NOTIFYICONDATA.hWnd field.");

        Shell32NotifyIconDataUIDField =
            shell32NotifyIconDataType.GetField("uID", BindingFlags.Public | BindingFlags.Instance) ??
            throw new InvalidOperationException(
                "Unable to get Wpf.Ui.Tray.Interop.Shell32.NOTIFYICONDATA.uID field.");
    }

    /// <summary>
    ///     Destroys an icon and frees any memory the icon occupied.
    /// </summary>
    /// <param name="hIcon">A handle to the icon to be destroyed. The icon must not be in use.</param>
    /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);

    [LibraryImport("Shell32.dll", EntryPoint = "Shell_NotifyIconW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Shell_NotifyIconW(uint dwMessage, ref NotifyIconDataW lpdata);

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == IconProperty)
        {
            var internalNotifyIconManager = InternalNotifyIconManagerField?.GetValue(this) ??
                                            throw new InvalidOperationException(
                                                "Unable to get internal notify icon manager.");

            var shellIconData = ShellIconDataProperty?.GetValue(internalNotifyIconManager) ??
                                throw new InvalidOperationException(
                                    "Unable to get ShellIconData from internal notify icon manager.");

            var shellIconDataHIcon = (IntPtr)(Shell32NotifyIconDataHIconField?.GetValue(shellIconData) ??
                                              throw new InvalidOperationException(
                                                  "Unable to get ShellIconData.hIcon."));

            if (shellIconDataHIcon != IntPtr.Zero && !DestroyIcon(shellIconDataHIcon))
            {
                // The whole point of this class is to plug WPF-UI's HICON leak; if DestroyIcon
                // ever fails, we want the failure to surface (About page picks it up) rather than
                // resume leaking handles silently.
                App.SetAppError(new Win32Exception(Marshal.GetLastPInvokeError(),
                    $"DestroyIcon(0x{shellIconDataHIcon.ToInt64():X}) failed."));
            }
        }

        base.OnPropertyChanged(e);

        // Workaround for lepoco/wpfui: NOTIFYICONDATA omits CharSet=Unicode, so szTip is ANSI-marshalled
        // and non-ASCII characters (Chinese, etc.) become '?' in the tray tooltip. After WPF-UI fires its
        // ANSI Shell_NotifyIcon for any property that includes the tooltip in the modify call, re-send the
        // tooltip via Shell_NotifyIconW with a Unicode struct so Windows stores the correct text.
        if (e.Property == TooltipTextProperty || e.Property == IconProperty)
        {
            ReapplyUnicodeTooltip();
        }
    }

    private void ReapplyUnicodeTooltip()
    {
        var manager = InternalNotifyIconManagerField?.GetValue(this);
        if (manager is null)
        {
            return;
        }

        var shellIconData = ShellIconDataProperty?.GetValue(manager);
        if (shellIconData is null)
        {
            return;
        }

        var hWnd = (IntPtr)(Shell32NotifyIconDataHWndField?.GetValue(shellIconData) ?? IntPtr.Zero);
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        var uID = (int)(Shell32NotifyIconDataUIDField?.GetValue(shellIconData) ?? 0);

        NotifyIconDataW data = default;
        data.hWnd = hWnd;
        data.uID = uID;
        data.uFlags = NifTip;
        data.cbSize = Unsafe.SizeOf<NotifyIconDataW>();

        // szTip is a 0x80-wchar fixed buffer; copy at most 0x7F characters and null-terminate.
        Span<char> tipSpan = data.szTip;
        var tooltip = TooltipText ?? string.Empty;
        var copyLength = Math.Min(tooltip.Length, tipSpan.Length - 1);
        tooltip.AsSpan(0, copyLength).CopyTo(tipSpan);
        tipSpan[copyLength] = '\0';

        Shell_NotifyIconW(NimModify, ref data);
    }

    /// <summary>
    ///     Unicode mirror of WPF-UI's NOTIFYICONDATA. Field order, sizes, and offsets match the upstream
    ///     ANSI struct so cbSize and the kernel-side layout are identical; only the szTip / szInfo /
    ///     szInfoTitle buffers are explicitly UTF-16 here.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NotifyIconDataW
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public uint uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        public TipBuffer szTip;
        public uint dwState;
        public uint dwStateMask;
        public InfoBuffer szInfo;
        public uint uVersion;
        public InfoTitleBuffer szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [InlineArray(0x80)]
    private struct TipBuffer
    {
        private char _element;
    }

    [InlineArray(0x100)]
    private struct InfoBuffer
    {
        private char _element;
    }

    [InlineArray(0x40)]
    private struct InfoTitleBuffer
    {
        private char _element;
    }
}
