using System.Text.RegularExpressions;
using Flow.Launcher.Plugin.RemoteDesktop.Logging;
using Flow.Launcher.Plugin.RemoteDesktop.Settings;

namespace Flow.Launcher.Plugin.RemoteDesktop;

public class UsernameSelector(PluginInitContext context, RemoteDesktopSettings settings)
{
    private readonly ContextLogger<UsernameSelector> _logger = new(context);
    private readonly RemoteDesktopSettings _settings = settings;
    private HashSet<string> _cachedPatterns = [];
    private Dictionary<Regex, string> _userOverride = new();

    public string? GetUsername(string ipOrHostname)
    {
        if (_settings.UserOverrides == null)
        {
            _logger.LogDebug("No user overrides configured");

            return _settings.DefaultUser;
        }

        CachePatterns(_settings.UserOverrides);

        foreach ((Regex regex, string user) in _userOverride)
        {
            if (!regex.IsMatch(ipOrHostname))
            {
                continue;
            }

            _logger.LogDebug($"Matched user override for {ipOrHostname} to {user}");

            return user;
        }

        _logger.LogDebug($"No user override found for {ipOrHostname}");

        return _settings.DefaultUser;
    }

    private void CachePatterns(Dictionary<string, string> userOverrides)
    {
        if (_cachedPatterns.IsSubsetOf(userOverrides.Keys) && _cachedPatterns.IsSupersetOf(userOverrides.Keys))
        {
            return;
        }

        Dictionary<Regex, string> overrides = userOverrides.ToDictionary(
            keySelector: pair => new Regex(pair.Key),
            elementSelector: pair => pair.Value
        );

        _cachedPatterns = new HashSet<string>(userOverrides.Keys);
        _userOverride = overrides;
    }
}