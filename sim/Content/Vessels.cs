namespace Stillwake.Content;

/// <summary>
/// What a vessel's compartment is for: its interior and its crates' loot follow it (generated-vessels). The big classes' rooms
/// after the first five (big-vessels), furnished by code; a liner's promenade and dome are lounges, a tanker's pipeway its first deck over
/// its bays.
/// </summary>
public enum VesselRole { Hold, Quarters, Engine, Life, Bridge, Pool, Restaurant, Lounge, Galley, Infirmary, Hangar, TankBay, FuelBay, PumpRoom, Pipeway }

/// <summary>How a class's vessel is laid out (big-vessels): rooms a deck tall along its decks, or a big class's blocks in bands.</summary>
public enum VesselLayout { Rooms, Liner, Tanker }

/// <summary>What wrecked a vessel, which its damage pass follows: none for one intact.</summary>
public enum VesselCause { None, Breach }

/// <summary>Where a vessel's bridge is: a cockpit in the bow on the first deck, or raised over the hull toward the bow.</summary>
public enum VesselBridge { Cockpit, Raised }

/// <summary>Which decks a room of a role may stand on: any, the first (the top), or one below it.</summary>
public enum VesselDeck { Any, Top, Low }

/// <summary>
/// Where a room of <c>Role</c> goes (a vessel of rooms): the decks it may stand on, the shortest of them taken, ties to the lower;
/// and its place along a deck, <c>Order</c> from the stern.
/// </summary>
public sealed record VesselRoomRule(VesselRole Role, VesselDeck Deck, int Order);

/// <summary>
/// A class of generated vessel (generated-vessels): its size as ranges the seed picks from, and how it is built. A vessel is
/// laid along its width, stern on the left: a nozzle out of the stern a deck but the first, a body of rooms deck over deck, at most
/// <c>Length</c> columns, in modules that bulge, a bow sloping to its tip, and its bridge (<c>BridgeKind</c>).
/// </summary>
/// <param name="Length">The body's most columns inside its stern armour: an extra room that would run a deck past it is not built.</param>
/// <param name="Decks">How many decks, least and most, each six open rows over a floor; two at least: the engine room is below.</param>
/// <param name="BowFlat">Columns of the bow at the body's height before it slopes.</param>
/// <param name="BowRun">Columns a step of the bow's slope runs, least and most, drawn: its top falls a row and its bottom rises one each.</param>
/// <param name="BowTip">Rows of the bow's tip, where the slope ends.</param>
/// <param name="ArmourDepth">Cells of armour inward from the hull's edge.</param>
/// <param name="BridgeKind">A cockpit in the bow, or a bridge raised on top.</param>
/// <param name="Extras">How many rooms past the ones every vessel has, least and most.</param>
/// <param name="ExtraRoles">What an extra room is, each as likely.</param>
/// <param name="Crater">A breach's radius, in cells.</param>
/// <param name="Yield">Whole items, every kind counted alike, a vessel of this class gives taken apart whole, least and most (a content test).</param>
/// <param name="Weight">How often the radar finds one against the others (big-vessels): one of weight 0 it never finds.</param>
/// <param name="Layout">How its rooms are laid out: a vessel of rooms, or a liner's blocks (big-vessels, <see cref="Vessels.Liner"/>).</param>
public sealed record VesselClass(
    string Id,
    int Length,
    (int Min, int Max) Decks,
    int BowFlat,
    (int Min, int Max) BowRun,
    int BowTip,
    int ArmourDepth,
    VesselBridge BridgeKind,
    (int Min, int Max) Extras,
    IReadOnlyList<VesselRole> ExtraRoles,
    int Crater,
    (int Min, int Max) Yield,
    int Weight,
    VesselLayout Layout = VesselLayout.Rooms)
{
    /// <summary>Raised rooms stacked over the body: a liner's dome stands on its promenade, a freighter's bridge alone.</summary>
    public int Tiers => Layout == VesselLayout.Liner ? 2 : 1;

    /// <summary>Rows of the body for <paramref name="decks"/> decks: armour, six open rows a deck and a floor between each two.</summary>
    public int BodyHeight(int decks) => 2 * ArmourDepth + Vessels.Pitch * decks - 1;

    /// <summary>Rows above the body: a raised bridge's armour and its six open rows; none for a cockpit.</summary>
    public int BridgeHeight => BridgeKind == VesselBridge.Raised ? ArmourDepth + Vessels.Open : 0;

    /// <summary>Rows the bow's top falls and its bottom rises over a body <paramref name="body"/> rows tall: the bottom the odd row more, as a keel.</summary>
    public (int Fall, int Rise) BowSteps(int body) => ((body - BowTip) / 2, body - BowTip - (body - BowTip) / 2);

    /// <summary>
    /// A box every vessel of the class fits, columns by rows: its frame. The nozzles left of the stern; a rib over the highest bulge
    /// above and below the body, a raised bridge above instead where it stands higher, the tallest mast over either; a truss and its
    /// container under the rib, or a tanker's sphere where it hangs lower; the bow at its longest run and a boom past its tip.
    /// </summary>
    public (int Width, int Height) Frame
    {
        get
        {
            int body = BodyHeight(Decks.Max), rib = Math.Max(Vessels.Bulge.Max, Vessels.SternBulge.Max) + 1;
            var (boom, mast, truss, container) = (Machines.Get(Vessels.Boom), Machines.Get(Vessels.MastTall), Machines.Get(Vessels.Truss),
                Machines.Get(Vessels.TrussContainer));
            int keel = Math.Max(truss.Height + container.Height, Machines.Get(Vessels.Sphere).Height);
            return (Vessels.NozzleSize.W + ArmourDepth + Length + BowFlat + BowRun.Max * BowSteps(body).Rise + boom.Width,
                Math.Max(BridgeHeight * Tiers, rib) + mast.Height + body + rib + keel);
        }
    }
}

/// <summary>
/// A compartment's furnishing drawn as ASCII (generated-vessels), a character a cell in <see cref="Legend"/>'s: <c>.</c> open,
/// <c>=</c> a floor (a shelf, a bunk), a machine by its whole footprint. Six rows, its first two columns open for a ladder or a
/// hatch over them, but the main engine's, flush with the stern.
/// </summary>
/// <param name="Main">Whether it holds the main engine, at its first column: a lower deck's engine room's, against the stern by its nozzle.</param>
public sealed record VesselInterior(string Id, VesselRole Role, IReadOnlyList<string> Rows, bool Main = false)
{
    public int Width => Rows[0].Length;
}

/// <summary>What a crate in a compartment of a role may hold: an item and how many, least and most, drawn per stack.</summary>
public sealed record VesselLoot(string Item, int Min, int Max);

