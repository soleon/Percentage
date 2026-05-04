using System.Windows;
using Percentage.App.Resources;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Percentage.App.Controls;

/// <summary>
///     <see cref="Button" /> with a built-in copy glyph that puts <see cref="TargetObject" />'s
///     <see cref="object.ToString" /> on the clipboard and shows a snackbar. Used in the Details
///     page next to each battery readout so the user can paste the value into a bug report.
/// </summary>
public class CopyButton : Button
{
    /// <summary>Identifies the <see cref="TargetObject" /> dependency property.</summary>
    public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register(
        nameof(TargetObject), typeof(object), typeof(CopyButton), new PropertyMetadata(default(object)));

    static CopyButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CopyButton), new FrameworkPropertyMetadata(typeof(CopyButton)));
        ToolTipProperty.OverrideMetadata(typeof(CopyButton),
            new FrameworkPropertyMetadata(Strings.Copy_Tooltip));
    }

    /// <summary>Initialises a <see cref="CopyButton" /> with the standard <c>Copy20</c> glyph.</summary>
    public CopyButton()
    {
        Style = (Style)FindResource(typeof(Button));
        Icon = new SymbolIcon(SymbolRegular.Copy20);
    }

    /// <summary>Source whose <see cref="object.ToString" /> is copied on click.</summary>
    public object? TargetObject
    {
        get => GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        var targetStringValue = TargetObject?.ToString();
        if (targetStringValue is null)
        {
            App.SnackBarService.Show(Strings.Copy_NothingToCopyTitle, Strings.Copy_NothingToCopyBody,
                ControlAppearance.Caution);
        }
        else
        {
            Clipboard.SetText(targetStringValue);
            App.SnackBarService.Show(Strings.Copy_CopiedTitle, targetStringValue, ControlAppearance.Success);
        }

        base.OnClick();
    }
}
