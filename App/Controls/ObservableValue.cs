using Codify.System.ComponentModel;

namespace Percentage.App.Controls;

/// <summary>
///     Bindable <c>Value</c> wrapper used by the Details-page rows so each row's value can fire its
///     own <c>PropertyChanged</c> without rebuilding the whole items source.
/// </summary>
/// <typeparam name="T">Underlying value type bound by the row's data template.</typeparam>
internal abstract class ObservableValue<T> : NotificationObject
{
    /// <summary>The current value; setting fires <c>PropertyChanged</c> when it differs.</summary>
    public T? Value
    {
        get;
        set => SetValue(ref field, value);
    }
}
