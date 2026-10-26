using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>What a luminous particle is drawn as (0075).</summary>
public enum Glimmer
{
    /// <summary>A thin line of light along its own recent path, as long as its speed says, white-hot at the head.</summary>
    Streak,
    /// <summary>A fine soft dot: a mote of dust or ash.</summary>
    Mote,
    /// <summary>A soft translucent puff that swells, stretched along its way: smoke, vapour.</summary>
    Wisp,
    /// <summary>A tiny tumbling triangle of metal, catching the light as it turns.</summary>
    Chip,
}

/// <summary>
/// How one kind of luminous particle looks and moves (0075), in pixels of the world, milliseconds and
/// seconds. Its colour runs along <see cref="Ramp"/> over its life (0..255 a channel) and its brightness
/// falls as <c>(1 - age)^</c><see cref="Fade"/>, so a spark cools and dims together. <see cref="Spin"/>
/// turns its way by smooth noise (radians a second at most) and <see cref="Turbulence"/> pushes it along a
/// curl field (pixels a second squared), so paths curl without speeding up. Light (<see cref="Matter"/>
/// false) is added over the world and may light the room (<see cref="Light"/>); matter is lit by the room
/// and catches a little light of its own.
/// </summary>
public sealed record Luminous(Glimmer Kind, (int R, int G, int B)[] Ramp, (double From, double To) LifeMs)
{
    public double Bright { get; init; } = 1;
    public double Fade { get; init; } = 1.3;
    public double Gravity { get; init; }
    public double Drag { get; init; }
    public double Spin { get; init; }
    public double Turbulence { get; init; }
    /// <summary>How finely the noise that turns it varies, a period every 1/this pixels.</summary>
    public double NoiseScale { get; init; } = 0.08;
    /// <summary>A streak's width, in pixels of the world, before it is held to a screen pixel at least.</summary>
    public double Width { get; init; } = 0.3;
    /// <summary>A streak's length for its speed (seconds of travel), and its shortest and longest in pixels.</summary>
    public double Stretch { get; init; }
    public double Shortest { get; init; } = 0.5;
    public double Longest { get; init; } = 40;
    /// <summary>How white its head is, from its colour (0) to white (1).</summary>
    public double Hot { get; init; } = 0.55;
    /// <summary>The soft halo round its head, in pixels across; none at 0.</summary>
    public double Halo { get; init; }
    /// <summary>Whether it lights the room round it, a glowing particle's light (<see cref="Fx.ParticleLight"/>).</summary>
    public bool Light { get; init; }
    public bool Matter { get; init; }
    /// <summary>Dust along the floor, matter drawn behind the walls and over the back walls, so a wall hides it (0077): no light of its own.</summary>
    public bool Low { get; init; }
    public bool Flickers { get; init; }
    /// <summary>How much of its speed it keeps bouncing off a floor it was given.</summary>
    public double Bounce { get; init; } = 0.3;
    /// <summary>A mote's radius, a wisp's at its birth, in pixels; a wisp swells by <see cref="Grow"/> times.</summary>
    public double Size { get; init; } = 1.2;
    public double Grow { get; init; } = 2;
    /// <summary>A wisp's opacity at its fullest.</summary>
    public double Alpha { get; init; } = 0.12;
    /// <summary>The share of its life over which it brightens from nothing, as air drawn in out of thin air; 0 for born bright.</summary>
    public double Rise { get; init; }

    // The luminous effects, by what makes them (the review page's B variants, 0075).

    private static readonly (int, int, int)[] Spark = [(255, 252, 238), (255, 214, 120), (255, 140, 50), (170, 50, 20)];

