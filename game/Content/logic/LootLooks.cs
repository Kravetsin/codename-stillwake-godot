namespace Stillwake.Game;

/// <summary>
/// How loot lying on the floor looks (2026-11-16-loot-pieces): small real objects, tumbling as they fly and lying on a flat side
/// where they stop, a thin warm outline on those near the hero. Placeholders for a play pass.
/// </summary>
public static class LootLooks
{
    /// <summary>A stack's picture drawn at this share of its 16 px icon: an object about ten pixels, a fourth of the hero's height.</summary>
    public const float Size = 0.7f;

    /// <summary>How many objects a stack shows by what it holds: one; two from <see cref="Two"/>; three from <see cref="Three"/>.</summary>
    public const int Two = 2, Three = 10;

    /// <summary>How far apart the objects of one stack lie, in pixels.</summary>
    public const float Apart = 4.5f;

    /// <summary>How long a stack takes to tip from how it landed onto the side it lies on, in ms.</summary>
    public const double SettleMs = 180;

    /// <summary>
    /// A side shorter than this share of the picture's longest is too small to lie on: a stack picks among the longer ones, the
    /// one nearest how it landed.
    /// </summary>
    public const float LeastSide = 0.5f;

    /// <summary>Within this many cells of the hero's body a stack is outlined; how long the outline takes to come or go, in ms.</summary>
    public const int OutlineReach = 4;
    public const double OutlineMs = 200;

    /// <summary>The outline: how much wider than the picture (a share), its colour and its strength.</summary>
    public const float OutlineSize = 0.16f, OutlineR = 0.91f, OutlineG = 0.72f, OutlineB = 0.46f, OutlineA = 0.85f;
}
