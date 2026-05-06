using System;
using System.Runtime.CompilerServices;

namespace Flow.Launcher.Plugin.RemoteDesktop;

internal class ContextLogger<T>(PluginInitContext context)
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