/// <summary>
/// A salvage object aboard a vessel (plan V-1b): a pile (hold-piles) copied from the piece of debris <c>From</c> that drifts by,
/// its size, hit points and picture that piece's, giving <c>Times</c> what the piece gives on average (<see cref="Vessels.SalvageCost"/>),
/// so its look says what it gives, drawn whole. It stands in compartments of its <c>Roles</c>; a <c>Torn</c> one is also what a breach
/// leaves by its crater.
/// </summary>
/// <param name="Size">Its footprint for a piece that is no wreckage (a crate, a pod), whose size the sim does not keep; else the wreck's.</param>
/// <param name="Hp">Its hit points for a piece that is no wreckage; else the wreck's.</param>
/// <param name="Looks">How many pictures it has, each a machine row of its own alike but for it (<see cref="Vessels.LookKind"/>): one
/// is drawn for each stamped, interiors drawing the first.</param>
/// <param name="Has">What its machine rows do besides being taken apart: a vending machine still glows.</param>
public sealed record VesselSalvage(
    string Kind, string From, char Char, IReadOnlyList<VesselRole> Roles, int Times = Vessels.SalvageTimes,
    (int W, int H)? Size = null, int? Hp = null, bool Torn = false, int Looks = 1, MachineTrait Has = MachineTrait.None);

/// <summary>
/// The kind of a surface holding one vessel of class <see cref="Class"/> in its middle (vessel-worlds): space as the ship's,
/// nothing drifting by, built as a planet allows and broken by tools, as a planet is, since a mark's plan would be erased
/// with it. Transient: it closes once nobody stands on it. Its grid is made as it opens, from the vessel.
/// </summary>
public sealed record VesselKind : Surfaces.Kind
{
    public VesselKind(string cls) : base("vessel." + cls, [])
    {
        Class = cls;
        Debris = false;
        Builds = PlanetTypes.First.Builds;
        ToolsBreak = true;
        Transient = true;
    }

    /// <summary>The class of the vessel on it (<see cref="Vessels.Classes"/>).</summary>
    public string Class { get; }
}

/// <summary>Generated vessels and their wrecks (generated-vessels): classes, interiors, loot and where a vessel lies.</summary>
public static class Vessels
{
    /// <summary>Open rows a deck holds: a doorway's height (the hero is under six tall).</summary>
    public const int Open = 6;

    /// <summary>Rows from one deck to the next: its open rows and the floor under them.</summary>
    public const int Pitch = Open + 1;

    /// <summary>Cells of space round a vessel on its own surface (vessel-worlds), every side: room to walk out and look at it.</summary>
    public const int Margin = 128;

    /// <summary>Contacts the radar keeps at once.</summary>
    public const int MaxContacts = 8;

    /// <summary>What a vessel's walls and back walls are made of (<see cref="Materials"/>): never built.</summary>
    public const string Hull = "hull";

    /// <summary>What a bridge's windows are made of: the game's clear glass, light through, a weak wall.</summary>
    public const string Glass = "glass";

    /// <summary>How many times its piece's loot a salvage object gives, unless it says otherwise.</summary>
    public const int SalvageTimes = 5;

    /// <summary>Items salvage never multiplies: what is kept rare (stamped-and-assembled) is given as its piece gives it.</summary>
    public static readonly IReadOnlyList<string> Unmultiplied = ["circuit"];

    /// <summary>Salvage aboard (plan V-1b): a pile for each piece of debris a vessel holds whole, where it stands.</summary>
    public static readonly IReadOnlyList<VesselSalvage> Salvage =
    [
        new("salvageContainer", "cargoContainer", 'j', [VesselRole.Hold]),
        new("salvageCrate", "cargoCrate", 'l', [VesselRole.Hold, VesselRole.Quarters, VesselRole.Hangar], Size: (3, 2), Hp: 6),
        new("salvagePod", "supplyPod", 'm', [VesselRole.Quarters, VesselRole.Life], Size: (2, 2), Hp: 6),
        new("salvageTanks", "tankCluster", 'p', [VesselRole.Life]),
        new("salvageMachine", "machineBlock", 'r', [VesselRole.Engine]),
        // An engine's piece already gives much; five of it would flood the hold with gears.
        new("salvageEngine", "engineBlock", 's', [VesselRole.Engine], Times: 2),
        new("salvageRack", "cableRack", 't', [VesselRole.Bridge]),
        // No soil clod stands aboard (derelict-looks): kept in no room, so a world saved on a vessel that had one still loads.
        new("salvageClod", "soilClod", 'z', []),
        // A hold module's piece gives as much as an engine's.
        new("salvageHold", "cargoHold", '3', [VesselRole.Hold], Times: 2),
        // Spares in a hold; and what tore the hull, left by a breach on the deck by its crater.
        new("salvageSection", "hullSection", '1', [VesselRole.Hold], Torn: true),
        new("salvageSlab", "shieldSlab", '2', [VesselRole.Hold], Torn: true),
        // The main engine (a vessel of rooms): one a deck below the first, against the stern by its nozzle, its dearest thing.
        new("salvageMainEngine", "engineBlock", '4', [VesselRole.Engine], Times: 3, Size: (12, 6), Looks: 3),
        // A bridge's console, its screens and switches a cable rack's insides.
        new("salvageConsole", "cableRack", '5', [VesselRole.Bridge], Size: (4, 3), Looks: 4),
        // A vending machine in the mess (food-and-the-body): a fridge's food, its sign still lit by a battery of its own, as the
        // emergency lamp is; an enamel cabinet or a glass case.
        new("salvageVending", "fridge", '6', [VesselRole.Quarters, VesselRole.Galley], Size: (4, 5), Hp: 8, Looks: 2, Has: MachineTrait.Glow),
    ];

    /// <summary>
    /// What <paramref name="s"/> gives: for each entry of its piece's loot (<see cref="Loot"/>), <c>Times</c> × its mean count ×
    /// its chance, rounded down, an entry under one dropped; an <see cref="Unmultiplied"/> item its own mean, rounded up.
    /// </summary>
    public static IReadOnlyList<(string Item, int Count)> SalvageCost(VesselSalvage s)
    {
        var cost = new List<(string Item, int Count)>();
        foreach (var e in global::Stillwake.Content.Loot.Of(s.From) ?? throw new InvalidOperationException($"vessels: {s.From} has no loot"))
        {
            int odds = e.OneIn ?? 1;
            int count = Unmultiplied.Contains(e.Item) ? (e.Min + e.Max + 2 * odds - 1) / (2 * odds) : s.Times * (e.Min + e.Max) / (2 * odds);
            if (count > 0) cost.Add((e.Item, count));
        }
        return cost;
    }

    /// <summary>
    /// The machines aboard but salvage that come in several looks (derelict-looks), each a machine row alike but for its picture
    /// (<see cref="LookKind(string, int)"/>), one drawn for each stamped: the cargo containers, a colour a look.
    /// </summary>
    public static readonly IReadOnlyList<(string Kind, int Looks)> Coloured =
        [(Container, ContainerColours), (Cargo, ContainerColours), (Capsule, CapsuleContents), (Chair, ChairFacings), (TankTall, TankTallContents), (Riser, RiserLooks),
            (Sphere, SphereContents)];

