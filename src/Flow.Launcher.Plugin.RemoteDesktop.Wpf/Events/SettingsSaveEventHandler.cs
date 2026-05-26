using Flow.Launcher.Plugin.RemoteDesktop.Settings;

namespace Flow.Launcher.Plugin.RemoteDesktop.Events;

public class SettingsSaveEventArgs(RemoteDesktopSettings settings) : EventArgs
{
    public RemoteDesktopSettings Settings { get; } = settings;
}

public delegate void SettingsSaveEventHandler(object? sender, SettingsSaveEventArgs e);