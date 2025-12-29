using System.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Percentage.App.Controls;

public class CopyButton : Button
{
    public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register(
        nameof(TargetObject), typeof(object), typeof(CopyButton), new PropertyMetadata(default(object)));

    static CopyButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CopyButton), new FrameworkPropertyMetadata(typeof(CopyButton)));
        ToolTipProperty.OverrideMetadata(typeof(CopyButton), new FrameworkPropertyMetadata("Copy to clipboard"));
    }

    public CopyButton()
    {
        Style = (Style)FindResource(typeof(Button));
        Icon = new SymbolIcon(SymbolRegular.Copy20);
    }

    public object? TargetObject
    {
        get => GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }

    protected override void OnClick()
    {
        var targetStringValue = TargetObject?.ToString();
        if (targetStringValue == null)
        {
            App.SnackBarService.Show("Nothing to copy", "There was nothing to copy to the clipboard.",
                ControlAppearance.Caution);
        }
        else
        {
            Clipboard.SetText(targetStringValue);
            App.SnackBarService.Show("Copied to clipboard", targetStringValue, ControlAppearance.Success);
        }

        base.OnClick();
    }
}