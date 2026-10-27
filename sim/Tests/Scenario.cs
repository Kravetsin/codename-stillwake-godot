using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
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

    /// <summary>Structure and machines share one slot; a back wall has its own (building's rule).</summary>
    private static string SlotOf(string layer) => layer is "back" ? layer : "solid";

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

    /// <summary>
    /// A world on <paramref name="map"/>, drawn a character a 16 px cell and doubled to the 8 px cells (<see cref="Layout.Doubled"/>,
    /// 0104), as the wreck and the strip are: machines, bodies and distances keep their sizes. A test of a rule per cell (a dug
    /// cell, rooms, pipes, air) passes <paramref name="cells"/> and reads its map a character a cell.
    /// </summary>
    public Scenario(string map, uint seed = 1, Func<Registry, IEnumerable<Module>>? with = null, bool cells = false)
        : this(Layout.Parse(map), seed, with, doubled: !cells) { }

    /// <param name="with">Test modules run after the registered ones, made anew on every load.</param>
    /// <param name="doubled">
    /// Whether <paramref name="layout"/> is drawn a character a 16 px cell, to be doubled, its characters' cells what
    /// <see cref="At"/> names; else it is the world's own, as <c>Registry.Wreck</c> and a grown ground are.
    /// </param>
    public Scenario(Layout layout, uint seed = 1, Func<Registry, IEnumerable<Module>>? with = null, bool doubled = false)
    {
        Scale = doubled ? 2 : 1;
        Layout = doubled ? layout.Doubled() : layout;
        _with = with;
        World = World.Create(seed, Layout, Modules());
    }

    /// <summary>The cells a character of the map is across and down: 2 on a doubled map, else 1.</summary>
    public int Scale { get; }

    /// <summary>
    /// A fresh registry, the builder after it, then the scenario's own test modules: the ship's. Another surface opened gets
    /// a registry alone (0076).
    /// </summary>
    public IReadOnlyList<Module> Modules()
    {
        M = new Registry();
        return new Ship(M.All, [.. M.All, new Builder(M), .. _with?.Invoke(M) ?? []]);
    }

    /// <summary>The registry surface <paramref name="surface"/> runs: <see cref="M"/> for the ship.</summary>
    public Registry On(int surface) => surface == 0 ? M : Registry.On(World, surface);

    /// <summary>The ship's modules, test ones with them, making other surfaces as its registry does.</summary>
    private sealed class Ship(IModules registry, Module[] modules) : IModules
    {
        public Module this[int index] => modules[index];
        public int Count => modules.Length;
        public IModules Another() => registry.Another();
        public IEnumerator<Module> GetEnumerator() => ((IEnumerable<Module>)modules).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => modules.GetEnumerator();
    }

    /// <summary>The cell of the map's character at (<paramref name="x"/>, <paramref name="y"/>): its block's top-left on a doubled map.</summary>
    public int At(int x, int y) => y * Scale * Layout.Width + x * Scale;
    /// <summary>
    /// Heroes come in bare, as a peer's first hero does in the game. Otherwise (the default) a peer's first <see cref="Enter"/>
    /// on the ship is preceded by <see cref="Suit"/>, so a scenario's heroes are suited, their air full, as before suits
    /// were worn (2026-10-27-suits-are-worn).
    /// </summary>
    public bool Bare { get; init; }

    private readonly HashSet<int> _dressed = [];

    public Scenario Do(Command c)
    {
        // Only a peer seated aboard: one carried to another surface keeps what it carries there.
        if (c is Enter && !Bare && World.SurfaceOf(c.From) == 0 && M.Crew.HeroOf(c.From) == null && _dressed.Add(c.From)) Suit(c.From);
        _pending.Add(c);
        return this;
    }

    /// <summary>The basic helmet's tank: a suited hero's full air.</summary>
    public static int Full => Content.Items.Get(Content.Helmet.Basic).Tank;

    /// <summary>
    /// Peer <paramref name="peer"/> wears the basic helmet and body, put straight into their slots (<see cref="ItemsTesting.Wear"/>), its kit
    /// given first as its first hero would get it. Before the peer's first <see cref="Enter"/>, that prints the hero suited, its
    /// air full; after it, the air fills as a suit put on fills.
    /// </summary>
    public Scenario Suit(int peer)
    {
        _dressed.Add(peer);
        M.Items.Kit(peer);
        if (M.Items.WornIn(peer, Content.Helmet.Slot) == null) this.Wear(peer, Content.Helmet.Basic);
        return M.Items.WornIn(peer, Content.Suit.Slot) == null ? this.Wear(peer, Content.Suit.Basic) : this;
    }

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
    public Scenario Reload() => Reload(World.Save());

    /// <summary><paramref name="save"/> loaded into fresh modules, which this scenario reads from then on.</summary>
    public Scenario Reload(string save)
    {
        World = World.Load(save, Modules());
        return this;
    }
}
