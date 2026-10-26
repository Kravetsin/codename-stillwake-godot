using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Landing;

/// <summary>Hero <c>Peer</c> sitting in drop pod <c>Pod</c>, a machine id.</summary>
public sealed record Seat(int Pod, int Peer);

/// <summary>
/// Peer <c>Peer</c>'s drop pod in the air, no machine while it flies: down to a planet, or <c>Up</c> back aboard, landing in
/// <c>TicksLeft</c> ticks. <c>Home</c> is the ship cell it stood at and docks at; <c>Floor</c> where its rider is held meanwhile, on
/// the surface it left; <c>Load</c> what its slot held, the burnt fuel taken.
/// </summary>
public sealed record Flight(int Peer, bool Up, int TicksLeft, int Home, int Floor, Stack? Load);

/// <summary>Drop pod <c>Pod</c> standing off the ship, which only <c>Peer</c>, who came down in it, flies back to <c>Home</c> aboard.</summary>
public sealed record Owner(int Pod, int Peer, int Home);

/// <summary>
/// Who sits in which drop pod, which pods fly and whose pods stand off the ship; a platform's state is its structure codes, the
/// grid's. Public so modules can be typed by it, its members internal.
/// </summary>
public sealed class LandingState
{
    /// <summary>Every seat taken, by pod id ascending: a pod holds one hero, a hero sits in one pod.</summary>
    [JsonInclude] internal List<Seat> Seats { get; set; } = [];

    /// <summary>Every pod in the air from this surface, by peer ascending: a hero rides one at most.</summary>
    [JsonInclude] internal List<Flight> Flights { get; set; } = [];

    /// <summary>Every pod standing on this surface off the ship, by pod id ascending.</summary>
    [JsonInclude] internal List<Owner> Owners { get; set; } = [];
}

/// <summary>The sender's hero boards drop pod <c>Pod</c>, a machine id, and sits in it: E on a pod sends it.</summary>
public sealed record Board(int Pod) : Command;

/// <summary>The sender's hero leaves the drop pod it sits in: E again, seated, sends it.</summary>
public sealed record Alight : Command;

/// <summary>
/// Opens (<c>Open</c>) or shuts the launch platform <c>Cell</c> is in, its whole run: what its sender saw it should become,
/// as <see cref="SetDoor"/> is for a door. E on a platform sends it.
/// </summary>
public sealed record SetPlatform(int Cell, bool Open) : Command;

/// <summary>
/// The drop pod the sender sits in flies: down to the planet from the ship, back aboard from anywhere else. The pod's panel sends it,
/// its Descend or Return button.
/// </summary>
public sealed record Launch : Command;

/// <summary>
/// The launch platform and the drop pod, and to come its landers (the 2026-10-24 launch platform ADR, the drop pod's, the
/// descent's): a run of platform pieces opens whole, only while its network has power enough, and shuts with or without; a hero
/// boards a pod standing on one and sits in it, one hero a pod; a launch flies the pod down to the planet through its run and
/// back again, its pod going wherever its hero goes.
/// </summary>
public sealed class LandingModule : Module<LandingState>, ICarries
{
    private readonly IGridQueries _grid;
    private readonly IGridOps _gridOps;
    private readonly IPowerQueries _power;
    private readonly ICrewQueries _crew;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;

    private static readonly Placement Pod = new("machine", "dropPod");

    public override string Id => "landing";

    public LandingModule(IGridQueries grid, IGridOps gridOps, IPowerQueries power, ICrewQueries crew, IItemsQueries items, IItemsOps itemsOps)
    {
        _grid = grid;
        _gridOps = gridOps;
        _power = power;
        _crew = crew;
        _items = items;
        _itemsOps = itemsOps;
        Handles<SetPlatform>(c => Problem(c.Cell, c.Open), c => gridOps.SetDoorway(c.Cell, c.Open));
        Handles<Board>(c => BoardProblem(c.From, c.Pod), c => Sit(c.Pod, c.From));
        Handles<Alight>(c => SeatOf(c.From) == null ? "landing.notSeated" : null, c => State.Seats.RemoveAll(s => s.Peer == c.From));
        Handles<Launch>(c => LaunchProblem(c.From), c => Fly(c.From));
        // The grid changes only in commands or construction: flights count down, shut their runs and end here.
        System(Phase.Construction, Count);
        // A pod taken apart, or its hero gone from the surface or passed out, frees its seat.
        System(Phase.Derivation, () => State.Seats.RemoveAll(s => !Holds(s)));
    }

