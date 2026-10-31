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
/// <param name="Count">How many of an item were obtained.</param>
/// <param name="Stands">Whether a machine of a kind stands.</param>
public sealed record BoardRead(IReadOnlyList<string> Found, IReadOnlyCollection<string> Granted, Func<string, int> Count, Func<string, bool> Stands);

/// <summary>What the board shows now: how each thing with a slot shows, and its threads.</summary>
public sealed record BoardPicture(IReadOnlyDictionary<string, BoardShow> Shows, IReadOnlyList<(string Note, string Shape)> Threads);

/// <summary>
/// Decides what the board shows (docs/plans/board.md). A note is what was obtained or is known. A shape shows only at a thread's end or
/// linked to what is picked.
/// </summary>
public static class BoardView
{
    /// <param name="picked">The note or shape clicked; null for none.</param>
    public static BoardPicture Of(IReadOnlyList<BoardSlot> slots, IReadOnlyList<BoardThing> things, BoardRead read, string? picked)
    {
        var byId = things.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var found = read.Found.ToHashSet(StringComparer.Ordinal);
        var known = read.Granted.ToHashSet(StringComparer.Ordinal);
        var slotted = slots.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        double Pencil(BoardThing thing) => BoardPencil.Of(thing, read.Count, read.Stands);
        var notes = slots.Select(s => s.Id).Where(id => found.Contains(id) || known.Contains(id)).ToHashSet(StringComparer.Ordinal);
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
        var shows = new Dictionary<string, BoardShow>(StringComparer.Ordinal);
        foreach (var slot in slots)
            shows[slot.Id] = notes.Contains(slot.Id) ? BoardShow.Note
                : ends.Contains(slot.Id) || (picked != null && Linked(picked, slot.Id, byId)) ? BoardShow.Silhouette
                : BoardShow.Unseen;
        return new BoardPicture(shows, threads);
    }

    /// <summary>Whether <paramref name="id"/> is one step from <paramref name="picked"/>, or is it: one takes the other, or one is made at the other.</summary>
    private static bool Linked(string picked, string id, Dictionary<string, BoardThing> byId)
    {
        if (picked == id) return true;
        return Ties(byId.GetValueOrDefault(picked), id) || Ties(byId.GetValueOrDefault(id), picked);
    }

    private static bool Ties(BoardThing? thing, string other) =>
        thing != null && (thing.Parts.Any(p => p.Item == other) || thing.Stations?.Contains(other) == true);
}
