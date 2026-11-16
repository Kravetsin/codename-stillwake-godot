using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Landing;

namespace Stillwake.Sim.Places;

/// <summary>
/// A vessel the radar found that nobody has gone to (generated-vessels): its class and the seed it grows from; <c>Offset</c>
/// radar units along the ship's orbit, minus left of the ship, and the ticks <c>Left</c> before it goes
/// (2026-11-08-radar-contacts).
/// </summary>
public sealed record Contact(int Id, string Class, uint Seed, int Offset, int Left);

/// <summary>Finds a vessel of <c>Class</c> at once: a contact as the radar's own come, its seed and place drawn from the world's Rng. Dev tools'.</summary>
public sealed record Find(string Class) : Command;

/// <summary>
/// Goes to contact <c>Contact</c> (vessel-worlds): a surface of its own opened with the vessel written in its middle, as built,
/// and the sender's hero moved there, just inside its door. The contact is used up.
/// </summary>
public sealed record Jump(int Contact) : Command;

/// <summary>Sets the course to contact <c>Contact</c> (shuttle-flight): where every shuttle aboard flies once set off. The radar's «Set course» sends it.</summary>
public sealed record SetCourse(int Contact) : Command;

/// <summary>
/// The shuttle the sender sits in sets off with its riders (shuttle-flight): aboard to the course's vessel, the contact used up; off the
/// ship home. The shuttle's panel sends it, its «Set off» key.
/// </summary>
public sealed record SetOff : Command;

/// <summary>The places slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class PlacesState
{
    /// <summary>The contacts the radar holds, in the order found: at most <c>Vessels.MaxContacts</c>.</summary>
    [JsonInclude] internal List<Contact> Contacts { get; set; } = [];
    [JsonInclude] internal int NextContactId { get; set; } = 1;
    /// <summary>Ticks till the radar finds the next contact by itself; 0 before the first is counted down.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int NextIn { get; set; }
    /// <summary>The contact every shuttle aboard flies to once set off, by id; null for none.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] internal int? Course { get; set; }
}

/// <summary>
/// The places the radar finds and the way to them (generated-vessels, vessel-worlds): the contacts, aboard, and the jump that
/// opens a contact's vessel world and moves its sender there; on a vessel world, the vessel written as it opens.
/// </summary>
public sealed class PlacesModule : Module<PlacesState>
{
    private readonly IGridQueries _grid;
    private readonly IGridOps _gridOps;
    private readonly IItemsOps _itemsOps;
    private readonly ICrewQueries _crew;
    private readonly ILandingQueries _landing;
    private readonly ILandingOps _landingOps;

    public override string Id => "places";

    public PlacesModule(IGridQueries grid, IGridOps gridOps, IItemsOps itemsOps, ICrewQueries crew, ILandingQueries landing, ILandingOps landingOps)
    {
        _grid = grid;
        _gridOps = gridOps;
        _itemsOps = itemsOps;
        _crew = crew;
        _landing = landing;
        _landingOps = landingOps;
        Handles<Find>(FindProblem, ApplyFind);
        Handles<Jump>(JumpProblem, ApplyJump);
        Handles<SetCourse>(CourseProblem, c => State.Course = c.Contact);
        Handles<SetOff>(c => SetOffProblem(c.From), ApplySetOff);
        System(Phase.Simulation, Watch);
    }

    /// <summary>The contact every shuttle aboard flies to once set off; null for none, or for one gone this tick.</summary>
    public Contact? Course => State.Course is int id ? State.Contacts.Find(k => k.Id == id) : null;

    /// <summary>
    /// The litres of fuel a trip to <paramref name="contact"/> and back costs: each leg its distance, <c>|Offset|</c> radar units of
    /// <c>Contacts.KmPerUnit</c> km, over <c>Shuttle.KmPerLitre</c>, rounded up: what setting off burns from the shuttle's store, in litres (fuel-liquid).
    /// </summary>
    public static int CostOf(Contact contact)
    {
        int km = (contact.Offset < 0 ? -contact.Offset : contact.Offset) * Content.Contacts.KmPerUnit;
        return 2 * ((km + Content.Shuttle.KmPerLitre - 1) / Content.Shuttle.KmPerLitre);
    }

