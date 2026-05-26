using System.Runtime.Loader;

namespace Flow.Launcher.Plugin.RemoteDesktop;

internal static class WpfDependencyLoader
{
    private static readonly object s_lock = new();
    private static bool s_loaded;

    internal static void Load()
    {
        lock (s_lock)
        {
            if (s_loaded)
            {
                return;
            }

            s_loaded = true;
        }

        // The BAML loader uses the default assembly resolver, which looks into the Flow.Launcher directory, instead of the plugin directory.
        // Explicitly load GongSolutions to ensure it's resolved from the plugin directory

        AssemblyLoadContext.Default.LoadFromAssemblyPath(typeof(GongSolutions.Wpf.DragDrop.DragDrop).Assembly.Location);
    }
}