using Windows.ApplicationModel;

namespace Percentage.App.Extensions;

/// <summary>Extensions for the WinRT <see cref="PackageVersion" /> struct.</summary>
internal static class PackageExtensions
{
    /// <summary>
    ///     Formats a <see cref="PackageVersion" /> as <c>Major.Minor.Build</c> (the Revision component
    ///     is intentionally dropped to match the user-visible store version).
    /// </summary>
    internal static string ToVersionString(this PackageVersion version) =>
        string.Join('.', version.Major, version.Minor, version.Build);
}
