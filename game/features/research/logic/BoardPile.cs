using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The unread entries of one showing of the board ([the pile](../../../../docs/decisions/2026-11-02-board-pile.md)). It is not stored: it is the notes less
/// what was seen, so only the top is held, and a close and a reopening work out the same order. Parts come before what they make.
/// </summary>
public sealed class BoardPile
{
    /// <summary>The entry shown large; null while not reading.</summary>
    public string? Top { get; private set; }

    /// <summary>This showing has read an empty pile: from now on a find lands at once, so a drag is left alone.</summary>
    private bool Whole { get; set; }

    /// <summary>The next to read among the unread in <paramref name="slots"/>: the first with no unread part or station; if none, the first, so a recipe cycle cannot stall the pile.</summary>
    public static string? Next(IReadOnlyCollection<string> unread, IReadOnlyList<BoardSlot> slots, IReadOnlyList<BoardThing> things)
    {
        var waiting = unread as IReadOnlySet<string> ?? unread.ToHashSet(StringComparer.Ordinal);
        string? first = null;
        foreach (var slot in slots)
        {
            if (!waiting.Contains(slot.Id)) continue;
            first ??= slot.Id;
            if (things.FirstOrDefault(t => t.Id == slot.Id) is not { } thing) return slot.Id;
            if (!thing.Parts.Any(waiting.Contains) && !(thing.Stations?.Any(waiting.Contains) ?? false)) return slot.Id;
        }
        return first;
    }

    /// <summary>A read of the board, after what is seen has read the notes.</summary>
    public void Read(BoardSeen seen, IReadOnlyList<BoardSlot> slots, IReadOnlyList<BoardThing> things)
    {
        var next = Next(seen.Unread, slots, things);
        if (next == null)
        {
            (Top, Whole) = (null, true);
            return;
        }
        if (Whole)
        {
            foreach (string id in seen.Unread) seen.Land(id);
            return;
        }
        Top ??= next;
    }

    /// <summary>Lands the top and turns to the next; returns what landed, or null if the top is gone or no longer unread.</summary>
    public string? Flip(BoardSeen seen, IReadOnlyList<BoardSlot> slots, IReadOnlyList<BoardThing> things)
    {
        if (Top is not { } top || !seen.Unread.Contains(top)) return null;
        seen.Land(top);
        Top = Next(seen.Unread, slots, things);
        return top;
    }

    /// <summary>Lands all that is unread; false if there is no pile to take.</summary>
    public bool TakeAll(BoardSeen seen)
    {
        if (Top is not { } top || !seen.Unread.Contains(top)) return false;
        foreach (string id in seen.Unread) seen.Land(id);
        Top = null;
        return true;
    }

    /// <summary>A showing over: the next read starts afresh.</summary>
    public void Reset() => (Top, Whole) = (null, false);

    /// <summary>The shape closest to done, ties in the order given; null for none.</summary>
    public static string? Closest(IEnumerable<string> shapes, BoardRead read, IReadOnlyList<BoardThing> things)
    {
        string? best = null;
        double most = -1;
        foreach (string id in shapes)
        {
            if (things.FirstOrDefault(t => t.Id == id) is not { } thing) continue;
            double pencil = BoardPencil.Of(thing, read.Has, read.Stands);
            if (pencil <= most) continue;
            (best, most) = (id, pencil);
        }
        return best;
    }

    /// <summary>What <paramref name="thing"/> still lacks: each part never had, and the first station if none stands.</summary>
    public static BoardLack Lack(BoardThing thing, BoardRead read) => new(
        thing.Parts.Where(p => !read.Has(p)).ToList(),
        thing.Stations is { Count: > 0 } stations && !stations.Any(read.Stands) ? stations[0] : null);
}

/// <summary>What a shape lacks.</summary>
/// <param name="Parts">Each part never had.</param>
/// <param name="Station">A machine to stand, if none making it does.</param>
public sealed record BoardLack(IReadOnlyList<string> Parts, string? Station);
