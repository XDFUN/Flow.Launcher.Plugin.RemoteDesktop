using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Flow.Launcher.Plugin.SharedModels;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.RemoteDesktop;

/// <summary>
///     A plugin for Flow.Launcher to open RDP connections.
/// </summary>
public class RemoteDesktop : IPlugin, IPluginI18n
{
    private const string ICO_PATH = "Images/icon.png";

    private PluginInitContext? _context;
    private ContextLogger<RemoteDesktop>? _logger;

    private RemoteDesktopSettings Settings
    {
        get => field ?? throw new InvalidOperationException("Settings not initialized");
        set;
    }

    private Localization Localization
    {
        get => field ?? throw new InvalidOperationException("Localization not initialized");
        set;
    }

    /// <summary>
    ///     Initializes the plugin.
    /// </summary>
    /// <param name="context"></param>
    public void Init(PluginInitContext context)
    {
        _context = context;
        Localization = new Localization(context.API);
        Settings = new RemoteDesktopSettings(); //_context.API.LoadSettingJsonStorage<RemoteDesktopSettings>();
        _logger = new ContextLogger<RemoteDesktop>(context);
    }

    /// <summary>
    ///     Queries the user registry for key Software\Microsoft\Terminal Server Client\Servers
    /// </summary>
    public List<Result> Query(Query? query)
    {
        if (_context == null)
        {
            return [];
        }

        if (!File.Exists(Settings.MstscPath))
        {
            _logger?.LogWarn("mstsc.exe not found");

            _context.API.ShowMsgError(
                "mstsc.exe not found",
                "Please ensure that mstsc.exe is installed and located at " + Settings.MstscPath
            );

            return [];
        }

        var results = new List<string>();

        QueryCore(query, results);
        QueryPostfix(query, results);

        return results.Select(GetResult).ToList();
    }

    /// <summary>
    ///     Retrieves the translated title of the plugin.
    /// </summary>
    /// <returns>The localized plugin title.</returns>
    public string GetTranslatedPluginTitle()
    {
        return Localization.PluginName;
    }

    /// <summary>
    ///     Retrieves the translated description of the plugin.
    /// </summary>
    /// <returns>The localized plugin description.</returns>
    public string GetTranslatedPluginDescription()
    {
        return Localization.PluginDescription;
    }

    private void QueryCore(Query? query, List<string> results)
    {
        if (_context == null)
        {
            return;
        }

        string search = query?.Search ?? string.Empty;

        if (string.IsNullOrWhiteSpace(search))
        {
            _logger?.LogDebug("Query executed with empty search term");

            results.AddRange(SearchRecent(search));

            return;
        }

        Dictionary<string, double> recentConnections = GetRecentConnection();

        string[] connectionHistory = GetConnectionHistory();

        if (connectionHistory.Length == 0)
        {
            results.AddRange(SearchRecent(search));

            return;
        }

        results.AddRange(
            ScoreConnections(search, connectionHistory, recentConnections).Select(matchResult => matchResult.Connection)
        );
    }

    private void QueryPostfix(Query? query, List<string> results)
    {
        string search = query?.Search ?? string.Empty;

        if (results.Contains(search))
        {
            results.Remove(search);
            results.Insert(0, search);

            return;
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            results.Add(search);
        }
    }

    private List<ScoredConnection> ScoreConnections(
        string search,
        string[] connectionHistory,
        Dictionary<string, double> recents
    )
    {
        if (_context == null)
        {
            return [];
        }

        var scoredConnections = new List<ScoredConnection>();
        int totalRecents = recents.Count;

        foreach (string connection in connectionHistory)
        {
            MatchResult? match = _context.API.FuzzySearch(search, connection);

            if (!match.Success)
            {
                continue;
            }

            double recencyBonus = 0;

            if (recents.TryGetValue(connection, out double weight))
            {
                recencyBonus = Settings.MaxRecentScore
                               - (weight * (Settings.MaxRecentScore / Math.Max(totalRecents, 1)));

                recencyBonus = Math.Max(0, recencyBonus);
            }

            double totalScore = match.Score + recencyBonus;

            scoredConnections.Add(
                new ScoredConnection
                {
                    Connection = connection,
                    FuzzyScore = match.Score,
                    RecencyBonus = recencyBonus,
                    TotalScore = totalScore,
                }
            );
        }

        return scoredConnections.OrderByDescending(c => c.TotalScore).ToList();
    }

