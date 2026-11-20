using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stillwake.Game.Platform;

/// <summary>
/// The user folder under the game's old name (2026-11-21-game-title): Godot names it after the project, so the rename
/// left the worlds and settings in <c>app_userdata/Stillwake</c>. Plain .NET, tested without Godot.
/// </summary>
public static class UserFolder
{
    /// <summary>The name the folder had before the rename.</summary>
    public const string OldName = "Stillwake";

    /// <summary>
    /// Moves every file and folder of <paramref name="from"/> that <paramref name="to"/> lacks into it, and returns their
    /// names; what both hold stays where it is (Godot writes <c>logs</c> before any code runs). Nothing without <paramref name="from"/>.
    /// </summary>
    public static IReadOnlyList<string> Adopt(string from, string to)
    {
        if (!Directory.Exists(from) || Path.GetFullPath(from) == Path.GetFullPath(to)) return [];
        Directory.CreateDirectory(to);
        var moved = new List<string>();
        foreach (string entry in Directory.EnumerateFileSystemEntries(from).Order(System.StringComparer.Ordinal))
        {
            string name = Path.GetFileName(entry), target = Path.Combine(to, name);
            if (File.Exists(target) || Directory.Exists(target)) continue;
            if (Directory.Exists(entry)) Directory.Move(entry, target);
            else File.Move(entry, target);
            moved.Add(name);
        }
        return moved;
    }
}
