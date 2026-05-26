using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Flow.Launcher.Plugin.RemoteDesktop.Resources;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

// ReSharper disable MemberCanBeMadeStatic.Global
// Justification: Used in Binding
[SuppressMessage("Performance", "CA1822", Justification = "Used in Binding")]
internal class LocalizationManager : INotifyPropertyChanged, IDisposable
{
    public LocalizationManager()
    {
        GuiCultureProvider.CultureChanged += OnCultureChanged;
    }

    ~LocalizationManager()
    {
        GuiCultureProvider.CultureChanged -= OnCultureChanged;
    }

    public string Add => Localization.Add;

    public string Delete => Localization.Delete;

    public string RegexExample => Localization.RegexExample;

    public string RegexHeader => Localization.RegexHeader;

    public string Save => Localization.Save;

    public string UserExample => Localization.UserExample;

    public string UserHeader => Localization.UserHeader;

    public void Dispose()
    {
        GuiCultureProvider.CultureChanged -= OnCultureChanged;

        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(CultureInfo culture)
    {
        Localization.Culture = culture;
        OnPropertyChanged(string.Empty);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}