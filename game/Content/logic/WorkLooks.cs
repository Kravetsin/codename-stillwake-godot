using System;
using Stillwake.Game.Render;

namespace Stillwake.Game;

/// <summary>
/// How a tool is seen at work (tool-contact, damage-looks), as the user picked it: a swung tool's downstroke trailing three pale
/// ink flicks ("flicks"); a running electric tool throwing a steady stream where it touches, hot sparks off a wreck ("sparks"),
/// what it touches looks hit off anything else, <see cref="ThrowsPerSecond"/> throws a second of as much in all as its hits
/// threw one by one; and the plasma cutter's jet, a flat violet sheath round a white heart ("beam"). Sizes in the world's pixels.
/// </summary>
public static class WorkLooks
{
    /// <summary>How many throws a second a tool touching anything but a wreck makes, every one a share of a whole hit (<see cref="ShareOf"/>).</summary>
    public const double ThrowsPerSecond = 30;

    /// <summary>
    /// The share of the axe's whole hit each of <paramref name="tool"/>'s throws is: its hits' each (their pace over the axe's) times
    /// its hits a second, spread over <see cref="ThrowsPerSecond"/>; none for no weapon.
    /// </summary>
    public static double ShareOf(string tool)
    {
        if (Content.Weapons.Of(tool) is not { } weapon) return 0;
        double each = Math.Min(1, (double)weapon.Pace / Content.Weapons.Axe.Pace), perSecond = (double)Sim.World.TicksPerSecond / weapon.Pace;
        return each * perSecond / ThrowsPerSecond;
    }

    /// <summary>The flicks reach back this many downstrokes' time and fade over this many after it ends; the edge is sampled this many times along.</summary>
    public const double FlickLength = 2.2, FlickFade = 3;
    public const int FlickSamples = 18;

    public static readonly Ink.Tone FlickInk = Ink.Tone.Of(176, 188, 206, 0.8f);

    /// <summary>Each flick: how far out along the edge (1 its tip), the share of the trail it runs, its width.</summary>
    public static readonly (double Along, double Share, double Width)[] Flicks = [(1.1, 1, 0.62), (0.8, 0.72, 0.45), (0.5, 0.45, 0.32)];

    /// <summary>
    /// Sparks a second off a wreck, each living <see cref="SparkLife"/> ms and flying <see cref="SparkSpeed"/> pixels a second at
    /// first, slowing by a third; its streak as long as <see cref="SparkStreak"/> seconds of its speed, <see cref="SparkWidth"/> wide.
    /// </summary>
    public const double SparksPerSecond = 150, SparkStreak = 0.016, SparkWidth = 0.32;
    public static readonly (double Least, double Most) SparkLife = (160, 420), SparkSpeed = (60, 150);

    /// <summary>How widely each tool's sparks fan, radians either way: the grinder's tight, off its wheel.</summary>
    public const double DrillSpread = 1.1, GrinderSpread = 0.3, CutterSpread = 1.3;

    /// <summary>The grinder's wheel throws back toward the tool by this much and down past it by this much.</summary>
    public const double WheelBack = 0.42, WheelDown = 0.91;

    /// <summary>White-hot for the first quarter of a spark's life, orange to six tenths, red after; the white bead where it touches.</summary>
    public static readonly Ink.Tone SparkHot = Ink.Tone.Of(255, 246, 220, 1), SparkWarm = Ink.Tone.Of(255, 181, 74, 1), SparkCool = Ink.Tone.Of(210, 74, 30, 1);
    public const double BeadRadius = 0.7, BeadMs = 40;

    /// <summary>
    /// The jet: its violet sheath and white heart, widths in pixels; its width flickering by <see cref="JetFlicker"/> either way
    /// <see cref="JetFlickerHz"/> times a second; at nothing it thins to a point over its last <see cref="JetTaper"/> pixels.
    /// </summary>
    public const double JetSheath = 1.5, JetHeart = 0.5, JetFlicker = 0.15, JetFlickerHz = 30, JetTaper = 10;

    /// <summary>The coils' violet, as the lit faces of the running cutter's picture; the jet's white-lilac heart.</summary>
    public static readonly Ink.Tone JetViolet = Ink.Tone.Of(172, 142, 238, 0.5f), JetWhite = Ink.Tone.Of(240, 234, 255, 0.95f);

    /// <summary>Where the jet cuts: a small hard splash, white in a violet ring, this wide, flickering every <see cref="SplashMs"/>.</summary>
    public const double SplashRadius = 1.3, SplashMs = 33, SplashFlicker = 0.25;
    public static readonly Ink.Tone SplashRing = Ink.Tone.Of(172, 142, 238, 0.35f);
}
