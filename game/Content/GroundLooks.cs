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
        // The ruins' alloy (0085): cool steel with a touch of teal, a placeholder till the user picks its art.
        ["alloy"] = new((78, 103, 114), (110, 138, 147), (20, 29, 36), (166, 194, 198)),
        // The homes' plaster (0091), the page's warm lime-washed stone: seen only as their back walls.
        ["plaster"] = new((92, 79, 69), (106, 90, 76), (36, 28, 22), (130, 112, 96)),
    };

    /// <summary>How many of a dig's chips fly per hit, and the dust of a break.</summary>
    public const int Chips = 5, BreakDust = 18, BreakChips = 8;

    /// <summary>
    /// How a natural back wall recedes behind its ground (0088), worked on its picture at load: paled toward its grey by
    /// <see cref="BackPale"/>, cooled by <see cref="BackCool"/> of <see cref="CoolShift"/> (each colour's change, cooled fully),
    /// then darkened to <see cref="BackDim"/>.
    /// </summary>
    public const double BackDim = 0.62, BackPale = 0.3, BackCool = 0.6;

    public static readonly (double R, double G, double B) CoolShift = (-0.14, -0.05, 0.16);

    /// <summary>
    /// The soft shadow the ground casts on a natural back wall along its open edges (0088): the ground's pixels blurred twice
    /// by a box <see cref="ShadowReach"/> pixels each way (about twice that in all), times <see cref="ShadowGain"/>, in
    /// <see cref="ShadowSteps"/> tones, the darkest taking <see cref="ShadowDepth"/> of the back wall's light away.
    /// </summary>
    public const int ShadowReach = 3, ShadowSteps = 3;

    public const double ShadowGain = 2.1, ShadowDepth = 0.5;

    /// <summary>
    /// The lit edge of the ground where it meets the open (0088), its lit colour times these: the line under a top's outline,
    /// the one under that, and the line inside the outline on its other sides. A floor slope's diagonal is lit as a top.
    /// </summary>
    public const double RimTop = 1.28, RimUnder = 1.02, RimSide = 0.96;
}
