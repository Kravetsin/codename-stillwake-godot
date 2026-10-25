using Anchor = Stillwake.Content.Flora.Anchor;
using Piece = Stillwake.Content.Flora.Piece;

namespace Stillwake.Content;

/// <summary>
/// What whoever came before left on the first planet (0085, 0091, 0096): prefabs drawn as ASCII, stamped into the ground as it
/// grows (<c>Terrain</c>), last, over the caves, the landmarks and what grew there. A prefab's characters: <c>#</c> a wall of
/// alloy, <c>.</c> open with an alloy back wall, <c>p</c> open with a plaster back wall, <c>s</c> a soil plug in a doorway (wall
/// and back wall soil), <c>t</c> a wall of stone, <c>,</c> open with a stone back wall, <c>c</c> a cache standing open on the
/// alloy floor under it, <c>l</c> the lens in a wall of alloy, a space the ground as it
/// grew. Where they go and how many: <c>Ground.Spires</c>, <c>Ground.RuinClear</c>, <c>Ground.SpireRise</c>; <c>Ground.Homes</c>,
/// <c>Ground.HomeClear</c>, <c>Ground.HomeDeepLeast</c>.
/// </summary>
public static class Ruins
{
    /// <summary>The growths a spire sets (<see cref="Flora.Cave"/>, chances all 0: never grown): its cache, the lens in its tip.</summary>
    public const string SpireCache = "spireCache", SpireLamp = "spireLamp";

    /// <summary>Every cave kind a spire sets, never one the caves grow; what furnishes it are pieces (<see cref="SpireFurnishing"/>).</summary>
    public static readonly IReadOnlyList<string> Growths = [SpireCache, SpireLamp];

    /// <summary>
    /// The arc relic (0096): a sky shard, black glass with white cracks alight, held upright in a copper cradle on a spire's chamber
    /// floor (<see cref="Cradle"/>), which splits into four shards as it is taken; and a chip of one in a wall niche of the hearth
    /// home (<see cref="Niche"/>). A shard (<see cref="Shard"/>) is what an arc module is made of; analysed, it opens its blueprint.
    /// </summary>
    public const string Cradle = "shardCradle", Niche = "shardNiche", Shard = "arcShard";

    /// <summary>The pieces holding a relic: a spire's light and a home's chip-fed lamps burn while theirs stands, from the felled cells.</summary>
    public static readonly IReadOnlyList<string> Relics = [Cradle, Niche];

    /// <summary>
    /// What a hit on a cradle costs its striker (0096), as the resonator's zap: the axe's hit <see cref="CradleShock"/> units of its
    /// suit; a faster tool's hit its share of the axe's pace, rounded up, so a second of hitting costs as much whatever strikes.
    /// </summary>
    public const int CradleShock = 5;

    /// <summary>The characters of a prefab.</summary>
    public const char Alloy = '#', Open = '.', Plaster = 'p', Plug = 's', Stone = 't', OpenStone = ',', Cache = 'c', Lamp = 'l', Keep = ' ';

    /// <summary>
    /// A spire, drawn 9 × 30 a character a 16 px cell and so 18 × 60 cells (0104), its top row first: a light well (0096). In the drawn rows: the lens in its tip over a flue open down its middle column, the
    /// light's own, open all the way to the chamber; a room in the tip walled at either side by a soil plug a hero's height (dug in
    /// two swings of the axe), its floor beside a hole three wide, over a body sunk in the ground, its roof at the anchor (row 10
    /// with a rise of 10). Ledges four rows apart either side by turns, so none is over the one under it on its side and each is a
    /// held jump's four rows up with room for the hero's head (a jump rises six, the hero is 2.625 of them tall: three rows apart, the ledge
    /// over the take-off stops it); a slab two rows over the chamber's floor the first step; at the bottom a chamber five rows tall,
    /// a cache on its floor and the relic's cradle under the light.
    /// </summary>
    public static readonly IReadOnlyList<string> Spire = Doubled(
    [
        "   #l#   ",
        "   #.#   ",
        "   #.#   ",
        "   #.#   ",
        "   #.#   ",
        "  ##.##  ",
        "  #...#  ",
        "  s...s  ",
        "  s...s  ",
        "  s...s  ",
        "##...####",
        "#.......#",
        "#.......#",
        "#.......#",
        "####....#",
        "#.......#",
        "#.......#",
        "#.......#",
        "#....####",
        "#.......#",
        "#.......#",
        "#.......#",
        "####....#",
        "#.......#",
        "#.......#",
        "#.......#",
        "#.....###",
        "#c......#",
        "#########",
        "#########",
    ]);

