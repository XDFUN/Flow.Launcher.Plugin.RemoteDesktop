using System.Windows;
using System.Windows.Data;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public partial class AliasWindow
{
    public AliasWindow()
    {
        InitializeComponent();

        if (FindResource("Localization") is not LocalizationManager localizationManager)
        {
            return;
        }

        var titleBinding = new Binding
        {
            Source = localizationManager,
            Path = new PropertyPath(nameof(localizationManager.AliasDialogTitle)),
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
