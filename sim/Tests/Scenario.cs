using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Tests;

public sealed record Place(int Cell, Placement Placement) : Command;
public sealed record ClearAt(int Cell, string Layer) : Command;

/// <summary>Builds and clears at once, for tests: what building does through a hero's work.</summary>
public sealed class Builder : Module<Builder.Nothing>
{
    public sealed class Nothing;
    public override string Id => "builder";

    public Builder(IGridQueries grid, IGridOps ops)
    {
        Handles<Place>(c => grid.PlacementProblem(c.Cell, c.Placement), c => ops.Place(c.Cell, c.Placement));
        Handles<ClearAt>(_ => null, c => ops.Clear(c.Cell, c.Layer));
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

    public Scenario(string map, uint seed = 1) : this(Layout.Parse(map), seed) { }

    public Scenario(Layout layout, uint seed = 1)
    {
        Layout = layout;
        World = World.Create(seed, layout, Modules());
    }

    /// <summary>A fresh registry, and the builder after it.</summary>
    public IReadOnlyList<Module> Modules()
    {
        M = new Registry();
        return [.. M.All, new Builder(M.Grid, M.Grid)];
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