    /// <summary>A spire's width, in cells.</summary>
    public static int SpireWide => Spire[0].Length;

    /// <summary>The resonator (0091): it zaps who comes right up to it.</summary>
    public const string Resonator = "arcDevice";

    /// <summary>
    /// What the resonator does to a hero whose reported cell lies within <see cref="ZapReach"/> cells of its own, at each breath
    /// (<see cref="Crew.BreathEvery"/>): its suit pays <see cref="Zap"/> units, a ninth of a full one, as the lava burns.
    /// </summary>
    public const int Zap = 5, ZapReach = 2;

    /// <summary>
    /// The pieces of the arc makers' homes (0091), the concept page's catalogue: by its groups, light, furniture, the kitchen,
    /// cloth and memory, the child's, knowledge and work, plants and time. Its columns and rows (<c>Across</c>, <c>Tall</c>) are
    /// the cells its picture mostly covers; its hit points (<c>Strength</c>) a lamp's or a picture's one swing of the axe, a
    /// chair's two, a table's three, the hearth's and the resonator's six, as a tree's; paper, cloth, crockery and the chest
    /// fall to any hit, the music box to the axe's second. Its yields are the page's table.
    /// </summary>
    public static readonly IReadOnlyList<Piece> Pieces =
    [
        // Light: the arc lamps, the homes' first light and their best find; a whole one gives itself (arcLamp).
        new("ceilLamp", Anchor.Ceiling, Tall: 2) { Strength = 4, Yields = [("arcLamp", 1)] },
        new("sconce", Anchor.Back, Tall: 2) { Behind = true, Strength = 4, Yields = [("arcLamp", 1)] },
        new("floorLamp", Anchor.Floor, Tall: 6) { Strength = 4, Yields = [("arcLamp", 1), ("alloyScrap", 1)] },
        new("lantern", Anchor.Floor, Tall: 2) { Strength = 4, Yields = [("glass", 1), ("wire", 1)] },
        new("arcCandle", Anchor.Floor, Tall: 2) { Yields = [("wire", 1)] },
        new("lightWindow", Anchor.Back, Tall: 4) { Across = 4, Behind = true, Strength = 4, Yields = [("glass", 2), ("alloyScrap", 1)] },
        new("garland", Anchor.Ceiling, Tall: 2) { Across = 8, Yields = [("wire", 2), ("glass", 1)] },
        new("brokenLamp", Anchor.Ceiling, Tall: 2) { Yields = [("glass", 1), ("wire", 1)] },
        // Furniture: what they sat on, slept on and kept things in.
        new("table", Anchor.Floor, Tall: 2) { Across = 4, Strength = 12, Yields = [("planks", 3), ("alloyScrap", 1)] },
        new("chair", Anchor.Floor, Tall: 4) { Strength = 8, Yields = [("planks", 2)] },
        new("stool", Anchor.Floor, Tall: 2) { Strength = 4, Yields = [("planks", 1)] },
        new("bench", Anchor.Floor, Tall: 2) { Across = 4, Strength = 8, Yields = [("planks", 2), ("cloth", 1)] },
        new("bed", Anchor.Floor, Tall: 4) { Across = 6, Strength = 12, Yields = [("planks", 3), ("cloth", 2)] },
        new("cradle", Anchor.Floor, Tall: 4) { Strength = 8, Yields = [("planks", 2), ("cloth", 1), ("wire", 1)] },
        new("wardrobe", Anchor.Floor, Tall: 6) { Across = 4, Strength = 12, Yields = [("planks", 4), ("cloth", 1)] },
        // A small cache: it opens to any hit, as a spire's does.
        new("chest", Anchor.Floor, Tall: 2) { Yields = [("planks", 2), ("alloyScrap", 1), ("wire", 1), ("glass", 1)] },
        // The kitchen: the hearth, its things and the supper they did not finish. The hearth stands floor to ceiling, its flue too.
        new("hearth", Anchor.Floor, Tall: 10) { Across = 4, Strength = 24, Yields = [("alloyScrap", 3), ("copper", 2), ("coil", 1)] },
        new("kettle", Anchor.Floor, Tall: 2) { Strength = 4, Yields = [("copper", 2)] },
        new("dishes", Anchor.Floor, Tall: 2) { Yields = [("rock", 1)] },
        new("pots", Anchor.Floor, Tall: 2) { Yields = [("rock", 1)] },
        new("basin", Anchor.Floor, Tall: 4) { Across = 4, Strength = 12, Yields = [("rock", 2), ("copper", 1)] },
        new("shelfJars", Anchor.Back, Tall: 2) { Across = 4, Behind = true, Strength = 4, Yields = [("glass", 1), ("spore", 2)] },
        new("utensils", Anchor.Back, Tall: 2) { Across = 4, Behind = true, Strength = 4, Yields = [("copper", 2), ("iron", 1)] },
        // The page's chance of a seed is a seed: bushes give them freely already.
        new("herbs", Anchor.Ceiling, Tall: 2) { Yields = [("seed", 1)] },
        // Cloth and memory: banners with the makers' emblem, the tapestry, pictures, the family.
        new("banner", Anchor.Back, Tall: 4) { Behind = true, Yields = [("cloth", 1)] },
        new("tapestry", Anchor.Back, Tall: 4) { Across = 6, Behind = true, Strength = 4, Yields = [("cloth", 3)] },
        new("rug", Anchor.Floor, Tall: 2) { Across = 6, Behind = true, Yields = [("cloth", 2)] },
        new("curtain", Anchor.Ceiling, Tall: 6) { Yields = [("cloth", 2)] },
        new("mural", Anchor.Back, Tall: 4) { Across = 8, Behind = true, Fixed = true },
        new("frame", Anchor.Back, Tall: 2) { Across = 4, Behind = true, Strength = 4, Yields = [("planks", 1)] },
        new("holoPortrait", Anchor.Back, Tall: 2) { Behind = true, Strength = 4, Yields = [("glass", 1), ("circuit", 1)] },
        new("clock", Anchor.Back, Tall: 4) { Behind = true, Strength = 4, Yields = [("planks", 1), ("gear", 1), ("copper", 1)] },
        new("coatPegs", Anchor.Back, Tall: 4) { Behind = true, Strength = 4, Yields = [("cloth", 1), ("alloyScrap", 1)] },
        new("basket", Anchor.Floor, Tall: 2) { Yields = [("planks", 1), ("cloth", 1)] },
        // The child's: toys, drawings hung low, the height marks on the door's jamb, the music box that plays when hit.
        new("toys", Anchor.Floor, Tall: 2) { Across = 4, Yields = [("planks", 1), ("wire", 1)] },
        new("musicBox", Anchor.Floor, Tall: 2) { Strength = 6, Yields = [("gear", 1), ("copper", 1)] },
        new("drawings", Anchor.Back, Tall: 2) { Across = 4, Behind = true },
        new("heightMarks", Anchor.Back, Tall: 6) { Behind = true },
        // Knowledge and work: the study's scroll cases, its dead screen, its tools, the resonator.
        new("desk", Anchor.Floor, Tall: 4) { Across = 4, Strength = 12, Yields = [("planks", 3), ("circuit", 1), ("glass", 1)] },
        new("bookshelf", Anchor.Floor, Tall: 6) { Across = 4, Strength = 12, Yields = [("planks", 3), ("copper", 1)] },
        new("wallShelf", Anchor.Back, Tall: 2) { Across = 4, Behind = true, Strength = 4, Yields = [("planks", 1), ("copper", 1)] },
        new("toolRack", Anchor.Back, Tall: 2) { Across = 4, Behind = true, Strength = 4, Yields = [("planks", 1), ("wire", 3), ("iron", 1)] },
        new(Resonator, Anchor.Floor, Tall: 6) { Strength = 24, Yields = [("copper", 3), ("wire", 2), ("crystal", 1), ("alloyScrap", 2)] },
        // Plants: one outlived its makers and glows, the other did not.
        new("plantAlive", Anchor.Floor, Tall: 4) { Yields = [("spore", 1), ("seed", 1)] },
        new("plantDead", Anchor.Floor, Tall: 2) { Yields = [("rock", 1)] },
        // Time: dust, the cobweb, a crack, the fallen beam and the rubble, which lie in walls of their own and block the way.
        new("cobweb", Anchor.Ceiling, Tall: 2),
        new("crack", Anchor.Back, Tall: 4) { Behind = true, Fixed = true },
        new("fallenBeam", Anchor.Rock, Tall: 2) { Across = 4, Yields = [("alloyScrap", 3)] },
        new("rubble", Anchor.Rock, Tall: 2) { Across = 4, Yields = [("rock", 2), ("alloyScrap", 1)] },
        // The relic (0096): the sky shard in its cradle splits into four shards as it is taken, every hit on it discharging into
        // its striker (CradleShock); the chip in the hearth home's niche is one.
        new(Cradle, Anchor.Floor, Tall: 6) { Across = 6, Strength = 16, Yields = [(Shard, 4)] },
        new(Niche, Anchor.Back, Tall: 4) { Behind = true, Strength = 4, Yields = [(Shard, 1)] },
    ];

