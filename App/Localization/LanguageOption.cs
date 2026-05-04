using System.ComponentModel;
using Percentage.App.Resources;

namespace Percentage.App.Localization;

/// <summary>
///     One entry in the Settings page's language picker. <see cref="NativeDisplayName" /> is fixed
///     for normal entries (the language renders in its own script regardless of the active UI
///     culture) and resolves dynamically for the "use system language" sentinel so the picker
///     re-labels itself when the user switches language at runtime.
/// </summary>
public sealed class LanguageOption : INotifyPropertyChanged
{
    private static readonly PropertyChangedEventArgs NativeDisplayNamePropertyChangedArgs =
        new(nameof(NativeDisplayName));

    private readonly string _fixedDisplayName;
    private readonly bool _isSystemEntry;

    /// <summary>
    ///     Creates a language option. When <paramref name="cultureName" /> is empty (the
    ///     <see cref="SupportedLanguages.SystemCultureName" /> sentinel), the entry treats itself as
    ///     the system-language picker and re-resolves its display name from
    ///     <see cref="Strings.Settings_LanguageSystem" /> on every culture change.
    /// </summary>
    public LanguageOption(string cultureName, string nativeDisplayName)
    {
        CultureName = cultureName;
        _fixedDisplayName = nativeDisplayName;
        _isSystemEntry = string.IsNullOrEmpty(cultureName);

        if (_isSystemEntry)
        {
            // Strong subscription is fine: SupportedLanguages.All is process-lifetime, so this
            // option lives as long as the LocalizationManager singleton.
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
        }
    }

    /// <summary>BCP-47 culture name (or empty string for "use system language").</summary>
    public string CultureName { get; }

    /// <summary>Display label rendered in the picker; localised for the system entry only.</summary>
    public string NativeDisplayName => _isSystemEntry
        ? Strings.Settings_LanguageSystem
        : _fixedDisplayName;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Used by the SettingsPage ComboBox for display.</summary>
    public override string ToString() => NativeDisplayName;

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, NativeDisplayNamePropertyChangedArgs);
}
