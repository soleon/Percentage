using System;
using System.Reflection;
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

            if (shellIconDataHIcon != IntPtr.Zero) DestroyIcon(shellIconDataHIcon);
        }

        base.OnPropertyChanged(e);

        // Workaround for lepoco/wpfui: NOTIFYICONDATA omits CharSet=Unicode, so szTip is ANSI-marshaled
        // and non-ASCII characters (Chinese, etc.) become '?' in the tray tooltip. After WPF-UI fires its
        // ANSI Shell_NotifyIcon for any property that includes the tooltip in the modify call, re-send the
        // tooltip via Shell_NotifyIconW with a Unicode struct so Windows stores the correct text.
        if (e.Property == TooltipTextProperty || e.Property == IconProperty)
            ReapplyUnicodeTooltip();
    }

    /// <summary>
    ///     Destroys an icon and frees any memory the icon occupied.
    /// </summary>
    /// <param name="hIcon">A handle to the icon to be destroyed. The icon must not be in use.</param>
    /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);

    private void ReapplyUnicodeTooltip()
    {
        var manager = InternalNotifyIconManagerField?.GetValue(this);
        if (manager is null) return;

        var shellIconData = ShellIconDataProperty?.GetValue(manager);
        if (shellIconData is null) return;

        var hWnd = (IntPtr)(Shell32NotifyIconDataHWndField?.GetValue(shellIconData) ?? IntPtr.Zero);
        if (hWnd == IntPtr.Zero) return;

        var uID = (int)(Shell32NotifyIconDataUIDField?.GetValue(shellIconData) ?? 0);

        var data = new NotifyIconDataW
        {
            hWnd = hWnd,
            uID = uID,
            uFlags = NifTip,
            szTip = TooltipText ?? string.Empty
        };
        data.cbSize = Marshal.SizeOf(data);

        Shell_NotifyIconW(NimModify, data);
    }

    private const uint NimModify = 0x00000001;
    private const uint NifTip = 0x00000004;

    [DllImport("Shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, [In] NotifyIconDataW lpdata);

    /// <summary>
    ///     Unicode-charset mirror of WPF-UI's NOTIFYICONDATA. Field order, sizes and offsets match the upstream
    ///     ANSI struct so cbSize and the kernel-side layout are identical; only the marshaling charset differs.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class NotifyIconDataW
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public uint uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
        public string szTip = string.Empty;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)]
        public string szInfo = string.Empty;

        public uint uVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
        public string szInfoTitle = string.Empty;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }
}
