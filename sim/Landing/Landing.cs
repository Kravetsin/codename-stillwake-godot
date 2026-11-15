using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Landing;

/// <summary>
/// Hero <c>Peer</c> sitting in drop pod <c>Pod</c>, a machine id, at <c>Place</c>, its seat's index there, <c>0..Riders−1</c>: always
/// 0 in a machine for one; a save from before places reads 0.
/// </summary>
public sealed record Seat(int Pod, int Peer, int Place = 0);

/// <summary>
/// Peer <c>Peer</c>'s drop pod in the air, no machine while it flies: down to a planet, or <c>Up</c> back aboard, landing in
/// <c>TicksLeft</c> ticks. <c>Home</c> is the ship cell it stood at and docks at; <c>Floor</c> where its rider is held meanwhile, on
/// the surface it left; <c>Load</c> what its slot held, the burnt fuel taken.
/// </summary>
public sealed record Flight(int Peer, bool Up, int TicksLeft, int Home, int Floor, Stack? Load);

/// <summary>Drop pod <c>Pod</c> standing off the ship, which only <c>Peer</c>, who came down in it, flies back to <c>Home</c> aboard.</summary>
public sealed record Owner(int Pod, int Peer, int Home);

/// <summary>
/// A shuttle in the air with its riders (shuttle-flight): <c>From</c> the cell it stood at on this surface, <c>Riders</c> in seat
/// order, arriving in <c>TicksLeft</c> ticks; <c>Home</c> the ship cell it docks at. <c>Out</c> the kind of vessel world it flies to,
/// grown from <c>Seed</c>; null for a flight home.
/// </summary>
public sealed record Voyage(int From, IReadOnlyList<int> Riders, int TicksLeft, int Home, string? Out = null, uint Seed = 0);

/// <summary>Shuttle <c>Shuttle</c>, a machine id, standing on a vessel world, its home aboard <c>Home</c>: where it docks.</summary>
public sealed record Parked(int Shuttle, int Home);

/// <summary>
/// Who sits in which drop pod, which pods fly and whose pods stand off the ship; a platform's state is its structure codes, the
/// grid's. Public so modules can be typed by it, its members internal.
/// </summary>
public sealed class LandingState
{
    /// <summary>
    /// Every seat taken, by pod id then place ascending: a machine holds its <c>Riders</c>, one a place, and a hero sits in one
    /// place. A save from before places, every place 0, is in pod order already.
    /// </summary>
    [JsonInclude] internal List<Seat> Seats { get; set; } = [];

    /// <summary>Every pod in the air from this surface, by peer ascending: a hero rides one at most.</summary>
    [JsonInclude] internal List<Flight> Flights { get; set; } = [];

    /// <summary>Every pod standing on this surface off the ship, by pod id ascending.</summary>
    [JsonInclude] internal List<Owner> Owners { get; set; } = [];

