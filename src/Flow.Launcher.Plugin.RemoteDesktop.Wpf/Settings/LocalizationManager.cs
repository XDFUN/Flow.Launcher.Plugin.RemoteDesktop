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

    public string AddAlias => Localization.AddAlias;

    public string AddOverride => Localization.AddOverride;

    public string AliasDialogGuide => Localization.AliasDialogGuide;

    public string AliasDialogTitle => Localization.AliasDialogTitle;

    public string AliasExample => Localization.AliasExample;

    public string AliasFieldLabel => Localization.AliasFieldLabel;

    public string AliasHeader => Localization.AliasHeader;

    public string DefaultUserFieldLabel => Localization.DefaultUserFieldLabel;

    public string DefaultUserHint => Localization.DefaultUserHint;

    public string DeleteAlias => Localization.DeleteAlias;

    public string DeleteOverride => Localization.DeleteOverride;

    public string EditAlias => Localization.EditAlias;

    public string EditOverride => Localization.EditOverride;

    public string HostExample => Localization.HostExample;

    public string HostFieldLabel => Localization.HostFieldLabel;

    public string HostHeader => Localization.HostHeader;

    public string RegexExample => Localization.RegexExample;

    public string RegexFieldLabel => Localization.RegexFieldLabel;

    public string RegexHeader => Localization.RegexHeader;

    public string Save => Localization.Save;

    public string SystemUserHint => Localization.SystemUserHint;

    public string UserExample => Localization.UserExample;

    public string UserFieldLabel => Localization.UserFieldLabel;

    public string UserHeader => Localization.UserHeader;

    public string UserOverrideDialogGuide => Localization.UserOverrideDialogGuide;

    public string UserOverrideDialogTitle => Localization.UserOverrideDialogTitle;

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