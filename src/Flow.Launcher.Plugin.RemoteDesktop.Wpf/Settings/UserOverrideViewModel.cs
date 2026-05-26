using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public class UserOverrideViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    public UserOverrideViewModel(string regex, string user)
    {
        Regex = regex;
        User = user;
    }

    public string Regex
    {
        get;
        set
        {
            if (field != value)
            {
                OnPropertyChanged();
            }

            field = value;
        }
    }

    public string User
    {
        get;
        set
        {
            if (field != value)
            {
                OnPropertyChanged();
            }

            field = value;
        }
    }

    /// <inheritdoc />
    public IEnumerable GetErrors(string? propertyName)
    {
        return Array.Empty<string>();
    }

    /// <inheritdoc />
    public bool HasErrors { get; private set; }

    /// <inheritdoc />
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}