    /// <summary>Every shuttle in the air from this surface, in the order they set off.</summary>
    [JsonIgnore] internal List<Voyage> Voyages { get; set; } = [];
    /// <summary>Voyages as saved: none written while none flies, so a world without one saves as before.</summary>
    [JsonInclude, JsonPropertyName("voyages"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Voyage>? SavedVoyages { get => Voyages.Count > 0 ? Voyages : null; set => Voyages = value ?? []; }

    /// <summary>Every shuttle standing on this surface off the ship, in the order they arrived.</summary>
    [JsonIgnore] internal List<Parked> Parked { get; set; } = [];
    /// <summary>Shuttles standing as saved: none written while none stands off the ship.</summary>
    [JsonInclude, JsonPropertyName("parked"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Parked>? SavedParked { get => Parked.Count > 0 ? Parked : null; set => Parked = value ?? []; }
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

/// <summary>What other modules read of Landing: whether a hero rides a drop pod, sitting or flying (vessel-worlds).</summary>
public interface ILandingQueries
{
    /// <summary>Whether <paramref name="peer"/>'s hero sits in a drop pod or a shuttle, or flies in one, from this surface.</summary>
    bool Rides(int peer);

    /// <summary>Why <paramref name="peer"/>'s hero cannot set off in the shuttle it sits in, its course aside; null when it can.</summary>
    string? SetOffProblem(int peer);
}

/// <summary>How places sends a shuttle off (shuttle-flight): its course is places', the flight Landing's.</summary>
public interface ILandingOps
{
    /// <summary>The shuttle <paramref name="peer"/>'s hero sits in sets off from the ship to a vessel world of <paramref name="kind"/> grown from <paramref name="seed"/>.</summary>
    void SetOff(int peer, string kind, uint seed);

    /// <summary>The shuttle <paramref name="peer"/>'s hero sits in, off the ship, sets off home.</summary>
    void SetOffHome(int peer);
}

/// <summary>
/// The launch platform and the drop pod, and to come its landers (the 2026-10-24 launch platform ADR, the drop pod's, the
/// descent's): a run of platform pieces opens whole, only while its network has power enough, and shuts with or without; a hero
/// boards a pod standing on one and sits in it, one hero a pod; a launch flies the pod down to the planet through its run and
/// back again, its pod going wherever its hero goes.
/// </summary>
public sealed class LandingModule : Module<LandingState>, ICarries, IOutlives, ILandingQueries, ILandingOps
{
    private readonly IGridQueries _grid;
    private readonly IGridOps _gridOps;
    private readonly IPowerQueries _power;
    private readonly IPowerOps _powerOps;
    private readonly ICrewQueries _crew;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;

    private static readonly Placement Pod = new("machine", "dropPod");
    /// <summary>The craft a voyage flies: the one machine with several seats.</summary>
    private static readonly Placement Craft = new("machine", "crewShuttle");

    /// <summary>The drop pod's spec: what the client's pictures of a flight size and place it by, the pod being off the grid then.</summary>
    public static MachineSpec PodSpec => Content.Machines.Get(Pod.Kind);
    /// <summary>The craft's spec: what the client holds a voyage's riders at the places of, the craft being off the grid then.</summary>
    public static MachineSpec CraftSpec => Content.Machines.Get(Craft.Kind);
    /// <summary>What its one slot takes and a descent burns.</summary>
    private static readonly string Fuel = Content.Machines.Get(Pod.Kind).Takes!;

    public override string Id => "landing";

    public LandingModule(IGridQueries grid, IGridOps gridOps, IPowerQueries power, IPowerOps powerOps, ICrewQueries crew, IItemsQueries items, IItemsOps itemsOps)
    {
        _grid = grid;
        _gridOps = gridOps;
        _power = power;
        _powerOps = powerOps;
        _crew = crew;
        _items = items;
        _itemsOps = itemsOps;
        Handles<SetPlatform>(c => Problem(c.Cell, c.Open), c => gridOps.SetDoorway(c.Cell, c.Open));
        Handles<Board>(c => BoardProblem(c.From, c.Pod), c => Sit(c.Pod, c.From));
        Handles<Alight>(c => SeatOf(c.From) == null ? "landing.notSeated" : null, c => State.Seats.RemoveAll(s => s.Peer == c.From));
        Handles<Launch>(c => LaunchProblem(c.From), c => Fly(c.From));
        // The grid changes only in commands or construction: flights count down, shut their runs and end here.
        System(Phase.Construction, Count);
        // After Building, before power's derivation: a seated maker works only under a rider running (power-chain).
        System(Phase.Construction, Turn);
        // A pod taken apart, or its hero gone from the surface or passed out, frees its seat.
        System(Phase.Derivation, () => State.Seats.RemoveAll(s => !Holds(s)));
    }

    /// <summary>
    /// Why <paramref name="peer"/>'s hero cannot board machine <paramref name="pod"/>: no machine with seats (<c>landing.noPod</c>),
    /// sitting in one already (<c>landing.seated</c>), every place of it taken (<c>landing.taken</c>: the pod's one, the shuttle's
    /// four), or off the ship another's (<c>landing.notYours</c>); null when it can.
    /// </summary>
    public string? BoardProblem(int peer, int pod)
    {
        if (_grid.MachineById(pod) is not { } m || !Content.Machines.Is(m.Kind, MachineTrait.Seats)) return "landing.noPod";
        // A hero flying in a pod sits in it still, with no seat to show it.
        if (Rides(peer)) return "landing.seated";
        if (FreePlace(pod, Content.Machines.Get(m.Kind).Riders) == null) return "landing.taken";
        return OwnerOf(pod) is int owner && owner != peer ? "landing.notYours" : null;
    }

    /// <summary>
    /// Why <paramref name="peer"/>'s hero cannot launch: in no pod, in a seat that does not fly (a wheel: no
    /// <see cref="MachineTrait.Lands"/>), or in one for several (the shuttle, which sets off its own way) (<c>landing.notSeated</c>); aboard, its pod no longer on a run
    /// (<c>landing.noPlatform</c>), anything solid under its run to the grid's edge (<c>landing.shaftBlocked</c>), the run without
    /// power enough (<c>landing.unpowered</c>), its slot short of <see cref="Content.Landing.DescentFuel"/> biofuel
    /// (<c>landing.noFuel</c>), or a world that cannot open a planet
    /// (<c>landing.cannotLeave</c>); off the ship, a pod it did not come down in (<c>landing.notYours</c>). Null when it can.
    /// </summary>
    public string? LaunchProblem(int peer)
    {
        // A seat outlives its hero gone or passed out until the derivation phase: it launches nothing, its fuel unburnt.
        if (SeatOf(peer) is not int pod || _grid.MachineById(pod) is not { } m || _crew.HeroOf(peer) is not { Out: 0 }) return "landing.notSeated";
        // A wheel seats a hero too, and stands on planets as aboard; a shuttle seats four and lands: only a pod, a seat for one, flies.
        if (!Content.Machines.Is(m.Kind, MachineTrait.Lands) || Content.Machines.Get(m.Kind).Riders > 1) return "landing.notSeated";
        if (Surfaces.Here != 0) return OwnerOf(pod) == peer ? null : "landing.notYours";
        // Built over a run, it may have been taken apart since.
        if (!_grid.OnRun(m.Kind, m.Cell)) return "landing.noPlatform";
        // Something built under its run before shafts were kept clear, in an older save: the pod would fall through it.
        for (int dx = 0; dx < Content.Machines.Get(m.Kind).Width; dx++)
            if (!_grid.ClearBelow(Under(m) + dx)) return "landing.shaftBlocked";
        if (!Powered(Under(m))) return "landing.unpowered";
        if (_items.HeldFor(pod, Fuel) < Content.Landing.DescentFuel) return "landing.noFuel";
        return Surfaces.CanOpen ? null : "landing.cannotLeave";
    }

    /// <summary>
    /// Why <paramref name="peer"/>'s hero cannot set off in its shuttle, its course aside (places'): in no seat of a machine for several,
    /// or passed out (<c>landing.notSeated</c>); aboard, its shuttle no longer on a run (<c>landing.noPlatform</c>), anything solid under
    /// its run (<c>landing.shaftBlocked</c>), the run without power enough (<c>landing.unpowered</c>), or a world that cannot open a vessel
    /// world (<c>landing.cannotLeave</c>); off the ship, a shuttle that came from no home (<c>landing.notYours</c>: a dev's). Null when it can.
    /// </summary>
    public string? SetOffProblem(int peer)
    {
        if (SeatOf(peer) is not int shuttle || _grid.MachineById(shuttle) is not { } m || Content.Machines.Get(m.Kind).Riders == 1
            || _crew.HeroOf(peer) is not { Out: 0 }) return "landing.notSeated";
        if (Surfaces.Here != 0) return ParkedOf(shuttle) == null ? "landing.notYours" : null;
        if (!_grid.OnRun(m.Kind, m.Cell)) return "landing.noPlatform";
        for (int dx = 0; dx < Content.Machines.Get(m.Kind).Width; dx++)
            if (!_grid.ClearBelow(Under(m) + dx)) return "landing.shaftBlocked";
        if (!Powered(Under(m))) return "landing.unpowered";
        return Surfaces.CanOpen ? null : "landing.cannotLeave";
    }

    /// <summary>The drop pod <paramref name="peer"/>'s hero sits in, by machine id; null for one standing.</summary>
    public int? SeatOf(int peer) => State.Seats.Find(s => s.Peer == peer)?.Pod;

    /// <summary>The peer whose hero sits in drop pod <paramref name="pod"/>, a machine for one; null for an empty one.</summary>
    public int? RiderOf(int pod) => State.Seats.Find(s => s.Pod == pod)?.Peer;

    /// <summary>The place <paramref name="peer"/>'s hero sits at in its machine; null for one standing.</summary>
    public int? PlaceOf(int peer) => State.Seats.Find(s => s.Peer == peer)?.Place;

    /// <summary>Every seat taken in machine <paramref name="machine"/>, by place ascending.</summary>
    public IReadOnlyList<Seat> RidersOf(int machine) => State.Seats.FindAll(s => s.Pod == machine);

    /// <summary>Every seat taken, by pod id then place ascending.</summary>
    public IReadOnlyList<Seat> Seats => State.Seats;

    public bool Rides(int peer) => SeatOf(peer) != null || FlightOf(peer) != null || VoyageOf(peer) != null;

    /// <summary>The shuttle <paramref name="peer"/>'s hero rides in the air from this surface; null for none.</summary>
    public Voyage? VoyageOf(int peer) => State.Voyages.Find(v => v.Riders.Contains(peer));

    /// <summary>Every shuttle in the air from this surface, in the order they set off.</summary>
    public IReadOnlyList<Voyage> Voyages => State.Voyages;

    /// <summary>Every shuttle standing on this surface off the ship, in the order they arrived.</summary>
    public IReadOnlyList<Parked> Parked => State.Parked;

    /// <summary>The home aboard of shuttle <paramref name="shuttle"/> standing off the ship; null aboard, or for one from no home.</summary>
    private Parked? ParkedOf(int shuttle) => State.Parked.Find(p => p.Shuttle == shuttle);

    /// <summary>The pod <paramref name="peer"/>'s hero rides in the air from this surface; null for none.</summary>
    public Flight? FlightOf(int peer) => State.Flights.Find(f => f.Peer == peer);

    /// <summary>Every pod in the air from this surface, by peer ascending.</summary>
    internal IReadOnlyList<Flight> Flights => State.Flights;

    /// <summary>Whether a launch from this surface flies back aboard, its key Return; from the ship it descends.</summary>
    public bool Returns => Surfaces.Here != 0;

    /// <summary>The peer who came down in drop pod <paramref name="pod"/>, standing off the ship; null aboard, or for one nobody did.</summary>
    internal int? OwnerOf(int pod) => State.Owners.Find(o => o.Pod == pod)?.Peer;

    /// <summary>Where a hero sits in a drop pod standing from <paramref name="cell"/>, its top-left: the middle of its bottom row.</summary>
    public static int FloorOf(int cell, int width) => FloorOf(Pod.Kind, cell, width);

    /// <summary>
    /// Where a hero sits at <paramref name="place"/> in a machine of <paramref name="kind"/> with seats standing from
    /// <paramref name="cell"/>: its bottom row, in the middle of the place's share of its width,
    /// <c>(2·place + 1) × Width / (2 × Riders) − 1</c> from its left, rounded down; for a machine for one, the middle.
    /// </summary>
    public static int FloorOf(string kind, int cell, int width, int place = 0)
    {
        var spec = Content.Machines.Get(kind);
        return (cell / width + spec.Height - 1) * width + cell % width + (2 * place + 1) * spec.Width / (2 * spec.Riders) - 1;
    }

    /// <summary>
    /// Whether seated maker <paramref name="machine"/> turns: its rider's hero is on this surface, awake and running
    /// (<c>Hero.Exerting</c>, its client's word, 0002). What rests it otherwise, and what the client turns its drum by.
    /// </summary>
    public bool Turning(int machine) => RiderOf(machine) is int rider && _crew.HeroOf(rider) is { Out: 0, Exerting: true };

    /// <summary>
    /// Each tick in construction, after Building and before power's derivation, every standing machine with seats that makes
    /// power rests unless it turns (power-chain). It has no draw and no fire, so no other module rests it.
    /// </summary>
    private void Turn()
    {
        foreach (var m in _grid.MachinesWith(MachineTrait.Seats))
            if (Content.Machines.Get(m.Kind).Makes != null) _powerOps.Rest(m.Id, !Turning(m.Id));
    }

    /// <summary>The hero sits at the lowest free place of <paramref name="pod"/>, kept in (pod, place) order.</summary>
    private void Sit(int pod, int peer)
    {
        int place = FreePlace(pod, Content.Machines.Get(_grid.MachineById(pod)!.Kind).Riders)!.Value;
        int at = State.Seats.FindIndex(s => s.Pod > pod || (s.Pod == pod && s.Place > place));
        State.Seats.Insert(at < 0 ? State.Seats.Count : at, new Seat(pod, peer, place));
    }

    /// <summary>The lowest place of machine <paramref name="pod"/>'s <paramref name="riders"/> nobody sits at; null for none.</summary>
    private int? FreePlace(int pod, int riders)
    {
        for (int place = 0; place < riders; place++)
            if (!State.Seats.Exists(s => s.Pod == pod && s.Place == place)) return place;
        return null;
    }

    /// <summary>Whether <paramref name="seat"/> still holds: its pod stands, its hero is here and awake.</summary>
    private bool Holds(Seat seat) =>
        _grid.MachineById(seat.Pod) is { } m && Content.Machines.Is(m.Kind, MachineTrait.Seats) && _crew.HeroOf(seat.Peer) is { Out: 0 };

    /// <summary>The first cell of the row under machine <paramref name="m"/>: its run's, aboard.</summary>
    private int Under(Machine m) => Under(m.Kind, m.Cell);

    /// <summary>The first cell of the row under a machine of <paramref name="kind"/> standing from <paramref name="cell"/>.</summary>
    private int Under(string kind, int cell) => cell + Content.Machines.Get(kind).Height * _grid.Width;

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
                if (f.TicksLeft > shut) Shut(Pod.Kind, f.Home);
                State.Flights.RemoveAt(i--);
                Dock(f.Peer, f.Home, f.Load);
                continue;
            }
            State.Flights[i] = f = f with { TicksLeft = f.TicksLeft - 1 };
            if (!f.Up && f.TicksLeft == shut) Shut(Pod.Kind, f.Home);
            if (f.TicksLeft == 0) Surfaces.Send(f.Peer, f.Up ? 0 : Surfaces.OpenPlanet());
        }
        Sail();
    }

    /// <summary>
    /// A tick of every voyage, after the flights (shuttle-flight): riders gone from this surface are dropped, the out ones fly on. Outbound
    /// it shuts its run once the shuttle is through, and left with no rider docks home empty at once, its contact still used. At zero
    /// it sends its riders in seat order, outbound to a vessel world opened for it, else home; it stays until they depart, and one
    /// homeward with no rider waits there until its world closes (<see cref="Closing"/>).
    /// </summary>
    private void Sail()
    {
        int shut = Content.Shuttle.FlightTicks - Content.Shuttle.FallTicks;
        for (int i = 0; i < State.Voyages.Count; i++)
        {
            var v = State.Voyages[i];
            if (v.TicksLeft == 0) continue;
            if (v.Riders.Any(r => _crew.HeroOf(r) == null)) v = v with { Riders = [.. v.Riders.Where(r => _crew.HeroOf(r) != null)] };
            if (v.Out != null && v.Riders.Count == 0)
            {
                if (v.TicksLeft > shut) Shut(Craft.Kind, v.Home);
                State.Voyages.RemoveAt(i--);
                Moor(null, v.Home);
                continue;
            }
            State.Voyages[i] = v = v with { TicksLeft = v.TicksLeft - 1 };
            if (v.Out != null && v.TicksLeft == shut) Shut(Craft.Kind, v.Home);
            if (v.TicksLeft > 0) continue;
            int to = v.Out != null ? Surfaces.Open(v.Out, v.Seed) : 0;
            foreach (int rider in v.Riders) Surfaces.Send(rider, to);
        }
    }

    /// <summary>Shuts the run a machine of <paramref name="kind"/> at home <paramref name="home"/> stood over, if it stands still.</summary>
    private void Shut(string kind, int home)
    {
        int under = Under(kind, home);
        if (Doorways.IsPlatform(_grid.StructureAt(under))) _gridOps.SetDoorway(under, false);
    }

    /// <summary>
    /// The shuttle <paramref name="peer"/>'s hero sits in sets off with its riders, the seats held, in place order: aboard its run opened
    /// under it, flying to a vessel world of <paramref name="out"/> from <paramref name="seed"/>; off the ship home. Its machine and
    /// seats go.
    /// </summary>
    private void Away(int peer, string? @out, uint seed)
    {
        int shuttle = SeatOf(peer)!.Value;
        var m = _grid.MachineById(shuttle)!;
        var riders = State.Seats.FindAll(s => s.Pod == shuttle && Holds(s)).ConvertAll(s => s.Peer);
        int home = m.Cell;
        if (Surfaces.Here != 0)
        {
            home = ParkedOf(shuttle)!.Home;
            State.Parked.RemoveAll(p => p.Shuttle == shuttle);
        }
        else _gridOps.SetDoorway(Under(m), true);
        _gridOps.Clear(m.Cell, "machine");
        State.Seats.RemoveAll(s => s.Pod == shuttle);
        State.Voyages.Add(new Voyage(m.Cell, riders, Content.Shuttle.FlightTicks, home, @out, seed));
    }

    public void SetOff(int peer, string kind, uint seed) => Away(peer, kind, seed);

    public void SetOffHome(int peer) => Away(peer, null, 0);

    /// <summary>
    /// A shuttle stood again at <paramref name="home"/> aboard; that spot built over or its run gone, laid there as its cost instead:
    /// counted to <paramref name="rider"/>, its first rider, as a pod's (<see cref="IItemsOps.DropAt"/>); for none, as obtained by nobody.
    /// Returns its machine id, or null for one laid.
    /// </summary>
    private int? Moor(int? rider, int home)
    {
        if (_grid.PlacementProblem(home, Craft) == null)
        {
            _gridOps.Place(home, Craft);
            return _grid.MachineAt(home)!.Id;
        }
        var cost = Content.Machines.Get(Craft.Kind).Cost;
        if (rider is int peer) _itemsOps.DropAt(peer, cost, home);
        else foreach (var (item, count) in cost) _itemsOps.LayDown(new Stack(item, count), home);
        return null;
    }

    /// <summary>
    /// The shuttle arrived off the ship, its home aboard <paramref name="home"/>: stopped in open space, its right edge
    /// <c>Shuttle.Gap</c> cells left of the vessel's frame, its bottom row on the spawn's row. The open space round the frame always
    /// holds it (a content test), so placing it never fails.
    /// </summary>
    private int Stop(int home)
    {
        var spec = Content.Machines.Get(Craft.Kind);
        int width = _grid.Width;
        int at = (_crew.Spawn / width - spec.Height + 1) * width + Content.Vessels.Margin - Content.Shuttle.Gap - spec.Width;
        _gridOps.Place(at, Craft);
        int shuttle = _grid.MachineAt(at)!.Id;
        State.Parked.Add(new Parked(shuttle, home));
        return shuttle;
    }

    /// <summary>What closing this vessel world leaves (rule 7): the home of every shuttle standing here, then of every one flying home.</summary>
    public object? Closing()
    {
        var homes = State.Parked.ConvertAll(p => p.Home);
        homes.AddRange(State.Voyages.Select(v => v.Home));
        return homes.Count > 0 ? homes : null;
    }

    /// <summary>Each shuttle a closing vessel world left docks at its home aboard, empty.</summary>
    public void Outlived(object left)
    {
        foreach (int home in (List<int>)left) Moor(null, home);
    }

    /// <summary>
    /// A pod stood again at <paramref name="home"/> aboard, <paramref name="load"/> put back in it; that spot built over or its run gone,
    /// laid there as its cost and its load instead. Returns its machine id, or null for one laid.
    /// </summary>
    private int? Dock(int peer, int home, Stack? load) => Stand(peer, load, _grid.PlacementProblem(home, Pod) == null ? home : null, home);

    /// <summary>
    /// A pod landed off the ship by the spawn, <paramref name="load"/> put back in it, its home aboard <paramref name="home"/>: the first spot outward from the spawn's column,
    /// left first, where its four columns' tops are level and it fits over them. None (a ground dug to nothing, a strip too narrow),
    /// it is laid at the spawn as its cost and load. Returns its machine id, or null for one laid.
    /// </summary>
    private int? Land(int peer, int home, Stack? load)
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
                    int? pod = Stand(peer, load, at, spawn);
                    State.Owners.Add(new Owner(pod!.Value, peer, home));
                    return pod;
                }
            }
        }
        return Stand(peer, load, null, spawn);
    }

    /// <summary>The row of the first solid cell of column <paramref name="x"/> from the top: where a pod would stand on it; null for none.</summary>
    private int? Top(int x)
    {
        for (int y = 0; y < _grid.Height; y++)
            if (_grid.Solid(y * _grid.Width + x)) return y;
        return null;
    }

    /// <summary>A pod placed at <paramref name="at"/> with <paramref name="load"/>; null, laid at <paramref name="lay"/> as its cost and load.</summary>
    private int? Stand(int peer, Stack? load, int? at, int lay)
    {
        if (at is int cell)
        {
            _gridOps.Place(cell, Pod);
            int pod = _grid.MachineAt(cell)!.Id;
            if (load != null) _itemsOps.PutIn(pod, load);
            return pod;
        }
        _itemsOps.DropAt(peer, Content.Machines.Get(Pod.Kind).Cost, lay);
        if (load != null) _itemsOps.LayDown(load, lay);
        return null;
    }

    /// <summary>What crosses with a peer: its pod's home aboard and load, and whether it rides in it (a flight) or it follows (arriving empty).</summary>
    private sealed record Carried(int Home, Stack? Load, bool Rides);

    /// <summary>The peer's flight; else, off the ship, the pod it came down in, lifted out of the grid; null for neither.</summary>
    /// <summary>
    /// What crosses with a voyage's rider at its end: its shuttle's home aboard, and whether it brings the shuttle (its first rider in seat
    /// order) or finds it there (the others).
    /// </summary>
    private sealed record Rode(int Home, bool Brings);

    /// <summary>
    /// The peer's voyage at its end: its first rider in seat order brings the shuttle, the others a seat, the last ends it; before its end
    /// nothing (its next tick drops the rider as gone). Else the peer's flight; else, off the ship, the pod it came down in, lifted out of
    /// the grid; null for none.
    /// </summary>
    public object? Depart(int peer)
    {
        int v = State.Voyages.FindIndex(x => x.TicksLeft == 0 && x.Riders.Contains(peer));
        if (v >= 0)
        {
            var voyage = State.Voyages[v];
            if (voyage.Riders[^1] == peer) State.Voyages.RemoveAt(v);
            return new Rode(voyage.Home, voyage.Riders[0] == peer);
        }
        int i = State.Flights.FindIndex(f => f.Peer == peer);
        if (i >= 0)
        {
            var flight = State.Flights[i];
            State.Flights.RemoveAt(i);
            // Beamed off mid-fall by the dev panel, it shuts the run it opened.
            if (!flight.Up && flight.TicksLeft > Content.Landing.DescentTicks - Content.Landing.FallTicks) Shut(Pod.Kind, flight.Home);
            return new Carried(flight.Home, flight.Load, true);
        }
        if (State.Owners.Find(o => o.Peer == peer) is not { } owner) return null;
        State.Owners.Remove(owner);
        // Nothing takes a pod apart off the ship: its machine stands.
        var m = _grid.MachineById(owner.Pod)!;
        var load = _itemsOps.TakeFrom(m.Id, 0, int.MaxValue);
        _gridOps.Clear(m.Cell, "machine");
        State.Seats.RemoveAll(s => s.Pod == m.Id);
        return new Carried(owner.Home, load, false);
    }

    /// <summary>A pod carried here docked aboard or landed off the ship; its hero seated in it when it rode and is here and awake.</summary>
    public void Arrive(int peer, object? carried, int? cell)
    {
        if (carried is Rode r)
        {
            Ride(peer, r);
            return;
        }
        if (carried is not Carried c) return;
        int? pod = Surfaces.Here == 0 ? Dock(peer, c.Home, c.Load) : Land(peer, c.Home, c.Load);
        if (c.Rides && pod is int p && _crew.HeroOf(peer) is { Out: 0 }) Sit(p, peer);
    }

    /// <summary>
    /// A voyage's rider arrived: the one bringing the shuttle docks it at its home aboard (laid there as its cost when it cannot) or stops it
    /// by the vessel; the others find it there. Seated at its lowest free place when it is awake.
    /// </summary>
    private void Ride(int peer, Rode r)
    {
        int? shuttle = (r.Brings, Surfaces.Here == 0) switch
        {
            (true, true) => Moor(peer, r.Home),
            (true, false) => Stop(r.Home),
            // Its home built over by another shuttle meanwhile, the rider sits in that one.
            (false, true) => _grid.MachineAt(r.Home) is { } m && m.Cell == r.Home && Content.Machines.Get(m.Kind).Riders > 1 ? m.Id : null,
            (false, false) => State.Parked.Find(p => p.Home == r.Home)?.Shuttle,
        };
        if (shuttle is int s && _crew.HeroOf(peer) is { Out: 0 } && FreePlace(s, Content.Machines.Get(_grid.MachineById(s)!.Kind).Riders) != null) Sit(s, peer);
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
        // A launch mostly from a hero in a pod, so the fuzz flies some; from anyone else it is refused.
        if (rng.Below(SeatOf(from) != null ? 2u : 8u) == 0) return new Launch { From = from };
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
