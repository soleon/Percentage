using System.Collections.Generic;
using Percentage.App.Resources;

namespace Percentage.App.Localization;

/// <summary>
///   Enumerates the languages the app exposes in the SettingsPage picker. The neutral
///   resource (<c>Strings.resx</c>) is English; future translations are added as
///   <c>Strings.&lt;culture&gt;.resx</c> files plus one entry here, with a matching
///   <c>Pack/Strings/&lt;culture&gt;/Resources.resw</c> and an explicit
///   <c>&lt;Resource Language="&lt;culture&gt;"/&gt;</c> in <c>Pack/Package.appxmanifest</c>.
/// </summary>
public static class SupportedLanguages
{
    /// <summary>
    ///   Sentinel CultureName used by the "Use system language" picker entry.
    ///   Matches the <see cref="Properties.Settings.Language"/> default of an empty string.
    /// </summary>
    public const string SystemCultureName = "";

    /// <summary>
    ///   Languages offered to the user. <c>NativeDisplayName</c> is intentionally written in
    ///   each language's own script so users can find their language regardless of the active
    ///   UI culture; the System entry is the only one that resolves through <see cref="Strings"/>.
    /// </summary>
    public static IReadOnlyList<LanguageOption> All { get; } =
    [
        new(SystemCultureName, Strings.Settings_LanguageSystem),
        new("en", "English"),
        new("de", "Deutsch"),
        new("es", "Español"),
        new("fr", "Français"),
        new("it", "Italiano"),
        new("pl", "Polski"),
        new("pt-BR", "Português (Brasil)"),
        new("ru", "Русский"),
        // TODO(ar, he): RTL layout pass — apply FlowDirection from LocalizationManager.Culture, audit NavigationView/tray menu/Settings cards.
        new("ar", "العربية"),
        new("he", "עברית"),
        new("ja", "日本語"),
        new("ko", "한국어"),
        new("zh-Hans", "中文(简体)"),
        new("zh-Hant", "中文(繁體)")
        // Add new languages here. Each addition requires a sibling
        // App/Resources/Strings.<culture>.resx, a Pack/Strings/<culture>/Resources.resw,
        // and a <Resource Language="<culture>"/> entry in Pack/Package.appxmanifest.
    ];
}

public readonly record struct LanguageOption(string CultureName, string NativeDisplayName)
{
    /// <summary>Used by the SettingsPage ComboBox for display.</summary>
    public override string ToString() => NativeDisplayName;
}
