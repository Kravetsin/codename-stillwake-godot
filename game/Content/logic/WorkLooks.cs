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

    /// <summary>The air a tool works (tool-reach): pale cyan as the air round a hero (<c>AirSwirl</c>).</summary>
    public static readonly Ink.Tone AirInk = Ink.Tone.Of(200, 240, 255, 1);

    /// <summary>A running tool's zone: eased in over <see cref="ZoneIn"/> ms and out over <see cref="ZoneOut"/>.</summary>
    public const double ZoneIn = 120, ZoneOut = 220;

    /// <summary>
    /// The grinder's working circle round its hub (grinder-contact): as far as its fan reaches, <paramref name="range"/> pixels from
    /// the shoulder, past the hub <paramref name="hub"/> pixels from it; none under <see cref="WheelLeast"/>.
    /// </summary>
    public static double? WheelRadius(double range, double hub) => range - hub is var r && r >= WheelLeast ? r : null;

    /// <summary>
    /// The smallest working circle drawn, pixels; how many points its rim is taken in as a haze (spark-rule).
    /// </summary>
    public const double WheelLeast = 2;
    public const int RimSteps = 48;

    /// <summary>
    /// The one rule every tool's sparks and chips follow (spark-rule): what is hit throws from at most <see cref="ContactPoints"/>
    /// of the points where the tool's haze lies in it, a cell counted only within <see cref="ContactReach"/> px of where it was hit;
    /// a wreck a struck blade did not reach throws from the haze's point nearest it within <see cref="HazeFar"/> px; the cutter's jet
    /// is taken as a haze every <see cref="JetStep"/> px.
    /// </summary>
    public const int ContactPoints = 3;
    public const double ContactReach = 16, HazeFar = 48, JetStep = 1;

    /// <summary>
    /// How what is hit throws off a haze (spark-rule): along the haze, never into what was hit. A way that leads into it
    /// <see cref="GlanceProbe"/> px on glances off its surface, found by <see cref="GlanceRays"/> ways round the point at the
    /// nearest of <see cref="GlanceRadii"/> that reaches out of it.
    /// </summary>
    public const double GlanceProbe = 2;
    public const int GlanceRays = 16;
    public static readonly double[] GlanceRadii = [3, 6, 12];

    /// <summary>
    /// How widely what is thrown off a haze fans either way of it, radians, by its kind, whatever it is made of (spark-rule):
    /// streaks (sparks, splinters) a thin jet, chips wider, motes and wisps (dust) wider still.
    /// </summary>
    public static double HazeSpread(Glimmer kind) => kind switch
    {
        Glimmer.Streak => 0.21,
        Glimmer.Chip => 0.44,
        _ => 0.7,
    };

    /// <summary>
    /// The drill's haze (drill-contact), as the user picked it («Двойная спираль»): two strands from its chuck to
    /// its reach, at most this far either side of the axis, grown over the first <see cref="HelixGrow"/> of its length, a turn every
    /// <see cref="HelixPitch"/> pixels along it, turning <see cref="HelixTurns"/> times a second; its loop taken in
    /// <see cref="HelixSteps"/> points as a haze (spark-rule).
    /// </summary>
    public const double HelixRadius = 2.6, HelixGrow = 0.1, HelixPitch = 5.6, HelixTurns = 3;
    public const int HelixSteps = 96;

    /// <summary>
    /// The helix drawn in dry-brush strokes, each its width, its offset from the strand and its strength; drawn in dabs every
    /// <see cref="HelixDab"/> pixels, a dab in <see cref="HelixDry"/> skipped, a strand behind the axis at <see cref="HelixBehind"/>.
    /// </summary>
    public static readonly (double Width, double Offset, double Alpha)[] HelixStrokes = [(0.75, 0, 0.2), (0.32, 0.38, 0.16), (0.28, -0.42, 0.11)];
    public const double HelixDab = 0.32, HelixDry = 0.18, HelixBehind = 0.45;

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