    /// <summary>How many looks a machine aboard of first look <paramref name="kind"/> comes in: one for any but those of several.</summary>
    public static int LooksOf(string kind)
    {
        foreach (var s in Salvage) if (s.Kind == kind) return s.Looks;
        foreach (var (k, looks) in Coloured) if (k == kind) return looks;
        return 1;
    }

    /// <summary>The first look of any look <paramref name="kind"/> is: the kind it is drawn as in interiors and named by in rules.</summary>
    public static string FirstLook(string kind)
    {
        if (SalvageOf(kind) is { } s) return s.Kind;
        foreach (var (k, looks) in Coloured)
            for (int look = 0; look < looks; look++)
                if (LookKind(k, look) == kind) return k;
        return kind;
    }

    /// <summary>The salvage that <paramref name="kind"/> is, any of its looks; null for any other machine.</summary>
    public static VesselSalvage? SalvageOf(string kind)
    {
        foreach (var s in Salvage)
            for (int look = 0; look < s.Looks; look++)
                if (LookKind(s, look) == kind) return s;
        return null;
    }

    /// <summary>The machine row of <paramref name="s"/>'s look <paramref name="look"/>: its own kind for the first, numbered from 2 after it.</summary>
    public static string LookKind(VesselSalvage s, int look) => LookKind(s.Kind, look);

    /// <summary>The machine row of <paramref name="kind"/>'s look <paramref name="look"/>: the kind itself for the first, numbered from 2 after it.</summary>
    public static string LookKind(string kind, int look) => look == 0 ? kind : kind + (look + 1);

    /// <summary><paramref name="row"/> in <paramref name="looks"/> looks, a row each, alike but for the kind; the first keeps its character.</summary>
    private static IEnumerable<MachineSpec> Looked(MachineSpec row, int looks) =>
        Enumerable.Range(0, looks).Select(look => look == 0 ? row : row with { Kind = LookKind(row.Kind, look), Char = null, Means = null });

    /// <summary>The machine rows of <see cref="Salvage"/>, a row a look, piles with no menu card, joined to <c>Machines.All</c>.</summary>
    public static readonly IReadOnlyList<MachineSpec> SalvageRows = Salvage.SelectMany(s =>
    {
        var (w, h) = s.Size ?? Wreckage.Sizes.Where(z => z.Item == s.From).Select(z => (z.W, z.H)).First();
        int hp = s.Hp ?? Wreckage.HpOf(s.From) ?? throw new InvalidOperationException($"vessels: {s.From} has no hit points");
        var cost = SalvageCost(s);
        return Enumerable.Range(0, s.Looks).Select(look => new MachineSpec(LookKind(s, look), w, h, [], cost, BuildTicks: 160, Hp: hp, Has: s.Has,
            Char: look == 0 ? s.Char : null, Means: look == 0 ? $"salvage, a {s.From} {s.Times} times over: draw its whole footprint" : null));
    }).ToList();

    /// <summary>The main engine's nozzle out through the stern (derelict-looks), a lower deck's: never the <c>nozzle</c> item, which the Former keeps.</summary>
    public const string Nozzle = "vesselNozzle";

    /// <summary>A nozzle's footprint, columns by rows: its deck's open rows and floor.</summary>
    public static readonly (int W, int H) NozzleSize = (8, 7);

    /// <summary>The parts outside a vessel's hull but the nozzle (derelict-looks), by the names <see cref="Outside"/> gives them.</summary>
    public const string Rcs = "vesselRcs", Mast = "vesselMast", MastTall = "vesselMastTall", Dish = "vesselDish", Solar = "vesselSolar",
        Tank = "vesselTank", Radiator = "vesselRadiator", Collar = "vesselCollar", Truss = "vesselTruss", Pylon = "vesselPylon",
        Boom = "vesselBoom";

    /// <summary>
    /// The big classes' parts outside (big-vessels), by the names <see cref="Outside"/> gives them: a liner's fin, its escape pods and
    /// the cradles of those gone; a tanker's manifold, its risers down to the bays, the dome hatches on them, the spheres under its keel.
    /// </summary>
    public const string Fin = "vesselFin", Pod = "vesselPod", Cradle = "vesselCradle", Manifold = "vesselManifold", Riser = "vesselRiser",
        Dome = "vesselDome", Sphere = "vesselSphere";

    /// <summary>A riser's looks: a length of pipe, and the one over the manifold carrying its valve's wheel.</summary>
    public const int RiserLooks = 2;

    /// <summary>A sphere's looks, its contents as a tall tank's: water, oxygen; it holds what the bay over it does.</summary>
    public const int SphereContents = TankTallContents;

    /// <summary>The columns of a sphere its two struts rise from, from its left: each reaches the hull's bottom row right over it.</summary>
    public static readonly IReadOnlyList<int> SpherePosts = [2, 7];

    /// <summary>
    /// The parts outside a vessel's hull (derelict-looks), each a machine row with no menu card, taken apart for what it gives; each
    /// stands in space against the hull, nothing behind it. A shuttle's mast is <c>Mast</c>, a freighter's on its bridge <c>MastTall</c>.
    /// </summary>
    public static readonly IReadOnlyList<MachineSpec> Outside =
    [
        new(Nozzle, NozzleSize.W, NozzleSize.H, [], [("plate", 4), ("copper", 2)], BuildTicks: 160, Hp: 24),
        new(Rcs, 3, 2, [], [("tube", 2), ("scrap", 3)], BuildTicks: 160, Hp: 8),
        new(Mast, 2, 8, [], [("wire", 3), ("coil", 1)], BuildTicks: 160, Hp: 8),
        new(MastTall, 2, 10, [], [("wire", 3), ("coil", 1)], BuildTicks: 160, Hp: 8),
        new(Dish, 5, 5, [], [("circuit", 1), ("plate", 2), ("wire", 2)], BuildTicks: 160, Hp: 10),
        new(Solar, 11, 6, [], [("crystal", 1), ("wire", 2), ("frame", 1)], BuildTicks: 160, Hp: 10),
        new(Tank, 16, 3, [], [("plate", 2), ("tube", 2)], BuildTicks: 160, Hp: 12),
        new(Radiator, 16, 6, [], [("copper", 4), ("plate", 3), ("tube", 2)], BuildTicks: 160, Hp: 16),
        new(Collar, 6, 3, [], [("plate", 4), ("gear", 2)], BuildTicks: 160, Hp: 16),
        new(Truss, 11, 3, [], [("frame", 2), ("scrap", 2)], BuildTicks: 160, Hp: 12),
        new(Pylon, 1, 1, [], [("scrap", 1)], BuildTicks: 80, Hp: 4),
        new(Boom, 8, 2, [], [("wire", 2), ("circuit", 1)], BuildTicks: 160, Hp: 8),
        // The mockup's tooltip: plates and the cargo inside, here a hold's planks, wire and tubes.
        .. Looked(new(Container, 10, 4, [], [("plate", 6), ("planks", 8), ("wire", 3), ("tube", 2)], BuildTicks: 160, Hp: 14), ContainerColours),
        // The big classes' (big-vessels, the design's outside rows): a pod's seat its cloth, the valve on a riser its own pipe.
        new(Fin, 13, 10, [], [("plate", 8), ("copper", 4)], BuildTicks: 240, Hp: 24),
        new(Pod, 5, 4, [], [("plate", 4), ("cloth", 1), ("wire", 2)], BuildTicks: 160, Hp: 12),
        new(Cradle, 5, 3, [], [("frame", 2)], BuildTicks: 120, Hp: 8),
        new(Manifold, 12, 2, [], [("tube", 6), ("gear", 1)], BuildTicks: 160, Hp: 12),
        .. Looked(new(Riser, 1, 1, [], [("tube", 1)], BuildTicks: 60, Hp: 4), RiserLooks),
        new(Dome, 4, 2, [], [("plate", 2), ("gear", 1)], BuildTicks: 120, Hp: 8),
        .. Looked(new(Sphere, 10, 11, [], [("plate", 10), ("tube", 4)], BuildTicks: 240, Hp: 30, Slots: TankSlots), SphereContents),
    ];