    /// <summary>Rising from a fire at work on a curling path, flickering as it cools from white through amber to dull red.</summary>
    public static readonly Luminous Ember = new(Glimmer.Streak, [(255, 250, 228), (255, 200, 96), (255, 120, 40), (150, 38, 18)], (1100, 2300))
    { Bright = 0.95, Fade = 1.2, Gravity = -8, Drag = 0.35, Spin = 3.2, NoiseScale = 0.07, Stretch = 0.2, Shortest = 1.4, Halo = 3.5, Light = true, Flickers = true };
    /// <summary>Thrown out fast by a cracking coal, arcing and falling, bouncing off the floor.</summary>
    public static readonly Luminous CrackSpark = new(Glimmer.Streak, [(255, 252, 235), (255, 206, 110), (255, 128, 40), (140, 40, 20)], (450, 900))
    { Fade = 1.1, Gravity = 120, Drag = 0.6, Spin = 2, Stretch = 0.05, Shortest = 0.8, Halo = 4, Light = true, Bounce = 0.25 };
    /// <summary>Ash drifting up off a fire.</summary>
    public static readonly Luminous Ash = new(Glimmer.Mote, [(255, 190, 120), (200, 90, 50)], (1500, 2600))
    { Bright = 0.5, Drag = 0.2, Turbulence = 14, Size = 0.6 };
    /// <summary>Off a screwdriver at work, in a fan, arcing down under gravity aboard and bouncing once.</summary>
    public static readonly Luminous BuildSpark = new(Glimmer.Streak, Spark, (300, 750))
    { Fade = 1.1, Gravity = 260, Drag = 1, Spin = 1.6, NoiseScale = 0.1, Stretch = 0.05, Shortest = 0.6, Width = 0.28, Halo = 3.5, Light = true, Bounce = 0.35 };
    /// <summary>The same out in space: nothing pulls them down, nothing to bounce off.</summary>
    public static readonly Luminous BuildSparkInSpace = BuildSpark with { Gravity = 0 };
    /// <summary>Rising off the line where a plan turns real, in the planner's sky blue.</summary>
    public static readonly Luminous EdgeMote = new(Glimmer.Mote, [(200, 246, 255), (120, 200, 255)], (700, 1300))
    { Bright = 0.7, Drag = 0.5, Turbulence = 18, Size = 0.5 };
    /// <summary>Thrown off the edge of a plan as it is finished.</summary>
    public static readonly Luminous FinishMote = new(Glimmer.Mote, [(220, 250, 255), (120, 200, 255)], (600, 1200))
    { Bright = 0.85, Drag = 1.2, Turbulence = 20, Size = 0.55 };
    /// <summary>Faint smoke off the hot spot a screwdriver works, its opacity times its heat.</summary>
    public static readonly Luminous WeldSmoke = new(Glimmer.Wisp, [(150, 150, 158)], (900, 1500))
    { Matter = true, Drag = 0.6, Turbulence = 30, Spin = 2, Size = 1.2, Grow = 3, Alpha = 0.07 };
    /// <summary>Sprayed back toward the hero from where an axe bites a wreck.</summary>
    public static readonly Luminous HitSpark = new(Glimmer.Streak, [(255, 252, 238), (255, 220, 140), (255, 150, 60), (160, 60, 30)], (250, 650))
    { Fade = 1.1, Drag = 1.8, Spin = 2.6, NoiseScale = 0.1, Stretch = 0.045, Shortest = 0.6, Width = 0.28, Halo = 3.5, Light = true };
    /// <summary>Slow metal dust knocked off a wreck by a hit.</summary>
    public static readonly Luminous HitDust = new(Glimmer.Mote, [(200, 206, 216)], (1000, 2000))
    { Matter = true, Bright = 0.8, Fade = 1, Drag = 0.8, Turbulence = 18, Size = 0.55 };
    /// <summary>A dark chip of the hull tumbling off a hit, glinting as it turns.</summary>
    public static readonly Luminous Chip = new(Glimmer.Chip, [(106, 115, 132)], (1100, 1250)) { Matter = true, Fade = 0 };
    /// <summary>Bursting every way from a wreck breaking apart, curling as they slow.</summary>
    public static readonly Luminous BreakSpark = new(Glimmer.Streak, Spark, (400, 1000))
    { Fade = 1.15, Drag = 1.4, Spin = 2.8, Stretch = 0.05, Halo = 4, Light = true };
    /// <summary>Fine dust spreading slowly from a broken wreck, lit by its flash.</summary>
    public static readonly Luminous BreakDust = new(Glimmer.Mote, [(206, 210, 220)], (1500, 2800))
    { Matter = true, Fade = 0.9, Drag = 0.7, Turbulence = 24, NoiseScale = 0.1, Size = 0.55 };
    /// <summary>A slow translucent cloud swelling and curling where a wreck broke.</summary>
    public static readonly Luminous Vapour = new(Glimmer.Wisp, [(190, 196, 210)], (1800, 2800))
    { Matter = true, Drag = 0.6, Turbulence = 22, Spin = 1.2, NoiseScale = 0.06, Size = 3.7, Grow = 3.2, Alpha = 0.12 };
    /// <summary>Trailing a piece of a broken wreck for its first half second.</summary>
    public static readonly Luminous ShardEmber = new(Glimmer.Streak, Spark, (300, 600))
    { Bright = 0.8, Drag = 1, Spin = 3, Stretch = 0.08, Width = 0.24, Halo = 2.5, Light = true };

