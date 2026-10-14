using System.Text.Json.Serialization;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Furnace;

/// <summary>
/// A furnace's fire and the item it smelts, by machine id: <c>Fuel</c> is the ticks of fire left, lit
/// while above 0; <c>Item</c> what it is at work on, with <c>Progress</c> its work on it in ticks of fire.
/// </summary>
public sealed record Fire(int Furnace, int Fuel, string? Item, int Progress);

/// <summary>
/// A furnace as its panel shows it: whether its fire burns; with salvage in its input, what it smelts
/// (<c>Item</c>), how far, out of how much work, and why it stands still if it does:
/// <c>furnace.full</c>, <c>furnace.noFuel</c> or <c>furnace.noAir</c>. With none, only <c>Burning</c>.
/// </summary>
public sealed record Smelting(bool Burning, string? Item = null, int Progress = 0, int Work = 0, string? Problem = null);

/// <summary>The furnace slice. Public so it can be named, its members internal: others read <see cref="FurnaceModule.At"/>.</summary>
public sealed class FurnaceState
{
    /// <summary>Every furnace burning or part-way through an item, by machine id, in id order.</summary>
    [JsonInclude] internal List<Fire> Fires { get; set; } = [];
}

/// <summary>
/// Smelting (ADR 0023): a furnace burns fuel and breathes its room's air, and turns the salvage in its
/// input into what it yields (packages/sim/src/modules/furnace).
/// </summary>
public sealed class FurnaceModule : Module<FurnaceState>
{
    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IAtmosphereQueries _air;
    private readonly IAtmosphereOps _airOps;

    public override string Id => "furnace";

    public FurnaceModule(IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IAtmosphereQueries air, IAtmosphereOps airOps)
    {
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _air = air;
        _airOps = airOps;
        System(Phase.Simulation, Burn);
    }

    protected override FurnaceState Init(Layout layout) => new();

    /// <summary>Furnace <paramref name="id"/> as its panel shows it.</summary>
    public Smelting At(int id)
    {
        var fire = State.Fires.Find(f => f.Furnace == id);
        bool burning = (fire?.Fuel ?? 0) > 0;
        var slots = _items.Crate(id);
        var input = SlotItem(slots, 0);
        int? work = input == null ? null : Content.Items.Get(input).SmeltTicks;
        if (input == null || work == null) return new Smelting(burning);
        var machine = _grid.Machines.FirstOrDefault(m => m.Id == id);
        bool aired = machine != null && AiredCell(machine) >= 0;
        var fuel = SlotItem(slots, Content.Furnace.FuelSlot) is { } f ? Content.Items.Get(f).BurnTicks : null;
        string? problem = !_items.CanSmelt(id) ? "furnace.full"
            : !burning && fuel == null ? "furnace.noFuel"
            : !aired ? "furnace.noAir"
            : null;
        int progress = fire?.Item == input ? fire.Progress : 0;
        return new Smelting(burning, input, progress, work.Value, problem);
    }

    private static string? SlotItem(IReadOnlyList<Stack?> slots, int slot) => slot < slots.Count ? slots[slot]?.Item : null;

    /// <summary>The first cell of machine <paramref name="m"/> with air in it, or -1: where its fire breathes.</summary>
    private int AiredCell(Machine m)
    {
        foreach (int c in _grid.Footprint(m.Kind, m.Cell) ?? [])
            if (_air.AiredAt(c)) return c;
        return -1;
    }

    private void Burn()
    {
        var kept = new List<Fire>();
        foreach (var m in _grid.Machines)
        {
            if (m.Kind != "furnace") continue;
            var was = State.Fires.Find(f => f.Furnace == m.Id);
            int fuel = was?.Fuel ?? 0;
            var slots = _items.Crate(m.Id);
            var input = SlotItem(slots, 0);
            int? work = input == null ? null : Content.Items.Get(input).SmeltTicks;
            bool smelts = work != null && _items.CanSmelt(m.Id);
            // Work on another item, or none, begins again.
            var item = was?.Item == input ? was?.Item : null;
            int progress = item == null ? 0 : was?.Progress ?? 0;
            int cell = AiredCell(m);
            // Lit only with something to smelt, fuel and air, which a fuel is used up for.
            var burns = SlotItem(slots, Content.Furnace.FuelSlot) is { } f ? Content.Items.Get(f).BurnTicks : null;
            if (fuel == 0 && smelts && burns != null && cell >= 0)
            {
                _itemsOps.Consume(m.Id, Content.Furnace.FuelSlot);
                fuel = burns.Value;
            }
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
                }
            }
            if (fuel > 0 || item != null) kept.Add(new Fire(m.Id, fuel, item, progress));
        }
        State.Fires = kept;
    }
}
