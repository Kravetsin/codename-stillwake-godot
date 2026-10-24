using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Stillwake.Game.Platform;

/// <summary>
/// Save files in <paramref name="dir"/>, one <c>&lt;slot&gt;.json</c> each (apps/desktop/src/saves.ts):
/// a world's save (<see cref="WorldIndex"/>) and the last desync report, the file before each kept as
/// <c>&lt;slot&gt;.bak.json</c>. Plain .NET files, so it is tested without Godot; the game gives it
/// Godot's user folder.
/// </summary>
public sealed partial class Saves(string dir)
{
    /// <summary>The slot a desync report goes to.</summary>
    public const string Desync = "desync";

    private readonly object _writing = new();
    private int _written;

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex Slot();

    /// <summary>A slot is a plain name: it becomes a file name, so nothing may climb out of the folder.</summary>
    private string FileOf(string slot)
    {
        if (!Slot().IsMatch(slot)) throw new ArgumentException($"'{slot}' is not a save slot name.");
        return Path.Combine(dir, $"{slot}.json");
    }

    /// <summary>What <paramref name="slot"/> holds, or null when it was never written.</summary>
    public string? Read(string slot)
    {
        string file = FileOf(slot);
        return File.Exists(file) ? File.ReadAllText(file) : null;
    }

    /// <summary>
    /// Writes <paramref name="text"/> to <paramref name="slot"/>: aside first, then moved over, the file it
    /// replaces kept as the slot's backup in the same step, so a crash mid-write leaves the last save whole.
    /// </summary>
    public void Write(string slot, string text)
    {
        string target = FileOf(slot);
        // Writes to a slot run one after another, a second waiting for the first.
        lock (_writing)
        {
            Directory.CreateDirectory(dir);
            string aside = $"{target}.{++_written}.tmp";
            File.WriteAllText(aside, text);
            if (File.Exists(target)) File.Replace(aside, target, Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(target)}.bak.json"));
            else File.Move(aside, target);
        }
    }
}
