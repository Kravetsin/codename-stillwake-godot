namespace Stillwake.Content;

/// <summary>
/// What whoever came before left on the first planet (0085): prefabs drawn as ASCII, stamped into the ground as it grows
/// (<c>Terrain</c>), last, over the caves, the landmarks and what grew there. A prefab's characters: <c>#</c> a wall of alloy,
/// <c>.</c> open with an alloy back wall, <c>s</c> a soil plug in a doorway (wall and back wall soil), <c>c</c> a cache
/// standing open on the alloy floor under it, <c>r</c> a relic's shrine in a wall of alloy, <c>l</c> a lamp in a wall of
/// alloy, a space the ground as it grew. Where they go and how many: <c>Ground.Spires</c>, <c>Ground.RuinClear</c>,
/// <c>Ground.SpireRise</c>.
/// </summary>
public static class Ruins
{
    /// <summary>The growths a ruin sets (<see cref="Flora.Cave"/>, chances all 0: never grown): its cache, its shrine, its lamp.</summary>
    public const string SpireCache = "spireCache", RelicShrine = "relicShrine", SpireLamp = "spireLamp";

    /// <summary>Every growth a ruin sets, never one the caves grow.</summary>
    public static readonly IReadOnlyList<string> Growths = [SpireCache, RelicShrine, SpireLamp];

    /// <summary>The characters of a prefab.</summary>
    public const char Alloy = '#', Open = '.', Plug = 's', Cache = 'c', Shrine = 'r', Lamp = 'l', Keep = ' ';

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
}
