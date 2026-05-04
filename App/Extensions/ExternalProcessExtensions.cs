using System.Diagnostics;

namespace Percentage.App.Extensions;

/// <summary>
///     Thin wrappers around <see cref="Process.Start(ProcessStartInfo)" /> for the menu items in
///     About / Settings / tray menu that need to open an external URL, a system settings page, or
///     trigger a power action.
/// </summary>
internal static class ExternalProcessExtensions
{
    /// <summary>Opens the project's PayPal donation page in the default browser.</summary>
    public static void OpenDonationLocation() =>
        StartShellExecutedProgress("https://www.paypal.com/donate/?hosted_button_id=EFS3E8WPF8SJL");

    /// <summary>Opens the GitHub issue tracker so the user can file feedback.</summary>
    internal static void OpenFeedbackLocation() =>
        StartShellExecutedProgress("https://github.com/soleon/Percentage/issues");

    /// <summary>Opens the GitHub issue tracker for an existing issue lookup.</summary>
    internal static void OpenGitHubIssuesLocation() =>
        StartShellExecutedProgress("https://github.com/soleon/Percentage/issues");

    /// <summary>Opens Windows Settings &gt; System &gt; Power &amp; sleep.</summary>
    internal static void OpenPowerSettings() => StartShellExecutedProgress("ms-settings:powersleep");

    /// <summary>Opens the project's source-code page on GitHub.</summary>
    internal static void OpenSourceCodeLocation() => StartShellExecutedProgress("https://github.com/soleon/Percentage");

    /// <summary>Opens the Microsoft Store review page for this app's product ID.</summary>
    internal static void ShowRatingView() =>
        StartShellExecutedProgress("ms-windows-store://review/?ProductId=9PCKT2B7DZMW");

    /// <summary>Initiates an immediate device shutdown via <c>shutdown /s /t 0</c>.</summary>
    public static void ShutDownDevice()
    {
        Process.Start(new ProcessStartInfo("shutdown", "/s /t 0")
        {
            CreateNoWindow = true
        });
    }

    /// <summary>Puts the device to sleep via the documented <c>powrprof.SetSuspendState</c> rundll32 entry point.</summary>
    internal static void SleepDevice()
    {
        // Parameter 0,0,0 for "SetSuspendState" native function:
        // 0: no hibernation
        // 0: deprecated
        // 0: allow wake-up events
        // See documentation for "powerprof":
        // https://learn.microsoft.com/windows/win32/api/powrprof/nf-powrprof-setsuspendstate
        Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 0,0,0");
    }

    private static void StartShellExecutedProgress(string fileName)
    {
        Process.Start(new ProcessStartInfo(fileName)
        {
            UseShellExecute = true
        });
    }
}
