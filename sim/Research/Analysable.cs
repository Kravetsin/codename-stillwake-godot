namespace Stillwake.Sim.Research;

/// <summary>
/// The items an analyzer has anything to learn from: those in the cost of a blueprint or a craft not known
/// from the start (0027, amended twice): not in <c>Content.Research.StartKnown</c>, and not the cable, always
/// known. Worked out from content, never listed: an item joining such a cost joins them.
/// </summary>
public static class Analysable
{
    private static readonly List<string> Sorted =
        Blueprints.All.Where(b => !Granted(b.Id)).SelectMany(b => b.Cost)
            .Concat(Content.Crafts.All.Where(c => !Granted(c.Item)).SelectMany(c => c.Cost))
            .Select(c => c.Item)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

    private static bool Granted(string id) => Content.Research.StartKnown.Contains(id, StringComparer.Ordinal);

    /// <summary>Every item in the cost of something not known from the start, each once, sorted ordinally.</summary>
    public static IReadOnlyList<string> All => Sorted;

    /// <summary>
    /// Whether <paramref name="item"/> needs analysis: it is in the cost of something not known from the start.
    /// Any other item is known from the start: no "?" on it, and an analyzer holding it rests
    /// (<c>research.nothingToLearn</c>).
    /// </summary>
    public static bool NeedsAnalysis(string item) => Sorted.BinarySearch(item, StringComparer.Ordinal) >= 0;
}
