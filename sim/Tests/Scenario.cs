using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Tests;

public sealed record Place(int Cell, Placement Placement) : Command;
public sealed record ClearAt(int Cell, string Layer) : Command;

/// <summary>
/// Builds and clears at once, for tests: what building does through a hero's work. With building in
/// the world it builds nowhere a blueprint claims, as no build can: the blueprint's work would throw.
/// </summary>
public sealed class Builder : Module<Builder.Nothing>
{
    public sealed class Nothing;
    public override string Id => "builder";

    public Builder(IGridQueries grid, IGridOps ops, BuildingModule? building = null)
    {
        Handles<Place>(c => grid.PlacementProblem(c.Cell, c.Placement) ?? Claimed(grid, building, c), c => ops.Place(c.Cell, c.Placement));
        Handles<ClearAt>(_ => null, c => ops.Clear(c.Cell, c.Layer));
    }

    /// <summary>A builder for the world of <paramref name="m"/>, respecting its blueprints.</summary>
    public Builder(Registry m) : this(m.Grid, m.Grid, m.Building) { }

    /// <summary>Structure and machines share one slot; a conduit and a back wall each have their own (building's rule).</summary>
    private static string SlotOf(string layer) => layer is "conduit" or "back" ? layer : "solid";

    /// <summary><c>building.planned</c> when a blueprint claims the slot of a cell <paramref name="c"/> would fill.</summary>
    private static string? Claimed(IGridQueries grid, BuildingModule? building, Place c)
    {
        if (building == null) return null;
        var cells = c.Placement.Layer == "machine" ? grid.Footprint(c.Placement.Kind, c.Cell) ?? [c.Cell] : [c.Cell];
        string slot = SlotOf(c.Placement.Layer);
        bool claimed = cells.Any(cell => building.PlansAt(cell).Any(p => p.Place is { } other && SlotOf(other.Layer) == slot));
        return claimed ? "building.planned" : null;
    }

    protected override Nothing Init(Layout layout) => new();
}

/// <summary>
/// A world of every registered module and the test <see cref="Builder"/> on an ASCII map, as
/// packages/sim/src/testing/scenario.ts. Modules are read through <see cref="M"/>: <c>w.M.Rooms</c>.
/// </summary>
public sealed class Scenario
{
    public World World { get; private set; }
    public Registry M { get; private set; } = null!;
    public Layout Layout { get; }
    public List<Refused> Refusals { get; } = [];
    private readonly List<Command> _pending = [];
    private readonly Func<Registry, IEnumerable<Module>>? _with;

    public Scenario(string map, uint seed = 1, Func<Registry, IEnumerable<Module>>? with = null)
        : this(Layout.Parse(map), seed, with) { }

    /// <param name="with">Test modules run after the registered ones, made anew on every load.</param>
    public Scenario(Layout layout, uint seed = 1, Func<Registry, IEnumerable<Module>>? with = null)
    {
        Layout = layout;
        _with = with;
        World = World.Create(seed, layout, Modules());
    }

    /// <summary>A fresh registry, the builder after it, then the scenario's own test modules.</summary>
    public IReadOnlyList<Module> Modules()
    {
        M = new Registry();
        return [.. M.All, new Builder(M), .. _with?.Invoke(M) ?? []];
    }

    public int At(int x, int y) => y * Layout.Width + x;
    public Scenario Do(Command c) { _pending.Add(c); return this; }

    public Scenario Run(int ticks = 1)
    {
        for (int i = 0; i < ticks; i++)
        {
            Refusals.AddRange(World.Step(_pending.ToList()));
            _pending.Clear();
        }
        return this;
    }

    /// <summary>The world saved and loaded into fresh modules, which this scenario reads from then on.</summary>
    public Scenario Reload()
    {
        World = World.Load(World.Save(), Modules());
        return this;
    }
}
