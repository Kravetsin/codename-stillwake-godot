using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Building;

/// <summary>What things cost and take to build, from content, and what a blueprint still lacks (costs.ts).</summary>
internal static class Costs
{
    private static Build Wall(Material material) => new(material.Id, material.Cost, material.BuildTicks);

    private static Build Machine(string kind)
    {
        var spec = Machines.Get(kind);
        return new Build(kind, spec.Cost, spec.BuildTicks);
    }

    /// <summary>What a placement costs and how long it takes to build.</summary>
    public static Build BuildOf(Placement placement) => placement.Layer switch
    {
        "structure" when placement.Kind == "wall" => Wall(Materials.Get(placement.Material!)),
        "structure" => Builds.Get(Builds.BuiltAs(placement.Kind)),
        "machine" => Machine(placement.Kind),
        "back" => Wall(Materials.Get(placement.Material!)),
        _ => throw new ArgumentException($"building: no layer {placement.Layer}"),
    };

    /// <summary>What <paramref name="layer"/> of <paramref name="cell"/> holds now, as a build: what taking it apart refunds.</summary>
    public static Build? BuiltAt(IGridQueries grid, int cell, string layer)
    {
        switch (layer)
        {
            case "structure":
                if (grid.WallAt(cell) is { } wall) return Wall(wall);
                return grid.StructureAt(cell) switch
                {
                    Structure.Empty => null,
                    Structure.Floor => Builds.Get("floor"),
                    Structure.Ladder => Share(Builds.Get("ladder"), grid.PieceOf(cell).Count),
                    _ => Share(Builds.Get("door"), grid.PieceOf(cell).Count),
                };
            case "machine":
                return grid.MachineAt(cell) is { } m ? Machine(m.Kind) : null;
            case "back":
                return grid.BackAt(cell) is { } back ? Wall(back) : null;
            default:
                return null;
        }
    }

    /// <summary>
    /// A piece of <paramref name="cells"/> cells built as <paramref name="build"/>: its cost for as many of its pieces as those cells
    /// make, each item rounded down, so a doorway of two doors gives back two and a ladder piece a meteor broke gives nothing;
    /// its ticks to take apart the whole piece's (0104).
    /// </summary>
    private static Build Share(Build build, int cells)
    {
        int whole = build.Across * build.Tall;
        if (cells == whole) return build;
        var cost = build.Cost.Select(c => (c.Item, Count: c.Count * cells / whole)).Where(c => c.Count > 0).ToList();
        return build with { Cost = cost };
    }

    /// <summary>Ticks to take apart what took <paramref name="buildTicks"/> to build: a share of them, rounded up.</summary>
    public static int ApartTicks(int buildTicks) =>
        (buildTicks + Builds.DeconstructSpeedup - 1) / Builds.DeconstructSpeedup;

    /// <summary>Ticks of work a plan takes: its build's, or those to take apart what a mark names.</summary>
    public static int TicksFor(IGridQueries grid, Plan plan)
    {
        if (plan.Place is { } place) return BuildOf(place).BuildTicks;
        return BuiltAt(grid, plan.Cell, plan.Clear!) is { } built ? ApartTicks(built.BuildTicks) : 1;
    }

    /// <summary>The parts a blueprint's work is split into, each paid with one item as it begins: as many as its cost has items.</summary>
    public static int PartsOf(Cost cost) => cost.Sum(c => c.Count);

    /// <summary>
    /// How many of <paramref name="parts"/> parts of <paramref name="ticks"/> ticks of work have begun by the tick
    /// at <paramref name="progress"/>, that tick counted. Part k begins at <c>ticks * k / parts</c>, rounded down,
    /// so the first begins with the first tick; content gives every part a tick at least.
    /// </summary>
    public static int PartsBegun(int progress, int ticks, int parts) =>
        parts == 0 ? 0 : Math.Min(parts, ((progress + 1) * parts + ticks - 1) / ticks);

    /// <summary>What a blueprint still lacks of <paramref name="cost"/>, <paramref name="delivered"/> so far: the items with some still due, in the cost's order.</summary>
    public static Cost Lacking(Cost cost, IReadOnlyList<Stack> delivered)
    {
        var due = new List<(string Item, int Count)>();
        foreach (var (item, need) in cost)
        {
            int left = need - (delivered.FirstOrDefault(d => d.Item == item)?.Count ?? 0);
            if (left > 0) due.Add((item, left));
        }
        return due;
    }
}
