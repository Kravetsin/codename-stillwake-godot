namespace Stillwake.Game;

/// <summary>
/// The knobs of a kill's feel (2026-10-29-kill-feel): the white hold, the burst into pixels and their heaps, and how loot on
/// the floor flies, lands and is pulled in. Placeholders for a play pass.
/// </summary>
public static class KillLooks
{
    /// <summary>How long a killed creature is held white before it bursts: the hit-stop, the world running on; two ticks.</summary>
    public const double PopMs = 100;

    /// <summary>The most pixels one creature bursts into, taken evenly from its picture; and the most flying or lying at once.</summary>
    public const int PopPixels = 600, MostPixels = 8000;

    /// <summary>A pixel's speed out from the body's middle, and away from the striker and up besides, in pixels a second: a draw between each pair.</summary>
    public const float OutLeast = 8, OutMost = 40, AwayLeast = 4, AwayMost = 30, UpLeast = 10, UpMost = 50;

    /// <summary>How many times a stack's fall a pixel falls by: heavier, so a burst heaps where the creature stood.</summary>
    public const float Weight = 4;

    /// <summary>How long a pixel lasts, a draw between the two, and the last seconds of it fading, in seconds.</summary>
    public const float LastsLeast = 2.2f, LastsMost = 4f, Fades = 0.8f;

    /// <summary>What a pixel keeps of its speed across bouncing off a wall.</summary>
    public const float WallKeeps = 0.3f;

    /// <summary>How many pixels of a landed column's height a pixel slides down to a lower neighbour, a step at a time, before it stays.</summary>
    public const int Slides = 4;

    /// <summary>How fast a stack on the floor spins in flight, in turns a second.</summary>
    public const float Spin = 2.5f;

    /// <summary>A stack landing: how long it squashes, how flat at most (share of its height), and its two hops' heights in pixels.</summary>
    public const double SquashMs = 140, Squash = 0.35, HopMs = 260;
    public const float HopFirst = 3, HopSecond = 1;

    /// <summary>The rare drops that glint lying on the floor, now and then, to be seen from afar.</summary>
    public static readonly System.Collections.Generic.IReadOnlySet<string> Glints = new System.Collections.Generic.HashSet<string> { "seed" };

    /// <summary>How often a rare drop on the floor glints, in seconds.</summary>
    public const double GlintEvery = 1.6;
}
