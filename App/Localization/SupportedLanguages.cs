namespace Percentage.App.Localization;

/// <summary>
///     Lists the languages the app exposes in the SettingsPage picker. The neutral
///     resource (<c>Strings.resx</c>) is English; future translations are added as
///     <c>Strings.&lt;culture&gt;.resx</c> files plus one entry here, with a matching
///     <c>Pack/Strings/&lt;culture&gt;/Resources.resw</c> and an explicit
///     <c>&lt;Resource Language="&lt;culture&gt;"/&gt;</c> in <c>Pack/Package.appxmanifest</c>.
/// </summary>
public static class SupportedLanguages
{
    /// <summary>
    ///     Sentinel CultureName used by the "Use system language" picker entry.
    ///     Matches the <see cref="Properties.Settings.Language" /> default of an empty string.
    /// </summary>
    public const string SystemCultureName = "";

    /// <summary>
    ///     Languages offered to the user. <c>NativeDisplayName</c> is intentionally written in
    ///     each language's own script so users can find their language regardless of the active
    ///     UI culture; the System entry is the only one that resolves through
    ///     <see cref="Resources.Strings" /> and re-labels itself on culture changes.
    /// </summary>
    public static IReadOnlyList<LanguageOption> All { get; } =
    [
        // Empty cultureName flips LanguageOption into "system entry" mode, and the
        // nativeDisplayName argument is ignored - the actual label resolves through
        // Strings.Settings_LanguageSystem on every culture change.
        new(SystemCultureName, string.Empty),
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