    /// <summary>
    /// Why <paramref name="peer"/>'s hero cannot board machine <paramref name="pod"/>: no drop pod (<c>landing.noPod</c>), sitting in a
    /// pod already (<c>landing.seated</c>), another hero in it (<c>landing.taken</c>), or off the ship another's (<c>landing.notYours</c>);
    /// null when it can.
    /// </summary>
    public string? BoardProblem(int peer, int pod)
    {
        if (_grid.MachineById(pod) is not { } m || !Content.Machines.Is(m.Kind, MachineTrait.Seats)) return "landing.noPod";
        if (SeatOf(peer) != null) return "landing.seated";
        if (RiderOf(pod) != null) return "landing.taken";
        return OwnerOf(pod) is int owner && owner != peer ? "landing.notYours" : null;
    }

    /// <summary>
    /// Why <paramref name="peer"/>'s hero cannot launch: in no pod (<c>landing.notSeated</c>); aboard, its pod no longer on a run
    /// (<c>landing.noPlatform</c>), the run without power enough (<c>landing.unpowered</c>), its slot short of
    /// <see cref="Content.Landing.DescentFuel"/> biofuel (<c>landing.noFuel</c>), or a world that cannot open a planet
    /// (<c>landing.cannotLeave</c>); off the ship, a pod it did not come down in (<c>landing.notYours</c>). Null when it can.
    /// </summary>
    public string? LaunchProblem(int peer)
    {
        if (SeatOf(peer) is not int pod || _grid.MachineById(pod) is not { } m) return "landing.notSeated";
        if (Surfaces.Here != 0) return OwnerOf(pod) == peer ? null : "landing.notYours";
        if (!OnRun(m)) return "landing.noPlatform";
        if (!Powered(Under(m))) return "landing.unpowered";
        if (_items.HeldFor(pod, "biofuel") < Content.Landing.DescentFuel) return "landing.noFuel";
        return Surfaces.CanOpen ? null : "landing.cannotLeave";
    }

    /// <summary>The drop pod <paramref name="peer"/>'s hero sits in, by machine id; null for one standing.</summary>
    public int? SeatOf(int peer) => State.Seats.Find(s => s.Peer == peer)?.Pod;

    /// <summary>The peer whose hero sits in drop pod <paramref name="pod"/>; null for an empty one.</summary>
    public int? RiderOf(int pod) => State.Seats.Find(s => s.Pod == pod)?.Peer;

    /// <summary>Every seat taken, by pod id ascending.</summary>
    public IReadOnlyList<Seat> Seats => State.Seats;

    /// <summary>The pod <paramref name="peer"/>'s hero rides in the air from this surface; null for none.</summary>
    public Flight? FlightOf(int peer) => State.Flights.Find(f => f.Peer == peer);

    /// <summary>Every pod in the air from this surface, by peer ascending.</summary>
    public IReadOnlyList<Flight> Flights => State.Flights;

    /// <summary>The peer who came down in drop pod <paramref name="pod"/>, standing off the ship; null aboard, or for one nobody did.</summary>
    public int? OwnerOf(int pod) => State.Owners.Find(o => o.Pod == pod)?.Peer;

    /// <summary>Where a hero sits in a drop pod standing from <paramref name="cell"/>, its top-left: the middle of its bottom row.</summary>
    public static int FloorOf(int cell, int width)
    {
        var spec = Content.Machines.Get(Pod.Kind);
        return (cell / width + spec.Height - 1) * width + cell % width + spec.Width / 2 - 1;
    }

    private void Sit(int pod, int peer)
    {
        int at = State.Seats.FindIndex(s => s.Pod > pod);
        State.Seats.Insert(at < 0 ? State.Seats.Count : at, new Seat(pod, peer));
    }

    /// <summary>Whether <paramref name="seat"/> still holds: its pod stands, its hero is here and awake.</summary>
    private bool Holds(Seat seat) =>
        _grid.MachineById(seat.Pod) is { } m && Content.Machines.Is(m.Kind, MachineTrait.Seats) && _crew.HeroOf(seat.Peer) is { Out: 0 };

    /// <summary>The first cell of the row under pod <paramref name="m"/>: its run's, aboard.</summary>
    private int Under(Machine m) => Under(m.Cell);

    /// <summary>The first cell of the row under a pod standing from <paramref name="cell"/>.</summary>
    private int Under(int cell) => cell + Content.Machines.Get(Pod.Kind).Height * _grid.Width;

