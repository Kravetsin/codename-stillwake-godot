using Godot;

namespace Stillwake.Game.Platform;

/// <summary>
/// Godot's own log files in <c>user://logs</c>: the engine writes them, a release too, and keeps the last
/// five runs. A friend whose game misbehaved sends us the one of that run.
/// </summary>
public static class Logs
{
    /// <summary>Opens the folder of logs in the system's file manager.</summary>
    public static void OpenFolder() => OS.ShellOpen(ProjectSettings.GlobalizePath("user://logs"));
}
