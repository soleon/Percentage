using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Percentage.App.Controls;

internal class ObservableValue<T> : INotifyPropertyChanged
{
    public T? Value
    {
        get;
        set => SetField(ref field, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<TValue>(ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TValue>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}