    /// <summary>
    /// What furnishes a vessel's rooms but salvage (derelict-looks, the mockup's furniture), each a machine row with no menu card, taken
    /// apart for what the mockup's tooltips say, in existing items: the ceiling lamp every room has, burning on the vessel's own battery
    /// with no power; bunks, a locker, a table and a pilot's seat; life support's tanks; a console; a machine block and a cable rack; a
    /// hold's cargo container, in the keel's colours.
    /// </summary>
    public static readonly IReadOnlyList<MachineSpec> Furniture =
    [
        new(Lamp, 2, 1, [], [("wire", 1), ("glass", 1)], BuildTicks: 60, Hp: 4, Has: MachineTrait.Glow | MachineTrait.Hangs, Char: '*', Means: "a derelict's ceiling lamp, on its own battery"),
        new(Bunk, 5, 5, [], [("plate", 2), ("cloth", 2)], BuildTicks: 160, Hp: 8, Char: '[', Means: "a derelict's bunks: draw its whole footprint"),
        new(Locker, 2, 5, [], [("plate", 2)], BuildTicks: 120, Hp: 6, Char: ']', Means: "a derelict's locker: draw its whole footprint"),
        new(Table, 3, 2, [], [("plate", 1)], BuildTicks: 80, Hp: 4, Char: '+', Means: "a derelict's table: draw its whole footprint"),
        new(Seat, 2, 3, [], [("plate", 1), ("cloth", 1)], BuildTicks: 80, Hp: 4, Char: '@', Means: "a derelict's pilot's seat: draw its whole footprint"),
        new(LifeTank, 4, 4, [], [("canister", 2), ("tube", 2)], BuildTicks: 160, Hp: 8, Char: '0', Means: "a derelict's life-support tank: draw its whole footprint"),
        new(Console, 4, 3, [], [("circuit", 2), ("wire", 3)], BuildTicks: 160, Hp: 8, Char: '>', Means: "a derelict's console: draw its whole footprint"),
        new(Machine, 6, 4, [], [("gear", 3), ("rotor", 1)], BuildTicks: 160, Hp: 12, Char: '{', Means: "a derelict's machine block: draw its whole footprint"),
        new(Rack, 4, 4, [], [("wire", 6), ("copper", 2)], BuildTicks: 160, Hp: 8, Char: '}', Means: "a derelict's cable rack: draw its whole footprint"),
        .. Looked(new(Cargo, 8, 4, [], [("plate", 4), ("planks", 6), ("wire", 2)], BuildTicks: 160, Hp: 12, Char: '7', Means: "a derelict's cargo container: draw its whole footprint"), ContainerColours),
        // A liner's (big-vessels, the mockup's furnish and its tooltips), set by code, never drawn in an interior: the restaurant's bar,
        // piano, round tables, chairs (facing either way) and chandeliers, burning as the ceiling lamp does; the pool's basin (its
        // half-drained water drawn in), diving board and loungers; the galley's stove and counters; the infirmary's beds and cabinet.
        new(Bar, 7, 3, [], [("planks", 3), ("glass", 2), ("plate", 1)], BuildTicks: 160, Hp: 10),
        new(Piano, 5, 3, [], [("planks", 4), ("wire", 3), ("copper", 1)], BuildTicks: 160, Hp: 10),
        new(RoundTable, 3, 2, [], [("planks", 1), ("cloth", 1)], BuildTicks: 80, Hp: 4),
        .. Looked(new(Chair, 1, 3, [], [("planks", 1)], BuildTicks: 60, Hp: 3), ChairFacings),
        new(Lounger, 4, 2, [], [("frame", 1), ("cloth", 1)], BuildTicks: 80, Hp: 4),
        new(Diving, 5, 8, [], [("frame", 2), ("planks", 1)], BuildTicks: 120, Hp: 8),
        new(Pool, 20, 5, [], [("plate", 6), ("tube", 2)], BuildTicks: 240, Hp: 20),
        new(Chandelier, 3, 3, [], [("glass", 3), ("wire", 2), ("copper", 1)], BuildTicks: 120, Hp: 6, Has: MachineTrait.Glow | MachineTrait.Hangs),
        new(Stove, 4, 3, [], [("plate", 2), ("coil", 1), ("wire", 1)], BuildTicks: 120, Hp: 8),
        new(Counter, 5, 2, [], [("plate", 2)], BuildTicks: 80, Hp: 6),
        new(Bed, 4, 2, [], [("frame", 1), ("cloth", 1)], BuildTicks: 80, Hp: 4),
        new(MedCab, 2, 5, [], [("plate", 2), ("glass", 1), ("canister", 1)], BuildTicks: 120, Hp: 6),
        // The hangar's fuel cart and the fuel tank by a liner's engines, a look a content (fuel first; the tanker's bays the rest).
        new(FuelCart, 3, 2, [], [("plate", 2), ("tube", 1)], BuildTicks: 80, Hp: 6),
        .. Looked(new(Capsule, 14, 11, [], [("plate", 10), ("tube", 4)], BuildTicks: 240, Hp: 30, Slots: TankSlots), CapsuleContents),
        // A tanker's (big-vessels, the mockup's furnish): the pump room's pump and its wall panel of valves, a pipeway's too; a bay's
        // tall tanks, a look a content but fuel (water, oxygen); the fuel bay's column, one for a span of three decks, one for four. The
        // valves' panel stands floor to ceiling of a deck. Every tank holds its liquid (Fits.SmallTank and on), spilled as it breaks.
        new(Pump, 4, 4, [], [("gear", 2), ("tube", 3), ("rotor", 1)], BuildTicks: 160, Hp: 12),
        new(Valves, 3, Open, [], [("tube", 3), ("gear", 1)], BuildTicks: 120, Hp: 8),
        .. Looked(new(TankTall, 8, 11, [], [("plate", 6), ("tube", 2)], BuildTicks: 200, Hp: 20, Slots: TankSlots), TankTallContents),
        new(FuelColumn3, 10, 19, [], [("plate", 14), ("tube", 4)], BuildTicks: 300, Hp: 40, Slots: TankSlots),
        new(FuelColumn4, 10, 26, [], [("plate", 18), ("tube", 4)], BuildTicks: 360, Hp: 48, Slots: TankSlots),
        // A hangar's shuttle, one of five models (the mockup's SHUTTLE_DRAW and yields; its motor a rotor, its small nozzle copper): only taken apart.
        new(ShuttleGull, 21, 7, [], [("plate", 14), ("wire", 6), ("glass", 2), ("tube", 3)], BuildTicks: 240, Hp: 30),
        new(ShuttleMule, 24, 9, [], [("plate", 20), ("frame", 4), ("tube", 4)], BuildTicks: 240, Hp: 36),
        new(ShuttleBeetle, 16, 9, [], [("plate", 12), ("gear", 4), ("rotor", 1)], BuildTicks: 240, Hp: 30),
        new(ShuttleWhale, 24, 7, [], [("plate", 16), ("glass", 6), ("cloth", 4)], BuildTicks: 240, Hp: 32),
        new(ShuttleHeron, 16, 11, [], [("plate", 12), ("frame", 3), ("copper", 1)], BuildTicks: 240, Hp: 30),
    ];

