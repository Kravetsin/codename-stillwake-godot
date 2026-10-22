using System;

namespace Stillwake.Game;

/// <summary>
/// How the arc bench looks (0099), the user's "vise" (<c>tools/ship-art/arc-bench/paint.js</c>): a ship's bench, copper vise
/// jaws on it with a chip of a sky shard set in the fixed jaw. Its picture is four cells across and four tall, its footprint
/// the bottom two rows, the vise standing up out of it (<c>ShipTiles.RiseOf</c>). A tool on it is drawn as the hand
/// draws it, its modules on it (<see cref="HeldParts"/>), clamped where it says (<see cref="HeldLook.Clamp"/>), the jaws laid
/// over it; so any tool sits on it with no picture drawn for it. In pixels of its picture from its top-left.
/// </summary>
public static class BenchLook
{
    public const string Kind = "arcBench";

    /// <summary>
    /// Its pictures: the body; the shard's cracks alight, drawn unlit; the jaws, laid over the clamped tool; the shard's glow
    /// and its brighter flash as a module goes in, each a cell's margin round the picture.
    /// </summary>
    public const string Body = "machine_arcBench", Alight = "arcBench_alight", Front = "arcBench_front", Glow = "arcBench_glow", FitGlow = "arcBench_fit_glow";

    /// <summary>The picture's size, in pixels, and the glows' margin round it.</summary>
    public const int Width = 32, Height = 32, GlowMargin = 16;

    /// <summary>The middle of the jaws, where they close on a tool: a tool's <see cref="HeldLook.Clamp"/> is laid here.</summary>
    public static readonly (double X, double Y) Clamp = (17.5, 11);

    /// <summary>The bench's table top, its first row: a clamped tool lies over it.</summary>
    public const int TableTop = 17;

    /// <summary>
    /// How far a clamped tool may stick out: past either side of the picture (a long tool's bit or stock, as the drill's
    /// bit does by a pixel), and down over the table's front edge (the drill's loop of frame hangs two rows over it). Never
    /// above the picture.
    /// </summary>
    public const int Overhang = 4, Hang = 2;

    /// <summary>Where a tool of <paramref name="look"/> has its picture's top-left on the bench: its clamp on the jaws', whole pixels.</summary>
    public static (int X, int Y) ToolAt(HeldLook look)
    {
        var clamp = look.Clamp ?? throw new InvalidOperationException($"{look.Picture} declares no clamp");
        return ((int)Math.Round(Clamp.X - clamp.X), (int)Math.Round(Clamp.Y - clamp.Y));
    }

    /// <summary>Whether a tool picture <paramref name="width"/> by <paramref name="height"/>, its top-left at <paramref name="at"/>, lies where a clamped tool may.</summary>
    public static bool Fits((int X, int Y) at, int width, int height) =>
        at.X >= -Overhang && at.X + width <= Width + Overhang && at.Y >= 0 && at.Y + height <= TableTop + Hang;

    /// <summary>The shard's light (0096's violet-white): where it stands, how far it reaches, in cells, and how strong it is.</summary>
    public static readonly (float X, float Y) LightAt = (17.5f, 14);
    public static readonly (float R, float G, float B) Light = (0.74f, 0.62f, 1f);
    public const int Reach = 8;
    public const float Strength = 0.75f;

    /// <summary>The shard's arcs at rest: now and then a small one off its tip into the air.</summary>
    public static readonly Emitter Idle = new(Emits.Arc, 17, 13) { ToAir = true, Reach = 4, Rate = 0.015f, Life = 3, Width = 0.5f };

    /// <summary>Where the arcs leave the shard as a module goes in: its tip and the two ends of its cracks.</summary>
    public static readonly (float X, float Y)[] Sources = [(17, 13), (20, 14), (15, 14)];

    /// <summary>Where they strike on a tool with no mount: the drill's coil, as the page drew it.</summary>
    public static readonly (float X, float Y) Strikes = (7, 9);

    /// <summary>How long a fit's burst lasts, in ms, how often its arcs strike in it, and how bright its flash is: toned down, so the bench is still seen.</summary>
    public const double BurstMs = 650, BurstEveryMs = 90;
    public const float FlashStrength = 0.3f, FitGlowMost = 0.3f, BurstLight = 0.4f;

    /// <summary>How bright its glow and its alight cracks breathe at rest, at their least and most.</summary>
    public const float BreathLeast = 0.7f, BreathMost = 1f;
}
