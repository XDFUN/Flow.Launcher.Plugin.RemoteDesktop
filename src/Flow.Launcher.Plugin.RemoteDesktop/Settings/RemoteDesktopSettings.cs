using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.RemoteDesktop.Settings;

/// <summary>
///     The settings for <see cref="RemoteDesktop" />.
/// </summary>
public class RemoteDesktopSettings
{
    /// <summary>
    /// The default user for the remote desktop connection.
    /// </summary>
    public string? DefaultUser { get; set; }

    /// <summary>
    /// User overrides for specific host names or ip addresses.
    /// </summary>
    public Dictionary<Regex, string>? UserOverride { get; set; }

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
}