    private List<string> SearchRecent(string search)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            return GetRecentConnection()
                   .Where(x => _context?.API.FuzzySearch(search, x.Key).Success ?? true)
                   .OrderBy(x => x.Value)
                   .Select(x => x.Key)
                   .ToList();
        }

        _logger?.LogDebug("Query executed with empty search term");

        return GetRecentConnection().OrderBy(x => x.Value).Select(x => x.Key).ToList();
    }

    private Dictionary<string, double> GetRecentConnection()
    {
        using RegistryKey? recentlyUsed = OpenRegistryKey(Settings.RecentConnectionsKey);

        var result = new Dictionary<string, double>();

        if (recentlyUsed == null)
        {
            _logger?.LogDebug("Failed to open registry key for recent connections");

            return result;
        }

        foreach (string keyName in recentlyUsed.GetValueNames())
        {
            object? value = recentlyUsed.GetValue(keyName);

            if (value is not string ipOrHostname)
            {
                _logger?.LogDebug($"Failed to get value for key {keyName}");

                continue;
            }

            // MRU<index>
            if (double.TryParse(keyName[3..], out double weight))
            {
                result[ipOrHostname] = weight;
            }
            else
            {
                _logger?.LogDebug($"Failed to parse weight for key {keyName}");
            }
        }

        return result;
    }

    private string[] GetConnectionHistory()
    {
        using RegistryKey? historyKey = OpenRegistryKey(Settings.ConnectionHistoryKey);

        if (historyKey != null)
        {
            return historyKey.GetSubKeyNames();
        }

        _logger?.LogDebug("Failed to open registry key for recent connections");

        return [];
    }

    private Result GetResult(string ipOrHostname)
    {
        return new Result
        {
            Title = ipOrHostname,
            AutoCompleteText = ipOrHostname,
            SubTitle = Localization.ResultSubtitle,
            IcoPath = ICO_PATH,
            Action = _ =>
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = Settings.MstscPath,
                        Arguments = $"/v:{ipOrHostname}",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                );

                return true;
            },
        };
    }

    private RegistryKey? OpenRegistryKey(string keyPath)
    {
        try
        {
            return Registry.CurrentUser.OpenSubKey(keyPath, true);
        }
        catch (Exception e)
        {
            _logger?.LogError($"Failed to open registry key {keyPath}", e);
        }

        return null;
    }

    private class ContextLogger<T>(PluginInitContext context)
    {
        private static readonly string s_className = typeof(T).Name;
        private readonly PluginInitContext _context = context;

        public void LogDebug(string message, [CallerMemberName] string methodName = "")
        {
            _context.API.LogDebug(s_className, message, methodName);
        }

        public void LogInfo(string message, [CallerMemberName] string methodName = "")
        {
            _context.API.LogInfo(s_className, message, methodName);
        }

        public void LogWarn(string message, [CallerMemberName] string methodName = "")
        {
            _context.API.LogWarn(s_className, message, methodName);
        }

        public void LogError(string message, Exception exception, [CallerMemberName] string methodName = "")
        {
            _context.API.LogException(s_className, message, exception, methodName);
        }
    }

    private class ScoredConnection
    {
        public required string Connection { get; set; }

        public int FuzzyScore { get; set; }

        public double RecencyBonus { get; set; }

        public double TotalScore { get; set; }
    }
}