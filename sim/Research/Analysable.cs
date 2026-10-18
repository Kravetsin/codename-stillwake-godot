namespace Stillwake.Sim.Research;

/// <summary>
/// The items an analyzer has anything to learn from: those in some cost, a blueprint's, a craft's or the
/// cable's (0027, amended). Worked out from content, never listed: an item joining a cost joins them.
/// </summary>
public static class Analysable
{
    private static readonly List<string> Sorted =
        Blueprints.All.SelectMany(b => b.Cost)
            .Concat(Content.Crafts.All.SelectMany(c => c.Cost))
            .Concat(Content.Power.CableCost)
            .Select(c => c.Item)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Every item in some cost, each once, sorted ordinally.</summary>
    public static IReadOnlyList<string> All => Sorted;

    /// <summary>
    /// Whether <paramref name="item"/> needs analysis: it is in some cost. Any other item is known from the
    /// start: no "?" on it, and an analyzer holding it rests (<c>research.nothingToLearn</c>).
    /// </summary>
    public static bool NeedsAnalysis(string item) => Sorted.BinarySearch(item, StringComparer.Ordinal) >= 0;
}
