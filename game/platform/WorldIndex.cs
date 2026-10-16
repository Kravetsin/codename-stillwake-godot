using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Stillwake.Game.Platform;

/// <summary>Whether a world can be played in this build: its save's format against what this build reads.</summary>
public enum WorldStatus
{
    Playable,
    /// <summary>Saved by a newer build: it is never loaded by an older one.</summary>
    Newer,
    /// <summary>Saved in a format older than any this build can bring up to date.</summary>
    TooOld,
    /// <summary>Its save cannot be read at all.</summary>
    Damaged,
}

/// <summary>A world on disk as its list shows it: its name, when it was last played, how long, and whether it can be.</summary>
public sealed record WorldEntry(string Id, string Name, DateTime LastPlayed, int PlaySeconds, WorldStatus Status);

/// <summary>
/// The player's worlds (0031), a folder each under <paramref name="dir"/>: <c>world.json</c>, the save
/// before it as <c>world.bak.json</c>, and <c>name.txt</c>. Nothing else is kept about a world: the list
/// reads <c>format</c> and <c>tick</c>, the first keys of a save, from its first bytes, and the file's
/// time as when it was last played. A save of a format from <paramref name="oldest"/> to
/// <paramref name="current"/> can be played; <paramref name="ticksPerSecond"/> turns its tick into play
/// time. Plain .NET files, tested without Godot.
/// </summary>
public sealed class WorldIndex(string dir, int oldest, int current, int ticksPerSecond)
{
    /// <summary>The longest name, in code points.</summary>
    public const int NameMost = 32;
    private const string Save = "world", Name = "name.txt";

    /// <summary>Every world, the last played first, ties by id.</summary>
    public IReadOnlyList<WorldEntry> List()
    {
        if (!Directory.Exists(dir)) return [];
        var worlds = new List<WorldEntry>();
        foreach (var folder in Directory.GetDirectories(dir))
        {
            string id = Path.GetFileName(folder), file = Path.Combine(folder, $"{Save}.json");
            if (!File.Exists(file)) continue;
            string name = File.Exists(Path.Combine(folder, Name)) ? File.ReadAllText(Path.Combine(folder, Name)).Trim() : id;
            var head = Head(file);
            var status = head is not var (format, _) ? WorldStatus.Damaged
                : format > current ? WorldStatus.Newer
                : format < oldest ? WorldStatus.TooOld
                : WorldStatus.Playable;
            worlds.Add(new WorldEntry(id, name, File.GetLastWriteTimeUtc(file), (head?.Tick ?? 0) / ticksPerSecond, status));
        }
        return worlds.OrderByDescending(w => w.LastPlayed).ThenBy(w => w.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>The world Continue plays: the last played that can be, or none.</summary>
    public WorldEntry? Continue() => List().FirstOrDefault(w => w.Status == WorldStatus.Playable);

    /// <summary>
    /// A new world's folder, named <paramref name="name"/> — trimmed, cut to <see cref="NameMost"/>, or
    /// "World N" when empty — its id the time it is made at, <paramref name="now"/>. Its save comes after.
    /// </summary>
    public string Create(string name, DateTime now)
    {
        string id = now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        for (int n = 2; Directory.Exists(Path.Combine(dir, id)); n++) id = $"{now:yyyyMMdd-HHmmss}-{n}";
        Directory.CreateDirectory(Path.Combine(dir, id));
        File.WriteAllText(Path.Combine(dir, id, Name), NameOf(name, List().Count));
        return id;
    }

    /// <summary>A world's name as given, or "World N" for the next of <paramref name="count"/> worlds when it is empty.</summary>
    public static string NameOf(string name, int count)
    {
        var points = name.Trim().EnumerateRunes().Take(NameMost).ToList();
        return points.Count == 0 ? $"World {count + 1}" : string.Concat(points.Select(r => r.ToString()));
    }

    /// <summary>World <paramref name="id"/>'s save, or null when it has none.</summary>
    public string? Read(string id) => new Saves(Path.Combine(dir, id)).Read(Save);

    /// <summary>Saves world <paramref name="id"/>: the save before it kept as its backup.</summary>
    public void Write(string id, string save) => new Saves(Path.Combine(dir, id)).Write(Save, save);

    /// <summary>The name world <paramref name="id"/> was given.</summary>
    public string NameOf(string id) =>
        File.Exists(Path.Combine(dir, id, Name)) ? File.ReadAllText(Path.Combine(dir, id, Name)).Trim() : id;

    /// <summary>Deletes world <paramref name="id"/>, its save and backup with it.</summary>
    public void Delete(string id)
    {
        string folder = Path.Combine(dir, id);
        if (Path.GetFileName(folder) != id || !Directory.Exists(folder)) return;
        Directory.Delete(folder, recursive: true);
    }

    /// <summary>A save's <c>format</c> and <c>tick</c>, its first keys, from its first bytes; null when they cannot be read.</summary>
    public static (int Format, int Tick)? Head(string file)
    {
        try
        {
            var bytes = new byte[256];
            int read;
            using (var stream = File.OpenRead(file)) read = stream.Read(bytes, 0, bytes.Length);
            var reader = new Utf8JsonReader(bytes.AsSpan(0, read), isFinalBlock: false, default);
            int? format = null, tick = null;
            while (reader.Read() && (format == null || tick == null))
            {
                if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1) continue;
                string key = reader.GetString()!;
                if (!reader.Read() || reader.TokenType != JsonTokenType.Number) break;
                if (key == "format") format = reader.GetInt32();
                if (key == "tick") tick = reader.GetInt32();
            }
            return format is int f && tick is int t ? (f, t) : null;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
