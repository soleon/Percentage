using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Codify.System.ComponentModel;
using Percentage.App.Resources;
using static Percentage.App.Properties.Settings;

namespace Percentage.App.Localization;

/// <summary>
///   Singleton that owns the application's current UI culture, exposes resource strings as an
///   indexer for XAML bindings, and broadcasts a single PropertyChanged notification on culture
///   changes so every <c>{loc:Localize ...}</c> binding refreshes live.
/// </summary>
public sealed class LocalizationManager : NotificationObject
{
    public static LocalizationManager Instance { get; } = new();

    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private bool _subscribed;

    private LocalizationManager()
    {
    }

    /// <summary>The active <see cref="CultureInfo"/>. Setting this triggers a UI refresh.</summary>
    public CultureInfo Culture
    {
        get => _culture;
        private set
        {
            if (Equals(_culture, value)) return;
            _culture = value;

            // Strings.Culture wins over CurrentUICulture when both are set, but we also update
            // the thread/default cultures so number/date formatting in interpolations is consistent.
            Strings.Culture = value;
            CultureInfo.DefaultThreadCurrentCulture = value;
            CultureInfo.DefaultThreadCurrentUICulture = value;
            Thread.CurrentThread.CurrentCulture = value;
            Thread.CurrentThread.CurrentUICulture = value;

            // "Item[]" is the WPF convention for "every indexer binding on this source must refresh".
            OnPropertyChanged("Item[]");
            OnPropertyChanged(nameof(Culture));
        }
    }

    /// <summary>Indexer used by the <c>{loc:Localize}</c> markup extension.</summary>
    public string this[string key] =>
        Strings.ResourceManager.GetString(key, _culture) ?? key;

    /// <summary>
    ///   Resolve the active culture from <see cref="Default.Language"/> (empty = system),
    ///   apply it, and start listening for future changes to that setting.
    /// </summary>
    public void ApplyFromSettings()
    {
        Culture = ResolveCulture(Default.Language);

        if (_subscribed) return;
        Default.PropertyChanged += OnSettingsPropertyChanged;
        _subscribed = true;
    }

    private static CultureInfo ResolveCulture(string? cultureName)
    {
        if (string.IsNullOrEmpty(cultureName)) return CultureInfo.InstalledUICulture;
        try { return CultureInfo.GetCultureInfo(cultureName); }
        catch (CultureNotFoundException) { return CultureInfo.InstalledUICulture; }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Default.Language))
            Culture = ResolveCulture(Default.Language);
    }
}
