using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BasicMvvmSample.ViewModels;

public class SimpleViewModel : INotifyPropertyChanged
{
    private string? _name;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public string? Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Greeting));
            }
        }
    }

    public string Greeting
    {
        get
        {
            if (string.IsNullOrEmpty(Name))
            {
                return "Hello World from Avalonia.Samples!";
            }
            else
            {
                return $"Hello {Name}!";
            }
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}