using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Hazards;

/// <summary>No state: meteors are struck by hand until locations bring them (see the README).</summary>
public sealed class HazardsState;

/// <summary>A meteor hits the wall at <paramref name="Cell"/>: from the dev panel, to try a breach and its repair.</summary>
public sealed record MeteorHit(int Cell) : Command;

/// <summary>Meteors breaking the hull (packages/sim/src/modules/hazards).</summary>
public sealed class HazardsModule : Module<HazardsState>
{
    private readonly IGridQueries _grid;

    public override string Id => "hazards";

    public HazardsModule(IGridQueries grid, IGridOps ops)
    {
        _grid = grid;
        Handles<MeteorHit>(c => HitProblem(c.Cell), c =>
        {
            // The wall goes; pipes are spans, running through it untouched (0109).
            ops.Clear(c.Cell, "structure");
        });
    }

    /// <summary>
    /// Why a meteor at <paramref name="cell"/> breaks nothing: no wall there (<c>hazards.noWall</c>), or one that never breaks, bedrock
    /// or lava, or in a planet's fog, out of the grid's zone (<c>hazards.neverBreaks</c>).
    /// </summary>
    private string? HitProblem(int cell)
    {
        if (_grid.WallAt(cell) is not { } wall) return "hazards.noWall";
        bool zone = _grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width);
        return wall.Strength == Content.Material.Unbreakable || !zone ? "hazards.neverBreaks" : null;
    }

    protected override HazardsState Init(Layout layout) => new();

    /// <summary>
    /// Meteors by hand, rarely: every hit opens the world a little more. Mostly at a standing wall, now
    /// and then anywhere: off the grid, into space, at a wall already gone.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(64) != 0) return null;
        var structure = _grid.StructureLayer;
        var walls = new List<int>();
        for (int c = 0; c < structure.Length; c++) if (Structures.Is(structure[c], Trait.Terrain)) walls.Add(c);
        int cell = rng.Below(3) != 0 && walls.Count > 0
            ? walls[(int)rng.Below((uint)walls.Count)]
            : (int)rng.Below((uint)structure.Length + 2) - 1;
        return new MeteorHit(cell) { From = from };
    }
}
