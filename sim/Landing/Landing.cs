using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;

namespace Stillwake.Sim.Landing;

/// <summary>Hero <c>Peer</c> sitting in drop pod <c>Pod</c>, a machine id.</summary>
public sealed record Seat(int Pod, int Peer);

/// <summary>
/// Who sits in which drop pod; a platform's state is its structure codes, the grid's. Public so modules can be typed by it, its
/// members internal.
/// </summary>
public sealed class LandingState
{
    /// <summary>Every seat taken, by pod id ascending: a pod holds one hero, a hero sits in one pod.</summary>
    [JsonInclude] internal List<Seat> Seats { get; set; } = [];
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
/// The launch platform and the drop pod, and to come its landers (the 2026-10-24 launch platform ADR, the drop pod's): a run of
/// platform pieces opens whole, only while its network has power enough, and shuts with or without; a hero boards a pod standing
/// on one and sits in it, one hero a pod. Nothing launches yet.
/// </summary>
public sealed class LandingModule : Module<LandingState>
{
    private readonly IGridQueries _grid;
    private readonly IPowerQueries _power;
    private readonly ICrewQueries _crew;

    public override string Id => "landing";

    public LandingModule(IGridQueries grid, IGridOps gridOps, IPowerQueries power, ICrewQueries crew)
    {
        _grid = grid;
        _power = power;
        _crew = crew;
        Handles<SetPlatform>(c => Problem(c.Cell, c.Open), c => gridOps.SetDoorway(c.Cell, c.Open));
        Handles<Board>(c => BoardProblem(c.From, c.Pod), c => Sit(c.Pod, c.From));
        Handles<Alight>(c => SeatOf(c.From) == null ? "landing.notSeated" : null, c => State.Seats.RemoveAll(s => s.Peer == c.From));
        // A pod taken apart, or its hero gone from the surface or passed out, frees its seat.
        System(Phase.Derivation, () => State.Seats.RemoveAll(s => !Holds(s)));
    }

    /// <summary>
    /// Why <paramref name="peer"/>'s hero cannot board machine <paramref name="pod"/>: no drop pod (<c>landing.noPod</c>), sitting in a
    /// pod already (<c>landing.seated</c>), or another hero in it (<c>landing.taken</c>); null when it can.
    /// </summary>
    public string? BoardProblem(int peer, int pod)
    {
        if (_grid.MachineById(pod) is not { } m || !Content.Machines.Is(m.Kind, MachineTrait.Seats)) return "landing.noPod";
        if (SeatOf(peer) != null) return "landing.seated";
        return RiderOf(pod) != null ? "landing.taken" : null;
    }

    /// <summary>The drop pod <paramref name="peer"/>'s hero sits in, by machine id; null for one standing.</summary>
    public int? SeatOf(int peer) => State.Seats.Find(s => s.Peer == peer)?.Pod;

    /// <summary>The peer whose hero sits in drop pod <paramref name="pod"/>; null for an empty one.</summary>
    public int? RiderOf(int pod) => State.Seats.Find(s => s.Pod == pod)?.Peer;

    /// <summary>Every seat taken, by pod id ascending.</summary>
    public IReadOnlyList<Seat> Seats => State.Seats;

    private void Sit(int pod, int peer)
    {
        int at = State.Seats.FindIndex(s => s.Pod > pod);
        State.Seats.Insert(at < 0 ? State.Seats.Count : at, new Seat(pod, peer));
    }

    /// <summary>Whether <paramref name="seat"/> still holds: its pod stands, its hero is here and awake.</summary>
    private bool Holds(Seat seat) =>
        _grid.MachineById(seat.Pod) is { } m && Content.Machines.Is(m.Kind, MachineTrait.Seats) && _crew.HeroOf(seat.Peer) is { Out: 0 };

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
    /// Now and then a pod boarded or left, a platform opened or shut: mostly on a pod or a platform, sometimes on anything, so
    /// refusals are drawn too.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(8) != 0) return null;
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
