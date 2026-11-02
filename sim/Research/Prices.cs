using Stillwake.Content;

namespace Stillwake.Sim.Research;

/// <summary>What a block of the tree costs: what it opens is worth, times its ring's multiple, to two figures as a shop prints it (2026-10-30-tech-rings).</summary>
public static class Prices
{
    private static readonly Dictionary<string, Blueprint> Prints = Blueprints.All.ToDictionary(b => b.Id, StringComparer.Ordinal);

    private static readonly Dictionary<string, int> ByNode = Content.Research.Nodes.Where(n => !n.Gate).ToDictionary(n => n.Id, Worked, StringComparer.Ordinal);

    /// <summary>What block <paramref name="node"/> costs in credits; 0 for a gate.</summary>
    public static int Of(TechNode node) => ByNode.GetValueOrDefault(node.Id);

    /// <summary>What the blueprint, craft or upgrade <paramref name="id"/> is worth: a craft some units of it, a blueprint what it is built of, an upgrade its own price.</summary>
    public static int WorthOf(string id)
    {
        if (Upgrades.Of(id) is { } upgrade) return upgrade.Credits;
        if (Crafts.TryGet(id, out var craft)) return Content.Research.CraftTimes * Market.WorthOf(craft.Item);
        return Prints[id].Cost.Sum(c => c.Count * Market.WorthOf(c.Item));
    }

    private static int Worked(TechNode node) => Rounded(node.Times * WorthOf(node.Id));

    /// <summary><paramref name="price"/> up to its first two figures.</summary>
    private static int Rounded(int price)
    {
        int unit = 1;
        while (price / unit >= 100) unit *= 10;
        return (price + unit - 1) / unit * unit;
    }
}
