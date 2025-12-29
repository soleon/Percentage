using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Wpf.Ui.Tray.Controls;

namespace Percentage.App.Controls;

/// <summary>
///     A version of <see cref="NotifyIcon" /> that ensures icons are properly destroyed to prevent memory leaks.
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
    }

    /// <summary>
    ///     Destroys an icon and frees any memory the icon occupied.
    /// </summary>
    /// <param name="hIcon">A handle to the icon to be destroyed. The icon must not be in use.</param>
    /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);
}