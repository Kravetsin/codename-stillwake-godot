using Stillwake.Content;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Logistics;

/// <summary>Makes crate <c>Crate</c> storage, a provider or a requester; leaving requester clears its requests.</summary>
public sealed record SetRole(int Crate, Role Role) : Command;

/// <summary>Sets what machine <c>Machine</c> requests, in order, replacing what it did; none clears them.</summary>
public sealed record SetRequests(int Machine, IReadOnlyList<Request> Requests) : Command;

/// <summary>
/// Drone logistics (0062): crate roles, what machines request, and the drones in flight. A drone bay launches a
/// docked drone to bring what a requester lacks from a provider or storage, or to carry what a fed station made to
/// storage; drones fly through air and doors within their bay's zone, and drop where they stand with no way on.
/// </summary>
public sealed partial class LogisticsModule : Module<LogisticsState>
{
    private readonly IGridQueries _grid;
    private readonly IAtmosphereQueries _air;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;

    public override string Id => "logistics";

    public LogisticsModule(IGridQueries grid, IAtmosphereQueries air, IItemsQueries items, IItemsOps itemsOps)
    {
        _grid = grid;
        _air = air;
        _items = items;
        _itemsOps = itemsOps;
        Handles<SetRole>(RoleProblem, ApplyRole);
        Handles<SetRequests>(RequestsProblem, ApplyRequests);
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

    /// <summary>What crate <paramref name="crate"/> is to drones: storage for one never set, and for anything not a crate.</summary>
    public Role RoleOf(int crate) => State.Roles.Find(r => r.Machine == crate)?.Role ?? Role.Storage;

    /// <summary>What machine <paramref name="machine"/> requests, in order; live and read-only, none for one requesting nothing.</summary>
    public IReadOnlyList<Request> RequestsOf(int machine) => State.Requests.Find(r => r.Machine == machine)?.Requests ?? (IReadOnlyList<Request>)[];

    /// <summary>
    /// The most requests machine <paramref name="machine"/> may hold now: a fed station's, a burner generator's, the
    /// Former's or the assembler's inputs, a requester crate's <c>RequestsMost</c>; 0 for anything else.
    /// </summary>
    public int RequestsMost(int machine) => _grid.MachineById(machine) is { } m ? Most(m) : 0;

    /// <summary>The drones in flight, in id order; live and read-only.</summary>
    public IReadOnlyList<Drone> Drones => State.Drones;

    /// <summary>The zone of bay <paramref name="bay"/>, its edges included; null for no bay by that id.</summary>
    public Box? ZoneOf(int bay) => _grid.MachineById(bay) is { } m && m.Kind == Content.Logistics.Bay ? ZoneBox(m) : null;

    /// <summary>
    /// Whether some bay could bring <paramref name="item"/> to machine <paramref name="machine"/>: one whose drones reach
    /// it and a provider or storage with some of the item, taken or not. A request unmet and not in reach says so.
    /// </summary>
    public bool InReach(int machine, string item)
    {
        if (_grid.MachineById(machine) is not { } target) return false;
        foreach (var bay in _grid.MachinesOf(Content.Logistics.Bay))
            if (Flood(bay) && InFlood(target) && Source(target, item, reserved: true) != null) return true;
        return false;
    }

    // Commands.

    private string? RoleProblem(SetRole c) =>
        _grid.MachineById(c.Crate)?.Kind == "crate" ? null : "logistics.notCrate";

    private void ApplyRole(SetRole c)
    {
        int at = State.Roles.FindIndex(r => r.Machine >= c.Crate);
        bool has = at >= 0 && State.Roles[at].Machine == c.Crate;
        if (c.Role != Role.Requester) State.Requests.RemoveAll(r => r.Machine == c.Crate);
        if (c.Role == Role.Storage)
        {
            if (has) State.Roles.RemoveAt(at);
        }
        else if (has) State.Roles[at].Role = c.Role;
        else State.Roles.Insert(at < 0 ? State.Roles.Count : at, new CrateRole { Machine = c.Crate, Role = c.Role });
    }

    private string? RequestsProblem(SetRequests c)
    {
        int most = RequestsMost(c.Machine);
        if (most == 0) return "logistics.cannotRequest";
        return c.Requests.Count > most ? "logistics.tooMany" : null;
    }

    private void ApplyRequests(SetRequests c)
    {
        int at = State.Requests.FindIndex(r => r.Machine >= c.Machine);
        bool has = at >= 0 && State.Requests[at].Machine == c.Machine;
        if (c.Requests.Count == 0)
        {
            if (has) State.Requests.RemoveAt(at);
        }
        else if (has) State.Requests[at].Requests = [.. c.Requests];
        else State.Requests.Insert(at < 0 ? State.Requests.Count : at, new Requesting { Machine = c.Machine, Requests = [.. c.Requests] });
    }

    /// <summary>
    /// The most requests machine <paramref name="m"/> may hold: one that takes items in to work them (inputs, or a fire's
    /// fuel slot) as many as its inputs, a requester crate <c>RequestsMost</c>. Not the pod (paid by hand), the analyzer,
    /// the charging station, bays or storage and provider crates.
    /// </summary>
    private int Most(Machine m)
    {
        if (m.Kind == "crate") return RoleOf(m.Id) == Role.Requester ? Content.Logistics.RequestsMost : 0;
        if (!Machines.TryGet(m.Kind, out var spec) || spec.Charges) return 0;
        if (spec.Inputs is int inputs && inputs > 0) return inputs;
        return spec.FuelSlot != null ? spec.Inputs ?? spec.Slots ?? 0 : 0;
    }
}
