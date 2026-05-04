using System.Windows;
using System.Windows.Controls;

namespace Percentage.App.Controls;

/// <summary>
///     Settings-page picker for one tray-icon colour. Renders a small palette of Microsoft accent
///     swatches with a leading "auto" toggle. When <see cref="IsAutoColour" /> is true the picker
///     defers to the system accent (foreground use) or paints the swatch transparent (when
///     <see cref="IsAutoTransparent" /> is also true, used by the background-colour pickers).
///     <see cref="IsAutoColour" /> and <see cref="SelectedColour" /> are separate dependency
///     properties so XAML can two-way bind each independently to its own
///     <c>Settings.IsAutoBattery*Colour</c> and <c>Settings.Battery*Colour</c> field.
/// </summary>
public class AccentColourPicker : Control
{
    /// <summary>
    ///     Microsoft accent palette shown in the swatch <c>ComboBox</c>. Hex strings (not Brushes) so
    ///     the same array round-trips to/from the user setting.
    /// </summary>
    public static readonly string[] AccentBrushes =
    [
        "#FF000000", // Black
        "#FFFFFFFF", // White
        "#FFFFB900", // Gold
        "#FFFF8C00", // Dark Orange
        "#FFF7630C", // Orange
        "#FFCA5010", // Burnt Orange
        "#FFDA3B01", // Red-Orange
        "#FFEF6950", // Salmon
        "#FFD13438", // Red
        "#FFFF4343", // Bright Red
        "#FFE74856", // Light Red
        "#FFE81123", // Crimson Red
        "#FFEA005E", // Pink
        "#FFC30052", // Magenta
        "#FFE3008C", // Fuchsia
        "#FFBF0077", // Dark Magenta
        "#FFC239B3", // Purple
        "#FF9A0089", // Dark Purple
        "#FF0078D7", // Blue
        "#FF0063B1", // Royal Blue
        "#FF8E8CD8", // Lavender
        "#FF6B69D6", // Indigo
        "#FF8764B8", // Violet
        "#FF744DA9", // Purple Indigo
        "#FFB146C2", // Orchid
        "#FF0099BC", // Teal
        "#FF2D7D9A", // Peacock Blue
        "#FF00B7C3", // Light Aqua
        "#FF038387", // Dark Cyan
        "#FF00CC6A", // Light Green
        "#FF10893E", // Green
        "#FF7A7574", // Taupe
        "#FF5D5A58", // Dark Gray
        "#FF68768A", // Cool Gray
        "#FF515C6B" // Slate
    ];

    /// <summary>Identifies the <see cref="IsAutoColour" /> dependency property.</summary>
    public static readonly DependencyProperty IsAutoColourProperty = DependencyProperty.Register(
        nameof(IsAutoColour), typeof(bool), typeof(AccentColourPicker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Identifies the <see cref="IsAutoTransparent" /> dependency property.</summary>
    public static readonly DependencyProperty IsAutoTransparentProperty = DependencyProperty.Register(
        nameof(IsAutoTransparent), typeof(bool), typeof(AccentColourPicker),
        new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the <see cref="Label" /> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(AccentColourPicker));

    /// <summary>Identifies the <see cref="SelectedColour" /> dependency property.</summary>
    public static readonly DependencyProperty SelectedColourProperty = DependencyProperty.Register(
        nameof(SelectedColour), typeof(object), typeof(AccentColourPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    static AccentColourPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AccentColourPicker),
            new FrameworkPropertyMetadata(typeof(AccentColourPicker)));
    }

    /// <summary>True when the picker should defer to "auto" (system accent or transparent).</summary>
    public bool IsAutoColour
    {
        get => (bool)GetValue(IsAutoColourProperty);
        set => SetValue(IsAutoColourProperty, value);
    }

    /// <summary>
    ///     When true, "auto" means transparent (used by the background-colour pickers); otherwise
    ///     "auto" means the system accent foreground colour. XAML-only flag; never bound to settings.
    /// </summary>
    public bool IsAutoTransparent
    {
        get => (bool)GetValue(IsAutoTransparentProperty);
        set => SetValue(IsAutoTransparentProperty, value);
    }

    /// <summary>Localised label rendered to the left of the swatch combobox.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    ///     The currently selected swatch as a hex string (e.g. <c>#FF0078D7</c>). Object-typed to
    ///     match the <c>ComboBox.SelectedItem</c> binding contract.
    /// </summary>
    public object SelectedColour
    {
        get => GetValue(SelectedColourProperty);
        set => SetValue(SelectedColourProperty, value);
    }
}
