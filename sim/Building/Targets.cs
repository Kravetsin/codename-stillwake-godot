using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Building;

/// <summary>What a hit at a cell strikes (hit-targets).</summary>
public enum TargetKind
{
    /// <summary>The front wall at the cell.</summary>
    Wall,
    /// <summary>A built piece over the cell: a floor, ladder, platform, door or hatch.</summary>
    Piece,
    /// <summary>A machine over the cell, a pile and the lifepod among them.</summary>
    Machine,
    /// <summary>A plant in an open cell; one a wall holds is that wall's.</summary>
    Plant,
}

/// <summary>
/// What a hit at a cell strikes (hit-targets): its kind, the cell its crack is kept at (<c>Key</c>: a wall's own, a piece's
/// head, a machine's top-left, a plant's own), and its <c>Strength</c>, null for what nothing breaks (bedrock, lava, the
/// lifepod). <c>Wreck</c>: a pile, whose strength is in a weapon's damage, as a wreck's; everything else's in tenths.
/// </summary>
public readonly record struct HitTarget(TargetKind Kind, int Key, int? Strength, bool Wreck);

public sealed partial class BuildingModule
{
    /// <summary>
    /// What a hit at <paramref name="cell"/> strikes (hit-targets), the one answer every hitter and view reads: a wall, else a
    /// machine, else a built piece, else a plant in an open cell; none for open air, a grate and what is off the grid. The fog
    /// is each hitter's own rule.
    /// </summary>
    public HitTarget? TargetAt(int cell)
    {
        if (cell < 0 || cell >= _grid.Width * _grid.Height) return null;
        if (_grid.WallAt(cell) is { } wall) return new(TargetKind.Wall, cell, wall.Strength == Material.Unbreakable ? null : wall.Strength, false);
        if (_grid.MachineAt(cell) is { } machine)
        {
            var spec = Machines.Get(machine.Kind);
            return new(TargetKind.Machine, machine.Cell, spec.Strength, spec.Wreck);
        }
        if (PieceAt(cell) is { } piece) return new(TargetKind.Piece, piece.Head, piece.Build.Strength, false);
        if (_grid.StructureAt(cell) == Structure.Empty && PlantAt(cell) is { } plant) return new(TargetKind.Plant, plant.Cell, Kinds[plant.Kind].Strength, false);
        return null;
    }

    /// <summary>The built piece at <paramref name="cell"/>, a floor, ladder, door or hatch: its first cell, kept by, and its build; none for a wall or nothing.</summary>
    private (int Head, Build Build)? PieceAt(int cell)
    {
        int structure = _grid.StructureAt(cell);
        if (structure == Structure.Empty || _grid.WallAt(cell) != null || Structures.Of(structure).Build is not { } build) return null;
        return (_grid.PieceOf(cell)[0], Builds.Get(build));
    }
}
