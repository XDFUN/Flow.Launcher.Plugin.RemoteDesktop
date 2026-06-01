using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public class UserOverrideViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly List<string> _regexErrors = [];
    private readonly bool _isInitialized;

    public UserOverrideViewModel(string regex, string user)
    {
        Regex = regex;
        User = user;
        HasErrors = string.IsNullOrWhiteSpace(regex);

        _isInitialized = true;
    }

    public string Regex
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            OnPropertyChanged();

            if (_isInitialized)
            {
                CheckRegex();
            }
        }
    }

    public string User
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public IEnumerable GetErrors(string? propertyName)
    {
        return propertyName switch
        {
            nameof(Regex) => _regexErrors,
            _ => Enumerable.Empty<string>(),
        };
    }

    /// <inheritdoc />
    public bool HasErrors
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnPropertyErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void CheckRegex()
    {
        _regexErrors.Clear();

        if (string.IsNullOrWhiteSpace(Regex))
        {
            _regexErrors.Add("Regex cannot be empty");
        }
        else
        {
            try
            {
                _ = new Regex(Regex);
            }
            catch (ArgumentException)
            {
                _regexErrors.Add("Regex is invalid");
            }
        }

        OnPropertyErrorsChanged(nameof(Regex));
        HasErrors = _regexErrors.Count > 0;
    }

#if DEBUG
    internal class Design() : UserOverrideViewModel(".*", "user");
#endif
}