    // Slice 2 (0075): the hero, the harpoon, the air, the drones, the doors, the lamps.

    private static readonly (int, int, int)[] Dust = [(214, 218, 228), (196, 200, 210)];

    /// <summary>Hot thrust from a jetpack's nozzle: short streaks, white to blue, curling as they slow. Its light is the plume's own.</summary>
    public static readonly Luminous Thrust = new(Glimmer.Streak, [(235, 248, 255), (120, 190, 255), (50, 90, 230)], (160, 340))
    { Bright = 0.9, Fade = 1.4, Drag = 4.5, Spin = 5, NoiseScale = 0.12, Stretch = 0.05, Shortest = 0.6, Width = 0.3, Halo = 3 };
    /// <summary>Blue sparkle swirling off a jetpack's plume.</summary>
    public static readonly Luminous ThrustMote = new(Glimmer.Mote, [(180, 220, 255), (80, 120, 255)], (350, 800))
    { Bright = 0.6, Drag = 2, Turbulence = 70, NoiseScale = 0.1, Size = 0.55 };
    /// <summary>Fine dust a running boot lifts, settling on the floor it was given.</summary>
    public static readonly Luminous BootDust = new(Glimmer.Mote, Dust, (600, 1300))
    { Matter = true, Low = true, Fade = 1, Gravity = 12, Drag = 2.4, Turbulence = 22, NoiseScale = 0.12, Bounce = 0 };
    /// <summary>A thin veil of dust behind a step, drifting back and settling.</summary>
    public static readonly Luminous DustVeil = new(Glimmer.Wisp, [(206, 210, 220)], (700, 1000))
    { Matter = true, Low = true, Drag = 1.6, Turbulence = 30, Size = 1.6, Grow = 2.6, Alpha = 0.2 };
    /// <summary>Grains a landing throws skittering along the floor, smeared by their speed, slowing into motes.</summary>
    public static readonly Luminous DustGrain = new(Glimmer.Streak, Dust, (380, 800))
    { Matter = true, Low = true, Bright = 0.85, Fade = 0.9, Gravity = 60, Drag = 3.2, Spin = 1.5, Stretch = 0.045, Shortest = 0.4, Width = 0.32, Hot = 0.2, Bounce = 0.2 };
    /// <summary>The motes a landing lifts, slower and longer than a step's.</summary>
    public static readonly Luminous LandDust = BootDust with { LifeMs = (900, 1700), Gravity = 8, Drag = 1.8, Turbulence = 26 };
    /// <summary>Veils rolling outward from a landing and curling.</summary>
    public static readonly Luminous LandVeil = new(Glimmer.Wisp, [(210, 214, 224)], (900, 1500))
    { Matter = true, Low = true, Drag = 1.5, Turbulence = 40, Spin = 1.5, NoiseScale = 0.07, Size = 2, Grow = 3.2, Alpha = 0.2 };
    /// <summary>A heavy landing's shock along the floor (0090): pale streaks of light racing out under its ring, gone in a moment.</summary>
    public static readonly Luminous LandStreak = new(Glimmer.Streak, [(240, 244, 250), (200, 208, 222)], (200, 240))
    { Bright = 0.55, Fade = 1.2, Drag = 7, Stretch = 0.05, Shortest = 1, Width = 0.24, Hot = 0.4 };
    /// <summary>
    /// Off the gloves sliding down a ladder's rails (0092): tiny hot streaks flicked up and out, arcing down and cooling in a
    /// blink; each lights its spot a little.
    /// </summary>
    public static readonly Luminous RailSpark = new(Glimmer.Streak, [(255, 240, 190), (255, 186, 90), (225, 100, 40)], (140, 300))
    { Bright = 0.75, Fade = 1.2, Gravity = 260, Drag = 1.2, Spin = 2, Stretch = 0.045, Shortest = 0.6, Longest = 4, Width = 0.22, Halo = 2.5, Light = true };
    /// <summary>Cold gas jetting from the harpoon's muzzle as it fires: fine pale streaks breaking into curls.</summary>
    public static readonly Luminous Gas = new(Glimmer.Streak, [(236, 244, 255), (190, 206, 230), (140, 156, 180)], (150, 320))
    { Bright = 0.65, Drag = 6, Spin = 4, Stretch = 0.05, Width = 0.26 };
    /// <summary>A shot's vapour, drifting and thinning out.</summary>
    public static readonly Luminous ShotVapour = new(Glimmer.Wisp, [(210, 216, 228)], (500, 800))
    { Matter = true, Drag = 2, Turbulence = 30, Size = 1.4, Grow = 2.6, Alpha = 0.12 };
    /// <summary>Left in the flying rod's wake.</summary>
    public static readonly Luminous Wake = new(Glimmer.Mote, [(255, 236, 200), (200, 180, 150)], (300, 700))
    { Bright = 0.55, Drag = 1.5, Turbulence = 30, Size = 0.45 };
    /// <summary>A short spray off the rod biting metal.</summary>
    public static readonly Luminous BiteSpark = new(Glimmer.Streak, [(255, 250, 230), (255, 210, 130), (230, 120, 50)], (200, 450))
    { Drag = 2, Spin = 3, Stretch = 0.045, Width = 0.26, Halo = 3, Light = true };
    /// <summary>Air jetting out of a breach into space, fast and pale.</summary>
    public static readonly Luminous Jet = new(Glimmer.Streak, [(236, 246, 255), (180, 214, 245)], (280, 560))
    { Bright = 0.55, Drag = 0.8, Spin = 3, Stretch = 0.06, Width = 0.24 };
    /// <summary>Condensation curling and thinning out of a breach into space.</summary>
    public static readonly Luminous Condensation = new(Glimmer.Wisp, [(232, 240, 250)], (900, 1600))
    { Matter = true, Drag = 1.1, Turbulence = 45, Spin = 1.5, NoiseScale = 0.07, Size = 1.8, Grow = 4, Alpha = 0.13 };
    /// <summary>Fresh air breathed out of a vent filling its room, a faint veil drifting up and settling.</summary>
    public static readonly Luminous FreshAir = new(Glimmer.Wisp, [(214, 236, 255)], (1600, 2600))
    { Matter = true, Drag = 1.6, Turbulence = 14, Spin = 0.8, NoiseScale = 0.06, Size = 1.6, Grow = 2.6, Alpha = 0.07 };
    /// <summary>Dust crept sideways where a drone's wash meets the floor, or where it sets down.</summary>
    public static readonly Luminous WashDust = new(Glimmer.Streak, [(214, 218, 228)], (300, 600))
    { Matter = true, Bright = 0.8, Fade = 1.3, Gravity = 4, Drag = 2.6, Turbulence = 14, Stretch = 0.04, Shortest = 0.4, Width = 0.3, Bounce = 0 };
    /// <summary>A drone's wash rolling a faint veil out along the floor, gone as fast as it came.</summary>
    public static readonly Luminous WashVeil = DustVeil with { LifeMs = (400, 650), Drag = 2.2 };
    /// <summary>Shaken loose from a door's slot as it opens, drifting down.</summary>
    public static readonly Luminous SlotDust = new(Glimmer.Mote, [(206, 210, 220)], (1200, 2200))
    { Matter = true, Bright = 0.75, Fade = 0.8, Gravity = 9, Drag = 1.2, Turbulence = 14, NoiseScale = 0.12, Bounce = 0 };
    /// <summary>Knocked out along the floor as a door shuts.</summary>
    public static readonly Luminous SealDust = SlotDust with { LifeMs = (700, 1300), Bright = 0.7, Fade = 1, Gravity = 10, Drag = 2.4, Turbulence = 18, NoiseScale = 0.08 };
    /// <summary>A pressure door's hiss as it shuts: vapour from its seal.</summary>
    public static readonly Luminous SealHiss = new(Glimmer.Wisp, [(226, 234, 246)], (600, 1100))
    { Matter = true, Drag = 2.2, Turbulence = 34, Spin = 1.5, Size = 1.2, Grow = 3.2, Alpha = 0.14 };
    /// <summary>Dropping from a lamp short of power as it catches, arcing down and bouncing once.</summary>
    public static readonly Luminous LampSpark = new(Glimmer.Streak, Spark, (300, 700))
    { Fade = 1.1, Gravity = 240, Drag = 1, Spin = 2, Stretch = 0.045, Shortest = 0.5, Width = 0.24, Halo = 3, Light = true, Bounce = 0.3 };

