using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Stillwake.Game.Features.Research;

/// <summary>Where a thing was moved to on the board.</summary>
public readonly record struct BoardSpot(double X, double Y);

/// <summary>What is kept of a world's board.</summary>
/// <param name="Moved">What was moved and to where: only that, every other thing standing in its slot.</param>
/// <param name="Seen">The notes this player last saw; none if there has been no read yet.</param>
public sealed record BoardKept(Dictionary<string, BoardSpot>? Moved = null, List<string>? Seen = null);

/// <summary>
/// What this machine's player keeps of a world's board, in a file of its own (docs/plans/board.md).
/// It is a convenience: a lost or bad file means things go back to their slots with nothing new, a failed write that they will.
/// </summary>
public static class BoardFile
{
    /// <summary>The file of world <paramref name="world"/> in <paramref name="folder"/>.</summary>
    public static string PathOf(string folder, uint world) => Path.Combine(folder, $"{world}.json");

    /// <summary>What was kept; nothing if the file is lost or bad.</summary>
    public static BoardKept Read(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<BoardKept>(File.ReadAllText(path)) ?? new BoardKept();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new BoardKept();
        }
    }

    /// <summary>Writes what is kept; false if it could not, which stops nothing.</summary>
    public static bool Write(string path, BoardKept kept)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(kept));
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
