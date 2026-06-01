using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public partial class UserOverrideWindow
{
    public UserOverrideWindow()
    {
        InitializeComponent();

        if (FindResource("Localization") is not LocalizationManager localizationManager)
        {
            return;
        }

        var titleBinding = new Binding
        {
            Source = localizationManager,
            Path = new PropertyPath(nameof(localizationManager.UserOverrideDialogTitle)),
        };

        SetBinding(TitleProperty, titleBinding);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}