    /// <summary>A liner's furniture (big-vessels), by the names <see cref="Furniture"/> gives it.</summary>
    public const string Bar = "vesselBar", Piano = "vesselPiano", RoundTable = "vesselRoundTable", Chair = "vesselChair", Lounger = "vesselLounger",
        Diving = "vesselDiving", Pool = "vesselPool", Chandelier = "vesselChandelier", Stove = "vesselStove", Counter = "vesselCounter",
        Bed = "vesselBed", MedCab = "vesselMedCab", FuelCart = "vesselFuelCart", Capsule = "vesselCapsule";

    /// <summary>A tanker's furniture (big-vessels), by the names <see cref="Furniture"/> gives it.</summary>
    public const string Pump = "vesselPump", Valves = "vesselValves", TankTall = "vesselTankTall", FuelColumn3 = "vesselFuelColumn3",
        FuelColumn4 = "vesselFuelColumn4";

    /// <summary>The fuel column standing in a fuel bay <paramref name="decks"/> decks tall: three or four.</summary>
    public static string FuelColumn(int decks) => decks == 3 ? FuelColumn3 : decks == 4 ? FuelColumn4
        : throw new ArgumentOutOfRangeException(nameof(decks), decks, "a fuel column spans three decks or four");

    /// <summary>A tall tank's looks, its contents: water, oxygen (the capsule's but fuel).</summary>
    public const int TankTallContents = CapsuleContents - 1;

    /// <summary>The five shuttles a hangar may hold, one a vessel (big-vessels): GULL, MULE, BEETLE, WHALE, HERON.</summary>
    public const string ShuttleGull = "vesselShuttleGull", ShuttleMule = "vesselShuttleMule", ShuttleBeetle = "vesselShuttleBeetle",
        ShuttleWhale = "vesselShuttleWhale", ShuttleHeron = "vesselShuttleHeron";

    public static readonly IReadOnlyList<string> Shuttles = [ShuttleGull, ShuttleMule, ShuttleBeetle, ShuttleWhale, ShuttleHeron];

    /// <summary>A capsule tank's looks, its contents (<see cref="Liquids"/>): fuel, water, oxygen; fuel the first.</summary>
    public const int CapsuleContents = 3;

    /// <summary>
    /// What a tank aboard holds, by its contents' place (the label stencilled on it): fuel is biofuel, water water, oxygen canisters.
    /// A capsule's look is its place; a tall tank's and a sphere's one past it (never fuel); a fuel column's fuel.
    /// </summary>
    public static readonly IReadOnlyList<string> Liquids = [Fuel.Item, Water.Item, Crew.Canister];

    /// <summary>Slots a tank holds its liquid in: the most a large tank holds (<see cref="Fits.LargeTank"/>) of the item that stacks fewest.</summary>
    public const int TankSlots = 7;

    /// <summary>The place in <see cref="Liquids"/> of what a tank of <paramref name="kind"/> (any look) holds; null for no tank.</summary>
    public static int? ContentsOf(string kind)
    {
        if (kind is FuelColumn3 or FuelColumn4) return 0;
        foreach (var (first, past) in (ReadOnlySpan<(string, int)>)[(Capsule, 0), (TankTall, 1), (Sphere, 1)])
            for (int look = 0; look < LooksOf(first); look++)
                if (LookKind(first, look) == kind) return look + past;
        return null;
    }

    /// <summary>A chair's looks: its back on the left, or mirrored.</summary>
    public const int ChairFacings = 2;

    /// <summary>How many radiators a freighter carries on top, least and most: as many of them as fit.</summary>
    public static readonly (int Min, int Max) Radiators = (1, 3);

    /// <summary>Columns between two parts side by side outside the hull.</summary>
    public const int PartGap = 3;

    /// <summary>
    /// Columns a part outside may move from its place to lie closer along the hull, off a rib; and the rows of gap a column past the
    /// hull's end counts as, so a part never hangs off it.
    /// </summary>
    public const int PartSlide = 4, PartFar = 8;

    /// <summary>Columns short of the bow a freighter's keel truss ends.</summary>
    public const int TrussShort = 6;

    /// <summary>
    /// The columns of a truss segment its two posts stand in, from its left: where the hull above it is shallower than the deepest along
    /// the truss, a <see cref="Pylon"/> a row reaches up from each to the hull.
    /// </summary>
    public static readonly IReadOnlyList<int> TrussPosts = [2, 8];

    /// <summary>A truss segment holds no container under it one time in this many.</summary>
    public const int TrussEmptyOneIn = 5;

    /// <summary>What hangs under a truss segment: a cargo container, its colour drawn.</summary>
    public const string TrussContainer = Container;

    /// <summary>The furniture inside a vessel and the keel's cargo container (derelict-looks), by the names <see cref="Furniture"/> and <see cref="Outside"/> give them.</summary>
    public const string Lamp = "vesselLamp", Bunk = "vesselBunk", Locker = "vesselLocker", Table = "vesselTable", Seat = "vesselSeat",
        LifeTank = "vesselLifeTank", Console = "vesselConsole", Machine = "vesselMachine", Rack = "vesselRack", Cargo = "vesselCargo",
        Container = "vesselContainer";