    /// <summary>Whether the row under pod <paramref name="m"/> is all a platform's still: built over one, it may have been taken apart since.</summary>
    private bool OnRun(Machine m)
    {
        int under = Under(m);
        if (under >= _grid.Width * _grid.Height) return false;
        for (int dx = 0; dx < Content.Machines.Get(m.Kind).Width; dx++)
            if (!Doorways.IsPlatform(_grid.StructureAt(under + dx))) return false;
        return true;
    }

    /// <summary>
    /// <paramref name="peer"/>'s pod takes off: aboard its fuel burnt and its run opened under it, off the ship back to its home.
    /// Its machine and seat go; what its slot holds flies with it.
    /// </summary>
    private void Fly(int peer)
    {
        int pod = SeatOf(peer)!.Value;
        var m = _grid.MachineById(pod)!;
        bool up = Surfaces.Here != 0;
        int home = m.Cell;
        if (up)
        {
            home = State.Owners.Find(o => o.Pod == pod)!.Home;
            State.Owners.RemoveAll(o => o.Pod == pod);
        }
        else
        {
            _itemsOps.TakeFrom(pod, 0, Content.Landing.DescentFuel);
            _gridOps.SetDoorway(Under(m), true);
        }
        var flight = new Flight(peer, up, up ? Content.Landing.AscentTicks : Content.Landing.DescentTicks, home,
            FloorOf(m.Cell, _grid.Width), _itemsOps.TakeFrom(pod, 0, int.MaxValue));
        _gridOps.Clear(m.Cell, "machine");
        State.Seats.RemoveAll(s => s.Pod == pod);
        int at = State.Flights.FindIndex(f => f.Peer > peer);
        State.Flights.Insert(at < 0 ? State.Flights.Count : at, flight);
    }

    /// <summary>
    /// A tick of every flight: a descent shuts its run once its pod is through, and ends at once, docked home, when its hero has
    /// left or passed out; at zero a flight sends its hero, and with it the pod, to the planet or aboard.
    /// </summary>
    private void Count()
    {
        int shut = Content.Landing.DescentTicks - Content.Landing.FallTicks;
        for (int i = 0; i < State.Flights.Count; i++)
        {
            var f = State.Flights[i];
            if (!f.Up && _crew.HeroOf(f.Peer) is not { Out: 0 })
            {
                if (f.TicksLeft > shut) Shut(f.Home);
                State.Flights.RemoveAt(i--);
                Dock(f.Peer, f);
                continue;
            }
            State.Flights[i] = f = f with { TicksLeft = f.TicksLeft - 1 };
            if (!f.Up && f.TicksLeft == shut) Shut(f.Home);
            if (f.TicksLeft == 0) Surfaces.Send(f.Peer, f.Up ? 0 : Surfaces.Planet());
        }
    }

    /// <summary>Shuts the run pod home <paramref name="home"/> stood over, if it stands still.</summary>
    private void Shut(int home)
    {
        int under = Under(home);
        if (Doorways.IsPlatform(_grid.StructureAt(under))) _gridOps.SetDoorway(under, false);
    }

    /// <summary>
    /// Pod <paramref name="f"/> stood again at its home aboard, its load put back; that spot built over or its run gone, laid there
    /// as its cost and its load instead. Returns its machine id, or null for one laid.
    /// </summary>
    private int? Dock(int peer, Flight f) => Stand(peer, f, _grid.PlacementProblem(f.Home, Pod) == null ? f.Home : null, f.Home);

    /// <summary>
    /// Pod <paramref name="f"/> landed off the ship by the spawn, its load put back: the first spot outward from the spawn's column,
    /// left first, where its four columns' tops are level and it fits over them. None (a ground dug to nothing, a strip too narrow),
    /// it is laid at the spawn as its cost and load. Returns its machine id, or null for one laid.
    /// </summary>
    private int? Land(int peer, Flight f)
    {
        var spec = Content.Machines.Get(Pod.Kind);
        int width = _grid.Width, spawn = _crew.Spawn, from = spawn % width - spec.Width / 2 + 1;
        for (int d = 0; d < width; d++)
        {
            int[] xs = d == 0 ? [from] : [from - d, from + d];
            foreach (int x in xs)
            {
                if (x < 0 || x + spec.Width > width || Top(x) is not int top || top < spec.Height) continue;
                bool level = true;
                for (int dx = 1; dx < spec.Width && level; dx++) level = Top(x + dx) == top;
                int at = (top - spec.Height) * width + x;
                if (level && _grid.PlacementProblem(at, Pod) == null)
                {
                    int? pod = Stand(peer, f, at, spawn);
                    State.Owners.Add(new Owner(pod!.Value, peer, f.Home));
                    return pod;
                }
            }
        }
        return Stand(peer, f, null, spawn);
    }