    /// <summary>
    /// The spire's chamber furnished (0091, the homes page; 0096): the relic's cradle in its middle under the light, and a post
    /// someone kept — a lamp hanging under the ledge over the cache, the makers' banner by it, a shelf over the slab, a cobweb up in
    /// the shaft. Pieces by kind at their own cells in <see cref="Spire"/>'s columns and rows, as a home's are, drawn at the
    /// prefab's characters.
    /// </summary>
    public static readonly IReadOnlyList<(string Kind, int X, int Y)> SpireFurnishing =
        Placed([("banner", 1, 24), ("ceilLamp", 2, 23), (Cradle, 3, 27), ("wallShelf", 6, 24), ("cobweb", 7, 19)]);

    /// <summary>
    /// A prefab drawn a character a 16 px cell, as the 8 px cells it covers (0104): each character two by two, so its walls are
    /// two cells thick and its rooms as tall as they were.
    /// </summary>
    private static IReadOnlyList<string> Doubled(IReadOnlyList<string> rows) =>
        [.. rows.SelectMany(r => Enumerable.Repeat(string.Concat(r.Select(c => new string(c, 2))), 2))];

    /// <summary>
    /// Pieces placed at a drawn prefab's characters, at their cells in its doubled one (0104): one on a floor by its block's
    /// bottom-left, any other by its top-left.
    /// </summary>
    private static IReadOnlyList<(string Kind, int X, int Y)> Placed(IReadOnlyList<(string Kind, int X, int Y)> drawn) =>
        [.. drawn.Select(p => (p.Kind, 2 * p.X, 2 * p.Y + (AnchorOf(p.Kind) == Anchor.Floor ? 1 : 0)))];

