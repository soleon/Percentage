using System.Windows;
using System.Windows.Controls;

namespace Percentage.App.Controls;

public class AccentColourPicker : Control
{
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

    public static readonly DependencyProperty IsAutoColourProperty = DependencyProperty.Register(
        nameof(IsAutoColour), typeof(bool), typeof(AccentColourPicker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsAutoTransparentProperty = DependencyProperty.Register(
        nameof(IsAutoTransparent), typeof(bool), typeof(AccentColourPicker),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(AccentColourPicker));

    public static readonly DependencyProperty SelectedColourProperty = DependencyProperty.Register(
        nameof(SelectedColour), typeof(object), typeof(AccentColourPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    static AccentColourPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AccentColourPicker),
            new FrameworkPropertyMetadata(typeof(AccentColourPicker)));
    }

    public bool IsAutoColour
    {
        get => (bool)GetValue(IsAutoColourProperty);
        set => SetValue(IsAutoColourProperty, value);
    }

    public bool IsAutoTransparent
    {
        get => (bool)GetValue(IsAutoTransparentProperty);
        set => SetValue(IsAutoTransparentProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public object SelectedColour
    {
        get => GetValue(SelectedColourProperty);
        set => SetValue(SelectedColourProperty, value);
    }
}