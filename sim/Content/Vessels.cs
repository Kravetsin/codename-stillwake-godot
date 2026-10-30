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

    /// <summary>The machines a vessel may hold: fair loot that keeps no state in another module by its id.</summary>
    public static readonly IReadOnlyList<string> Machines = ["crate", "lamp"];

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
        new("holdCrates", VesselRole.Hold,
        [
            "..........LL....",
            "..........LL....",
            "................",
            "................",
            "..KKKKKKKK..KKKK",
            "..KKKKKKKK..KKKK",
        ]),
        new("holdRacks", VesselRole.Hold,
        [
            "....LL..........",
            "....LL..........",
            "..======..======",
            "..KKKK....KKKK..",
            "..KKKK....KKKK..",
            "................",
        ]),
        new("quartersBunks", VesselRole.Quarters,
        [
            "......LL........",
            "......LL........",
            "..=====...=====.",
            "................",
            "..=====...KKKK..",
            "..........KKKK..",
        ]),
        new("quartersMess", VesselRole.Quarters,
        [
            "....LL....LL....",
            "....LL....LL....",
            "................",
            "................",
            "..KKKK..........",
            "..KKKK..........",
        ]),
        new("engineRoom", VesselRole.Engine,
        [
            "....LL..........",
            "....LL..........",
            "..=====..=====..",
            "................",
            "..KKKK....KKKK..",
            "..KKKK....KKKK..",
        ]),
        new("lifeSupport", VesselRole.Life,
        [
            "......LL........",
            "......LL........",
            "................",
            "..=====.........",
            "..KKKK....KKKK..",
            "..KKKK....KKKK..",
        ]),
        new("bridge", VesselRole.Bridge,
        [
            "....LL......",
            "....LL......",
            "............",
            "..========..",
            "..KKKK......",
            "..KKKK......",
        ]),
        new("cabin", VesselRole.Quarters,
        [
            "....LL......",
            "....LL......",
            "............",
            "..====......",
            "......KKKK..",
            "......KKKK..",
        ]),
        new("engineCell", VesselRole.Engine,
        [
            "..LL........",
            "..LL........",
            "............",
            "............",
            "..KKKK......",
            "..KKKK......",
        ]),
    ];

    /// <summary>What a crate in a compartment of each role may hold: one to three stacks, each drawn from its role's list.</summary>
    public static readonly IReadOnlyDictionary<VesselRole, IReadOnlyList<VesselLoot>> Loot = new Dictionary<VesselRole, IReadOnlyList<VesselLoot>>
    {
        [VesselRole.Hold] = [new("plate", 2, 8), new("scrapChunk", 6, 20), new("planks", 4, 12), new("wire", 4, 10), new("tube", 1, 4)],
        [VesselRole.Quarters] = [new("planks", 2, 6), new("canister", 1, 2), new("cloth", 1, 3), new("biofuel", 1, 3)],
        [VesselRole.Engine] = [new("gear", 1, 4), new("coil", 1, 2), new("tube", 1, 4), new("rotor", 1, 2), new("biofuel", 2, 6)],
        [VesselRole.Life] = [new("canister", 2, 4), new("tube", 1, 3), new("glass", 1, 4)],
        [VesselRole.Bridge] = [new("wire", 2, 6), new("circuit", 1, 1), new("glass", 1, 3)],
    };
}
