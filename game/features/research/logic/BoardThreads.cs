using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>What a note's threads run to (docs/decisions/2026-11-05-the-board.md): few, so a plate's many uses do not fill the board.</summary>
public static class BoardThreads
{
    /// <summary>How many threads a note runs to the closest to done; a shape left with no thread may add one (<see cref="BoardView"/>).</summary>
    public const int Most = 3;

    /// <summary>The shapes <paramref name="note"/> is a part of, closest to done first, ties in content order. Never to what the market opens.</summary>
    /// <param name="things">Every blueprint and craft, in content order.</param>
    /// <param name="shape">Whether a thing is a shape yet: it has a slot and no ink.</param>
    /// <param name="pencil">How far a thing has come.</param>
    public static IReadOnlyList<string> Of(string note, IReadOnlyList<BoardThing> things, Func<string, bool> shape, Func<BoardThing, double> pencil) =>
        things.Where(t => !t.Market && t.Parts.Contains(note) && shape(t.Id))
            .OrderByDescending(pencil)
            .Take(Most)
            .Select(t => t.Id)
            .ToList();
}
