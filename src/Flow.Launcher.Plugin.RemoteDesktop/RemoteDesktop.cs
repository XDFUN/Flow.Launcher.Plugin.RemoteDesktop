using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Flow.Launcher.Plugin.RemoteDesktop.Settings;
using Flow.Launcher.Plugin.SharedModels;

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

    private RegistryManager RegistryManager
    {
        get => field ?? throw new InvalidOperationException("RegistryManager not initialized");
        set;
    }

    private UsernameSelector UsernameSelector
    {
        get => field ?? throw new InvalidOperationException("UsernameSelector not initialized");
        set;
    }

    /// <summary>
    ///     Initializes the plugin.
    /// </summary>
    /// <param name="context"></param>
    public void Init(PluginInitContext context)
    {
        _context = context;
        _logger = new ContextLogger<RemoteDesktop>(context);
        Localization = new Localization(context.API);
        Settings = _context.API.LoadSettingJsonStorage<RemoteDesktopSettings>();
        RegistryManager = new RegistryManager(context);
        UsernameSelector = new UsernameSelector(context, Settings);
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

        string search = query?.Search ?? string.Empty;
        search = search.Trim();

        QueryCore(search, results);
        QueryPostfix(search, results);

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

    private void QueryCore(string search, List<string> results)
    {
        if (_context == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(search))
        {
            _logger?.LogDebug("Query executed with empty search term");

            results.AddRange(SearchRecent(search));

            return;
        }

        Dictionary<string, double> recentConnections = RegistryManager.GetRecentConnection();

        string[] connectionHistory = RegistryManager.GetConnectionHistory();

        if (connectionHistory.Length == 0)
        {
            results.AddRange(SearchRecent(search));

            return;
        }

        results.AddRange(
            ScoreConnections(search, connectionHistory, recentConnections).Select(matchResult => matchResult.Connection)
        );
    }

    private void QueryPostfix(string search, List<string> results)
    {
        List<string> others = results.FindAll(x => x.Equals(search, StringComparison.OrdinalIgnoreCase));

        if (others.Count > 0)
        {
            results.RemoveAll(others.Contains);
            results.Insert(0, others[0]);

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
            return RegistryManager.GetRecentConnection()
                                  .Where(x => _context?.API.FuzzySearch(search, x.Key).Success ?? true)
                                  .OrderBy(x => x.Value)
                                  .Select(x => x.Key)
                                  .ToList();
        }

        _logger?.LogDebug("Query executed with empty search term");

        return RegistryManager.GetRecentConnection().OrderBy(x => x.Value).Select(x => x.Key).ToList();
    }

    private Result GetResult(string ipOrHostname)
    {
        string? user = GetDefaultUser(ipOrHostname);
        string title = ipOrHostname;

        if (!string.IsNullOrWhiteSpace(user))
        {
            title += $" ({user})";
        }

        return new Result
        {
            Title = title,
            AutoCompleteText = ipOrHostname,
            SubTitle = Localization.ResultSubtitle,
            IcoPath = ICO_PATH,
            Action = _ =>
            {
                _logger?.LogDebug($"Opening connection to {ipOrHostname}");
                RegistryManager.CreateServerHint(ipOrHostname, user);

                var processInfo = new ProcessStartInfo
                {
                    FileName = Settings.MstscPath,
                    Arguments = $"/v:{ipOrHostname}",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                };

                var rdcProcess = new Process
                {
                    StartInfo = processInfo,
                };

                rdcProcess.Start();

                return true;
            },
        };
    }

    private string? GetDefaultUser(string ipOrHostname)
    {
        return RegistryManager.TryGetUserHint(ipOrHostname, out string? usernameHint)
            ? usernameHint
            : UsernameSelector.GetUsername(ipOrHostname);
    }

    private class ScoredConnection
    {
        public required string Connection { get; set; }

        public int FuzzyScore { get; set; }

        public double RecencyBonus { get; set; }

        public double TotalScore { get; set; }
    }
}