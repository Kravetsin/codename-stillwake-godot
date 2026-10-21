using Anchor = Stillwake.Content.Flora.Anchor;
using Piece = Stillwake.Content.Flora.Piece;

namespace Stillwake.Content;

/// <summary>
/// What whoever came before left on the first planet (0085, 0090): prefabs drawn as ASCII, stamped into the ground as it grows
/// (<c>Terrain</c>), last, over the caves, the landmarks and what grew there. A prefab's characters: <c>#</c> a wall of alloy,
/// <c>.</c> open with an alloy back wall, <c>p</c> open with a plaster back wall, <c>s</c> a soil plug in a doorway (wall and
/// back wall soil), <c>t</c> a wall of stone, <c>,</c> open with a stone back wall, <c>c</c> a cache standing open on the alloy
/// floor under it, <c>r</c> a relic's shrine in a wall of alloy, <c>l</c> a lamp in a wall of alloy, a space the ground as it
/// grew. Where they go and how many: <c>Ground.Spires</c>, <c>Ground.RuinClear</c>, <c>Ground.SpireRise</c>; <c>Ground.Homes</c>,
/// <c>Ground.HomeClear</c>, <c>Ground.HomeDeepLeast</c>.
/// </summary>
public static class Ruins
{
    /// <summary>The growths a ruin sets (<see cref="Flora.Cave"/>, chances all 0: never grown): its cache, its shrine, its lamp.</summary>
    public const string SpireCache = "spireCache", RelicShrine = "relicShrine", SpireLamp = "spireLamp";

    /// <summary>Every growth a spire sets, never one the caves grow.</summary>
    public static readonly IReadOnlyList<string> Growths = [SpireCache, RelicShrine, SpireLamp];

    /// <summary>The characters of a prefab.</summary>
    public const char Alloy = '#', Open = '.', Plaster = 'p', Plug = 's', Stone = 't', OpenStone = ',', Cache = 'c', Shrine = 'r', Lamp = 'l', Keep = ' ';

    /// <summary>
    /// A spire, 9 × 30, its top row first: a tip of alloy with its lamp, a room in it walled at either side by a soil plug a
    /// hero's height (dug in two swings of the axe), over a body sunk in the ground, its roof at the anchor (row 10 with a
    /// rise of 10). A hole in the roof drops into a shaft of ledges, three rows apart and either side by turns, so a hero
    /// jumps back up as it fell down; at the bottom a chamber with a cache on its floor and a shrine in its right wall.
    /// </summary>
    public static readonly IReadOnlyList<string> Spire =
    [
        "    l    ",
        "    #    ",
        "   ###   ",
        "   ###   ",
        "   #.#   ",
        "  ##.##  ",
        "  #...#  ",
        "  s...s  ",
        "  s...s  ",
        "  s...s  ",
        "###...###",
        "#.......#",
        "#.......#",
        "####....#",
        "#.......#",
        "#.......#",
        "#....####",
        "#.......#",
        "#.......#",
        "####....#",
        "#.......#",
        "#.......#",
        "#....####",
        "#.......#",
        "#.......#",
        "####....#",
        "#.......r",
        "#....c..#",
        "#########",
        "#########",
    ];

    /// <summary>A spire's width, in cells.</summary>
    public static int SpireWide => Spire[0].Length;

    /// <summary>
    /// The spire's chamber furnished (0090, the homes page): a post, not a home, but someone kept it — a table and a chair, a lamp
    /// hanging over them, a shelf with an empty scroll case, the makers' banner by the shrine, a cobweb in the corner. Pieces by
    /// kind at their own cells in <see cref="Spire"/>'s columns and rows, as a home's are.
    /// </summary>
    public static readonly IReadOnlyList<(string Kind, int X, int Y)> SpireFurnishing =
        [("cobweb", 1, 26), ("ceilLamp", 2, 26), ("table", 1, 27), ("chair", 3, 27), ("wallShelf", 4, 26), ("banner", 6, 26)];

    /// <summary>The resonator (0090): it zaps who comes right up to it.</summary>
    public const string Resonator = "arcDevice";

    /// <summary>
    /// What the resonator does to a hero whose reported cell lies within <see cref="ZapReach"/> cells of its own, at each breath
    /// (<see cref="Crew.BreathEvery"/>): its suit pays <see cref="Zap"/> units, a ninth of a full one, as the lava burns.
    /// </summary>
    public const int Zap = 5, ZapReach = 1;