    /// <summary>Why a course cannot be set to <paramref name="c"/>'s contact: off the ship (<c>places.notAboard</c>), or one the radar does not hold (<c>places.noContact</c>).</summary>
    private string? CourseProblem(SetCourse c) =>
        Rules != Content.Surfaces.Ship ? "places.notAboard"
        : State.Contacts.Exists(k => k.Id == c.Contact) ? null : "places.noContact";

    /// <summary>
    /// Why <paramref name="peer"/>'s hero cannot set off in its shuttle: aboard, no course set, or its contact gone (<c>places.noCourse</c>);
    /// then what Landing says (<see cref="ILandingQueries.SetOffProblem"/>): aboard its store short of the round trip, <see cref="CostOf"/>
    /// in millilitres, is <c>landing.noFuel</c>; the return is free. Null when it can.
    /// </summary>
    public string? SetOffProblem(int peer) =>
        Surfaces.Here != 0 ? _landing.SetOffProblem(peer, 0)
        : Course is { } course ? _landing.SetOffProblem(peer, Burnt(course)) : "places.noCourse";

    /// <summary>The fuel a trip to <paramref name="contact"/> and back burns from the shuttle's store, in millilitres, all at setting off.</summary>
    private static int Burnt(Contact contact) => CostOf(contact) * Content.Fuel.Litre;

    /// <summary>Aboard, the course's contact used up and the course cleared, the shuttle off to its vessel; elsewhere, off home.</summary>
    private void ApplySetOff(SetOff c)
    {
        if (Surfaces.Here != 0)
        {
            _landingOps.SetOffHome(c.From);
            return;
        }
        var contact = Course!;
        State.Contacts.Remove(contact);
        State.Course = null;
        _landingOps.SetOff(c.From, Vessels.Kinds[Vessels.IndexOf(contact.Class)].Id, contact.Seed, Burnt(contact));
    }

    protected override PlacesState Init(Layout layout) => new();

    /// <summary>The contacts the radar holds, in the order found: live and read-only.</summary>
    public IReadOnlyList<Contact> Contacts => State.Contacts;

    /// <summary>Ticks till the radar finds the next contact by itself; 0 before the world's first tick aboard.</summary>
    public int NextIn => State.NextIn;

    private string? FindProblem(Find c) =>
        Rules != Content.Surfaces.Ship ? "places.notAboard"
        : Vessels.Class(c.Class) == null ? "places.noClass"
        : State.Contacts.Count >= Vessels.MaxContacts ? "places.full" : null;

    private void ApplyFind(Find c) => Add(c.Class);

    /// <summary>
    /// A contact of <paramref name="cls"/>: its seed drawn first, then its side and distance on the orbit, each evenly; seen
    /// <c>Contacts.Lasts</c> ticks.
    /// </summary>
    private void Add(string cls)
    {
        uint seed = (uint)Random.Below(1UL << 32);
        int side = Random.Below(2) == 0 ? -1 : 1;
        int distance = Content.Contacts.Nearest + (int)Random.Below((uint)(Content.Contacts.Reach - Content.Contacts.Nearest + 1));
        State.Contacts.Add(new(State.NextContactId++, cls, seed, side * distance, Content.Contacts.Lasts));
    }

    /// <summary>
    /// The radar's watch, aboard, each tick (2026-11-08-radar-contacts): every contact's time runs down and one run out goes;
    /// then the countdown, the first started at <c>First</c>; at 0 a contact of a class drawn by its weight comes (big-vessels), while the radar has
    /// room, and the next is drawn. Gone before found, so with <c>Lasts</c> no more than <c>EveryMin</c> two of its own are
    /// never up at once.
    /// </summary>
    private void Watch()
    {
        if (Rules != Content.Surfaces.Ship) return;
        var s = State;
        for (int i = s.Contacts.Count - 1; i >= 0; i--)
        {
            if (s.Contacts[i].Left > 1) s.Contacts[i] = s.Contacts[i] with { Left = s.Contacts[i].Left - 1 };
            else s.Contacts.RemoveAt(i);
        }
        // Its contact run out, or jumped to from the dev panel: the course goes with it.
        if (s.Course != null && Course == null) s.Course = null;
        if (s.NextIn == 0) s.NextIn = Content.Contacts.First;
        if (--s.NextIn > 0) return;
        if (s.Contacts.Count < Vessels.MaxContacts) Add(Vessels.Weighed(Vessels.Classes, (int)Random.Below((uint)Vessels.Weights(Vessels.Classes))).Id);
        s.NextIn = Content.Contacts.EveryMin + (int)Random.Below((uint)(Content.Contacts.EveryMax - Content.Contacts.EveryMin + 1));
    }

