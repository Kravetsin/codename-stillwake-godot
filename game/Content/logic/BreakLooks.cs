using System;
using Stillwake.Game.Render;

namespace Stillwake.Game;

/// <summary>
/// How a wreck breaks apart (0075), by what it is: its picture cut into wedges that fly from its middle, their torn edges; what it
/// throws besides. Every wreck's metal glows and sparks; the ice belt's druse (docs/decisions/2026-11-15-stretches-of-space.md, the pick «Друза») shatters cold.
/// </summary>
/// <param name="Cuts">The angles its picture is cut at, from its middle, in radians: a wedge between each and the next.</param>
/// <param name="Speed">How fast a wedge flies out, pixels a second; <paramref name="Spin"/> the most it turns, radians a second either way.</param>
/// <param name="Hot">Whether its torn edges glow and cool, trailing embers; else they are <paramref name="Torn"/> at <paramref name="TornAlpha"/>.</param>
/// <param name="Glitter">How many glints of <see cref="HitLooks.Glitter"/> it scatters, floating; <paramref name="Sparks"/> whether sparks, dust and a cloud fly too.</param>
public sealed record BreakLook(float[] Cuts, (double From, double To) Speed, double Spin, bool Hot, (int R, int G, int B) Torn, float TornAlpha,
    int Glitter, bool Sparks, (int R, int G, int B) Glint, (int R, int G, int B) Flash);

public static class BreakLooks
{
    /// <summary>A wreck's metal: six wedges, their edges hot, sparks, dust and a cloud, a warm glint and flash.</summary>
    public static readonly BreakLook Wreck = new([.. Fan(6, 0.3f)], (22, 42), 3.5, Hot: true, Torn: (255, 240, 200), TornAlpha: 1,
        Glitter: 0, Sparks: true, Glint: (255, 214, 150), Flash: (255, 226, 180));

    /// <summary>The druse: eight long splinters flying faster, turning more, their edges a cold white; glitter and a cold glint, no sparks, dust or cloud.</summary>
    public static readonly BreakLook Ice = new([0, 0.7f, 1.5f, 2.3f, 3.0f, 3.8f, 4.6f, 5.4f], (35, 67), 5.6, Hot: false, Torn: (242, 248, 251), TornAlpha: 0.4f,
        Glitter: 12, Sparks: false, Glint: (214, 240, 255), Flash: (200, 228, 255));

    /// <summary>The glitter a breaking druse scatters: floating off in space as its pieces do.</summary>
    public static readonly Luminous Glitter = HitLooks.Glitter with { Gravity = 0 };

    /// <summary>How <paramref name="item"/> breaks: the druse its own way, every other wreck as metal.</summary>
    public static BreakLook Of(string item) => item == StretchLooks.IceBlock ? Ice : Wreck;

    private static float[] Fan(int count, float from)
    {
        var cuts = new float[count];
        for (int i = 0; i < count; i++) cuts[i] = i * MathF.Tau / count + from;
        return cuts;
    }
}
