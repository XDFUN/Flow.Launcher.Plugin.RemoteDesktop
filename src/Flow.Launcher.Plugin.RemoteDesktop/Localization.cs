namespace Flow.Launcher.Plugin.RemoteDesktop;

/// <summary>
/// Provides the localization for the plugin.
/// </summary>
/// <param name="api"></param>
public class Localization(IPublicAPI api)
{
    private const string PREFIX = "flow_launcher.plugin.remote_desktop.";

    private readonly IPublicAPI _api = api;

    /// <summary>
    /// Gets the localized name of the plugin.
    /// </summary>
    /// <remarks>
    /// This property retrieves the translation for the plugin's name using the specified key
    /// and provides a fallback value ("Remote Desktop") if no translation is found or available.
    /// </remarks>
    public string PluginName => GetTranslation("plugin_name", "Remote Desktop");

    /// <summary>
    /// Gets the localized description of the plugin.
    /// </summary>
    /// <remarks>
    /// This property retrieves the translation for the plugin's description using the specified key
    /// and provides a fallback value ("Open Remote Desktop connections from Flow.Launcher")
    /// if no translation is found or available.
    /// </remarks>
    public string PluginDescription => GetTranslation("plugin_description", "Open Remote Desktop connections from Flow.Launcher");

    /// <summary>
    /// Gets the subtitle text displayed for a result item.
    /// </summary>
    /// <remarks>
    /// This property retrieves the localized subtitle for a result item using a predefined
    /// translation key. It provides a default subtitle ("Connect with Remote Desktop")
    /// if no translation is available.
    /// </remarks>
    public string ResultSubtitle => GetTranslation("result.subtitle", "Connect with Remote Desktop");

    private string GetTranslation(string key, string fallback)
    {
        string fullKey = PREFIX + key;
        string? translation = _api.GetTranslation(fullKey);

        // See https://github.com/Flow-Launcher/Flow.Launcher/blob/d0d41c65fc0fae7366ded6b538d5c0129c6523ba/Flow.Launcher.Core/Resource/Internationalization.cs#L353
        if (translation.Contains(fullKey))
        {
            return fallback;
        }

        return string.IsNullOrEmpty(translation) ? fallback : translation;
    }
}