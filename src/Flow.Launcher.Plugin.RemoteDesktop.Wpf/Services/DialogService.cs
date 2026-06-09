using System.Windows;
using Flow.Launcher.Plugin.RemoteDesktop.Settings;

namespace Flow.Launcher.Plugin.RemoteDesktop.Services;

public class DialogService : IDialogService
{
    private readonly Dictionary<Type, Func<Window>> _windowFactories = new();

    public DialogService()
    {
        _windowFactories[typeof(UserOverrideViewModel)] = () => new UserOverrideWindow();
    }

    public bool Show<T>(T viewModel)
    {
        Type type = typeof(T);

        if (!_windowFactories.TryGetValue(type, out Func<Window>? windowFactory))
        {
            return false;
        }

        Window window = windowFactory();
        window.DataContext = viewModel;

        return window.ShowDialog() == true;
    }
}