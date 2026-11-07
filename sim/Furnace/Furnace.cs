using System.Text.Json.Serialization;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Furnace;

/// <summary>
/// A fire burning, a furnace's or a burner generator's, by machine id: <c>Fuel</c> is the ticks of fire left, above 0;
/// <c>Blown</c> while it takes its breaths from the tanks on its liquid input (industrial-oxygen).
/// </summary>
public sealed record Fire(int Machine, int Fuel, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Blown = false);

/// <summary>
/// A furnace or a burner generator as its panel shows it: whether its fire burns; for a furnace with
/// salvage in its input, what it smelts (<c>Item</c>), how far, out of how much work, both in thousandths of
/// a tick (the crafting module's measure); and why it stands still if it does: <c>crafting.tooCold</c> (what is
/// in its input needs more heat), <c>crafting.tooFew</c> (fewer than one smelt takes) or <c>furnace.full</c> (a furnace only), <c>furnace.noFuel</c> or <c>furnace.noAir</c>; and whether it is blown. A furnace with nothing to smelt says only <c>Burning</c> and that.
/// </summary>
public sealed record Smelting(bool Burning, string? Item = null, int Progress = 0, int Work = 0, string? Problem = null, bool Blown = false);

/// <summary>The furnace slice. Public so it can be named, its members internal: others read <see cref="FurnaceModule.At"/>.</summary>
public sealed class FurnaceState
{
    /// <summary>Every furnace or generator burning, by machine id, in id order.</summary>
    [JsonInclude] internal List<Fire> Fires { get; set; } = [];
}

/// <summary>
/// Fire (ADR 0023): a furnace or a burner generator burns fuel and breathes its room's air; a furnace's
/// fire heats it, so <c>Crafting</c> smelts what is in its input (0057), a generator's makes power while it
/// burns (packages/sim/src/modules/furnace, and the crafting plan's burner generator).
/// </summary>
public sealed class FurnaceModule : Module<FurnaceState>
{
    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IAtmosphereQueries _air;
    private readonly IAtmosphereOps _airOps;
    private readonly IPowerQueries _power;
    private readonly IPowerOps _powerOps;
    private readonly ICraftingQueries _crafting;
    private readonly ICraftingOps _craftingOps;

    public override string Id => "furnace";

    public FurnaceModule(
        IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IAtmosphereQueries air, IAtmosphereOps airOps,
        IPowerQueries power, IPowerOps powerOps, ICraftingQueries crafting, ICraftingOps craftingOps)
    {
        _power = power;
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _air = air;
        _airOps = airOps;
        _powerOps = powerOps;
        _crafting = crafting;
        _craftingOps = craftingOps;
        // Before power's derivation, so a generator just built, or one whose fire went out, makes nothing this
        // tick; before crafting's simulation, so a furnace smelts while its fire burns at the tick's start.
        System(Phase.Construction, () =>
        {
            RestCold();
            foreach (var m in _grid.MachinesWith(Content.MachineTrait.Fire))
            {
                var spec = Content.Machines.Get(m.Kind);
                var fire = FireOf(m.Id);
                // As the last tick left it, a blown fire works harder and reserves nothing (industrial-oxygen).
                bool blown = fire is { Fuel: > 0, Blown: true };
                if (spec.Makes != null) _powerOps.Blow(m.Id, blown);
                if (fire is not { Fuel: > 0 }) continue;
                // A fire with inputs smelts them (a furnace), not one that makes power.
                if (spec.Inputs > 0) _craftingOps.Heat(m.Id, blown ? Content.Furnace.Blown * 1000 / Content.Furnace.BlownOf : 1000);
                // Atmosphere, registered first, cleared last tick's reserves in this phase.
                if (!blown && AiredCell(m) is int cell and >= 0) _airOps.Reserve(cell);
            }
        });
        System(Phase.Simulation, Burn);
    }

    protected override FurnaceState Init(Layout layout) => new();

    /// <summary>Furnace or generator <paramref name="id"/> as its panel shows it.</summary>
    public Smelting At(int id)
    {
        bool burning = Burning(id);
        var machine = _grid.MachineById(id);
        if (machine == null) return new Smelting(burning);
        bool smelter = Content.Machines.Get(machine.Kind).Inputs > 0;
        var input = smelter ? SlotItem(_items.Crate(id), 0) : null;
        // What a furnace does not smelt, a seed (0060), sits in its input as if it held nothing.
        int? work = input == null ? null : _crafting.WorkOf(id);
        if (smelter && work == null) return new Smelting(burning);
        bool blown = FireOf(id) is { Blown: true };
        // A fire with a breath in its tanks needs no room air.
        bool aired = AiredCell(machine) >= 0 || blown || _air.LiquidFor(id) >= Content.Furnace.BlownBreath;
        string? problem = smelter && _crafting.TooCold(id) ? "crafting.tooCold"
            : smelter && _crafting.TooFew(id) ? "crafting.tooFew"
            : smelter && !_crafting.CanSmelt(id) ? "furnace.full"
            : !burning && FuelIn(machine) == null ? "furnace.noFuel"
            : !aired ? "furnace.noAir"
            : !smelter && !burning && !_power.Needed(id) ? "furnace.unneeded"
            : null;
        if (!smelter) return new Smelting(burning, Problem: problem, Blown: blown);
        return new Smelting(burning, input, _crafting.Progress(id), work!.Value, problem, blown);
    }

