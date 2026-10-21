using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// How a planet's natural ground looks (0077, 0079): each material's colour and fleck, the chips a dig knocks off; and the
/// lines its edges are drawn in where it meets the open, a dark outline and, on top, a lit line inside it. Its walls and back
/// walls are drawn in <c>art/planet</c>, in the "Stillwake" style.
/// </summary>
public static class GroundLooks
{
    /// <summary>A natural material's look, 0..255 a channel: its colour, its fleck, its outline, its lit top line.</summary>
    public sealed record Look((int R, int G, int B) Colour, (int R, int G, int B) Fleck, (int R, int G, int B) Outline, (int R, int G, int B) Lit);

    public static readonly IReadOnlyDictionary<string, Look> Of = new Dictionary<string, Look>
    {
        ["soil"] = new((98, 74, 58), (125, 96, 74), (36, 25, 15), (138, 106, 80)),
        ["stone"] = new((97, 103, 119), (128, 136, 152), (26, 28, 36), (138, 146, 160)),
        ["bedrock"] = new((56, 53, 66), (90, 86, 106), (20, 18, 24), (78, 74, 92)),
        // Molten under a dark crust: drawn glowing over its tiles by the lava view, these only its chips and edges.
        ["lava"] = new((150, 40, 12), (255, 150, 40), (40, 10, 4), (255, 190, 80)),
    };

    /// <summary>How many of a dig's chips fly per hit, and the dust of a break.</summary>
    public const int Chips = 5, BreakDust = 18, BreakChips = 8;
}