    /// <summary>How many colours a cargo container comes in, a look each: the mockup's rust red, faded blue, ochre, olive, grey, plum.</summary>
    public const int ContainerColours = 6;

    /// <summary>The machines a vessel may hold: fair loot that keeps no state in another module by its id.</summary>
    public static readonly IReadOnlyList<string> Machines =
        ["crate", .. SalvageRows.Select(r => r.Kind), .. Outside.Select(r => r.Kind), .. Furniture.Select(r => r.Kind)];

    /// <summary>Columns from a module's start to the next's past the aft rooms, least and most (derelict-looks).</summary>
    public static readonly (int Min, int Max) ModuleEvery = (22, 38);

    /// <summary>Columns short of the bow no module boundary comes nearer than.</summary>
    public const int ModuleStop = 14;

    /// <summary>Rows a module bulges above the body and below it, each drawn; the stern module's, which holds the engines.</summary>
    public static readonly (int Min, int Max) Bulge = (0, 2), SternBulge = (1, 2);

    /// <summary>Columns of a rib at a module boundary.</summary>
    public const int RibWidth = 2;

    /// <summary>Columns of the housing behind a raised bridge, and rows below the bridge's top it starts.</summary>
    public const int HousingWidth = 6, HousingDrop = 4;

    /// <summary>An inner door stands open one time in this many; the way in is always shut.</summary>
    public const int DoorOpenOneIn = 5;

    public static readonly IReadOnlyList<VesselClass> Classes =
    [
        new("shuttle", Length: 72, Decks: (2, 2), BowFlat: 3, BowRun: (1, 2), BowTip: 5, ArmourDepth: 2, BridgeKind: VesselBridge.Cockpit,
            Extras: (0, 1), ExtraRoles: [VesselRole.Hold, VesselRole.Quarters, VesselRole.Life], Crater: 6, Yield: (1600, 2600), Weight: 1),
        new("freighter", Length: 200, Decks: (2, 3), BowFlat: 8, BowRun: (1, 2), BowTip: 8, ArmourDepth: 2, BridgeKind: VesselBridge.Raised,
            Extras: (5, 10), ExtraRoles: [VesselRole.Hold, VesselRole.Quarters, VesselRole.Life], Crater: 14, Yield: (3300, 6000), Weight: 3),
        // A liner (big-vessels): its length is its blocks' (Liner.Run), no extras; seeds 1–200 gave 17 486 to 25 161.
        new("liner", Length: 320, Decks: (5, 6), BowFlat: 6, BowRun: (1, 1), BowTip: 9, ArmourDepth: 2, BridgeKind: VesselBridge.Raised,
            Extras: (0, 0), ExtraRoles: [], Crater: 18, Yield: (16500, 26500), Weight: 5, Layout: VesselLayout.Liner),
        // A tanker (big-vessels): one band over every deck, its length its blocks' (Tanker.Run), no extras; a blunt bow; seeds 1–200 gave 14 133 to 22 053.
        new("tanker", Length: 340, Decks: (4, 5), BowFlat: 5, BowRun: (1, 2), BowTip: 20, ArmourDepth: 2, BridgeKind: VesselBridge.Raised,
            Extras: (0, 0), ExtraRoles: [], Crater: 18, Yield: (13000, 23500), Weight: 4, Layout: VesselLayout.Tanker),
    ];

    /// <summary>The classes' weights summed: what the radar draws below.</summary>
    public static int Weights(IReadOnlyList<VesselClass> classes) => classes.Sum(c => c.Weight);

    /// <summary>
    /// The class of <paramref name="classes"/> a draw below their <see cref="Weights"/> falls on (big-vessels): each in order takes as many
    /// draws as its weight, so a class of weight 0 none.
    /// </summary>
    public static VesselClass Weighed(IReadOnlyList<VesselClass> classes, int draw)
    {
        foreach (var c in classes)
        {
            if (draw < c.Weight) return c;
            draw -= c.Weight;
        }
        throw new ArgumentOutOfRangeException(nameof(draw), draw, "past the classes' weights");
    }

    /// <summary>
    /// A liner's layout (big-vessels), in columns unless said: blocks across its bands, each a width and a stack of rooms; the raised
    /// rooms over them. Where a share of its length is drawn it is whole percent, the column rounded down.
    /// </summary>
    public static class Liner
    {
        /// <summary>Columns of rooms inside its armour, from the stern's to the bow's: drawn, the forward block taking what is left.</summary>
        public static readonly (int Min, int Max) Run = (260, 320);

        /// <summary>The bottom band's decks: holds, the fuel tank and the hangar, each this many decks tall.</summary>
        public const int LowDecks = 2;

        /// <summary>A block's width, least and most: the service block, a block of cabins, a public block, the pool's and the restaurant's, the forward one.</summary>
        public static readonly (int Min, int Max) Service = (12, 14), Cabins = (12, 14), Public = (16, 20), Tall = (34, 38), Forward = (20, 22);

        /// <summary>A block past the service block is a public one this many times in a hundred, else cabins; past the pool and the restaurant.</summary>
        public const int PublicPercent = 15;

        /// <summary>Where along its run, in whole percent, the pool, the infirmary's public block and the restaurant start: the first block from there.</summary>
        public static readonly (int Min, int Max) PoolAt = (22, 30), InfirmaryAt = (36, 44), RestaurantAt = (50, 58);

        /// <summary>The bottom band: a hold's width, least and most; the hangar at the bow at least this wide.</summary>
        public static readonly (int Min, int Max) Hold = (24, 32);

        public const int HangarMin = 38;

        /// <summary>
        /// The promenade over the first block at least <see cref="PromenadePast"/> columns past the service block, drawn this wide, at
        /// least <see cref="PromenadeMin"/> or none; the dome on its stern end at most <see cref="DomeMax"/> wide and <see cref="DomeShort"/> short of it.
        /// </summary>
        public static readonly (int Min, int Max) Promenade = (44, 60);

        public const int PromenadePast = 10, PromenadeMin = 24, DomeMax = 18, DomeShort = 4;

        /// <summary>Columns kept between the promenade's hull and the bridge's housing.</summary>
        public const int PromenadeGap = 3;

        /// <summary>Rows a module bulges under the hull at most: a liner's keel is smooth.</summary>
        public const int BulgeDown = 1;

        /// <summary>The promenade's glazed roof: a panel this many columns wide every <see cref="PanelEvery"/>, clear of the dome.</summary>
        public const int PanelWidth = 3, PanelEvery = 5;
    }

