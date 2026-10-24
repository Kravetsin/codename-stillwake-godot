using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Landing;

/// <summary>Nothing saved yet: a platform's state is its structure codes, the grid's. The drop pod's seats come here.</summary>
public sealed class LandingState;

/// <summary>
/// Opens (<c>Open</c>) or shuts the launch platform <c>Cell</c> is in, its whole run: what its sender saw it should become,
/// as <see cref="SetDoor"/> is for a door. E on a platform sends it.
/// </summary>
public sealed record SetPlatform(int Cell, bool Open) : Command;

/// <summary>
/// The launch platform and, to come, the drop pod and its landers (the 2026-10-24 launch platform ADR): a run of platform
/// pieces opens whole, only while its network has power enough, and shuts with or without.
/// </summary>
public sealed class LandingModule : Module<LandingState>
{
    private readonly IGridQueries _grid;
    private readonly IPowerQueries _power;

    public override string Id => "landing";

    public LandingModule(IGridQueries grid, IGridOps gridOps, IPowerQueries power)
    {
        _grid = grid;
        _power = power;
        Handles<SetPlatform>(c => Problem(c.Cell, c.Open), c => gridOps.SetDoorway(c.Cell, c.Open));
    }

    protected override LandingState Init(Layout layout) => new();

    /// <summary>
    /// Why the platform at <paramref name="cell"/> cannot be set <paramref name="open"/> now: no platform there
    /// (<c>landing.noPlatform</c>), or opening a run whose network has not power enough (<c>landing.unpowered</c>); null when
    /// it can. Shutting needs no power, so a run left open on a network gone dark can still be shut.
    /// </summary>
    public string? Problem(int cell, bool open)
    {
        if (!Doorways.IsPlatform(_grid.StructureAt(cell))) return "landing.noPlatform";
        return open && !Powered(cell) ? "landing.unpowered" : null;
    }

    /// <summary>Whether the run of the platform at <paramref name="cell"/> has power enough: a cable on any of its cells fed (<see cref="IPowerQueries.Feeds"/>).</summary>
    public bool Powered(int cell)
    {
        var (first, length) = RunOf(_grid, cell);
        for (int i = 0; i < length; i++)
            if (_power.Feeds(first + i)) return true;
        return false;
    }

    /// <summary>The run of platform pieces <paramref name="cell"/> is in, shut and open alike: its first cell and how many cells it runs.</summary>
    public static (int First, int Length) RunOf(IGridQueries grid, int cell) =>
        StructureRuns.RunOf(cell, grid.Width, c => Doorways.IsPlatform(grid.StructureAt(c)) ? 1 : 0);

    /// <summary>Now and then a platform opened or shut, mostly on a platform, sometimes anywhere, so refusals are drawn too.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(8) != 0) return null;
        var structure = _grid.StructureLayer;
        var platforms = new List<int>();
        for (int c = 0; c < structure.Length; c++) if (Doorways.IsPlatform(structure[c])) platforms.Add(c);
        int cell = platforms.Count > 0 && rng.Below(4) != 0
            ? platforms[(int)rng.Below((uint)platforms.Count)]
            : (int)rng.Below((uint)structure.Length + 2) - 1;
        return new SetPlatform(cell, rng.Below(2) == 0) { From = from };
    }
}
