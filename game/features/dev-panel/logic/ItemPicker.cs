using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.I18n;

namespace Stillwake.Game.Features.Dev;

/// <summary>What the dev panel's item picker lists: every item a hero can carry, found by its name or its id.</summary>
public static class ItemPicker
{
    /// <summary>
    /// The items whose name or id holds <paramref name="query"/>, case ignored; every item for none. Sorted as a bag
    /// sorts (0008): by group, then by name.
    /// </summary>
    public static IReadOnlyList<ItemSpec> Matching(string query)
    {
        string words = query.Trim();
        return Content.Items.All
            .Where(item => words == "" || NameOf(item.Id).Contains(words, System.StringComparison.OrdinalIgnoreCase)
                || item.Id.Contains(words, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Group)
            .ThenBy(item => NameOf(item.Id), System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>The name the game shows for <paramref name="item"/>.</summary>
    public static string NameOf(string item) => En.T($"item.{item}");
}
