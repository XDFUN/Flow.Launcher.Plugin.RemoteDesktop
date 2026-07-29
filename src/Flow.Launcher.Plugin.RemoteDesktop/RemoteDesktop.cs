using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Flow.Launcher.Plugin.RemoteDesktop.Logging;
using Flow.Launcher.Plugin.RemoteDesktop.Resources;
using Flow.Launcher.Plugin.RemoteDesktop.Services;
using Flow.Launcher.Plugin.RemoteDesktop.Settings;
using Flow.Launcher.Plugin.SharedModels;
using Localization = Flow.Launcher.Plugin.RemoteDesktop.Resources.Localization;

namespace Flow.Launcher.Plugin.RemoteDesktop;

/// <summary>
///     A plugin for Flow.Launcher to open RDP connections.
/// </summary>
public class RemoteDesktop : IPlugin, IPluginI18n, ISettingProvider
{
    private const string ICO_PATH = "Images/icon.png";
    private readonly Lazy<SettingsControl> _settingsControl;

    private ContextLogger<RemoteDesktop>? _logger;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public RemoteDesktop()
    {
        _settingsControl = new Lazy<SettingsControl>(CreateSettingsControl);
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private PluginInitContext Context
    {
        get => field ?? throw new InvalidOperationException("Context not initialized");
        set;
    }

    private RegistryManager RegistryManager
    {
        get => field ?? throw new InvalidOperationException("RegistryManager not initialized");
        set;
    }

    private RemoteDesktopSettings Settings
    {
        get => field ?? throw new InvalidOperationException("Settings not initialized");
        set;
    }

    private UsernameSelector UsernameSelector
    {
        get => field ?? throw new InvalidOperationException("UsernameSelector not initialized");
        set;
    }

    /// <inheritdoc />
    public void Init(PluginInitContext context)
    {
        Context = context;
        _logger = new ContextLogger<RemoteDesktop>(context);
        Settings = Context.API.LoadSettingJsonStorage<RemoteDesktopSettings>();
        RegistryManager = new RegistryManager(context);
        UsernameSelector = new UsernameSelector(context, Settings);
    }

    /// <inheritdoc />
    public List<Result> Query(Query? query)
    {
        if (!File.Exists(Settings.MstscPath))
        {
            _logger?.LogWarn("mstsc.exe not found");

            Context.API.ShowMsgError(
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

    /// <inheritdoc />
    public void OnCultureInfoChanged(CultureInfo newCulture)
    {
        Localization.Culture = newCulture;
        GuiCultureProvider.ChangeCulture(newCulture);
    }

    /// <inheritdoc />
    public string GetTranslatedPluginTitle()
    {
        return Localization.PluginName;
    }

    /// <inheritdoc />
    public string GetTranslatedPluginDescription()
    {
        return Localization.PluginDescription;
    }

    /// <inheritdoc />
    public Control CreateSettingPanel()
    {
        return _settingsControl.Value;
    }

    private SettingsControl CreateSettingsControl()
    {
        _logger?.LogDebug("Creating settings panel");

        var vm = new SettingsViewModel(Settings, new DialogService());

        vm.Save += (_, args) =>
        {
            Settings.DefaultUser = args.Settings.DefaultUser;
            Settings.UserOverrides = args.Settings.UserOverrides;
            Settings.Aliases = args.Settings.Aliases;

            Context.API.SaveSettingJsonStorage<RemoteDesktopSettings>();
        };

        return new SettingsControl
        {
            DataContext = vm,
        };
    }

    private string? GetDefaultUser(string ipOrHostname)
    {
        return RegistryManager.TryGetUserHint(ipOrHostname, out string? usernameHint)
            ? usernameHint
            : UsernameSelector.GetUsername(ipOrHostname);
    }

    private Result GetResult(string displayName)
    {
        // Resolve alias → actual host if needed
        string actualHost = ResolveAlias(displayName);

        string? user = GetDefaultUser(actualHost);
        string title = displayName;

        if (!string.IsNullOrWhiteSpace(user))
        {
            title += $" ({user})";
        }

        return new Result
        {
            Title = title,
            AutoCompleteText = displayName,
            SubTitle = displayName.Equals(actualHost, StringComparison.OrdinalIgnoreCase)
                ? Localization.ResultSubtitle
                : string.Format(Localization.ResultSubtitleWithHost, actualHost),
            IcoPath = ICO_PATH,
            Action = _ =>
            {
                _logger?.LogDebug($"Opening connection to {actualHost} (requested '{displayName}')");
                RegistryManager.CreateServerHint(actualHost, user);

                var processInfo = new ProcessStartInfo
                {
                    FileName = Settings.MstscPath,
                    Arguments = $"/v:{actualHost}",
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

    private void QueryCore(string search, List<string> results)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            _logger?.LogDebug("Query executed with empty search term");

            results.AddRange(SearchRecent(search));

            return;
        }

        Dictionary<string, double> recentConnections = RegistryManager.GetRecentConnection();

        string[] connectionHistory = RegistryManager.GetConnectionHistory();
        Dictionary<string, string> aliases = Settings.Aliases ?? new Dictionary<string, string>();

        if (connectionHistory.Length == 0)
        {
            results.AddRange(SearchRecent(search));

            return;
        }

        string[] candidates = connectionHistory.Concat(aliases.Keys)
                                               .Distinct(StringComparer.OrdinalIgnoreCase)
                                               .ToArray();

        results.AddRange(
            ScoreConnections(search, candidates, recentConnections, aliases)
                .Select(matchResult => matchResult.Connection)
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

    private string ResolveAlias(string displayName)
    {
        if (Settings.Aliases != null && Settings.Aliases.TryGetValue(displayName, out string? host))
        {
            return host;
        }

        return displayName;
    }

    private List<ScoredConnection> ScoreConnections(
        string search,
        string[] candidates,
        Dictionary<string, double> recents,
        Dictionary<string, string> aliases
    )
    {
        var scoredConnections = new List<ScoredConnection>();
        int totalRecents = recents.Count;

        foreach (string connection in candidates)
        {
            MatchResult? match = Context.API.FuzzySearch(search, connection);

            if (!match.Success)
            {
                continue;
            }

            double recencyBonus = 0;

            string actualHost = aliases.TryGetValue(connection, out string? host) ? host : connection;

            if (recents.TryGetValue(actualHost, out double weight))
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
                                  .Where(x => Context.API.FuzzySearch(search, x.Key).Success)
                                  .OrderBy(x => x.Value)
                                  .Select(x => x.Key)
                                  .ToList();
        }

        _logger?.LogDebug("Query executed with empty search term");

        return RegistryManager.GetRecentConnection().OrderBy(x => x.Value).Select(x => x.Key).ToList();
    }

    private class ScoredConnection
    {
        public required string Connection { get; set; }

        public int FuzzyScore { get; set; }

        public double RecencyBonus { get; set; }

        public double TotalScore { get; set; }
    }
}