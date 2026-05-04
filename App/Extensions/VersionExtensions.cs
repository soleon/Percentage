using System.Reflection;
using Windows.ApplicationModel;

namespace Percentage.App.Extensions;

/// <summary>App-version helpers that work in both MSIX and unpackaged (portable) flavours.</summary>
public static class VersionExtensions
{
    /// <summary>
    ///     Returns the running app's version. Reads <see cref="Package.Current" /> when running as a
    ///     packaged MSIX; falls back to the executing assembly's <see cref="AssemblyName.Version" />
    ///     for portable builds where Package.Current throws.
    /// </summary>
    internal static string GetAppVersion()
    {
        string version;
        try
        {
            version = Package.Current.Id.Version.ToVersionString();
        }
        catch
        {
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }

        return version;
    }
}
