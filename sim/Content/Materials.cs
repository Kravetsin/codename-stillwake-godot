using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>What a wall or a back wall is made of: a hull material, or a planet's natural ground (0077).</summary>
/// <param name="Code">Stored in saves instead of the name: never reuse or renumber a code.</param>
/// <param name="Strength">
/// A natural wall's hit points: the damage of weapons' hits (0054) that breaks it out (0077). <see cref="Unbreakable"/>
/// for one nothing breaks, bedrock and lava. A tool's hits on a hull wall where tools break things, a planet.
/// </param>
/// <param name="Armour">
/// How it stands up to meteors (2026-10-29-wall-roles), apart from how hard it digs: the least power that breaks it, and twice
/// it what breaking it soaks of a blast. 0 for one no meteor breaks. Armour and <see cref="Leak"/> pull apart, so no wall is best
/// at both: metal and laid stone stand, their joints let air through; glass, mica and hide seal and give way.
/// </param>
/// <param name="Leak">
/// Oxygen units lost every <see cref="Air.LeakEvery"/> ticks (ten minutes) through one cell of it that an
/// aired room's air touches: a back wall behind the room, or a wall of its hull.
/// </param>
/// <param name="Cost">
/// Whole units of items one cell of it takes: scrap itself, iron as plates; what a natural wall yields when it breaks. Half the
/// 16 px cell's, rounded up (0104), so a wall of the same length, one cell thick, costs about what it did.
/// None for one never built, bedrock.
/// </param>
/// <param name="BuildTicks">
/// Ticks of a hero's work to build one cell; 0 for one never built. Half the 16 px cell's it was, rounded to the nearest, a half up:
/// a wall of the same area takes twice as long as at 16 px, so building is not instant (0104 had a quarter).
/// </param>
/// <param name="Natural">
/// A planet's ground (0077): its walls dug by weapons, never marked to take apart; walls and back walls of it may be planned,
/// and a back wall a hero built is taken apart as any (0084), the ground's own never.
/// </param>
/// <param name="Hurts">A hero touching a wall of it burns (<see cref="Lava"/>).</param>
/// <param name="Grows">Moss grows on a wall of it under the open sky (0079).</param>
/// <param name="Footing">What boots on it sound like (0093).</param>
/// <param name="Look">How it is drawn (<see cref="MaterialLook"/>).</param>
/// <param name="Menu">Its wall's card in the build menu (0114); none for one never built.</param>
/// <param name="BackMenu">Its back wall's card (0108, 0114); none for one never built.</param>
/// <param name="In">
/// A vein's (<see cref="Vein"/>): the material it runs through, drawn as that one flecked with its ore, the back wall it
/// leaves when dug that one's. None for any other.
/// </param>
/// <param name="Clear">
/// See-through (2026-10-29-clear-glass): light passes a wall or back wall of it as air. Drawn only: the sim gives it no rule.
/// </param>
public sealed record Material(
    string Id, int Code, int Strength, int Leak, IReadOnlyList<(string Item, int Count)> Cost, int BuildTicks, bool Natural = false,
    bool Hurts = false, bool Grows = false, Tread Footing = Tread.Deck, MaterialLook Look = MaterialLook.Cut, MenuSlot? Menu = null,
    MenuSlot? BackMenu = null, string? In = null, int Armour = 0, bool Clear = false)
{
    /// <summary>The <see cref="Strength"/> of a material nothing breaks.</summary>
    public const int Unbreakable = 0;

    /// <summary>Whether weapons break a wall of it (0077): natural and not <see cref="Unbreakable"/>.</summary>
    public bool Breaks => Natural && Strength != Unbreakable;

    /// <summary>Whether a wall of it may be planned and built: it takes work, and costs something.</summary>
    public bool Built => BuildTicks > 0;
}

/// <summary>What boots on a material sound like (0093): a deck's plates, rock, soil (grass or moss over it heard as such), the ruins' metal.</summary>
public enum Tread { Deck, Stone, Soil, Alloy }

/// <summary>How a material is drawn.</summary>
public enum MaterialLook
{
    /// <summary>Its pictures cut over the world a cell at a time, its back wall too: scrap's, the ground's.</summary>
    Cut,
    /// <summary>Its back wall a panel a 16 px cell, now and then a grille: iron's.</summary>
    Panels,
    /// <summary>Cut, but its surface, haze and embers are the lava view's: it shows no living edge.</summary>
    Molten,
}

