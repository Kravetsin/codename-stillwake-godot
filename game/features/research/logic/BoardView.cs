using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>How a thing with a slot shows on the board.</summary>
public enum BoardShow
{
    Unseen,
    Silhouette,
    Note,
}

/// <summary>What the board reads of the world.</summary>
/// <param name="Found">What was obtained, in the order first obtained.</param>
/// <param name="Granted">What is known.</param>
/// <param name="Has">Whether an item was ever obtained.</param>
/// <param name="Stands">Whether a machine of a kind stands.</param>
public sealed record BoardRead(IReadOnlyList<string> Found, IReadOnlyCollection<string> Granted, Func<string, bool> Has, Func<string, bool> Stands);

/// <summary>What the board shows now: how each thing with a slot shows, and its threads.</summary>
public sealed record BoardPicture(IReadOnlyDictionary<string, BoardShow> Shows, IReadOnlyList<(string Note, string Shape)> Threads);

/// <summary>
/// Decides what the board shows (docs/decisions/2026-11-05-the-board.md). A note is what was obtained or is known. A shape shows at a thread's end. One with
/// every part noted always shows: crowded off its parts' threads, it hangs one from the part noted last; what the market opens never shows as one. A click adds nothing to the board.
/// </summary>
public static class BoardView
{
    /// <summary>What of what has a slot shows as a note: what was obtained or is known.</summary>
    public static HashSet<string> NotesOf(IReadOnlyList<BoardSlot> slots, BoardRead read)
    {
        var found = read.Found.ToHashSet(StringComparer.Ordinal);
        var known = read.Granted.ToHashSet(StringComparer.Ordinal);
        return slots.Select(s => s.Id).Where(id => found.Contains(id) || known.Contains(id)).ToHashSet(StringComparer.Ordinal);
    }

    /// <param name="picked">The note or shape clicked, which stays shown; null for none.</param>
    public static BoardPicture Of(IReadOnlyList<BoardSlot> slots, IReadOnlyList<BoardThing> things, BoardRead read, string? picked)
    {
        var byId = things.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var slotted = slots.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        double Pencil(BoardThing thing) => BoardPencil.Of(thing, read.Has, read.Stands);
        var notes = NotesOf(slots, read);
        bool Shape(string id) => slotted.Contains(id) && !notes.Contains(id);
        var threads = new List<(string Note, string Shape)>();
        var ends = new HashSet<string>(StringComparer.Ordinal);
        foreach (var slot in slots)
        {
            if (!notes.Contains(slot.Id)) continue;
            foreach (string shape in BoardThreads.Of(slot.Id, things, Shape, Pencil))
            {
                threads.Add((slot.Id, shape));
                ends.Add(shape);
            }
        }
        foreach (var slot in slots)
        {
            if (notes.Contains(slot.Id) || ends.Contains(slot.Id) || byId.GetValueOrDefault(slot.Id) is not { Parts.Count: > 0, Market: false } thing) continue;
            if (!thing.Parts.All(notes.Contains)) continue;
            threads.Add((slots.Last(s => thing.Parts.Contains(s.Id)).Id, slot.Id));
            ends.Add(slot.Id);
        }
        var shows = new Dictionary<string, BoardShow>(StringComparer.Ordinal);
        foreach (var slot in slots)
            shows[slot.Id] = notes.Contains(slot.Id) ? BoardShow.Note
                : ends.Contains(slot.Id) || slot.Id == picked ? BoardShow.Silhouette
                : BoardShow.Unseen;
        return new BoardPicture(shows, threads);
    }
}
