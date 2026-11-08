using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Research;

/// <summary>A thing on the board as the search lists it: its id, its name in the language played and in the other one, and whether it is a note (known) or only a shape.</summary>
public sealed record BoardFound(string Id, string Name, string Other, bool Note);

/// <summary>A row of the search: the thing, and how it ties to <paramref name="Named"/>, the thing the query named; neither for a thing named itself.</summary>
public sealed record BoardHit(BoardFound Found, Tie? Tie, BoardFound? Named);

/// <summary>
/// The board's search (board-search, board-search-related): what on the board a query names, and what that is tied to by
/// the recipes. Only what the board shows is offered, so it gives nothing away.
/// </summary>
public static class BoardSearch
{
    /// <summary>The most rows the list under the field shows.</summary>
    public const int Most = 8;

    /// <summary>The most things a query may name and still bring in their ties: a broad one names no one thing.</summary>
    public const int MostNamed = 3;

    /// <summary>
    /// What among <paramref name="shown"/> <paramref name="query"/> names (<see cref="TextMatch"/>), in either language, best match
    /// first, then notes before shapes, then by name. When the best matches are at most <see cref="MostNamed"/>, each is followed by
    /// what <paramref name="tie"/> says is tied to it, by the kind of tie, and the weaker matches come last. A thing is listed once,
    /// at most <see cref="Most"/> rows. An empty query names nothing.
    /// </summary>
    /// <param name="tie">How a thing (the second) ties to a named one (the first): <see cref="TechLinks.TieOf"/>.</param>
    public static IReadOnlyList<BoardHit> Matching(IEnumerable<BoardFound> shown, string query, Func<string, string, Tie?> tie)
    {
        var words = TextMatch.Words(query);
        if (words.Length == 0) return [];
        var all = shown.ToList();
        var named = all
            .Select(f => (Found: f, Tier: Best(TextMatch.Of(words, TextMatch.Words(f.Name)), TextMatch.Of(words, TextMatch.Words(f.Other)))))
            .Where(m => m.Tier != null)
            .OrderBy(m => m.Tier)
            .ThenBy(m => m.Found.Note ? 0 : 1)
            .ThenBy(m => m.Found.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Found.Id, StringComparer.Ordinal)
            .ToList();
        var subjects = named.TakeWhile(m => m.Tier == named[0].Tier).ToList();
        if (subjects.Count == 0 || subjects.Count > MostNamed) return named.Take(Most).Select(m => new BoardHit(m.Found, null, null)).ToList();
        var rows = subjects.Select(m => new BoardHit(m.Found, null, null)).ToList();
        var listed = subjects.Select(m => m.Found.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var (subject, _) in subjects)
        {
            var tied = all
                .Where(f => !listed.Contains(f.Id))
                .Select(f => (Found: f, Tie: tie(subject.Id, f.Id)))
                .Where(t => t.Tie != null)
                .OrderBy(t => t.Tie)
                .ThenBy(t => t.Found.Note ? 0 : 1)
                .ThenBy(t => t.Found.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Found.Id, StringComparer.Ordinal)
                .Select(t => new BoardHit(t.Found, t.Tie, subject))
                .ToList();
            rows.AddRange(tied);
            listed.UnionWith(tied.Select(h => h.Found.Id));
        }
        rows.AddRange(named.Where(m => !listed.Contains(m.Found.Id)).Select(m => new BoardHit(m.Found, null, null)));
        return rows.Take(Most).ToList();
    }

    /// <summary>The key of the line under a tied row saying why it is listed: "makes Iron".</summary>
    public static string WhyOf(Tie tie) => tie switch { Tie.MadeAt => "research.search.madeAt", Tie.From => "research.search.from", _ => "research.search.usedIn" };

    private static MatchTier? Best(MatchTier? a, MatchTier? b) => a is null ? b : b is null ? a : a < b ? a : b;

    /// <summary>The row lit after a step of <paramref name="by"/> from <paramref name="lit"/> among <paramref name="count"/> rows, wrapping round; none with no rows.</summary>
    public static int? Step(int? lit, int by, int count) =>
        count == 0 ? null : lit is not { } at ? (by > 0 ? 0 : count - 1) : ((at + by) % count + count) % count;
}
