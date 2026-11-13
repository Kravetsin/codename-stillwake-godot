using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>How far a silhouette has come towards being understood, from 0 to 1 (docs/decisions/2026-11-05-the-board.md).</summary>
public static class BoardPencil
{
    /// <summary>The share of parts ever had, a standing station as one more part; 1 for nothing to have.</summary>
    /// <param name="station">Whether a machine making it stands, for a craft; null for a blueprint.</param>
    public static double Of(IReadOnlyList<bool> parts, bool? station)
    {
        int had = parts.Count(p => p) + (station == true ? 1 : 0);
        int of = parts.Count + (station == null ? 0 : 1);
        return of == 0 ? 1 : had / (double)of;
    }

    /// <summary>How far <paramref name="thing"/> has come, by what the crew has obtained and what stands.</summary>
    public static double Of(BoardThing thing, Func<string, bool> has, Func<string, bool> stands) =>
        Of(thing.Parts.Select(has).ToList(), thing.Stations?.Any(stands));
}
