namespace Stillwake.Content;

/// <summary>What a vessel's compartment is for: its interior and its crates' loot follow it (generated-vessels).</summary>
public enum VesselRole { Hold, Quarters, Engine, Life, Bridge }

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
/// laid along its width, stern on the left: the engine bells, a body of rooms deck over deck, at most <c>Length</c> columns, a
/// bow tapering over <c>Bow</c> columns, and its bridge (<c>BridgeKind</c>).
/// </summary>
/// <param name="Bells">Columns of engine bells left of the hull, one a deck but the first, which has the way in.</param>
/// <param name="Length">The body's most columns inside its stern armour: an extra room that would run a deck past it is not built.</param>
/// <param name="Decks">How many decks, least and most, each six open rows over a floor; two at least: the engine room is below.</param>
/// <param name="Bow">Columns of the bow, its top and bottom tapering toward the tip.</param>
/// <param name="ArmourDepth">Cells of armour inward from the hull's edge.</param>
/// <param name="BridgeKind">A cockpit in the bow, or a bridge raised on top.</param>
/// <param name="Extras">How many rooms past the ones every vessel has, least and most.</param>
/// <param name="ExtraRoles">What an extra room is, each as likely.</param>
/// <param name="Crater">A breach's radius, in cells.</param>
/// <param name="Yield">Whole items, every kind counted alike, a vessel of this class gives taken apart whole, least and most (a content test).</param>
public sealed record VesselClass(
    string Id,
    int Bells,
    int Length,
    (int Min, int Max) Decks,
    int Bow,
    int ArmourDepth,
    VesselBridge BridgeKind,
    (int Min, int Max) Extras,
    IReadOnlyList<VesselRole> ExtraRoles,
    int Crater,
    (int Min, int Max) Yield)
{
    /// <summary>Rows of the body for <paramref name="decks"/> decks: armour, six open rows a deck and a floor between each two.</summary>
    public int BodyHeight(int decks) => 2 * ArmourDepth + Vessels.Pitch * decks - 1;

    /// <summary>Rows above the body: a raised bridge's armour and its six open rows; none for a cockpit.</summary>
    public int BridgeHeight => BridgeKind == VesselBridge.Raised ? ArmourDepth + Vessels.Open : 0;

    /// <summary>The largest vessel of the class, columns by rows: its frame.</summary>
    public (int Width, int Height) Frame => (Bells + ArmourDepth + Length + Bow, BridgeHeight + BodyHeight(Decks.Max));
}

/// <summary>
/// A compartment's furnishing drawn as ASCII (generated-vessels), a character a cell in <see cref="Legend"/>'s: <c>.</c> open,
/// <c>=</c> a floor (a shelf, a bunk), a machine by its whole footprint. Six rows, its first two columns open for a ladder or a
/// hatch over them, but the main engine's, flush with the stern.
/// </summary>
/// <param name="Main">Whether it holds the main engine, at its first column: the bottom engine room's, against the stern.</param>
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

/// <summary>Generated vessels and their wrecks (generated-vessels): classes, interiors, loot and where a vessel lies.</summary>
public static class Vessels
{
    /// <summary>Open rows a deck holds: a doorway's height (the hero is under six tall).</summary>
    public const int Open = 6;

    /// <summary>Rows from one deck to the next: its open rows and the floor under them.</summary>
    public const int Pitch = Open + 1;

    /// <summary>Columns of space between the ship's right edge and a vessel: more than a drone bay reaches (a content test).</summary>
    public const int Gap = 40;

