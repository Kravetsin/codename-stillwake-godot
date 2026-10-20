using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// How a planet's natural ground looks (0077), placeholders till its art is picked: each material's colour, which its
/// walls, back walls and the chips a dig knocks off are drawn in, and how far its pixels stray from it either way.
/// </summary>
public static class GroundLooks
{
    /// <summary>A natural material's look: its colour (0..255 a channel), its speckle, and how dark its back wall is drawn.</summary>
    public sealed record Look((int R, int G, int B) Colour, int Stray, (int R, int G, int B) Fleck, double Back);

    public static readonly IReadOnlyDictionary<string, Look> Of = new Dictionary<string, Look>
    {
        ["soil"] = new((116, 80, 50), 12, (150, 112, 72), 0.5),
        ["stone"] = new((104, 106, 114), 10, (140, 144, 152), 0.45),
        ["bedrock"] = new((54, 48, 62), 8, (84, 76, 94), 0.6),
    };

    /// <summary>How many of a dig's chips fly per hit, and the dust of a break.</summary>
    public const int Chips = 5, BreakDust = 18, BreakChips = 8;
}
