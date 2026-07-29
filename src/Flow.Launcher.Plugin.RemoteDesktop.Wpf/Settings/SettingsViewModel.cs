using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Flow.Launcher.Plugin.RemoteDesktop.Events;
using Flow.Launcher.Plugin.RemoteDesktop.Services;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IDialogService _dialogService;

    public SettingsViewModel(RemoteDesktopSettings settings, IDialogService dialogService)
    {
        _dialogService = dialogService;
        DefaultUser = settings.DefaultUser ?? string.Empty;
        UserOverrides = [];
        Aliases = [];

        if (settings.UserOverrides != null)
        {
            foreach (KeyValuePair<string, string> userOverride in settings.UserOverrides)
            {
                UserOverrides.Add(new UserOverrideViewModel(userOverride.Key, userOverride.Value));
            }
        }

        if (settings.Aliases != null)
        {
            foreach (KeyValuePair<string, string> alias in settings.Aliases)
            {
                Aliases.Add(new AliasViewModel(alias.Key, alias.Value));
            }
        }

        InitCommands();
    }

#if DEBUG
    private SettingsViewModel(string defaultUser, ObservableCollection<UserOverrideViewModel> userOverrides, ObservableCollection<AliasViewModel> aliases)
    {
        _dialogService = new DialogService();
        DefaultUser = defaultUser;
        UserOverrides = userOverrides;
        Aliases = aliases;

        InitCommands();
    }
