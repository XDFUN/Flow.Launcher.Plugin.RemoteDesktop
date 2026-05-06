using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.RemoteDesktop;

internal class RegistryManager(PluginInitContext context)
{
    private const string SERVER_KEY = @"Software\Microsoft\Terminal Server Client\Servers";
    private const string USERNAME_HINT_KEY = "UsernameHint";

    private readonly ContextLogger<RegistryManager> _logger = new(context);

    public void CreateServerHint(string ipOrHostname, string? username)
    {
        if (!TryOpenServerKey(out RegistryKey? historyKey))
        {
            return;
        }

        using RegistryKey _ = historyKey;

        if (historyKey.GetValue(ipOrHostname) != null)
        {
            _logger.LogDebug($"Server hint already exists for {ipOrHostname}");

            return;
        }

        using RegistryKey serverKey = historyKey.CreateSubKey(ipOrHostname);

        if (!string.IsNullOrWhiteSpace(username))
        {
            serverKey.SetValue(USERNAME_HINT_KEY, username);
        }
    }

    public bool TryGetUserHint(string ipOrHostname, [NotNullWhen(true)] out string? usernameHint)
    {
        usernameHint = null;

        if (!TryOpenServerKey(out RegistryKey? historyKey))
        {
            return false;
        }

        using RegistryKey _ = historyKey;

        RegistryKey? serverKey = historyKey.OpenSubKey(ipOrHostname);

        if (serverKey == null)
        {
            _logger.LogDebug($"Server hint does not exist for {ipOrHostname}");

            return false;
        }

        usernameHint = serverKey.GetValue(USERNAME_HINT_KEY)?.ToString();

        return usernameHint != null;
    }

    public string[] GetConnectionHistory()
    {
        if (!TryOpenServerKey(out RegistryKey? historyKey))
        {
            return [];
        }

        using RegistryKey _ = historyKey;

        return historyKey.GetSubKeyNames();
    }

    public Dictionary<string, double> GetRecentConnection()
    {
        using RegistryKey? recentlyUsed = OpenRegistryKey(@"Software\Microsoft\Terminal Server Client\Default");

        var result = new Dictionary<string, double>();

        if (recentlyUsed == null)
        {
            _logger.LogDebug("Failed to open registry key for recent connections");

            return result;
        }

        foreach (string keyName in recentlyUsed.GetValueNames())
        {
            object? value = recentlyUsed.GetValue(keyName);

            if (value is not string ipOrHostname)
            {
                _logger.LogDebug($"Failed to get value for key {keyName}");

                continue;
            }

            // MRU<index>
            if (double.TryParse(keyName[3..], out double weight))
            {
                result[ipOrHostname] = weight;
            }
            else
            {
                _logger.LogDebug($"Failed to parse weight for key {keyName}");
            }
        }

        return result;
    }

    private bool TryOpenServerKey([NotNullWhen(true)] out RegistryKey? historyKey)
    {
        historyKey = OpenRegistryKey(SERVER_KEY);

        if (historyKey != null)
        {
            return true;
        }

        _logger.LogDebug("Failed to open registry key for recent connections");

        return false;
    }

    private RegistryKey? OpenRegistryKey(string keyPath)
    {
        try
        {
            return Registry.CurrentUser.OpenSubKey(keyPath, true);
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to open registry key {keyPath}", e);
        }

        return null;
    }
}