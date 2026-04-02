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
public class RemoteDesktop : IPlugin
{
    private const string RECENT_CONNECTIONS = @"Software\Microsoft\Terminal Server Client\Default";
    private const string CONNECTION_HISTORY = @"Software\Microsoft\Terminal Server Client\Servers";
    private const string ICO_PATH = "Images/icon.png";
    private const double MAX_RECENT_SCORE = 20.0;

    private PluginInitContext? _context;
    private ContextLogger<RemoteDesktop>? _logger;
    private string? _mstscPath;

    /// <summary>
    ///     Initializes the plugin.
    /// </summary>
    /// <param name="context"></param>
    public void Init(PluginInitContext context)
    {
        // mstsc
        _context = context;
        _logger = new ContextLogger<RemoteDesktop>(context);

        string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
        _mstscPath = Path.Combine(systemDir, "mstsc.exe");
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

        var results = new List<Result>();
        string search = query?.Search ?? string.Empty;

        if (string.IsNullOrWhiteSpace(search))
        {
            _logger?.LogDebug("Query executed with empty search term");

            return SearchRecent(search);
        }

        Dictionary<string, double> recentConnections = GetRecentConnection();

        string[] connectionHistory = GetConnectionHistory();

        if (connectionHistory.Length == 0)
        {
            return SearchRecent(search);
        }

        results.AddRange(
            ScoreConnections(search, connectionHistory, recentConnections)
                .Select(matchResult => GetResult(matchResult.Connection))
        );

        if (!string.IsNullOrWhiteSpace(search))
        {
            results.Add(GetResult(search));
        }

        return results;
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
                recencyBonus = MAX_RECENT_SCORE - (weight * (MAX_RECENT_SCORE / Math.Max(totalRecents, 1)));
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

    private List<Result> SearchRecent(string search)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            return GetRecentConnection()
                   .Where(x => _context?.API.FuzzySearch(search, x.Key).Success ?? true)
                   .OrderBy(x => x.Value)
                   .Select(x => GetResult(x.Key))
                   .ToList();
        }

        _logger?.LogDebug("Query executed with empty search term");

        return GetRecentConnection().OrderBy(x => x.Value).Select(x => GetResult(x.Key)).ToList();
    }

    private Dictionary<string, double> GetRecentConnection()
    {
        using RegistryKey? recentlyUsed = Registry.CurrentUser.OpenSubKey(RECENT_CONNECTIONS);

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
        using RegistryKey? historyKey = Registry.CurrentUser.OpenSubKey(CONNECTION_HISTORY);

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
            SubTitle = "Connect with Remote Desktop",
            IcoPath = ICO_PATH,
            Action = _ =>
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = _mstscPath,
                        Arguments = $"/v:{ipOrHostname}",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                );

                return true;
            },
        };
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