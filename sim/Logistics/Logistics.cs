using Stillwake.Content;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Logistics;

/// <summary>Tells drones to serve machine <c>Machine</c>, the default, or to leave it alone: neither take from it nor bring to it.</summary>
public sealed record SetServed(int Machine, bool On) : Command;

/// <summary>
/// Drone logistics (0062): drones keep the machines in their bay's zone going with nothing set. An input holding an
/// item is refilled with it from crates, a queued station's head entry fed from its inputs gets what it needs, and what
/// machines make goes to crates; drones fly through air and doors, chain job after job, and fly home when there is no
/// work or their charge runs low. A machine switched off is left alone.
/// </summary>
public sealed partial class LogisticsModule : Module<LogisticsState>
{
    private readonly IGridQueries _grid;
    private readonly IAtmosphereQueries _air;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    private readonly ICraftingQueries _crafting;

    public override string Id => "logistics";

    public LogisticsModule(IGridQueries grid, IAtmosphereQueries air, IItemsQueries items, IItemsOps itemsOps, ICraftingQueries crafting)
    {
        _grid = grid;
        _air = air;
        _items = items;
        _itemsOps = itemsOps;
        _crafting = crafting;
        Handles<SetServed>(ServedProblem, ApplyServed);
        // After the air's simulation, so a drone reads the air of this tick; drones fly before bays look.
        System(Phase.Simulation, () =>
        {
            State.Ticks += 1;
            Fly();
            Look();
        });
    }

    protected override LogisticsState Init(Layout layout) => new();

    // Queries.

    /// <summary>Whether drones serve machine <paramref name="machine"/>: every machine but one switched off.</summary>
    public bool Served(int machine) => State.Off.BinarySearch(machine) < 0;

    /// <summary>
    /// Whether drones ever have work at machine <paramref name="machine"/>, served or not: a crate, a machine with outputs,
    /// one whose inputs or fuel they refill. Not a bay, the analyzer or the charging station: where the badge shows.
    /// </summary>
    public bool Servable(int machine) => _grid.MachineById(machine) is { } m && (m.Kind == "crate" || Takes(m) || OutputsOf(m) != null);

    /// <summary>Whether the drones of some bay reach machine <paramref name="machine"/>: through air and doors inside its zone.</summary>
    public bool Reached(int machine)
    {
        if (_grid.MachineById(machine) is not { } target) return false;
        foreach (var bay in _grid.MachinesOf(Tune.Bay))
            if (Flood(bay) && InFlood(target)) return true;
        return false;
    }

    /// <summary>
    /// Whether drones could bring <paramref name="item"/> to machine <paramref name="machine"/>: a bay reaches it, and a
    /// served crate or output in that bay's reach holds some, taken or not. What a remembered slot's tooltip says.
    /// </summary>
    public bool Supplied(int machine, string item)
    {
        if (_grid.MachineById(machine) is not { } target) return false;
        foreach (var bay in _grid.MachinesOf(Tune.Bay))
        {
            if (!Flood(bay) || !InFlood(target)) continue;
            foreach (var m in _grid.Machines)
            {
                if (m.Id == machine || FirstOffered(m) is not int start || !Served(m.Id) || !InFlood(m)) continue;
                var slots = _items.Crate(m.Id);
                for (int slot = start; slot < slots.Count; slot++)
                    if (slots[slot]?.Item == item) return true;
            }
        }
        return false;
    }

    /// <summary>The drones in flight, in id order; live and read-only.</summary>
    public IReadOnlyList<Drone> Drones => State.Drones;

    /// <summary>Whether drone <paramref name="drone"/> flies home for want of charge, work left behind: it charges before it flies again.</summary>
    public static bool Low(Drone drone) => drone.Leg == Leg.Home && drone.Low;

    /// <summary>The zone of bay <paramref name="bay"/>, its edges included; null for no bay by that id.</summary>
    public Box? ZoneOf(int bay) => _grid.MachineById(bay) is { } m && m.Kind == Tune.Bay ? ZoneBox(m) : null;

    // Commands.

    private string? ServedProblem(SetServed c) => _grid.MachineById(c.Machine) switch
    {
        null => "logistics.noMachine",
        { Kind: var kind } when kind == Tune.Bay => "logistics.bay",
        _ => null,
    };

    private void ApplyServed(SetServed c)
    {
        int at = State.Off.BinarySearch(c.Machine);
        if (c.On && at >= 0) State.Off.RemoveAt(at);
        else if (!c.On && at < 0) State.Off.Insert(~at, c.Machine);
    }
}
