using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>What the world has just learned (apps/game/src/features/research/news.ts).</summary>
public static class News
{
    /// <summary>
    /// The blueprints in <paramref name="known"/> that were not in <paramref name="seen"/>, in order: what
    /// was learned since the last read. Nothing on the first read, which only sees what is known already,
    /// nor after a load that knows less than before.
    /// </summary>
    public static IReadOnlyList<string> LearnedSince(IReadOnlyList<string>? seen, IReadOnlyList<string> known)
    {
        if (seen == null || !seen.All(id => known.Contains(id, StringComparer.Ordinal))) return [];
        return known.Where(id => !seen.Contains(id, StringComparer.Ordinal)).ToList();
    }
}