    /// <summary>
    /// The pieces of the arc makers' homes (0090), the concept page's catalogue: by its groups, light, furniture, the kitchen,
    /// cloth and memory, the child's, knowledge and work, plants and time. Its columns and rows (<c>Across</c>, <c>Tall</c>) are
    /// the cells its picture mostly covers; its hit points (<c>Strength</c>) a lamp's or a picture's one swing of the axe, a
    /// chair's two, a table's three, the hearth's and the resonator's six, as a tree's; paper, cloth, crockery and the chest
    /// fall to any hit, the music box to the axe's second. Its yields are the page's table.
    /// </summary>
    public static readonly IReadOnlyList<Piece> Pieces =
    [
        // Light: the arc lamps, the homes' first light and their best find; a whole one gives itself (arcLamp).
        new("ceilLamp", Anchor.Ceiling, Tall: 1) { Strength = 2, Yields = [("arcLamp", 1)] },
        new("sconce", Anchor.Back, Tall: 1) { Behind = true, Strength = 2, Yields = [("arcLamp", 1)] },
        new("floorLamp", Anchor.Floor, Tall: 3) { Strength = 2, Yields = [("arcLamp", 1), ("alloyScrap", 1)] },
        new("lantern", Anchor.Floor, Tall: 1) { Strength = 2, Yields = [("glass", 1), ("wire", 1)] },
        new("arcCandle", Anchor.Floor, Tall: 1) { Yields = [("wire", 1)] },
        new("lightWindow", Anchor.Back, Tall: 2) { Across = 2, Behind = true, Strength = 2, Yields = [("glass", 2), ("alloyScrap", 1)] },
        new("garland", Anchor.Ceiling, Tall: 1) { Across = 4, Yields = [("wire", 2), ("glass", 1)] },
        new("brokenLamp", Anchor.Ceiling, Tall: 1) { Yields = [("glass", 1), ("wire", 1)] },
        // Furniture: what they sat on, slept on and kept things in.
        new("table", Anchor.Floor, Tall: 1) { Across = 2, Strength = 6, Yields = [("planks", 3), ("alloyScrap", 1)] },
        new("chair", Anchor.Floor, Tall: 2) { Strength = 4, Yields = [("planks", 2)] },
        new("stool", Anchor.Floor, Tall: 1) { Strength = 2, Yields = [("planks", 1)] },
        new("bench", Anchor.Floor, Tall: 1) { Across = 2, Strength = 4, Yields = [("planks", 2), ("cloth", 1)] },
        new("bed", Anchor.Floor, Tall: 2) { Across = 3, Strength = 6, Yields = [("planks", 3), ("cloth", 2)] },
        new("cradle", Anchor.Floor, Tall: 2) { Strength = 4, Yields = [("planks", 2), ("cloth", 1), ("wire", 1)] },
        new("wardrobe", Anchor.Floor, Tall: 3) { Across = 2, Strength = 6, Yields = [("planks", 4), ("cloth", 1)] },
        // A small cache: it opens to any hit, as a spire's does.
        new("chest", Anchor.Floor, Tall: 1) { Yields = [("planks", 2), ("alloyScrap", 1), ("wire", 1), ("glass", 1)] },
        // The kitchen: the hearth, its things and the supper they did not finish. The hearth stands floor to ceiling, its flue too.
        new("hearth", Anchor.Floor, Tall: 5) { Across = 2, Strength = 12, Yields = [("alloyScrap", 3), ("copper", 2), ("coil", 1)] },
        new("kettle", Anchor.Floor, Tall: 1) { Strength = 2, Yields = [("copper", 2)] },
        new("dishes", Anchor.Floor, Tall: 1) { Yields = [("rock", 1)] },
        new("pots", Anchor.Floor, Tall: 1) { Yields = [("rock", 1)] },
        new("basin", Anchor.Floor, Tall: 2) { Across = 2, Strength = 6, Yields = [("rock", 2), ("copper", 1)] },
        new("shelfJars", Anchor.Back, Tall: 1) { Across = 2, Behind = true, Strength = 2, Yields = [("glass", 1), ("spore", 2)] },
        new("utensils", Anchor.Back, Tall: 1) { Across = 2, Behind = true, Strength = 2, Yields = [("copper", 2), ("iron", 1)] },
        // The page's chance of a seed is a seed: bushes give them freely already.
        new("herbs", Anchor.Ceiling, Tall: 1) { Yields = [("seed", 1)] },
        // Cloth and memory: banners with the makers' emblem, the tapestry, pictures, the family.
        new("banner", Anchor.Back, Tall: 2) { Behind = true, Yields = [("cloth", 1)] },
        new("tapestry", Anchor.Back, Tall: 2) { Across = 3, Behind = true, Strength = 2, Yields = [("cloth", 3)] },
        new("rug", Anchor.Floor, Tall: 1) { Across = 3, Behind = true, Yields = [("cloth", 2)] },
        new("curtain", Anchor.Ceiling, Tall: 3) { Yields = [("cloth", 2)] },
        new("mural", Anchor.Back, Tall: 2) { Across = 4, Behind = true, Fixed = true },
        new("frame", Anchor.Back, Tall: 1) { Across = 2, Behind = true, Strength = 2, Yields = [("planks", 1)] },
        new("holoPortrait", Anchor.Back, Tall: 1) { Behind = true, Strength = 2, Yields = [("glass", 1), ("circuit", 1)] },
        new("clock", Anchor.Back, Tall: 2) { Behind = true, Strength = 2, Yields = [("planks", 1), ("gear", 1), ("copper", 1)] },
        new("coatPegs", Anchor.Back, Tall: 2) { Behind = true, Strength = 2, Yields = [("cloth", 1), ("alloyScrap", 1)] },
        new("basket", Anchor.Floor, Tall: 1) { Yields = [("planks", 1), ("cloth", 1)] },
        // The child's: toys, drawings hung low, the height marks on the door's jamb, the music box that plays when hit.
        new("toys", Anchor.Floor, Tall: 1) { Across = 2, Yields = [("planks", 1), ("wire", 1)] },
        new("musicBox", Anchor.Floor, Tall: 1) { Strength = 3, Yields = [("gear", 1), ("copper", 1)] },
        new("drawings", Anchor.Back, Tall: 1) { Across = 2, Behind = true },
        new("heightMarks", Anchor.Back, Tall: 3) { Behind = true },
        // Knowledge and work: the study's scroll cases, its dead screen, its tools, the resonator.
        new("desk", Anchor.Floor, Tall: 2) { Across = 2, Strength = 6, Yields = [("planks", 3), ("circuit", 1), ("glass", 1)] },
        new("bookshelf", Anchor.Floor, Tall: 3) { Across = 2, Strength = 6, Yields = [("planks", 3), ("copper", 1)] },
        new("wallShelf", Anchor.Back, Tall: 1) { Across = 2, Behind = true, Strength = 2, Yields = [("planks", 1), ("copper", 1)] },
        new("toolRack", Anchor.Back, Tall: 1) { Across = 2, Behind = true, Strength = 2, Yields = [("planks", 1), ("wire", 3), ("iron", 1)] },
        new(Resonator, Anchor.Floor, Tall: 3) { Strength = 12, Yields = [("copper", 3), ("wire", 2), ("crystal", 1), ("alloyScrap", 2)] },
        // Plants: one outlived its makers and glows, the other did not.
        new("plantAlive", Anchor.Floor, Tall: 2) { Yields = [("spore", 1), ("seed", 1)] },
        new("plantDead", Anchor.Floor, Tall: 1) { Yields = [("rock", 1)] },
        // Time: dust, the cobweb, a crack, the fallen beam and the rubble, which lie in walls of their own and block the way.
        new("cobweb", Anchor.Ceiling, Tall: 1),
        new("crack", Anchor.Back, Tall: 2) { Behind = true, Fixed = true },
        new("fallenBeam", Anchor.Rock, Tall: 1) { Across = 2, Yields = [("alloyScrap", 3)] },
        new("rubble", Anchor.Rock, Tall: 1) { Across = 2, Yields = [("rock", 2), ("alloyScrap", 1)] },
    ];

