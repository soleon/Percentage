using System.Windows;
using System.Windows.Controls;

namespace Percentage.App.Controls;

/// <summary>
///     <see cref="ItemsControl" /> base for the Details page's two-column "name : value" lists.
///     Subclassed by <see cref="ApplicationInformation" /> and <see cref="BatteryInformation" />.
///     Overrides <see cref="ToString" /> so the <see cref="CopyButton" /> on the Details page can
///     copy the entire list as a multi-line string for bug reports.
/// </summary>
public class KeyValueItemsControl : ItemsControl
{
    static KeyValueItemsControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(KeyValueItemsControl),
            new FrameworkPropertyMetadata(typeof(KeyValueItemsControl)));
    }

    /// <summary>
    ///     Returns the items as <c>Key: Value</c> lines joined by newlines (when the items source is
    ///     <see cref="KeyValuePair{TKey,TValue}" /> of <see cref="string" /> and <see cref="object" />).
    ///     Used by <see cref="CopyButton" /> to copy the whole list to the clipboard.
    /// </summary>
    public override string? ToString()
    {
        if (ItemsSource is IEnumerable<KeyValuePair<string, object>> pairs)
        {
            return string.Join(Environment.NewLine, pairs.Select(pair => $"{pair.Key}: {pair.Value}"));
        }

        return base.ToString();
    }
}
