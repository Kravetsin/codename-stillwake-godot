using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Fuel;

/// <summary>Takes <c>Count</c> litres out of machine <c>Machine</c>'s fuel store as cans into the sender's inventory, one at least.</summary>
public sealed record DrawFuel(int Machine, int Count = 1) : Command;

/// <summary>The fuel's slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class FuelState
{
    /// <summary>The biofuel in each store holding any, in millilitres, by machine id, in id order.</summary>
    [JsonInclude] internal List<Stored> Stores { get; set; } = [];
}

/// <summary>What any module may read of the fuel.</summary>
public interface IFuelQueries
{
    /// <summary>The biofuel in machine <paramref name="machine"/>'s store, in millilitres; none for one never filled, or no store.</summary>
    int FuelIn(int machine);
    /// <summary>What machine <paramref name="machine"/>'s store has room for; none for a full one, one loaded fuller, or no store.</summary>
    int RoomIn(int machine);
    /// <summary>Why peer <paramref name="peer"/> could not draw <paramref name="count"/> cans out of machine <paramref name="machine"/>: no store, less fuel in it, or no room.</summary>
    string? DrawProblem(int machine, int peer, int count = 1);
}

/// <summary>What the modules that make, move and burn fuel may do to the stores.</summary>
public interface IFuelOps
{
    /// <summary>Puts up to <paramref name="amount"/> into machine <paramref name="machine"/>'s store, as much as it has room for; returns what went in.</summary>
    int Fill(int machine, int amount);
    /// <summary>Takes up to <paramref name="amount"/> out of machine <paramref name="machine"/>'s store; returns what it took.</summary>
    int Take(int machine, int amount);
    /// <summary>Empties machine <paramref name="machine"/>'s store, a pod's before it leaves the grid; returns what it held.</summary>
    int TakeAll(int machine);
}

/// <summary>
/// Fuel (fuel-liquid): the biofuel every machine with a store holds — a fuel tank, the drop pod, the biofuel generator, the engine,
/// the fermenter — poured in from its hopper by the can, piped on from store to store, drawn out as cans. The fermenter fills its
/// store through Crafting, the generator burns from it through Furnace, the pod through Landing.
/// </summary>
public sealed partial class FuelModule : Module<FuelState>, IFuelQueries, IFuelOps
{
    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly IPipesQueries _pipes;
    private readonly IPowerOps _powerOps;

    public override string Id => "fuel";

    public FuelModule(IGridQueries grid, IItemsQueries items, IItemsOps itemsOps, IPipesQueries pipes, IPowerOps powerOps)
    {
        _powerOps = powerOps;
        _grid = grid;
        _items = items;
        _itemsOps = itemsOps;
        _pipes = pipes;
        Handles<DrawFuel>(c => DrawProblem(c.Machine, c.From, c.Count), c =>
        {
            int count = Math.Max(1, c.Count);
            itemsOps.Give(c.From, [(Content.Fuel.Item, count)]);
            Stores.Take(State.Stores, c.Machine, count * Content.Fuel.Can);
        });
        // A store taken apart loses its fuel, as a cistern its water: a few stores at most, looked at every tick.
        System(Phase.Construction, () =>
        {
            State.Stores.RemoveAll(s => _grid.MachineById(s.Machine) is not { } m || !Machines.Is(m.Kind, MachineTrait.HoldsFuel));
            RestEmpty();
        });
        System(Phase.Simulation, () =>
        {
            Pour();
            PourHoppers();
        });
    }

    protected override FuelState Init(Layout layout) => new();

    public int FuelIn(int machine) => Stores.AmountIn(State.Stores, machine);

    public int RoomIn(int machine) => Math.Max(0, CapacityOf(machine) - FuelIn(machine));

    public string? DrawProblem(int machine, int peer, int count = 1)
    {
        // A count under one is no rule of the game: it is taken as one.
        count = Math.Max(1, count);
        if (CapacityOf(machine) == 0) return "fuel.noStore";
        if (FuelIn(machine) < (long)count * Content.Fuel.Can) return "fuel.low";
        return _items.FullFor(peer, [(Content.Fuel.Item, count)]);
    }

    public int Fill(int machine, int amount)
    {
        int put = Math.Min(amount, RoomIn(machine));
        Stores.Add(State.Stores, machine, put);
        return Math.Max(0, put);
    }

    public int Take(int machine, int amount) => Stores.Take(State.Stores, machine, amount);

    public int TakeAll(int machine) => Stores.Take(State.Stores, machine, int.MaxValue);

    /// <summary>
    /// Every store that draws power and is no station (the engine: its pumps and igniter kept ready) rests while it holds no
    /// fuel, before power's derivation; a station (the fermenter) is rested by Crafting.
    /// </summary>
    private void RestEmpty()
    {
        foreach (var m in _grid.MachinesWith(MachineTrait.HoldsFuel))
            if (Machines.Get(m.Kind).Draws != null && !Stations.TryGet(m.Kind, out _)) _powerOps.Rest(m.Id, FuelIn(m.Id) == 0);
    }

    /// <summary>What machine <paramref name="machine"/>'s store holds full; none for no machine or no store.</summary>
    private int CapacityOf(int machine) => _grid.MachineById(machine) is { } m ? Machines.Get(m.Kind).FuelCapacity : 0;

    /// <summary>
    /// The lines (tanks-in-a-line): each store in id order holding fuel gives up to <c>Fuel.Flow</c> of its own to the stores whose
    /// fuel input stands on its fuel output's network, in id order, as each has room.
    /// </summary>
    private void Pour() =>
        Stores.Pour(State.Stores, _grid.MachinesWith(MachineTrait.HoldsFuel).Select(m => m.Id),
            id => _pipes.MachinesOn(MachineTrait.HoldsFuel, Way.In, Fluids.Fuel, _pipes.NetworkOf(_grid.MachineById(id)!, Way.Out, Fluids.Fuel)),
            CapacityOf, Content.Fuel.Flow);

    /// <summary>Each store in id order absorbs one can from its hopper (<c>SlotTakes</c>) when a litre fits, after the lines pour.</summary>
    private void PourHoppers()
    {
        foreach (var m in _grid.MachinesWith(MachineTrait.HoldsFuel))
        {
            if (Machines.Get(m.Kind).SlotTakes is not (int slot, string item)) continue;
            var slots = _items.Crate(m.Id);
            if (slot >= slots.Count || slots[slot]?.Item != item || RoomIn(m.Id) < Content.Fuel.Can) continue;
            _itemsOps.Consume(m.Id, slot);
            Stores.Add(State.Stores, m.Id, Content.Fuel.Can);
        }
    }
}
