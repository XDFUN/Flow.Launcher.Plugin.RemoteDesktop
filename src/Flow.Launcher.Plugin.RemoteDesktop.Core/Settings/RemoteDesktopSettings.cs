namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

public class RemoteDesktopSettings
{
    /// <summary>
    ///     Aliases for hosts. Key = alias shown in search, Value = actual host (dns or ip).
    /// </summary>
    public Dictionary<string, string>? Aliases { get; set; }

    /// <summary>
    ///     The default user for the remote desktop connection.
    /// </summary>
    public string? DefaultUser { get; set; }

    /// <summary>
    ///     The max additional score that is added to the fuzzy search score, depending on how recent the connection is.
    /// </summary>
    public double MaxRecentScore { get; set; } = 20.0;

    /// <summary>
    ///     The path to the mstsc.exe executable.
    /// </summary>
    public string MstscPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "mstsc.exe"
    );

    /// <summary>
    ///     User overrides for specific host names or ip addresses.
    /// </summary>
    public Dictionary<string, string>? UserOverrides { get; set; }
}