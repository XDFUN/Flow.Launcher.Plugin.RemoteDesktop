using System;
using System.IO;

namespace Flow.Launcher.Plugin.RemoteDesktop;

/// <summary>
///     The settings for <see cref="RemoteDesktop" />.
/// </summary>
public class RemoteDesktopSettings
{
    /// <summary>
    ///     The path to the mstsc.exe executable.
    /// </summary>
    public string MstscPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "mstsc.exe"
    );

    /// <summary>
    ///     The max additional score that is added to the fuzzy search score, depending on how recent the connection is.
    /// </summary>
    public double MaxRecentScore { get; set; } = 20.0;

    /// <summary>
    ///     The key to the user registry where the recent connections are stored.
    /// </summary>
    /// <remarks>
    ///     The recent connections are stored as key-value pairs.
    /// </remarks>
    public string RecentConnectionsKey { get; set; } = @"Software\Microsoft\Terminal Server Client\Default";

    /// <summary>
    ///     The key to the user registry where the connection history is stored.
    /// </summary>
    /// <remarks>
    ///     The connection history is the list of subfolders in the registry key.
    /// </remarks>
    public string ConnectionHistoryKey { get; set; } = @"Software\Microsoft\Terminal Server Client\Servers";
}