    /// <summary>
    /// A tanker's layout (big-vessels), in columns unless said: one band of blocks over every deck, the raised bridge over the stern.
    /// Where a share of its length is drawn it is whole percent, the column rounded down.
    /// </summary>
    public static class Tanker
    {
        /// <summary>Columns of rooms inside its armour, from the stern's to the bow's: drawn, the forward block taking what is left.</summary>
        public static readonly (int Min, int Max) Run = (260, 340);

        /// <summary>
        /// A block's width, least and most: the fuel bay's past the engine rooms, the service block, a tank bay's (wide enough for two
        /// tall tanks and a ladder), a block of pump rooms, the forward one. A tanker has no hangar: shuttles are a liner's.
        /// </summary>
        public static readonly (int Min, int Max) FuelBay = (16, 18), Service = (12, 14), Bay = (22, 26), Pumps = (11, 13), Forward = (18, 20);

        /// <summary>The bridge's room stands at least this many columns past the stern's armour, its hatch where the airlock's drawing keeps two columns clear.</summary>
        public const int BridgeFrom = 8;
    }

    /// <summary>
    /// Guards aboard an intact vessel (derelict-enemies): a spider asleep in a capsule on a small room's back wall, a robot in a stand on
    /// a big room's floor. One of a kind for every <see cref="Rule.Per"/> rooms of its size, rounded up, each in a room of its own,
    /// never the arrival room, nor one where its dock's box fits nowhere clear of every fitting, ladder and shelf.
    /// </summary>
    public static class Aboard
    {
        /// <summary>
        /// One kind of guard: its <paramref name="Species"/> (<c>Creatures</c>), one for every <paramref name="Per"/> rooms of its size,
        /// <paramref name="Big"/> rooms more than a deck tall, else one deck tall; its dock's box, <paramref name="Width"/> columns by
        /// <paramref name="Height"/> rows, standing on its room's floor, the guard asleep on the floor in the middle of it.
        /// </summary>
        public sealed record Rule(string Species, int Per, bool Big, int Width, int Height);

        /// <summary>
        /// A spider's capsule G1 «Зрачок — впадина» for every five small rooms, 4¾ cells across, its bottom just above the floor; a
        /// robot's stand S1 «Люлька» for every three big rooms, some 6⅝ cells across and 8 tall.
        /// </summary>
        public static readonly Rule Capsule = new("spider", Per: 5, Big: false, Width: 5, Height: 5),
            Stand = new("escort", Per: 3, Big: true, Width: 7, Height: 8);

        /// <summary>In the order a vessel's docks are listed: the spiders' capsules first.</summary>
        public static readonly IReadOnlyList<Rule> Rules = [Capsule, Stand];
    }

    /// <summary>How the big classes' rooms are furnished by code (big-vessels, the mockup's furnish): sizes in cells, odds in whole percent.</summary>
    public static class Fits
    {
        /// <summary>Columns a room keeps clear at its left, a walkway, and a room's width past which it hangs two ceiling lamps.</summary>
        public const int Walkway = 2, TwoLampsPast = 26;

        /// <summary>Groups of furniture at most: a lounge's loungers and tables, a promenade's, a restaurant's tables, a hold's stacks.</summary>
        public const int LoungeGroups = 4, PromenadeGroups = 9, RestaurantTables = 6, HoldStacks = 9;

        /// <summary>A hold's stack is containers this often, else crates; how many more stand on the first, least and most.</summary>
        public const int ContainerPercent = 62;

        public static readonly (int Min, int Max) ContainersOver = (1, 2), CratesOver = (1, 3);

        /// <summary>The restaurant's piano stands this many columns from its right; its chandeliers at a third and two thirds across.</summary>
        public const int PianoFromRight = 7;

        /// <summary>The hangar's shuttle stands this many columns short of its right; the fuel cart and stacks from this column past its left.</summary>
        public const int ShuttleFromRight = 4, HangarFrom = 3;

        /// <summary>A tanker's bay holds two tall tanks this often, else one capsule; a ladder up a tank room's side this many columns from its right.</summary>
        public const int TallTanksPercent = 35, TankLadderFromRight = 3;

        /// <summary>The salvage a hold and a hangar stack, and the galley's fridge.</summary>
        public const string Crate = "salvageCrate", Fridge = "salvageVending";

        // Outside (big-vessels, the mockup's outside parts), on top unless said.

        /// <summary>
        /// A liner's fin stands this many columns into its service block; its escape pods from this many columns past the fin, one
        /// every <c>PodEvery</c> columns where none is near, short of the bow by <c>PodShort</c>; a pod still in its cradle this often, else
        /// the cradle left empty.
        /// </summary>
        public const int FinInto = 1, PodPast = 2, PodEvery = 6, PodShort = 4, PodPercent = 45;

        /// <summary>
        /// A tanker's manifold: segments in one line from a part's gap past its radiator to <c>ManifoldShort</c> columns short of the bow,
        /// <c>ManifoldRise</c> rows over the hull's highest row along it; a strut down from each of its segment's posts; a riser
        /// <c>RiserPast</c> columns past the middle of each tank bay and the fuel bay, a valve's wheel over it.
        /// </summary>
        public const int ManifoldShort = 4, ManifoldRise = 4, RiserPast = 2;

        /// <summary>The columns of a manifold segment its struts hang from, from its left (the truss's own, turned upward).</summary>
        public static readonly IReadOnlyList<int> ManifoldPosts = [2, 8];

        /// <summary>A tanker carries a sphere under the keel at each tank bay's middle this often, else none.</summary>
        public const int SpherePercent = 55;

        /// <summary>
        /// The liquid a tank holds, least and most, drawn per tank: a small one (a tall tank, a sphere), a medium one (a bay's capsule), a
        /// large one (a fuel column, a liner's fuel tank by its engines).
        /// </summary>
        public static readonly (int Min, int Max) SmallTank = (5, 15), MediumTank = (10, 25), LargeTank = (15, 35);
    }

    /// <summary>The surface kind each class's vessel lies on (vessel-worlds), in the order of <see cref="Classes"/>.</summary>
    public static readonly IReadOnlyList<VesselKind> Kinds = [.. Classes.Select(c => new VesselKind(c.Id))];

    /// <summary>
    /// Where each role's rooms go (a vessel of rooms), aft to fore: the engine and the hold low, life support anywhere, quarters and
    /// the bridge on the first deck.
    /// </summary>
    public static readonly IReadOnlyList<VesselRoomRule> RoomRules =
    [
        new(VesselRole.Engine, VesselDeck.Low, 0),
        new(VesselRole.Hold, VesselDeck.Low, 1),
        new(VesselRole.Life, VesselDeck.Any, 2),
        new(VesselRole.Quarters, VesselDeck.Top, 3),
        new(VesselRole.Bridge, VesselDeck.Top, 4),
    ];

    /// <summary>The rule of <paramref name="role"/>'s rooms.</summary>
    public static VesselRoomRule RuleOf(VesselRole role) => RoomRules.First(r => r.Role == role);