    // Slice 3 (0075): the arc drill's coil, as fine as the furnace's embers.

    /// <summary>
    /// Leaping a short way off the arc drill's coil: a tiny streak of white-blue light jittering as it goes, stopped within
    /// a few pixels, gone in a blink. No light of its own: the coil's pulse lights the room.
    /// </summary>
    public static readonly Luminous ArcSpark = new(Glimmer.Streak, [(240, 252, 255), (150, 215, 255), (70, 130, 255)], (110, 240))
    { Fade = 1.3, Drag = 6, Spin = 9, NoiseScale = 0.35, Stretch = 0.05, Shortest = 0.7, Longest = 4, Width = 0.22, Hot = 0.8, Halo = 1.6 };
    /// <summary>A tiny hot fleck drifting up off an electric smelter's element, curling and cooling from white to red.</summary>
    public static readonly Luminous HeatSpark = new(Glimmer.Streak, [(255, 246, 220), (255, 180, 90), (230, 90, 40)], (450, 950))
    { Bright = 0.85, Fade = 1.2, Gravity = -10, Drag = 1.6, Spin = 3.5, NoiseScale = 0.1, Stretch = 0.12, Shortest = 0.8, Longest = 3, Width = 0.2, Halo = 2 };

    // Air coming back (0081): a canister's air rising round the body, steered up its helix by the crew's AirSwirl; a breath of room air at the visor.