    /// <summary>Cells a vessel keeps from the grid's edge.</summary>
    public const int EdgeClear = 16;

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
        new("salvageCrate", "cargoCrate", 'l', [VesselRole.Hold, VesselRole.Quarters], Size: (3, 2), Hp: 6),
        new("salvagePod", "supplyPod", 'm', [VesselRole.Quarters, VesselRole.Life], Size: (2, 2), Hp: 6),
        new("salvageTanks", "tankCluster", 'p', [VesselRole.Life]),
        new("salvageMachine", "machineBlock", 'r', [VesselRole.Engine]),
        // An engine's piece already gives much; five of it would flood the hold with gears.
        new("salvageEngine", "engineBlock", 's', [VesselRole.Engine], Times: 2),
        new("salvageRack", "cableRack", 't', [VesselRole.Bridge]),
        new("salvageClod", "soilClod", 'z', [VesselRole.Quarters]),
        // A hold module's piece gives as much as an engine's.
        new("salvageHold", "cargoHold", '3', [VesselRole.Hold], Times: 2),
        // Spares in a hold; and what tore the hull, left by a breach on the deck by its crater.
        new("salvageSection", "hullSection", '1', [VesselRole.Hold], Torn: true),
        new("salvageSlab", "shieldSlab", '2', [VesselRole.Hold], Torn: true),
        // The main engine (a vessel of rooms): one a vessel, against the stern by its bell, its dearest thing.
        new("salvageMainEngine", "engineBlock", '4', [VesselRole.Engine], Times: 3, Size: (12, 6), Looks: 3),
        // A bridge's console, its screens and switches a cable rack's insides.
        new("salvageConsole", "cableRack", '5', [VesselRole.Bridge], Size: (4, 3), Looks: 4),
        // A vending machine in the mess (food-and-the-body): a fridge's food, its sign still lit by a battery of its own, as the
        // emergency lamp is; an enamel cabinet or a glass case.
        new("salvageVending", "fridge", '6', [VesselRole.Quarters], Size: (4, 5), Hp: 8, Looks: 2, Has: MachineTrait.Glow),
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

    /// <summary>The salvage that <paramref name="kind"/> is, any of its looks; null for any other machine.</summary>
    public static VesselSalvage? SalvageOf(string kind)
    {
        foreach (var s in Salvage)
            for (int look = 0; look < s.Looks; look++)
                if (LookKind(s, look) == kind) return s;
        return null;
    }

    /// <summary>The machine row of <paramref name="s"/>'s look <paramref name="look"/>: its own kind for the first, numbered from 2 after it.</summary>
    public static string LookKind(VesselSalvage s, int look) => look == 0 ? s.Kind : s.Kind + (look + 1);

    /// <summary>The machine rows of <see cref="Salvage"/>, a row a look, piles with no menu card, joined to <c>Machines.All</c>.</summary>
    public static readonly IReadOnlyList<MachineSpec> SalvageRows = Salvage.SelectMany(s =>
    {
        var (w, h) = s.Size ?? Wreckage.Sizes.Where(z => z.Item == s.From).Select(z => (z.W, z.H)).First();
        int hp = s.Hp ?? Wreckage.HpOf(s.From) ?? throw new InvalidOperationException($"vessels: {s.From} has no hit points");
        var cost = SalvageCost(s);
        return Enumerable.Range(0, s.Looks).Select(look => new MachineSpec(LookKind(s, look), w, h, [], cost, BuildTicks: 160, Hp: hp, Has: s.Has,
            Char: look == 0 ? s.Char : null, Means: look == 0 ? $"salvage, a {s.From} {s.Times} times over: draw its whole footprint" : null));
    }).ToList();

    /// <summary>The machines a vessel may hold: fair loot that keeps no state in another module by its id.</summary>
    public static readonly IReadOnlyList<string> Machines = ["crate", "lamp", .. SalvageRows.Select(r => r.Kind)];

    public static readonly IReadOnlyList<VesselClass> Classes =
    [
        new("shuttle", Bells: 6, Length: 72, Decks: (2, 2), Bow: 12, ArmourDepth: 2, BridgeKind: VesselBridge.Cockpit,
            Extras: (0, 1), ExtraRoles: [VesselRole.Hold, VesselRole.Quarters, VesselRole.Life], Crater: 6, Yield: (1300, 2800)),
        new("freighter", Bells: 10, Length: 200, Decks: (2, 3), Bow: 20, ArmourDepth: 2, BridgeKind: VesselBridge.Raised,
            Extras: (5, 10), ExtraRoles: [VesselRole.Hold, VesselRole.Quarters, VesselRole.Life], Crater: 14, Yield: (2400, 8000)),
    ];

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

