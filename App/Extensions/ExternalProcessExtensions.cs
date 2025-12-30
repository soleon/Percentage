using System.Diagnostics;

namespace Percentage.App.Extensions;

internal static class ExternalProcessExtensions
{
    public static void OpenDonationLocation()
    {
        StartShellExecutedProgress("https://www.paypal.com/donate/?hosted_button_id=EFS3E8WPF8SJL");
    }

    internal static void OpenFeedbackLocation()
    {
        StartShellExecutedProgress("https://github.com/soleon/Percentage/issues");
    }

    internal static void OpenPowerSettings()
    {
        StartShellExecutedProgress("ms-settings:powersleep");
    }

    internal static void OpenSourceCodeLocation()
    {
        StartShellExecutedProgress("https://github.com/soleon/Percentage");
    }
    
    internal static void OpenGitHubIssuesLocation()
    {
        StartShellExecutedProgress("https://github.com/soleon/Percentage/issues");
    }

    internal static void ShowRatingView()
    {
        StartShellExecutedProgress("ms-windows-store://review/?ProductId=9PCKT2B7DZMW");
    }

    public static void ShutDownDevice()
    {
        Process.Start(new ProcessStartInfo("shutdown", "/s /t 0")
        {
            CreateNoWindow = true
        });
    }

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