    private static readonly (int, int, int)[] Air = [(236, 252, 255), (170, 232, 252), (110, 196, 240)];

    /// <summary>A fine streak of a canister's air, brightening out of nothing at the feet and fading out above the head, stretched by its speed round the body.</summary>
    public static readonly Luminous VortexStreak = new(Glimmer.Streak, Air, (650, 900))
    { Bright = 1, Fade = 0.6, Rise = 0.12, Stretch = 0.05, Shortest = 0.8, Longest = 12, Width = 0.2, Hot = 0.65 };
    /// <summary>A fine mote of it, carried up the same way.</summary>
    public static readonly Luminous VortexMote = new(Glimmer.Mote, Air, (700, 1000)) { Bright = 0.9, Fade = 0.7, Rise = 0.15, Size = 0.4 };
    /// <summary>A breath of room air at the visor: a pale mote drifting off and fading.</summary>
    public static readonly Luminous VisorMote = new(Glimmer.Mote, [(220, 246, 255), (150, 214, 245)], (900, 1400))
    { Bright = 0.85, Fade = 1.1, Rise = 0.12, Drag = 1.2, Turbulence = 10, Size = 0.55 };
    // The tick (the sentry page's candidate 3): its amber burner, its fine sparks, its snip.

    /// <summary>A tick's burner: short amber streaks, white at the nozzle, curling and flickering as they slow.</summary>
    public static readonly Luminous Burner = new(Glimmer.Streak, [(255, 250, 228), (255, 200, 96), (255, 120, 40), (150, 38, 18)], (180, 380))
    { Bright = 0.95, Fade = 1.3, Drag = 4, Spin = 4, NoiseScale = 0.1, Stretch = 0.05, Shortest = 0.6, Width = 0.3, Halo = 3.2, Flickers = true };
    /// <summary>Warm motes swirling off its plume.</summary>
    public static readonly Luminous BurnerMote = new(Glimmer.Mote, [(255, 190, 120), (200, 90, 50)], (500, 1000))
    { Bright = 0.5, Drag = 1.2, Turbulence = 30, NoiseScale = 0.1, Size = 0.55 };
    /// <summary>Grey smoke left along its flight, thin and swelling.</summary>
    public static readonly Luminous BurnerSmoke = new(Glimmer.Wisp, [(118, 116, 122)], (900, 1500))
    { Matter = true, Drag = 1.4, Turbulence = 30, NoiseScale = 0.08, Size = 1.3, Grow = 3, Alpha = 0.11 };
    /// <summary>Struck: fine short sparks, quick to cool.</summary>
    public static readonly Luminous FineSpark = HitSpark with { LifeMs = (140, 340), Width = 0.22, Halo = 2.4, Longest = 6, Drag = 2 };
    /// <summary>Spitting from its cracks as it dies.</summary>
    public static readonly Luminous CrackSpit = HitSpark with { LifeMs = (120, 260) };
    /// <summary>A line snipped: a few white streaks flicked off the claw.</summary>
    public static readonly Luminous Snip = new(Glimmer.Streak, [(255, 255, 255), (255, 236, 220)], (70, 140))
    { Bright = 0.9, Fade = 1, Drag = 3, Stretch = 0.03, Shortest = 1, Longest = 9, Width = 0.3, Halo = 2, Hot = 0.8 };