    private bool Burning(int id) => (FireOf(id)?.Fuel ?? 0) > 0;

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

    /// <summary>Every fire that makes power, a burner generator's, rests while it is out: power counts no resting maker.</summary>
    private void RestCold()
    {
        foreach (var m in _grid.MachinesWith(Content.MachineTrait.Fire))
            if (Content.Machines.Get(m.Kind).Makes != null) _powerOps.Rest(m.Id, !Burning(m.Id));
    }

    private void Burn()
    {
        var kept = new List<Fire>();
        var fires = State.Fires;
        int next = 0;
        // Every machine with a fire, in id order, as their fires are kept.
        foreach (var m in _grid.MachinesWith(Content.MachineTrait.Fire))
        {
            while (next < fires.Count && fires[next].Machine < m.Id) next++;
            var was = next < fires.Count && fires[next].Machine == m.Id ? fires[next] : null;
            if (Burn(m, was?.Fuel ?? 0, was?.Blown ?? false) is { } fire) kept.Add(fire);
        }
        State.Fires = kept;
    }

    /// <summary>Machine <paramref name="m"/>'s fire a tick on from <paramref name="fuel"/> left: null when it is out.</summary>
    private Fire? Burn(Machine m, int fuel, bool blown)
    {
        int cell = AiredCell(m);
        if (fuel == 0) (fuel, blown) = Light(m, cell);
        if (fuel == 0) return null;
        if (fuel % Content.Furnace.BreathEvery == 0)
        {
            // Each breath decides it (industrial-oxygen): from its tanks while they hold one, blown, else from the room as a
            // hero does. Turning, the fuel left is scaled, rounded down, so a fuel lasts longer only while blown.
            bool fromTanks = _airOps.BurnLiquid(m.Id, Content.Furnace.BlownBreath);
            if (fromTanks != blown)
            {
                fuel = Math.Max(1, fromTanks ? Blown(fuel) : fuel * Content.Furnace.BlownOf / Content.Furnace.Blown);
                blown = fromTanks;
            }
            // Given less, its fire goes out and the fuel left is lost.
            if (!fromTanks && (cell < 0 || _airOps.Breathe(cell, Content.Furnace.Breath) < Content.Furnace.Breath)) return null;
        }
        fuel -= 1;
        // Its last tick of fuel burnt, the next is lit at once: a fire fed in time never blinks out.
        if (fuel == 0) (fuel, blown) = Light(m, cell);
        return fuel > 0 ? new Fire(m.Id, fuel, blown) : null;
    }

    /// <summary>Ticks of fuel blown: <c>Furnace.Blown</c> / <c>Furnace.BlownOf</c> of them, rounded down.</summary>
    private static int Blown(int ticks) => ticks * Content.Furnace.Blown / Content.Furnace.BlownOf;

    /// <summary>
    /// Lights machine <paramref name="m"/>'s fire, breathing at <paramref name="cell"/> or blown by a breath in its tanks, if
    /// it can: with fuel, a furnace only with what it can smelt (<c>ICraftingQueries.CanSmelt</c>). One fuel is used up
    /// for it, and the ticks it burns for come back, more if blown; 0 when it does not light.
    /// </summary>
    private (int Fuel, bool Blown) Light(Machine m, int cell)
    {
        bool blown = _air.LiquidFor(m.Id) >= Content.Furnace.BlownBreath;
        if ((cell < 0 && !blown) || FuelIn(m) is not int burns) return (0, false);
        var spec = Content.Machines.Get(m.Kind);
        if (spec.Inputs > 0 && !_crafting.CanSmelt(m.Id)) return (0, false);
        // A generator burns only what its network would take: a fuel lit burns to its end, the next waits (0101).
        if (spec.Makes != null && !_power.Needed(m.Id)) return (0, false);
        _itemsOps.Consume(m.Id, spec.FuelSlot!.Value);
        return (blown ? Blown(burns) : burns, blown);
    }
}