    /// <summary>Columns of a cockpit's glass, from where the bow starts: the top armour over them, down its slope.</summary>
    public const int CockpitGlass = 8;

    /// <summary>The class <paramref name="id"/>; null for none.</summary>
    public static VesselClass? Class(string id)
    {
        foreach (var c in Classes) if (c.Id == id) return c;
        return null;
    }

    /// <summary>Where class <paramref name="id"/> is in <see cref="Classes"/>, what its seed is mixed with; -1 for none.</summary>
    public static int IndexOf(string id)
    {
        for (int i = 0; i < Classes.Count; i++) if (Classes[i].Id == id) return i;
        return -1;
    }

    public static readonly IReadOnlyList<VesselInterior> Interiors =
    [
        // Salvage stands on the deck, its bottom row the interior's last: j container, l crate, m pod, p tanks, r machine block,
        // s engine block, t cable rack, 3 cargo hold, 1 hull section, 2 shield slab, 4 the main engine, 5 a console, 6 a vending
        // machine. Furniture (derelict-looks): * the ceiling lamp on the first row, [ bunks, ] a locker, + a table, @ a pilot's seat,
        // 0 a life-support tank, > a console, { a machine block, } a cable rack, 7 a cargo container; K a storage crate, = a shelf.
        new("holdContainer", VesselRole.Hold,
        [
            "............**..",
            "................",
            "..jjjjjjjj......",
            "..jjjjjjjj......",
            "..jjjjjjjj.lll..",
            "..jjjjjjjj.lll..",
        ]),
        new("holdCrates", VesselRole.Hold,
        [
            "....**......**..",
            "................",
            "..lll.77777777..",
            "..lll.77777777..",
            "..KKKK77777777..",
            "..KKKK77777777..",
        ]),
        new("holdContainers", VesselRole.Hold,
        [
            "..........**..........",
            "......................",
            "..jjjjjjjj..77777777..",
            "..jjjjjjjj..77777777..",
            "..jjjjjjjj..77777777..",
            "..jjjjjjjj..77777777..",
        ]),
        new("holdBay", VesselRole.Hold,
        [
            "..333333333333..**..",
            "..333333333333......",
            "..333333333333......",
            "..333333333333......",
            "..333333333333.lll..",
            "..333333333333.lll..",
        ]),
        new("holdSpares", VesselRole.Hold,
        [
            "....**........**....",
            "....................",
            "..111111..22222222..",
            "..111111..22222222..",
            "..111111..22222222..",
            "..111111..22222222..",
        ]),
        new("quartersBunks", VesselRole.Quarters,
        [
            "......**........",
            "..[[[[[.]]......",
            "..[[[[[.]]......",
            "..[[[[[.]]......",
            "..[[[[[.]].+++..",
            "..[[[[[.]].+++..",
        ]),
        new("quartersStores", VesselRole.Quarters,
        [
            "....**....**....",
            ".........]].....",
            ".........]].....",
            ".........]].....",
            "..lll.mm.]]KKKK.",
            "..lll.mm.]]KKKK.",
        ]),
        new("mess", VesselRole.Quarters,
        [
            "....**..........",
            "..mm......6666..",
            "..mm......6666..",
            "..====....6666..",
            "..lll.+++.6666..",
            "..lll.+++.6666..",
        ]),
        new("cabin", VesselRole.Quarters,
        [
            ".......**...",
            "..[[[[[.....",
            "..[[[[[.....",
            "..[[[[[.....",
            "..[[[[[.lll.",
            "..[[[[[.lll.",
        ]),
        new("engineRoom", VesselRole.Engine,
        [
            "....**..........",
            "................",
            "..rrrrrr.}}}}...",
            "..rrrrrr.}}}}...",
            "..rrrrrr.}}}}...",
            "..rrrrrr.}}}}...",
        ]),
        new("engineHall", VesselRole.Engine,
        [
            "..ssssssssss....**..",
            "..ssssssssss........",
            "..ssssssssss{{{{{{..",
            "..ssssssssss{{{{{{..",
            "..ssssssssss{{{{{{..",
            "..ssssssssss{{{{{{..",
        ]),
        new("engineCell", VesselRole.Engine,
        [
            "..**........",
            "............",
            "..{{{{{{....",
            "..{{{{{{....",
            "..{{{{{{.mm.",
            "..{{{{{{.mm.",
        ]),
        new("lifeSupport", VesselRole.Life,
        [
            "......**........",
            "................",
            "..0000.pppp.0000",
            "..0000.pppp.0000",
            "..0000.pppp.0000",
            "..0000.pppp.0000",
        ]),
        new("mainEngine", VesselRole.Engine,
        [
            "444444444444..**..",
            "444444444444......",
            "444444444444.}}}}.",
            "444444444444.}}}}.",
            "444444444444.}}}}.",
            "444444444444.}}}}.",
        ], Main: true),
        new("mainEngineHall", VesselRole.Engine,
        [
            "444444444444....**....",
            "444444444444..........",
            "444444444444..{{{{{{..",
            "444444444444..{{{{{{..",
            "444444444444..{{{{{{..",
            "444444444444..{{{{{{..",
        ], Main: true),
        new("bridge", VesselRole.Bridge,
        [
            "......**......**..",
            "..................",
            "..............tttt",
            "..5555.@@.>>>>tttt",
            "..5555.@@.>>>>tttt",
            "..5555.@@.>>>>tttt",
        ]),
        new("cockpit", VesselRole.Bridge,
        [
            "....**........",
            "..............",
            "..............",
            "..>>>>.@@.5555",
            "..>>>>.@@.5555",
            "..>>>>.@@.5555",
        ]),
    ];

    /// <summary>What a storage crate in a compartment of each role may hold: a stack or two, each drawn from its role's list; salvage holds the rest.</summary>
    public static readonly IReadOnlyDictionary<VesselRole, IReadOnlyList<VesselLoot>> Loot = new Dictionary<VesselRole, IReadOnlyList<VesselLoot>>
    {
        [VesselRole.Hold] = [new("plate", 2, 8), new("scrapChunk", 6, 20), new("planks", 4, 12), new("wire", 4, 10), new("tube", 1, 4)],
        [VesselRole.Quarters] = [new("planks", 2, 6), new("canister", 1, 2), new("cloth", 1, 3), new("biofuel", 1, 3)],
        [VesselRole.Engine] = [new("gear", 1, 4), new("coil", 1, 2), new("tube", 1, 4), new("rotor", 1, 2), new("biofuel", 2, 6)],
        [VesselRole.Life] = [new("canister", 2, 4), new("tube", 1, 3), new("glass", 1, 4)],
        [VesselRole.Bridge] = [new("wire", 2, 6), new("circuit", 1, 1), new("glass", 1, 3)],
    };
}