#endif

    public ICommand AddAliasCommand { get; set; }

    public ICommand AddOverrideCommand { get; set; }

    public ObservableCollection<AliasViewModel> Aliases { get; }

    public string DefaultUser
    {
        get;
        set
        {
            bool hasChanged = field != value;

            field = value;

            if (hasChanged)
            {
                OnPropertyChanged();
            }
        }
    }

    public ICommand DeleteAliasesCommand { get; set; }

    public ICommand DeleteOverridesCommand { get; set; }

    public ICommand EditAliasCommand { get; set; }

    public ICommand EditOverrideCommand { get; set; }

    public ICommand SaveCommand { get; set; }

    public ObservableCollection<AliasViewModel> SelectedAliases { get; } = [];

    public ObservableCollection<UserOverrideViewModel> SelectedOverrides { get; } = [];

    public ObservableCollection<UserOverrideViewModel> UserOverrides { get; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    public event SettingsSaveEventHandler? Save;

    [MemberNotNull(
        nameof(AddOverrideCommand),
        nameof(EditOverrideCommand),
        nameof(DeleteOverridesCommand),
        nameof(AddAliasCommand),
        nameof(EditAliasCommand),
        nameof(DeleteAliasesCommand),
        nameof(SaveCommand)
    )]
    private void InitCommands()
    {
        AddOverrideCommand = new AddOverrideCommandImpl(this);
        EditOverrideCommand = new EditOverrideCommandImpl(this);
        DeleteOverridesCommand = new DeleteOverridesCommandImpl(this);
        AddAliasCommand = new AddAliasCommandImpl(this);
        EditAliasCommand = new EditAliasCommandImpl(this);
        DeleteAliasesCommand = new DeleteAliasesCommandImpl(this);
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
            Aliases = Aliases.ToDictionary(keySelector: x => x.Alias, elementSelector: x => x.Host),
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
            var item = new UserOverrideViewModel(string.Empty, string.Empty);

            if (_viewModel._dialogService.Show(item))
            {
                _viewModel.UserOverrides.Add(item);
            }
        }

        public event EventHandler? CanExecuteChanged;
    }

    private class AddAliasCommandImpl(SettingsViewModel viewModel) : ICommand
    {
        private readonly SettingsViewModel _viewModel = viewModel;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            var item = new AliasViewModel(string.Empty, string.Empty);

            if (_viewModel._dialogService.Show(item))
            {
                _viewModel.Aliases.Add(item);
            }
        }

        public event EventHandler? CanExecuteChanged;
    }

    private class EditOverrideCommandImpl : ICommand
    {
        private readonly SettingsViewModel _viewModel;

        public EditOverrideCommandImpl(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;

            _viewModel.SelectedOverrides.CollectionChanged += (_, _) =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public bool CanExecute(object? parameter)
        {
            return _viewModel.SelectedOverrides.Count == 1;
        }

        public void Execute(object? parameter)
        {
            UserOverrideViewModel item = _viewModel.SelectedOverrides.First();
            var vm = new UserOverrideViewModel(item.Regex, item.User);

            if (!_viewModel._dialogService.Show(vm))
            {
                return;
            }

            item.Regex = vm.Regex;
            item.User = vm.User;
        }

        public event EventHandler? CanExecuteChanged;
    }

    private class EditAliasCommandImpl : ICommand
    {
        private readonly SettingsViewModel _viewModel;

        public EditAliasCommandImpl(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;

            _viewModel.SelectedAliases.CollectionChanged += (_, _) =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public bool CanExecute(object? parameter)
        {
            return _viewModel.SelectedAliases.Count == 1;
        }

        public void Execute(object? parameter)
        {
            AliasViewModel item = _viewModel.SelectedAliases.First();
            var vm = new AliasViewModel(item.Alias, item.Host);

            if (!_viewModel._dialogService.Show(vm))
            {
                return;
            }

            item.Alias = vm.Alias;
            item.Host = vm.Host;
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

    private class DeleteAliasesCommandImpl : ICommand
    {
        private readonly SettingsViewModel _viewModel;

        public DeleteAliasesCommandImpl(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;

            _viewModel.SelectedAliases.CollectionChanged += (_, _) =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public bool CanExecute(object? parameter)
        {
            return _viewModel.SelectedAliases.Count > 0;
        }

        public void Execute(object? parameter)
        {
            List<AliasViewModel> selected = _viewModel.SelectedAliases.ToList();

            foreach (AliasViewModel item in selected)
            {
                _viewModel.Aliases.Remove(item);
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

            _viewModel.PropertyChanged += MarkChanged;

            _viewModel.SelectedOverrides.CollectionChanged += (_, args) =>
            {
                IList empty = new List<object>();

                foreach (object oldItem in args.OldItems ?? empty)
                {
                    if (oldItem is UserOverrideViewModel userOverride)
                    {
                        userOverride.PropertyChanged -= MarkChanged;
                    }
                }

                foreach (object newItem in args.NewItems ?? empty)
                {
                    if (newItem is UserOverrideViewModel userOverride)
                    {
                        userOverride.PropertyChanged += MarkChanged;
                    }
                }

                MarkChanged();
            };

            foreach (UserOverrideViewModel userOverride in _viewModel.UserOverrides)
            {
                userOverride.PropertyChanged += MarkChanged;
            }

            _viewModel.Aliases.CollectionChanged += (_, args) =>
            {
                IList empty = new List<object>();

                foreach (object oldItem in args.OldItems ?? empty)
                {
                    if (oldItem is AliasViewModel alias)
                    {
                        alias.PropertyChanged -= MarkChanged;
                    }
                }

                foreach (object newItem in args.NewItems ?? empty)
                {
                    if (newItem is AliasViewModel alias)
                    {
                        alias.PropertyChanged += MarkChanged;
                    }
                }

                MarkChanged();
            };

            foreach (AliasViewModel alias in _viewModel.Aliases)
            {
                alias.PropertyChanged += MarkChanged;
            }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute;
        }

        public void Execute(object? parameter)
        {
            _viewModel.OnSave();
            SetCanExecute(false);
        }

        public event EventHandler? CanExecuteChanged;

        private void MarkChanged(object? sender = null, object? args = null)
        {
            SetCanExecute(true);
        }

        private void SetCanExecute(bool value)
        {
            if (_canExecute == value)
            {
                return;
            }

            _canExecute = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

#if DEBUG
    internal class Design() : SettingsViewModel(
        "User",
        [new UserOverrideViewModel("^[0-9]{3}.[0-9]{3}.[0-9]{3}.[0-9]{3}", "IpUser")],
        [new AliasViewModel("Alias", "192.168.0.1")]
    );
#endif
}