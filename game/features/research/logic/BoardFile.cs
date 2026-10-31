using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Stillwake.Game.Features.Research;

/// <summary>Where a thing was moved to on the board.</summary>
public readonly record struct BoardSpot(double X, double Y);

/// <summary>
/// What this machine's player moved on a world's board, in a file of its own (docs/plans/board.md): only that, every other thing standing in its slot.
/// It is a convenience: a lost or bad file means things go back to their slots, a failed write that they will.
/// </summary>
public static class BoardFile
{
    /// <summary>The file of world <paramref name="world"/> in <paramref name="folder"/>.</summary>
    public static string PathOf(string folder, uint world) => Path.Combine(folder, $"{world}.json");

    /// <summary>What was moved; nothing if the file is lost or bad.</summary>
    public static Dictionary<string, BoardSpot> Read(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, BoardSpot>>(File.ReadAllText(path)) ?? [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    /// <summary>Writes what was moved; false if it could not, which stops nothing.</summary>
    public static bool Write(string path, IReadOnlyDictionary<string, BoardSpot> moved)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(moved));
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