    /// <summary>
    /// A home of the arc makers (0090): its shell as ASCII, its top row first (<see cref="Ruins"/>' characters), and its pieces,
    /// each by kind at its own cell — a floor's bottom-left, a ceiling's, a back wall's or one in the rock's top-left — in the
    /// prefab's columns and rows. The caves' growth on a collapsed end is listed as its pieces are.
    /// </summary>
    public sealed record Home(string Id, IReadOnlyList<string> Rows, IReadOnlyList<(string Kind, int X, int Y)> Pieces)
    {
        public int Wide => Rows[0].Length;
        public int Tall => Rows.Count;
    }

    /// <summary>
    /// A: the hearth room, the family's table with a supper left on it and a candle still lit, the kitchen through an arch at the
    /// right: the hearth with its coil, the kettle on a stool, herbs drying, the jar of what still glows; by the door the coat pegs
    /// and the height marks. A soil plug a hero's height in either end wall.
    /// </summary>
    public static readonly Home Hearth = new("hearth",
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
        ("clock", 15, 1), ("herbs", 17, 1), ("herbs", 18, 1), ("hearth", 20, 5), ("utensils", 22, 2), ("stool", 22, 5),
        ("kettle", 22, 4), ("shelfJars", 24, 2), ("sconce", 26, 2), ("basin", 27, 5), ("wallShelf", 28, 2), ("pots", 29, 5),
    ]);

    /// <summary>
    /// B: the bedroom, the grown-ups' bed and the wardrobe, the tapestry of the spire and the arc; behind the curtain the child's
    /// corner, the cradle under its mobile, the garland, the toys, the drawings hung low, the music box on a stool. A plug either end.
    /// </summary>
    public static readonly Home Bedroom = new("bedroom",
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
    public static readonly Home Study = new("study",
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
