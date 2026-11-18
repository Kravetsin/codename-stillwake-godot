using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Stillwake.Game.Features.Research;

/// <summary>What is kept of a world's board. A file from before the islands also holds where things were dragged, which is dropped.</summary>
/// <param name="Seen">What this player has seen; none if there has been no read yet.</param>
/// <param name="Orders">How many of the market's orders met this player last saw; none if there has been no read yet.</param>
/// <param name="Marked">An older file's notes landed and not touched yet: read as seen, never written.</param>
/// <param name="Batches">What the reads brought, oldest first.</param>
/// <param name="Pinned">The goal pinned; none for none.</param>
public sealed record BoardKept(List<string>? Seen = null, int? Orders = null, List<string>? Marked = null, List<BoardBatch>? Batches = null, string? Pinned = null);

/// <summary>
/// What this machine's player keeps of a world's board, in a file of its own (board-islands).
/// It is a convenience: a lost or bad file means everything known is seen, a failed write that it will be.
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