    /// <summary>Rammed (0103): a jet of the suit's air knocked out of it, a quick pale streak slowing to nothing.</summary>
    public static readonly Luminous SuitLeak = new(Glimmer.Streak, Air, (260, 480))
    { Bright = 0.9, Fade = 1.2, Drag = 5, Turbulence = 14, Stretch = 0.05, Shortest = 0.6, Longest = 9, Width = 0.22, Hot = 0.5 };
}

/// <summary>One luminous particle in flight.</summary>
public sealed class Glim
{
    /// <summary>How many points of its path a streak keeps: a sample each 60 Hz frame, its longest trail.</summary>
    public const int Kept = 24;
    /// <summary>How often a streak's path is sampled, in ms: as the page drew it, whatever the frame rate.</summary>
    public const double SampleMs = 1000.0 / 60;

    public double X, Y, Vx, Vy, AgeMs, Turn, TurnSpeed;
    public required double LifeMs { get; init; }
    public required Luminous Look { get; init; }
    /// <summary>Its own offset into the noise, so no two curl alike.</summary>
    public required double Seed { get; init; }
    /// <summary>Its brightness, or a wisp's opacity, as a share of its look's.</summary>
    public double Bright { get; init; } = 1;
    /// <summary>The floor it bounces off, a row of pixels; none to fall through.</summary>
    public double? Floor { get; init; }
    /// <summary>A mote's radius or a wisp's at birth, in pixels.</summary>
    public required double Size { get; init; }

    // The path, newest first from History[Head], Count of them; the time since the last sample.
    internal readonly double[] Hx = new double[Kept], Hy = new double[Kept];
    internal int Head, Count;
    internal double SinceMs;

    /// <summary>How far through its life it is, 0 to 1.</summary>
    public double Age => AgeMs / LifeMs;

    /// <summary>The <paramref name="i"/>-th point of its path back from where it is now.</summary>
    public (double X, double Y) Behind(int i) => (Hx[(Head + i) % Kept], Hy[(Head + i) % Kept]);

    internal void Sample()
    {
        Head = (Head + Kept - 1) % Kept;
        Hx[Head] = X;
        Hy[Head] = Y;
        Count = Math.Min(Kept, Count + 1);
        SinceMs = 0;
    }
}

/// <summary>
/// The luminous particles (0075): streaks, motes, wisps and chips, emitted by the features, moved and
/// aged each frame by the effects layer, which draws them. Drawn only, never part of the world: each peer
/// throws its own from what it sees. <paramref name="random"/> draws from 0 to 1; tests pass their own.
/// </summary>
public sealed class Glims(int limit, Func<double>? random = null)
{
    /// <summary>At most this many fly at once: a big break thins out rather than costing frames.</summary>
    public const int Limit = 1600;

    /// <summary>The game's, one set: features emit into it, the effects layer moves and draws it, the light lights it.</summary>
    public static Glims Shared { get; } = new(Limit);

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly List<Glim> _live = [];
    private double _seconds;

    public IReadOnlyList<Glim> Live => _live;

    /// <summary>A number from <paramref name="from"/> to <paramref name="to"/>, drawn from this set's own random.</summary>
    public double Between(double from, double to) => from + (to - from) * _random();

