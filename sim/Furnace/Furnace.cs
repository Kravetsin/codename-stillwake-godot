using System.Text.Json.Serialization;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Furnace;

/// <summary>
/// A fire, a furnace's or a burner generator's, by machine id: <c>Fuel</c> is the ticks of fire left,
/// lit while above 0; <c>Item</c> what a furnace is at work on, with <c>Progress</c> its work on it in
/// ticks of fire. A generator smelts nothing: its <c>Item</c> is always null.
/// </summary>
public sealed record Fire(int Machine, int Fuel, string? Item, int Progress);

/// <summary>
/// A furnace or a burner generator as its panel shows it: whether its fire burns; for a furnace with
/// salvage in its input, what it smelts (<c>Item</c>), how far, out of how much work; and why it stands
/// still if it does: <c>furnace.full</c> (a furnace only), <c>furnace.noFuel</c> or <c>furnace.noAir</c>.
/// A furnace with nothing to smelt says only <c>Burning</c>.
/// </summary>
public sealed record Smelting(bool Burning, string? Item = null, int Progress = 0, int Work = 0, string? Problem = null);

/// <summary>The furnace slice. Public so it can be named, its members internal: others read <see cref="FurnaceModule.At"/>.</summary>
public sealed class FurnaceState
{
    /// <summary>Every furnace or generator burning, or a furnace part-way through an item, by machine id, in id order.</summary>
    [JsonInclude] internal List<Fire> Fires { get; set; } = [];
}

/// <summary>
/// Fire (ADR 0023): a furnace or a burner generator burns fuel and breathes its room's air; a furnace
/// turns the salvage in its input into what it yields, a generator makes power while it burns
/// (packages/sim/src/modules/furnace, and the crafting plan's burner generator).
/// </summary>
public sealed class FurnaceModule : Module<FurnaceState>
{
    /// <summary>The two machines with a fire.</summary>
    public const string Smelter = "furnace", Generator = "burnerGenerator";

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IAtmosphereQueries _air;
    private readonly IAtmosphereOps _airOps;
    private readonly IPowerOps _powerOps;

    public override string Id => "furnace";

    public FurnaceModule(IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IAtmosphereQueries air, IAtmosphereOps airOps, IPowerOps powerOps)
    {
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _air = air;
        _airOps = airOps;
        _powerOps = powerOps;
        // Before power's derivation, so a generator just built, or one whose fire went out, makes nothing this tick.
        System(Phase.Construction, RestCold);
        System(Phase.Simulation, Burn);
    }

    protected override FurnaceState Init(Layout layout) => new();

    /// <summary>Furnace or generator <paramref name="id"/> as its panel shows it.</summary>
    public Smelting At(int id)
    {
        var fire = FireOf(id);
        bool burning = (fire?.Fuel ?? 0) > 0;
        var machine = _grid.MachineById(id);
        if (machine == null) return new Smelting(burning);
        var slots = _items.Crate(id);
        bool smelter = machine.Kind == Smelter;
        var input = smelter ? SlotItem(slots, 0) : null;
        int? work = input == null ? null : Content.Items.Get(input).SmeltTicks;
        if (smelter && work == null) return new Smelting(burning);
        bool aired = AiredCell(machine) >= 0;
        string? problem = smelter && !_items.CanSmelt(id) ? "furnace.full"
            : !burning && FuelIn(machine) == null ? "furnace.noFuel"
            : !aired ? "furnace.noAir"
            : null;
        if (!smelter) return new Smelting(burning, Problem: problem);
        int progress = fire is { } f && f.Item == input ? f.Progress : 0;
        return new Smelting(burning, input, progress, work!.Value, problem);
    }

    private static string? SlotItem(IReadOnlyList<Stack?> slots, int slot) => slot < slots.Count ? slots[slot]?.Item : null;

    /// <summary>The ticks one of the fuel in machine <paramref name="m"/>'s fuel slot burns for, or null for none there.</summary>
    private int? FuelIn(Machine m) =>
        SlotItem(_items.Crate(m.Id), Content.Machines.Get(m.Kind).FuelSlot!.Value) is { } f ? Content.Items.Get(f).BurnTicks : null;

