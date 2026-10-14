using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Research;

/// <summary>A blueprint: its id, as the build menu names its card, and what it places.</summary>
public sealed record Blueprint(string Id, Placement Placement);

/// <summary>What blueprints there are and their names (blueprints.ts).</summary>
public static class Blueprints
{
    /// <summary>The blueprint <paramref name="placement"/> is built by, named as the build menu names its card (<c>wallIron</c>).</summary>
    public static string Of(Placement placement) =>
        placement.Kind is "wall" or "backWall" ? placement.Kind + Titled(placement.Material ?? "") : placement.Kind;

    private static string Titled(string id) => id.Length == 0 ? "" : char.ToUpperInvariant(id[0]) + id[1..];

    /// <summary>Every blueprint there is, with what it places: walls and back walls by material, builds, machines.</summary>
    public static readonly IReadOnlyList<Blueprint> All = ((Placement[])
    [
        .. Content.Materials.All.Select(m => Placement.Wall(m.Id)),
        .. Content.Materials.All.Select(m => Placement.BackWall(m.Id)),
        .. Content.Builds.All.Select(b => new Placement(b.Id is "pipe" or "valve" ? "conduit" : "structure", b.Id)),
        .. Content.Machines.All.Select(m => new Placement("machine", m.Kind)),
    ]).Select(p => new Blueprint(Of(p), p)).ToArray();
}