    /// <summary>
    /// One <paramref name="look"/> at (<paramref name="x"/>, <paramref name="y"/>) flying at (<paramref name="vx"/>,
    /// <paramref name="vy"/>) pixels a second; null past the limit.
    /// </summary>
    public Glim? Add(Luminous look, double x, double y, double vx, double vy, double? floor = null, double bright = 1, double? size = null)
    {
        if (_live.Count >= limit) return null;
        var g = new Glim
        {
            X = x,
            Y = y,
            Vx = vx,
            Vy = vy,
            Look = look,
            LifeMs = Between(look.LifeMs.From, look.LifeMs.To),
            Seed = _random() * 50,
            Bright = bright,
            Floor = floor,
            Size = size ?? look.Size,
        };
        if (look.Kind == Glimmer.Chip) (g.Turn, g.TurnSpeed) = (_random() * Math.Tau, Between(-9, 9));
        g.Sample();
        _live.Add(g);
        return g;
    }

    /// <summary>
    /// <paramref name="count"/> of <paramref name="look"/> from one point, each at <paramref name="heading"/> turned
    /// by a share of <paramref name="turn"/> and at a speed in <paramref name="speed"/>.
    /// </summary>
    public void Spray(Luminous look, double x, double y, int count, double heading, (double From, double To) turn,
        (double From, double To) speed, double? floor = null, double bright = 1)
    {
        for (int i = 0; i < count; i++)
        {
            double a = heading + Between(turn.From, turn.To), s = Between(speed.From, speed.To);
            Add(look, x, y, Math.Cos(a) * s, Math.Sin(a) * s, floor, bright);
        }
    }

    /// <summary>Moves every particle on by <paramref name="elapsedMs"/> and lets go of those past their life.</summary>
    public void Step(double elapsedMs)
    {
        _seconds += elapsedMs / 1000;
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var g = _live[i];
            g.AgeMs += elapsedMs;
            if (g.AgeMs >= g.LifeMs)
            {
                _live[i] = _live[^1];
                _live.RemoveAt(_live.Count - 1);
                continue;
            }
            LuminousMath.Move(g, elapsedMs / 1000, _seconds);
            g.SinceMs += elapsedMs;
            if (g.Look.Kind == Glimmer.Streak && g.SinceMs >= Glim.SampleMs) g.Sample();
        }
    }

    public void Clear() => _live.Clear();
}

