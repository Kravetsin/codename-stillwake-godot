using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// What an analyzer's panel teaches (0027), filtered from <see cref="IResearchQueries.Recipes"/>: what
/// the item in its slot opened, what is analysed so far, and what is one item short of opening.
/// </summary>
/// <param name="Opened">The blueprints and crafts the item is part of, opened by analysis, not granted.</param>
/// <param name="OneShort">What would open with one more item analysed, and that item.</param>
public sealed record Lessons(IReadOnlyList<Recipe> Opened, IReadOnlyList<string> Analysed, IReadOnlyList<(Recipe Recipe, string Item)> OneShort)
{
    /// <summary>The lessons of an analyzer holding <paramref name="item"/>, or none; nothing opened until it is analysed.</summary>
    public static Lessons Of(IResearchQueries research, string? item)
    {
        var recipes = research.Recipes();
        bool analysed = item != null && research.Analysed.Contains(item);
        var opened = analysed
            ? recipes.Where(r => !r.Granted && r.Known && r.Cost.Any(c => c.Item == item)).ToList()
            : [];
        var oneShort = recipes.Where(r => !r.Known && !r.Locked && r.Unanalysed.Count == 1)
            .Select(r => (r, r.Unanalysed[0])).ToList();
        return new Lessons(opened, research.Analysed.ToList(), oneShort);
    }

    /// <summary>What the lessons show, as text: the panel is built again only when it changes.</summary>
    public string Key =>
        string.Join(",", Opened.Select(r => r.Id)) + "|" + string.Join(",", Analysed) + "|"
        + string.Join(",", OneShort.Select(s => s.Recipe.Id + ":" + s.Item));
}