    /// <summary>Columns of a cockpit's glass, from where the bow starts: the top armour over them, down its taper.</summary>
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
        // Salvage stands on the deck, its bottom row the interior's last: j container, l crate, m pod, p tanks, r machine
        // block, s engine block, t cable rack, z soil clod, 3 cargo hold, 1 hull section, 2 shield slab, 4 the main engine, 5 a
        // console, 6 a vending machine; K a storage crate, L a lamp, = a shelf or bunk.
        new("holdContainer", VesselRole.Hold,
        [
            "............LL..",
            "............LL..",
            "..jjjjjjjj......",
            "..jjjjjjjj......",
            "..jjjjjjjj.lll..",
            "..jjjjjjjj.lll..",
        ]),
        new("holdCrates", VesselRole.Hold,
        [
            "....LL......LL..",
            "....LL......LL..",
            "................",
            "................",
            "..lll.lll.KKKK..",
            "..lll.lll.KKKK..",
        ]),
        new("holdContainers", VesselRole.Hold,
        [
            "..........LL..........",
            "..........LL..........",
            "..jjjjjjjj..jjjjjjjj..",
            "..jjjjjjjj..jjjjjjjj..",
            "..jjjjjjjj..jjjjjjjj..",
            "..jjjjjjjj..jjjjjjjj..",
        ]),
        new("holdBay", VesselRole.Hold,
        [
            "..333333333333..LL..",
            "..333333333333..LL..",
            "..333333333333......",
            "..333333333333......",
            "..333333333333.lll..",
            "..333333333333.lll..",
        ]),
        new("holdSpares", VesselRole.Hold,
        [
            "....LL........LL....",
            "....LL........LL....",
            "..111111..22222222..",
            "..111111..22222222..",
            "..111111..22222222..",
            "..111111..22222222..",
        ]),
        new("quartersBunks", VesselRole.Quarters,
        [
            "......LL........",
            "......LL........",
            "..=====...zzzz..",
            "..........zzzz..",
            "..=====mm.zzzz..",
            ".......mm.zzzz..",
        ]),
        new("quartersStores", VesselRole.Quarters,
        [
            "....LL....LL....",
            "....LL....LL....",
            "................",
            "................",
            "..lll..mm.KKKK..",
            "..lll..mm.KKKK..",
        ]),
        new("mess", VesselRole.Quarters,
        [
            "....LL..........",
            "....LL....6666..",
            "..........6666..",
            "..====....6666..",
            "..lll.mm..6666..",
            "..lll.mm..6666..",
        ]),
        new("cabin", VesselRole.Quarters,
        [
            "....LL......",
            "....LL......",
            "..====......",
            "............",
            "..lll..mm...",
            "..lll..mm...",
        ]),
        new("engineRoom", VesselRole.Engine,
        [
            "....LL..........",
            "....LL..........",
            "..rrrrrr..======",
            "..rrrrrr........",
            "..rrrrrr..KKKK..",
            "..rrrrrr..KKKK..",
        ]),
        new("engineHall", VesselRole.Engine,
        [
            "..ssssssssss....LL..",
            "..ssssssssss....LL..",
            "..ssssssssssrrrrrr..",
            "..ssssssssssrrrrrr..",
            "..ssssssssssrrrrrr..",
            "..ssssssssssrrrrrr..",
        ]),
        new("engineCell", VesselRole.Engine,
        [
            "..LL........",
            "..LL........",
            "..rrrrrr....",
            "..rrrrrr....",
            "..rrrrrr.mm.",
            "..rrrrrr.mm.",
        ]),
        new("lifeSupport", VesselRole.Life,
        [
            "......LL........",
            "......LL........",
            "..pppp..pppp....",
            "..pppp..pppp....",
            "..pppp..pppp.mm.",
            "..pppp..pppp.mm.",
        ]),
        new("mainEngine", VesselRole.Engine,
        [
            "444444444444..LL..",
            "444444444444..LL..",
            "444444444444......",
            "444444444444......",
            "444444444444.KKKK.",
            "444444444444.KKKK.",
        ], Main: true),
        new("mainEngineHall", VesselRole.Engine,
        [
            "444444444444....LL....",
            "444444444444....LL....",
            "444444444444..rrrrrr..",
            "444444444444..rrrrrr..",
            "444444444444..rrrrrr..",
            "444444444444..rrrrrr..",
        ], Main: true),
        new("bridge", VesselRole.Bridge,
        [
            "......LL......LL..",
            "......LL......LL..",
            "..............tttt",
            "..5555..5555..tttt",
            "..5555..5555..tttt",
            "..5555..5555..tttt",
        ]),
        new("cockpit", VesselRole.Bridge,
        [
            "....LL........",
            "....LL........",
            "..............",
            "..5555..5555..",
            "..5555..5555..",
            "..5555..5555..",
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
