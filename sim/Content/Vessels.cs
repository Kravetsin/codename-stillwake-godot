namespace Stillwake.Content;

/// <summary>What a vessel's compartment is for: its interior and its crates' loot follow it (generated-vessels).</summary>
public enum VesselRole { Hold, Quarters, Engine, Life, Bridge }

/// <summary>What wrecked a vessel, which its damage pass follows: none for one intact.</summary>
public enum VesselCause { None, Breach }

/// <summary>
/// A class of generated vessel (generated-vessels): its size as ranges the seed picks from, and how it is built. A vessel is
/// laid along its width, stern on the left: the engine bells, <c>Mids</c> midsections of <c>MidWidth</c> columns each, a bow
/// tapering over <c>Bow</c> columns, and a bridge on top over the last midsection when <c>Bridge</c> is wider than nothing.
/// </summary>
/// <param name="Bells">Columns of engine bells left of the hull, one a deck but the first, which has the way in.</param>
/// <param name="Mids">How many midsections, least and most: a vessel's length.</param>
/// <param name="MidWidth">Columns of a midsection: a compartment on every deck, its first two columns kept for a shaft.</param>
/// <param name="Decks">How many decks, least and most, each six open rows over a floor.</param>
/// <param name="Bow">Columns of the bow, its top and bottom tapering toward the tip.</param>
/// <param name="ArmourDepth">Cells of armour inward from the hull's edge.</param>
/// <param name="Bridge">Columns of the bridge on top, its armour included; 0 for none.</param>
/// <param name="Crater">A breach's radius, in cells.</param>
/// <param name="Yield">Whole items, every kind counted alike, a vessel of this class gives taken apart whole, least and most (a content test).</param>
public sealed record VesselClass(
    string Id,
    int Bells,
    (int Min, int Max) Mids,
    int MidWidth,
    (int Min, int Max) Decks,
    int Bow,
    int ArmourDepth,
    int Bridge,
    int Crater,
    (int Min, int Max) Yield)
{
    /// <summary>Rows of the body for <paramref name="decks"/> decks: armour, six open rows a deck and a floor between each two.</summary>
    public int BodyHeight(int decks) => 2 * ArmourDepth + Vessels.Pitch * decks - 1;

    /// <summary>Rows above the body: the bridge's armour and its six open rows; none without a bridge.</summary>
    public int BridgeHeight => Bridge > 0 ? ArmourDepth + Vessels.Open : 0;

    /// <summary>The largest vessel of the class, columns by rows: its frame.</summary>
    public (int Width, int Height) Frame => (Bells + Mids.Max * MidWidth + Bow, BridgeHeight + BodyHeight(Decks.Max));
}

/// <summary>
/// A compartment's furnishing drawn as ASCII (generated-vessels), a character a cell in <see cref="Legend"/>'s: <c>.</c> open,
/// <c>=</c> a floor (a shelf, a bunk), a machine by its whole footprint. Six rows, its first two columns open for a shaft.
/// </summary>
public sealed record VesselInterior(string Id, VesselRole Role, IReadOnlyList<string> Rows)
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
public sealed record VesselSalvage(
    string Kind, string From, char Char, IReadOnlyList<VesselRole> Roles, int Times = Vessels.SalvageTimes,
    (int W, int H)? Size = null, int? Hp = null, bool Torn = false);

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

    /// <summary>The salvage that is <paramref name="kind"/>; null for any other machine.</summary>
    public static VesselSalvage? SalvageOf(string kind)
    {
        foreach (var s in Salvage) if (s.Kind == kind) return s;
        return null;
    }

    /// <summary>The machine rows of <see cref="Salvage"/>, piles with no menu card, joined to <c>Machines.All</c>.</summary>
    public static readonly IReadOnlyList<MachineSpec> SalvageRows = Salvage.Select(s =>
    {
        var (w, h) = s.Size ?? Wreckage.Sizes.Where(z => z.Item == s.From).Select(z => (z.W, z.H)).First();
        int hp = s.Hp ?? Wreckage.HpOf(s.From) ?? throw new InvalidOperationException($"vessels: {s.From} has no hit points");
        return new MachineSpec(s.Kind, w, h, [], SalvageCost(s), BuildTicks: 160, Hp: hp, Char: s.Char,
            Means: $"salvage, a {s.From} {s.Times} times over: draw its whole footprint");
    }).ToList();

    /// <summary>The machines a vessel may hold: fair loot that keeps no state in another module by its id.</summary>
    public static readonly IReadOnlyList<string> Machines = ["crate", "lamp", .. Salvage.Select(s => s.Kind)];

    public static readonly IReadOnlyList<VesselClass> Classes =
    [
        new("shuttle", Bells: 6, Mids: (1, 2), MidWidth: 16, Decks: (1, 2), Bow: 12, ArmourDepth: 2, Bridge: 0, Crater: 6, Yield: (300, 1300)),
        new("freighter", Bells: 10, Mids: (4, 7), MidWidth: 22, Decks: (2, 3), Bow: 20, ArmourDepth: 2, Bridge: 20, Crater: 14, Yield: (2400, 8000)),
    ];

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
        // block, s engine block, t cable rack, z soil clod, 3 cargo hold, 1 hull section, 2 shield slab; K a storage crate, L a
        // lamp, = a shelf or bunk.
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
        new("bridge", VesselRole.Bridge,
        [
            "....LL......",
            "....LL......",
            "..tttt..====",
            "..tttt......",
            "..tttt.KKKK.",
            "..tttt.KKKK.",
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
