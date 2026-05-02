using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Percentage.App.Extensions;
using Percentage.App.Localization;
using Percentage.App.Resources;

namespace Percentage.App.Controls;

public sealed class ApplicationInformation : KeyValueItemsControl
{
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

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(RebuildItemsSource);
    }

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
