using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>A thing on the board as the search lists it: its id, its name in the language played, and whether it is a note (known) or only a shape.</summary>
public sealed record BoardFound(string Id, string Name, bool Note);

/// <summary>The board's search (board-search): what on the board a query names. Only what the board shows is offered, so it gives nothing away.</summary>
public static class BoardSearch
{
    /// <summary>The most rows the list under the field shows.</summary>
    public const int Most = 8;

    /// <summary>
    /// What among <paramref name="shown"/> has a name holding <paramref name="query"/>, case ignored: names starting with it first, then
    /// notes before shapes, then by name; at most <see cref="Most"/>. An empty query names nothing.
    /// </summary>
    public static IReadOnlyList<BoardFound> Matching(IEnumerable<BoardFound> shown, string query)
    {
        string words = query.Trim();
        if (words.Length == 0) return [];
        return shown
            .Select(f => (Found: f, At: f.Name.IndexOf(words, StringComparison.OrdinalIgnoreCase)))
            .Where(m => m.At >= 0)
            .OrderBy(m => m.At == 0 ? 0 : 1)
            .ThenBy(m => m.Found.Note ? 0 : 1)
            .ThenBy(m => m.Found.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Found.Id, StringComparer.Ordinal)
            .Take(Most)
            .Select(m => m.Found)
            .ToList();
    }

    /// <summary>The row lit after a step of <paramref name="by"/> from <paramref name="lit"/> among <paramref name="count"/> rows, wrapping round; none with no rows.</summary>
    public static int? Step(int? lit, int by, int count) =>
        count == 0 ? null : lit is not { } at ? (by > 0 ? 0 : count - 1) : ((at + by) % count + count) % count;
}
