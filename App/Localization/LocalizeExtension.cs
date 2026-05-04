using System.Windows.Data;
using System.Windows.Markup;

namespace Percentage.App.Localization;

/// <summary>
///     XAML markup extension that returns a one-way binding to
///     <see cref="LocalizationManager.Instance" /> for the given key. Usage:
///     <c>Text="{loc:Localize Settings_AutoStart}"</c>.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension()
    {
    }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")] public string? Key { get; set; }

    public override object? ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
        {
            throw new InvalidOperationException("Localize markup extension requires a Key.");
        }

        return new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay
        }.ProvideValue(serviceProvider);
    }
}