/// <summary>Hull materials, weakest first, then the natural ones.</summary>
public static class Materials
{
    public static readonly IReadOnlyList<Material> All =
    [
        // Metal hull (2026-10-29-wall-roles): patched scrap and riveted plates stand up to meteors, and leak at their seams. Scrap is
        // patched of a raw chunk, so a hole is mended with no furnace and no air (2026-10-29-weak-meteors-till-landing).
        new("scrap", Code: 1, Strength: 2, Leak: 30, Cost: [("scrapChunk", 1)], BuildTicks: 7, Menu: new(MenuTabs.Structure, 10), BackMenu: new(MenuTabs.Structure, 110), Armour: 2),
        new("iron", Code: 2, Strength: 5, Leak: 24, Cost: [("plate", 1)], BuildTicks: 14, Look: MaterialLook.Panels, Menu: new(MenuTabs.Structure, 20), BackMenu: new(MenuTabs.Structure, 120), Armour: 5),
        // The seals: whole sheets no air gets through, and any meteor breaks. Alike but for their look, the player's choice.
        new("glass", Code: 11, Strength: 1, Leak: 6, Cost: [("glass", 2)], BuildTicks: 10, Menu: new(MenuTabs.Structure, 22), BackMenu: new(MenuTabs.Structure, 122), Armour: 1, Clear: true),
        new("mica", Code: 12, Strength: 1, Leak: 6, Cost: [("mica", 2)], BuildTicks: 8, Menu: new(MenuTabs.Structure, 24), BackMenu: new(MenuTabs.Structure, 124), Armour: 1),
        new("hide", Code: 13, Strength: 1, Leak: 6, Cost: [("hide", 1), ("fat", 1)], BuildTicks: 6, Menu: new(MenuTabs.Structure, 26), BackMenu: new(MenuTabs.Structure, 126), Armour: 1),
        // A planet's ground (0077), far leakier than any hull, soil the worst. Soil breaks at a swing of the axe, stone at three; a
        // shovelful of soil and a lump of rock, the asteroids' own, come out of it, and build it back. Laid stone stands up to meteors
        // better than scrap, soil worse.
        new("soil", Code: 3, Strength: 4, Leak: 120, Cost: [("soil", 1)], BuildTicks: 5, Natural: true, Grows: true, Footing: Tread.Soil, Menu: new(MenuTabs.Structure, 30), BackMenu: new(MenuTabs.Structure, 130), Armour: 1),
        new("stone", Code: 4, Strength: 12, Leak: 90, Cost: [("rock", 1)], BuildTicks: 10, Natural: true, Footing: Tread.Stone, Menu: new(MenuTabs.Structure, 40), BackMenu: new(MenuTabs.Structure, 140), Armour: 4),
        new("bedrock", Code: 5, Strength: Material.Unbreakable, Leak: 36, Cost: [], BuildTicks: 0, Natural: true, Footing: Tread.Stone),
        // The planet's core over its bedrock: molten rock nothing digs or builds, burning a hero who touches it (Content.Lava).
        new(Lava.Material, Code: 6, Strength: Material.Unbreakable, Leak: 36, Cost: [], BuildTicks: 0, Natural: true,
            Hurts: true, Footing: Tread.Stone, Look: MaterialLook.Molten),
        // The ruins' metal (0085): five times stone, a second and a half of the drill, under one with the arc fitted (0098); a scrap of
        // it a cell, what digging gives too. The best armour, its plates leaking as iron's do.
        new("alloy", Code: 7, Strength: 60, Leak: 24, Cost: [("alloyScrap", 3)], BuildTicks: 20, Natural: true, Footing: Tread.Alloy, Menu: new(MenuTabs.Structure, 50), BackMenu: new(MenuTabs.Structure, 150), Armour: 7),
        // The homes' warm back walls (0091): smoothed stone washed with lime. Never built, so no blueprint; no wall of it stands.
        new("plaster", Code: 8, Strength: 12, Leak: 90, Cost: [], BuildTicks: 0, Natural: true, Footing: Tread.Stone, Armour: 4),
        // Veins of ore in a planet's stone: as hard as it, a lump of ore or ice a cell, never built.
        new("copperVein", Code: 9, Strength: 12, Leak: 90, Cost: [("copperOre", 1)], BuildTicks: 0, Natural: true, Footing: Tread.Stone, In: "stone", Armour: 4),
        new("iceVein", Code: 10, Strength: 12, Leak: 90, Cost: [("ice", 1)], BuildTicks: 0, Natural: true, Footing: Tread.Stone, In: "stone", Armour: 4),
    ];

    /// <summary>What a <c>#</c> on a map is built of.</summary>
    public const string DefaultHull = "scrap";

    private static readonly Dictionary<string, Material> ById = All.ToDictionary(m => m.Id, StringComparer.Ordinal);

    private static readonly Material?[] ByCodes = Enumerable.Range(0, All.Max(m => m.Code) + 1)
        .Select(code => All.FirstOrDefault(m => m.Code == code)).ToArray();

    /// <summary>The material stored as <paramref name="code"/>; null for a code none has.</summary>
    public static Material? ByCode(int code) => code >= 0 && code < ByCodes.Length ? ByCodes[code] : null;

    /// <summary>The material by its id; throws for an id that is none.</summary>
    public static Material Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out Material material) => ById.TryGetValue(id, out material);
}