    /// <summary>What a piece or a cave growth set in a home holds on to.</summary>
    private static Anchor AnchorOf(string kind) =>
        Pieces.FirstOrDefault(p => p.Id == kind)?.Holds ?? Flora.Cave.First(k => k.Id == kind).Holds;

    /// <summary>A home drawn as its prefab's characters and its pieces at them, at the cell's scale (0104).</summary>
    private static Home Drawn(string id, IReadOnlyList<string> rows, IReadOnlyList<(string Kind, int X, int Y)> pieces) =>
        new(id, Doubled(rows), Placed(pieces));

    /// <summary>
    /// A home of the arc makers (0091): its shell as ASCII, its top row first (<see cref="Ruins"/>' characters), and its pieces,
    /// each by kind at its own cell — a floor's bottom-left, a ceiling's, a back wall's or one in the rock's top-left — in the
    /// prefab's columns and rows. The caves' growth on a collapsed end is listed as its pieces are. Drawn a character a 16 px
    /// cell, each made the 2×2 of 8 px cells it covers (<see cref="Drawn"/>, 0104).
    /// </summary>
    public sealed record Home(string Id, IReadOnlyList<string> Rows, IReadOnlyList<(string Kind, int X, int Y)> Pieces)
    {
        public int Wide => Rows[0].Length;
        public int Tall => Rows.Count;
    }

