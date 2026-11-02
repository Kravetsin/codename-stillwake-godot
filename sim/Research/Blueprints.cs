using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Research;

/// <summary>A blueprint: its id, as the build menu names its card, what it places and what that costs.</summary>
public sealed record Blueprint(string Id, Placement Placement, IReadOnlyList<(string Item, int Count)> Cost);

/// <summary>What blueprints there are, their names and costs (blueprints.ts).</summary>
public static class Blueprints
{
    /// <summary>The blueprint <paramref name="placement"/> is built by, named as the build menu names its card (<c>wallIron</c>).</summary>
    public static string Of(Placement placement) =>
        placement.Kind is "wall" or "backWall" ? placement.Kind + Titled(placement.Material ?? "") : placement.Kind;

    private static string Titled(string id) => id.Length == 0 ? "" : char.ToUpperInvariant(id[0]) + id[1..];

    /// <summary>
    /// Every blueprint there is, with what it places and costs: walls and back walls by material but bedrock, never built
    /// (0077, 0084); builds; machines with a card in the menu, never the lifepod nor a pile.
    /// </summary>
    public static readonly IReadOnlyList<Blueprint> All =
    [
        .. Content.Materials.All.Where(m => m.Built).Select(m => Of(Placement.Wall(m.Id), m.Cost)),
        .. Content.Materials.All.Where(m => m.Built).Select(m => Of(Placement.BackWall(m.Id), m.Cost)),
        .. Content.Builds.All.Select(b => Of(new Placement("structure", b.Id), b.Cost)),
        .. Content.Machines.All.Where(m => m.Menu != null).Select(m => Of(new Placement("machine", m.Kind), m.Cost)),
    ];

    private static readonly HashSet<string> Ids = All.Select(b => b.Id).ToHashSet(StringComparer.Ordinal);

    /// <summary>Whether a blueprint places <paramref name="placement"/>: none for a bedrock wall or back wall.</summary>
    public static bool Has(Placement placement) => Ids.Contains(Of(placement));

    private static Blueprint Of(Placement placement, IReadOnlyList<(string Item, int Count)> cost) => new(Of(placement), placement, cost);
}