    /// <summary>The row of the first solid cell of column <paramref name="x"/> from the top: where a pod would stand on it; null for none.</summary>
    private int? Top(int x)
    {
        for (int y = 0; y < _grid.Height; y++)
            if (_grid.Solid(y * _grid.Width + x)) return y;
        return null;
    }

    /// <summary>Pod <paramref name="f"/> placed at <paramref name="at"/> with its load; null, laid at <paramref name="lay"/> as its cost and load.</summary>
    private int? Stand(int peer, Flight f, int? at, int lay)
    {
        if (at is int cell)
        {
            _gridOps.Place(cell, Pod);
            int pod = _grid.MachineAt(cell)!.Id;
            if (f.Load is { } load) _itemsOps.PutIn(pod, load);
            return pod;
        }
        _itemsOps.DropAt(peer, Content.Machines.Get(Pod.Kind).Cost, lay);
        if (f.Load is { } laid) _itemsOps.LayDown(laid, lay);
        return null;
    }

    /// <summary>What crosses with a peer: its flight, or the pod it owns following it (<c>Rides</c> false: it arrives empty).</summary>
    private sealed record Carried(Flight Flight, bool Rides);

    /// <summary>The peer's flight; else, off the ship, the pod it came down in, lifted out of the grid; null for neither.</summary>
    public object? Depart(int peer)
    {
        int i = State.Flights.FindIndex(f => f.Peer == peer);
        if (i >= 0)
        {
            var flight = State.Flights[i];
            State.Flights.RemoveAt(i);
            return new Carried(flight, true);
        }
        if (State.Owners.Find(o => o.Peer == peer) is not { } owner) return null;
        State.Owners.Remove(owner);
        // Nothing takes a pod apart off the ship: its machine stands.
        var m = _grid.MachineById(owner.Pod)!;
        var load = _itemsOps.TakeFrom(m.Id, 0, int.MaxValue);
        _gridOps.Clear(m.Cell, "machine");
        State.Seats.RemoveAll(s => s.Pod == m.Id);
        return new Carried(new Flight(peer, true, 0, owner.Home, FloorOf(m.Cell, _grid.Width), load), false);
    }

    /// <summary>A pod carried here docked aboard or landed off the ship; its hero seated in it when it rode and is here and awake.</summary>
    public void Arrive(int peer, object? carried, int? cell)
    {
        if (carried is not Carried c) return;
        int? pod = Surfaces.Here == 0 ? Dock(peer, c.Flight) : Land(peer, c.Flight);
        if (c.Rides && pod is int p && _crew.HeroOf(peer) is { Out: 0 }) Sit(p, peer);
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

    /// <summary>
    /// Whether the run of the platform at <paramref name="cell"/> has power enough: its plug, its left end, where every cable to it
    /// ends, fed (<see cref="IPowerQueries.Feeds"/>).
    /// </summary>
    public bool Powered(int cell) => _power.Feeds(RunOf(_grid, cell).First);

    /// <summary>The run of platform pieces <paramref name="cell"/> is in, shut and open alike: its first cell and how many cells it runs.</summary>
    public static (int First, int Length) RunOf(IGridQueries grid, int cell) =>
        StructureRuns.RunOf(cell, grid.Width, c => Doorways.IsPlatform(grid.StructureAt(c)) ? 1 : 0);

    /// <summary>
    /// Now and then a pod boarded or left, launched, a platform opened or shut: mostly on a pod or a platform, sometimes on anything,
    /// so refusals are drawn too.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(8) != 0) return null;
        if (rng.Below(4) == 0) return new Launch { From = from };
        if (rng.Below(3) == 0)
        {
            if (rng.Below(3) == 0) return new Alight { From = from };
            var pods = _grid.MachinesWith(MachineTrait.Seats);
            int pod = pods.Count > 0 && rng.Below(4) != 0 ? pods[(int)rng.Below((uint)pods.Count)].Id : (int)rng.Below(64) - 1;
            return new Board(pod) { From = from };
        }
        var structure = _grid.StructureLayer;
        var platforms = new List<int>();
        for (int c = 0; c < structure.Length; c++) if (Doorways.IsPlatform(structure[c])) platforms.Add(c);
        int cell = platforms.Count > 0 && rng.Below(4) != 0
            ? platforms[(int)rng.Below((uint)platforms.Count)]
            : (int)rng.Below((uint)structure.Length + 2) - 1;
        return new SetPlatform(cell, rng.Below(2) == 0) { From = from };
    }
}