    /// <summary>Machine <paramref name="id"/>'s fire, or null.</summary>
    private Fire? FireOf(int id)
    {
        var fires = State.Fires;
        int lo = 0, hi = fires.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (fires[mid].Machine == id) return fires[mid];
            if (fires[mid].Machine < id) lo = mid + 1;
            else hi = mid - 1;
        }
        return null;
    }

    /// <summary>The first cell of machine <paramref name="m"/> with air in it, in reading order, or -1: where its fire breathes.</summary>
    private int AiredCell(Machine m)
    {
        var spec = Content.Machines.Get(m.Kind);
        for (int dy = 0; dy < spec.Height; dy++)
            for (int dx = 0; dx < spec.Width; dx++)
            {
                int c = m.Cell + dy * _grid.Width + dx;
                if (_air.AiredAt(c)) return c;
            }
        return -1;
    }

    /// <summary>Every generator rests while its fire is out: power counts no resting maker.</summary>
    private void RestCold()
    {
        foreach (var m in _grid.MachinesOf(Generator)) _powerOps.Rest(m.Id, (FireOf(m.Id)?.Fuel ?? 0) == 0);
    }

    private void Burn()
    {
        var kept = new List<Fire>();
        var fires = State.Fires;
        int next = 0;
        IReadOnlyList<Machine> smelters = _grid.MachinesOf(Smelter), generators = _grid.MachinesOf(Generator);
        int s = 0, g = 0;
        // Both kinds in one pass in id order, as their fires are kept.
        while (s < smelters.Count || g < generators.Count)
        {
            var m = g >= generators.Count || (s < smelters.Count && smelters[s].Id < generators[g].Id) ? smelters[s++] : generators[g++];
            while (next < fires.Count && fires[next].Machine < m.Id) next++;
            var was = next < fires.Count && fires[next].Machine == m.Id ? fires[next] : null;
            if (Burn(m, was) is { } fire) kept.Add(fire);
        }
        State.Fires = kept;
    }

    /// <summary>Machine <paramref name="m"/>'s fire a tick on from <paramref name="was"/>: null when it is out with nothing in hand.</summary>
    private Fire? Burn(Machine m, Fire? was)
    {
        int fuel = was?.Fuel ?? 0;
        bool smelter = m.Kind == Smelter;
        var input = smelter ? SlotItem(_items.Crate(m.Id), 0) : null;
        int? work = input == null ? null : Content.Items.Get(input).SmeltTicks;
        bool smelts = work != null && _items.CanSmelt(m.Id);
        // Work on another item, or none, begins again.
        var item = was?.Item == input ? was?.Item : null;
        int progress = item == null ? 0 : was?.Progress ?? 0;
        int cell = AiredCell(m);
        if (fuel == 0) fuel = Light(m, cell);
        if (fuel > 0)
        {
            // It breathes as a hero does: given less, its fire goes out and the fuel left is lost.
            bool breathes = fuel % Content.Furnace.BreathEvery == 0;
            int had = !breathes ? Content.Furnace.Breath
                : cell < 0 ? 0
                : _airOps.Breathe(cell, Content.Furnace.Breath);
            if (had < Content.Furnace.Breath) fuel = 0;
            else
            {
                fuel -= 1;
                if (smelts && input != null)
                {
                    item = input;
                    progress += 1;
                    if (progress >= work)
                    {
                        _itemsOps.Smelt(m.Id);
                        item = null;
                        progress = 0;
                    }
                }
                // Its last tick of fuel burnt, the next is lit at once: a fire fed in time never blinks out.
                if (fuel == 0) fuel = Light(m, cell);
            }
        }
        return fuel > 0 || item != null ? new Fire(m.Id, fuel, item, progress) : null;
    }

    /// <summary>
    /// Lights machine <paramref name=m/>'s fire, breathing at <paramref name=cell/>, if it can: with fuel
    /// and air, a furnace only with salvage to smelt and room for its yields. One fuel is used up for it,
    /// and the ticks it burns for come back; 0 when it does not light.
    /// </summary>
    private int Light(Machine m, int cell)
    {
        if (cell < 0 || FuelIn(m) is not int burns) return 0;
        if (m.Kind == Smelter && !(SlotItem(_items.Crate(m.Id), 0) is { } input && Content.Items.Get(input).SmeltTicks != null && _items.CanSmelt(m.Id)))
            return 0;
        _itemsOps.Consume(m.Id, Content.Machines.Get(m.Kind).FuelSlot!.Value);
        return burns;
    }
}
