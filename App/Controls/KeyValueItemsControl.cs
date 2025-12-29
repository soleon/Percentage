using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Percentage.App.Controls;

public class KeyValueItemsControl : ItemsControl
{
    static KeyValueItemsControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(KeyValueItemsControl),
            new FrameworkPropertyMetadata(typeof(KeyValueItemsControl)));
    }

    public override string? ToString()
    {
        if (ItemsSource is IEnumerable<KeyValuePair<string, object>> pairs)
            return string.Join(Environment.NewLine, pairs.Select(pair => $"{pair.Key}: {pair.Value}"));
        return base.ToString();
    }
}