    /// <summary>
    /// A: the hearth room, the family's table with a supper left on it and a candle still lit, the kitchen through an arch at the
    /// right: the hearth with its coil, the kettle on a stool, herbs drying, the jar of what still glows; by the door the coat pegs
    /// and the height marks; in the wall by the arch, the home's heart (0096): a chip of a sky shard in a niche, which still feeds
    /// the hearth and the lamp over the table. A soil plug a hero's height in either end wall.
    /// </summary>
    public static readonly Home Hearth = Drawn("hearth",
    [
        "################################",
        "#ppppppppppppppp#pppppppppppppp#",
        "#pppppppppppppppppppppppppppppp#",
        "spppppppppppppppppppppppppppppps",
        "spppppppppppppppppppppppppppppps",
        "s..............................s",
        "################################",
    ],
    [
        ("cobweb", 1, 1), ("heightMarks", 1, 3), ("coatPegs", 2, 1), ("basket", 2, 5), ("lightWindow", 3, 1), ("bench", 3, 5),
        ("rug", 5, 5), ("chair", 6, 5), ("table", 7, 5), ("dishes", 7, 4), ("arcCandle", 8, 4), ("chair", 9, 5), ("ceilLamp", 7, 1),
        ("frame", 10, 2), ("stool", 10, 5), ("plantAlive", 11, 5), ("holoPortrait", 12, 2), ("banner", 13, 1), ("pots", 14, 5),
        ("clock", 15, 1), ("herbs", 17, 1), ("herbs", 18, 1), (Niche, 17, 3), ("hearth", 20, 5), ("utensils", 22, 2), ("stool", 22, 5),
        ("kettle", 22, 4), ("shelfJars", 24, 2), ("sconce", 26, 2), ("basin", 27, 5), ("wallShelf", 28, 2), ("pots", 29, 5),
    ]);

    /// <summary>
    /// B: the bedroom, the grown-ups' bed and the wardrobe, the tapestry of the spire and the arc; behind the curtain the child's
    /// corner, the cradle under its mobile, the garland, the toys, the drawings hung low, the music box on a stool. A plug either end.
    /// </summary>
    public static readonly Home Bedroom = Drawn("bedroom",
    [
        "##############################",
        "#pppppppppppppppppppppppppppp#",
        "#pppppppppppppppppppppppppppp#",
        "spppppppppppppppppppppppppppps",
        "spppppppppppppppppppppppppppps",
        "s............................s",
        "##############################",
    ],
    [
        ("cobweb", 1, 1), ("wardrobe", 1, 5), ("sconce", 3, 2), ("tapestry", 4, 1), ("bed", 4, 5), ("chest", 7, 5), ("floorLamp", 8, 5),
        ("lightWindow", 10, 1), ("basket", 10, 5), ("plantDead", 11, 5), ("curtain", 14, 1), ("garland", 15, 1), ("rug", 15, 5),
        ("cradle", 16, 5), ("drawings", 17, 3), ("toys", 17, 5), ("stool", 19, 5), ("musicBox", 19, 4), ("holoPortrait", 20, 2),
        ("chair", 22, 5), ("wallShelf", 23, 2), ("plantAlive", 24, 5), ("lantern", 25, 5), ("banner", 26, 1), ("pots", 28, 5),
    ]);

    /// <summary>
    /// C: the study, half caved in: the shelves of scroll cases, nearly all empty, one still glowing violet; the desk with its
    /// dead screen, the tool board, the mural of the stair going down, the resonator still talking to itself. At the right the
    /// ceiling came down: the beam fallen across the floor, rubble, a mound of stone grown over with the caves' moss and caps,
    /// threads and roots hanging from what is left above. A plug at its left only.
    /// </summary>
    public static readonly Home Study = Drawn("study",
    [
        "##########################ttttttt#",
        "#ppppppppppppppppppppppppp,,ttttt#",
        "#ppppppppppppppppppppppppp,,,,ttt#",
        "sppppppppppppppppppppppppp,,,tttt#",
        "sppppppppppppppppppppppppp,,ttttt#",
        "s...................##tt..ttttttt#",
        "##################################",
    ],
    [
        ("cobweb", 1, 1), ("bookshelf", 1, 5), ("wallShelf", 4, 2), ("desk", 4, 5), ("chair", 6, 5), ("toolRack", 7, 2), ("table", 7, 5),
        ("lantern", 7, 4), ("pots", 8, 4), ("mural", 10, 1), ("stool", 10, 5), ("brokenLamp", 12, 1), ("plantDead", 12, 5),
        (Resonator, 14, 5), ("sconce", 16, 2), ("chest", 17, 5), ("crack", 19, 1), ("fallenBeam", 20, 5), ("rubble", 22, 5),
        ("threadTeal", 24, 1), ("moss", 26, 5), ("moss", 27, 5), ("glowcapTeal", 26, 4), ("hangcap", 27, 1), ("moss", 28, 4),
        ("moss", 28, 1), ("roots", 28, 2), ("moss", 29, 3), ("moss", 29, 1), ("glowcapViolet", 29, 2),
    ]);

    /// <summary>The homes' schemes, in the order a planet's homes take them.</summary>
    public static readonly IReadOnlyList<Home> Homes = [Hearth, Bedroom, Study];
}
