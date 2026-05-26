using System.Globalization;

namespace Flow.Launcher.Plugin.RemoteDesktop.Resources;

public static class GuiCultureProvider
{
    public static void ChangeCulture(CultureInfo culture)
    {
        CultureChanged?.Invoke(culture);
    }

    internal static event GuiCultureChangedEventHandler? CultureChanged;
}