/// <summary>The luminous particles' arithmetic, drawn nowhere: colour ramps, brightness, noise and curl, paths.</summary>
public static class LuminousMath
{
    /// <summary>Its colour <paramref name="q"/> of the way along <paramref name="ramp"/>, 0..255 a channel.</summary>
    public static (double R, double G, double B) RampAt((int R, int G, int B)[] ramp, double q)
    {
        if (ramp.Length == 0) return (255, 255, 255);
        if (ramp.Length == 1 || q <= 0) return ramp[0];
        double x = Math.Clamp(q, 0, 0.9999) * (ramp.Length - 1);
        int i = (int)Math.Floor(x);
        double t = x - i;
        var (a, b) = (ramp[i], ramp[i + 1]);
        return (a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    }

    /// <summary>How bright <paramref name="g"/> is now, 0 to its look's: fading by its look's power over its life, an ember flickering.</summary>
    public static double Brightness(Glim g)
    {
        double b = g.Look.Bright * g.Bright * Math.Pow(Math.Max(0, 1 - g.Age), g.Look.Fade);
        if (g.Look.Flickers) b *= 0.72 + 0.28 * Math.Sin(g.AgeMs / 41 + g.Seed * 10);
        if (g.Look.Rise > 0) b *= Math.Min(1, g.Age / g.Look.Rise);
        return b;
    }

    /// <summary>A hash of a lattice point, 0 to 1 (the page's <c>hash2</c>, its integer wrapping kept).</summary>
    public static double Hash(int i, int j)
    {
        int h = unchecked(i * 374761393 + j * 668265263);
        h = unchecked((h ^ (int)((uint)h >> 13)) * 1274126177);
        return (uint)(h ^ (int)((uint)h >> 16)) / 4294967296.0;
    }

    /// <summary>Smooth value noise, 0 to 1.</summary>
    public static double Noise(double x, double y)
    {
        int i = (int)Math.Floor(x), j = (int)Math.Floor(y);
        double fx = x - i, fy = y - j, u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
        double a = Hash(i, j) + (Hash(i + 1, j) - Hash(i, j)) * u;
        double b = Hash(i, j + 1) + (Hash(i + 1, j + 1) - Hash(i, j + 1)) * u;
        return a + (b - a) * v;
    }

    /// <summary>The noise's gradient turned a quarter: a field that swirls and never gathers, drifting with <paramref name="t"/> seconds.</summary>
    public static (double X, double Y) Curl(double x, double y, double t)
    {
        const double e = 0.35;
        double X = x + t * 0.31, Y = y - t * 0.23;
        return ((Noise(X, Y + e) - Noise(X, Y - e)) / (2 * e), -(Noise(X + e, Y) - Noise(X - e, Y)) / (2 * e));
    }

    /// <summary>
    /// <paramref name="g"/> moved on <paramref name="dt"/> seconds, <paramref name="t"/> seconds into the effects' clock:
    /// its way turned by noise, pushed along the curl, dragged, pulled down, bounced off its floor; a chip turned.
    /// </summary>
    public static void Move(Glim g, double dt, double t)
    {
        var look = g.Look;
        if (look.Spin != 0)
        {
            double n = (Noise(g.X * look.NoiseScale + g.Seed, g.Y * look.NoiseScale + t * 0.6) - 0.5) * 2;
            double a = n * look.Spin * Fx.Turbulence * dt, c = Math.Cos(a), s = Math.Sin(a);
            (g.Vx, g.Vy) = (g.Vx * c - g.Vy * s, g.Vx * s + g.Vy * c);
        }
        if (look.Turbulence != 0)
        {
            var (cx, cy) = Curl(g.X * look.NoiseScale + g.Seed, g.Y * look.NoiseScale, t);
            g.Vx += cx * look.Turbulence * Fx.Turbulence * dt;
            g.Vy += cy * look.Turbulence * Fx.Turbulence * dt;
        }
        double keep = Math.Max(0, 1 - look.Drag * dt);
        g.Vx *= keep;
        g.Vy = g.Vy * keep + look.Gravity * dt;
        g.X += g.Vx * dt;
        g.Y += g.Vy * dt;
        g.Turn += g.TurnSpeed * dt;
        if (g.Floor is double floor && g.Y > floor)
        {
            g.Y = floor;
            g.Vy *= -look.Bounce;
            g.Vx *= 0.7;
        }
    }

    /// <summary>How long <paramref name="g"/>, a streak, is drawn: its speed times its stretch, between its shortest and longest.</summary>
    public static double LengthOf(Glim g) =>
        Math.Clamp(Math.Sqrt(g.Vx * g.Vx + g.Vy * g.Vy) * g.Look.Stretch * Fx.Length, g.Look.Shortest, g.Look.Longest);

    /// <summary>
    /// The points of a streak's stroke into <paramref name="xs"/> and <paramref name="ys"/>, head first, along its own
    /// path back <paramref name="length"/> pixels at most; a streak younger than its trail is drawn on back along its
    /// way a little, so it is never a dot. Answers how many.
    /// </summary>
    public static int StreakPoints(Glim g, double length, Span<double> xs, Span<double> ys)
    {
        int n = 0;
        xs[n] = g.X;
        ys[n++] = g.Y;
        double acc = 0, px = g.X, py = g.Y;
        for (int i = 0; i < g.Count && acc < length && n < xs.Length; i++)
        {
            var (hx, hy) = g.Behind(i);
            double dx = hx - px, dy = hy - py, d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 1e-9) continue;
            if (acc + d >= length)
            {
                double r = (length - acc) / d;
                xs[n] = px + dx * r;
                ys[n++] = py + dy * r;
                acc = length;
                break;
            }
            acc += d;
            xs[n] = px = hx;
            ys[n++] = py = hy;
        }
        if (acc < length * 0.5 && n < xs.Length)
        {
            double sp = Math.Sqrt(g.Vx * g.Vx + g.Vy * g.Vy);
            if (sp > 1e-9)
            {
                double m = Math.Min(length - acc, 1.2);
                xs[n] = xs[n - 1] - g.Vx / sp * m;
                ys[n] = ys[n - 1] - g.Vy / sp * m;
                n++;
            }
        }
        return n;
    }
}
