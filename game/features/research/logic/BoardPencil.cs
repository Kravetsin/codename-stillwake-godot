using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>How far a silhouette has come towards being understood, from 0 to 1 (docs/plans/board.md).</summary>
public static class BoardPencil
{
    /// <summary>The mean of <c>min(count, enough) / enough</c> over the parts, a standing station as one more part; 1 for nothing to have.</summary>
    /// <param name="station">Whether a machine making it stands, for a craft; null for a blueprint.</param>
    public static double Of(IReadOnlyList<(int Count, int Enough)> parts, bool? station)
    {
        double sum = parts.Sum(p => Math.Min(p.Count, p.Enough) / (double)p.Enough) + (station == true ? 1 : 0);
        int of = parts.Count + (station == null ? 0 : 1);
        return of == 0 ? 1 : sum / of;
    }

    /// <summary>How far <paramref name="thing"/> has come, by what the crew has obtained and what stands.</summary>
    public static double Of(BoardThing thing, Func<string, int> count, Func<string, bool> stands) =>
        Of(thing.Parts.Select(p => (count(p.Item), p.Enough)).ToList(), thing.Stations?.Any(stands));
}