    /// <summary>
    /// Aboard, of a contact the radar holds, by a hero standing: one with no hero yet would hold the vessel world open with no
    /// farewell to close it, and a drop pod's rider would bring its pod along (vessel-worlds).
    /// </summary>
    private string? JumpProblem(Jump c)
    {
        if (Rules != Content.Surfaces.Ship) return "places.notAboard";
        if (State.Contacts.Find(k => k.Id == c.Contact) is null) return "places.noContact";
        if (_crew.HeroOf(c.From) is null) return "places.noHero";
        return _landing.Rides(c.From) ? "places.seated" : null;
    }

    /// <summary>The contact used up, and a surface of its class's vessel kind opened from its seed with the sender on it (vessel-worlds).</summary>
    private void ApplyJump(Jump c)
    {
        var contact = State.Contacts.Find(k => k.Id == c.Contact)!;
        State.Contacts.Remove(contact);
        Surfaces.Visit(c.From, Vessels.Kinds[Vessels.IndexOf(contact.Class)].Id, contact.Seed);
    }

    /// <summary>
    /// On a vessel world as it opens: the vessel described from its class and the surface's seed, its parts laid in reading order
    /// with its frame's top-left at (<c>Vessels.Margin</c>, <c>Margin</c>), so its machines' ids follow it; its crates filled, the doors
    /// it left open opened.
    /// </summary>
    protected override void Opened()
    {
        if (Rules is not VesselKind kind) return;
        var vessel = Shipyard.Describe(Vessels.Class(kind.Class)!, Surfaces.Seed);
        int width = _grid.Width, m = Vessels.Margin;
        var parts = Shipyard.Stamp(vessel);
        foreach (var part in parts)
        {
            int cell = (m + part.Y) * width + m + part.X;
            // The map's spawn (its S) comes with a back wall of the hull, the vessel's own inside its door.
            if (part.Layer == "back" && _grid.BackAt(cell) != null) continue;
            _gridOps.Lay(cell, new Placement(part.Layer, part.Kind, part.Material));
            // A crate's loot is born here, so it is counted obtained here (the-board), as a salvage drone's catch is.
            if (part.Holds is { } holds && _grid.MachineAt(cell) is { } crate)
            {
                foreach (var (item, count) in holds) _itemsOps.PutIn(crate.Id, new Stack(item, count));
                _itemsOps.Caught(holds);
            }
        }
        // A door left open is laid shut, then opened whole once all its pieces stand (derelict-looks).
        foreach (var part in parts)
            if (part.Open) _gridOps.SetDoorway((m + part.Y) * width + m + part.X, true);
    }

    /// <summary>
    /// Now and then, as a jump opens a whole surface: a contact found, of a class or none; a jump to one, or a course set to one, held or
    /// not; a set off, mostly refused but from a shuttle's rider.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(16) != 0) return null;
        uint what = rng.Below(4);
        if (what == 0)
        {
            string cls = rng.Below(5) == 0 ? "nothing" : Vessels.Classes[(int)rng.Below((uint)Vessels.Classes.Count)].Id;
            return new Find(cls) { From = from };
        }
        if (what == 3) return new SetOff { From = from };
        int contact = State.Contacts.Count > 0 && rng.Below(4) != 0
            ? State.Contacts[(int)rng.Below((uint)State.Contacts.Count)].Id
            : (int)rng.Below(4);
        return what == 1 ? new Jump(contact) { From = from } : new SetCourse(contact) { From = from };
    }
}
