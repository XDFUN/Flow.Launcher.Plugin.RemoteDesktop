namespace Flow.Launcher.Plugin.RemoteDesktop.Services;

public interface IDialogService
{
    public bool Show<T>(T viewModel);
}