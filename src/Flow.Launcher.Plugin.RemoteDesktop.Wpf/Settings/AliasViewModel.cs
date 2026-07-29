using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public class AliasViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();
    private readonly bool _isInitialized;
    private bool _aliasTouched;
    private bool _hostTouched;

    public AliasViewModel(string alias, string host)
    {
        _alias = alias;
        _host = host;

        HasErrors = string.IsNullOrWhiteSpace(_alias) || string.IsNullOrWhiteSpace(_host);

        _isInitialized = true;
    }

    public string Alias
    {
        get => _alias;
        set => SetAndValidate(value, ref _alias, ref _aliasTouched, nameof(Alias));
    }

    public string Host
    {
        get => _host;
        set => SetAndValidate(value, ref _host, ref _hostTouched, nameof(Host));
    }

    private void SetAndValidate(string value, ref string field, ref bool touched, string name)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        OnPropertyChanged();

        if (!_isInitialized)
        {
            return;
        }

        touched = true;
        ValidateField(name, field, touched);
        UpdateHasErrors();
    }

    public bool HasErrors { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return Array.Empty<string>();
        }

        return _errors.TryGetValue(propertyName, out List<string>? list) ? list : Array.Empty<string>();
    }

    private string _alias;
    private string _host;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ValidateField(string property, string? value, bool touched)
    {
        bool hadError = _errors.ContainsKey(property);

        if (touched && string.IsNullOrWhiteSpace(value))
        {
            _errors[property] = ["Required"];
        }
        else
        {
            _errors.Remove(property);
        }

        bool hasError = _errors.ContainsKey(property);
        if (hadError != hasError)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
        }
    }

    private void UpdateHasErrors()
    {
        bool hasErrors = string.IsNullOrWhiteSpace(_alias)
                          || string.IsNullOrWhiteSpace(_host)
                          || _errors.Count > 0;

        if (HasErrors == hasErrors)
        {
            return;
        }

        HasErrors = hasErrors;
        OnPropertyChanged(nameof(HasErrors));
    }

#if DEBUG
    internal class Design() : AliasViewModel("AliasName", "192.168.0.1");
#endif
}
