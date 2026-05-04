using System.ComponentModel;
using System.Runtime.InteropServices;
using Percentage.App.Extensions;
using Percentage.App.Localization;
using Percentage.App.Resources;

namespace Percentage.App.Controls;

/// <summary>
///     Details-page control listing app-version + runtime-version + runtime-architecture as
///     <c>KeyValuePair</c> rows. Rebuilt on language change so the localised row labels stay in
///     sync.
/// </summary>
public sealed class ApplicationInformation : KeyValueItemsControl
{
    /// <summary>
    ///     Subscribes to <see cref="LocalizationManager" /> while the control is loaded and rebuilds
    ///     the items source on language change.
    /// </summary>
    public ApplicationInformation()
    {
        RebuildItemsSource();

        // Rebuild on Loaded too: the host page may use NavigationCacheMode="Enabled", so the
        // language could have changed while this control was detached and unsubscribed.
        Loaded += (_, _) =>
        {
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
            RebuildItemsSource();
        };
        Unloaded += (_, _) => LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e) =>
        _ = Dispatcher.InvokeAsync(RebuildItemsSource);

    private void RebuildItemsSource()
    {
        ItemsSource = new Dictionary<string, object>
        {
            { Strings.AppInfo_Version, VersionExtensions.GetAppVersion() },
            { Strings.AppInfo_RuntimeVersion, RuntimeInformation.FrameworkDescription },
            { Strings.AppInfo_RuntimeArchitecture, RuntimeInformation.RuntimeIdentifier }
        };
    }
}
