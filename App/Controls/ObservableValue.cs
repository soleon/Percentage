using Codify.System.ComponentModel;

namespace Percentage.App.Controls;

internal abstract class ObservableValue<T> : NotificationObject
{
    public T? Value
    {
        get;
        set => SetValue(ref field, value);
    }
}
