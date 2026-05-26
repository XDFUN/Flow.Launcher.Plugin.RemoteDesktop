using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Flow.Launcher.Plugin.RemoteDesktop.Events;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public class SettingsViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    public SettingsViewModel(RemoteDesktopSettings settings)
    {
        DefaultUser = settings.DefaultUser ?? string.Empty;
        UserOverrides = [];

        if (settings.UserOverrides != null)
        {
            foreach (KeyValuePair<string, string> userOverride in settings.UserOverrides)
            {
                UserOverrides.Add(new UserOverrideViewModel(userOverride.Key, userOverride.Value));
            }
        }

        InitCommands();
    }

#if DEBUG
    private SettingsViewModel(string defaultUser, ObservableCollection<UserOverrideViewModel> userOverrides)
    {
        DefaultUser = defaultUser;
        UserOverrides = userOverrides;

        InitCommands();
    }
#endif

    public ICommand AddOverrideCommand { get; set; }

    public string DefaultUser
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

    public ICommand DeleteOverridesCommand { get; set; }

    public ICommand SaveCommand { get; set; }

    public ObservableCollection<UserOverrideViewModel> SelectedOverrides { get; } = [];

    public ObservableCollection<UserOverrideViewModel> UserOverrides { get; }

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

    public event SettingsSaveEventHandler? Save;

    [MemberNotNull(nameof(AddOverrideCommand), nameof(DeleteOverridesCommand), nameof(SaveCommand))]
    private void InitCommands()
    {
        AddOverrideCommand = new AddOverrideCommandImpl(this);
        DeleteOverridesCommand = new DeleteOverridesCommandImpl(this);
        SaveCommand = new SaveCommandImpl(this);
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnSave()
    {
        var settings = new RemoteDesktopSettings
        {
            DefaultUser = DefaultUser,
            UserOverrides = UserOverrides.ToDictionary(keySelector: x => x.Regex, elementSelector: x => x.User),
        };

        Save?.Invoke(this, new SettingsSaveEventArgs(settings));
    }

    private class AddOverrideCommandImpl(SettingsViewModel viewModel) : ICommand
    {
        private readonly SettingsViewModel _viewModel = viewModel;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _viewModel.UserOverrides.Add(new UserOverrideViewModel(string.Empty, string.Empty));
        }

        public event EventHandler? CanExecuteChanged;
    }

    private class DeleteOverridesCommandImpl : ICommand
    {
        private readonly SettingsViewModel _viewModel;

        public DeleteOverridesCommandImpl(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;

            _viewModel.SelectedOverrides.CollectionChanged += (_, _) =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public bool CanExecute(object? parameter)
        {
            return _viewModel.SelectedOverrides.Count > 0;
        }

        public void Execute(object? parameter)
        {
            List<UserOverrideViewModel> selectedOverrides = _viewModel.SelectedOverrides.ToList();

            foreach (UserOverrideViewModel selected in selectedOverrides)
            {
                _viewModel.UserOverrides.Remove(selected);
            }
        }

        public event EventHandler? CanExecuteChanged;
    }

    private class SaveCommandImpl : ICommand
    {
        private readonly SettingsViewModel _viewModel;
        private bool _canExecute;

        public SaveCommandImpl(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;

            _viewModel.PropertyChanged += (_, _) =>
            {
                MarkChanged();
            };

            _viewModel.SelectedOverrides.CollectionChanged += (_, _) =>
            {
                MarkChanged();
            };
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute;
        }

        public void Execute(object? parameter)
        {
            _viewModel.OnSave();
        }

        public event EventHandler? CanExecuteChanged;

        private void MarkChanged()
        {
            _canExecute = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

#if DEBUG
    internal class Design() : SettingsViewModel(
        "User",
        [new UserOverrideViewModel("^[0-9]{3}.[0-9]{3}.[0-9]{3}.[0-9]{3}", "IpUser")]
    );
#endif
}