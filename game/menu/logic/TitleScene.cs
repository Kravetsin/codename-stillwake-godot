using System;

namespace Stillwake.Game.Menu;

/// <summary>Something drifting past the title's plate, an astronaut or a shard of metal: its way across, its turn, how far off it is.</summary>
/// <param name="FromX">Where it comes in, in the scene's pixels, off the view's right.</param>
/// <param name="ToX">Where it goes out, off its left.</param>
/// <param name="FromY">Its height coming in.</param>
/// <param name="ToY">Its height going out.</param>
/// <param name="Seconds">How long it takes to cross.</param>
/// <param name="Spin">How fast it turns over, in radians a second, either way.</param>
/// <param name="Depth">How far off it is, 0 to 1: nearer is bigger, quicker and less faded into the dark.</param>
public readonly record struct Drifter(double FromX, double ToX, double FromY, double ToY, double Seconds, double Spin, double Depth)
{
    /// <summary>Where it is <paramref name="seconds"/> after coming in, and how far it has turned; null once it has crossed.</summary>
    public (double X, double Y, double Turn)? At(double seconds)
    {
        if (seconds < 0 || seconds > Seconds) return null;
        double t = seconds / Seconds;
        return (FromX + (ToX - FromX) * t, FromY + (ToY - FromY) * t, Spin * seconds);
    }
}

/// <summary>
/// The scene behind the title (2026-10-25-title-scene), in its own pixels, <see cref="Wide"/> by <see cref="High"/>: the
/// plate drifting right through the dark with its hero, so all else comes at it from the right and goes by to the left —
/// the stars, shards of metal, now and then an astronaut gone limp — always behind the plate. Plain arithmetic.
/// </summary>
public static class TitleScene
{
    /// <summary>The scene's size in its pixels, each drawn <see cref="Scale"/> screen pixels square on a 1280 × 720 screen.</summary>
    public const int Wide = 640, High = 360, Scale = 2;

    /// <summary>Where the plate's middle hangs, in the scene's pixels: right of the middle, the menu's side left clear.</summary>
    public const double PlateX = 420, PlateY = 200;

    /// <summary>How far the plate turns about its middle, in radians, and how long a turn there and back takes, in seconds; how far it bobs.</summary>
    public const double Tilt = 0.06, TiltSeconds = 53, Bob = 3, BobSeconds = 19;

    /// <summary>The seconds between one astronaut and the next, at least and at most; the first comes sooner.</summary>
    public const double DrifterGapLeast = 18, DrifterGapMost = 40, FirstDrifter = 5;

    /// <summary>The seconds between one shard and the next, at least and at most.</summary>
    public const double ShardGapLeast = 1.2, ShardGapMost = 4;

    /// <summary>
    /// How far the hero's head hangs, how far at most it lifts to an astronaut going by, in radians down; how much a sigh lifts
    /// it; how slowly it moves, in seconds.
    /// </summary>
    public const double HeadHangs = 0.8, HeadLifts = 0.05, HeadSighs = 0.25, HeadSeconds = 2.2;

    /// <summary>How much larger than the scene's pixels the plate and its hero are drawn: they are what the scene is about.</summary>
    public const float PlateScale = 1.5f;


    private const double Margin = 40;

    /// <summary>How the plate is turned and how far it has bobbed, <paramref name="seconds"/> into the title.</summary>
    public static (double Turn, double Dy) Sway(double seconds) =>
        (Tilt * Math.Sin(seconds / TiltSeconds * 2 * Math.PI), Bob * Math.Sin(seconds / BobSeconds * 2 * Math.PI));

    /// <summary>The next astronaut: across the whole view, slowly, above or below the plate, limp and far.</summary>
    public static Drifter NextDrifter(Random rng)
    {
        double depth = 0.3 + 0.5 * rng.NextDouble();
        double y = rng.NextDouble() < 0.5 ? High * (0.12 + 0.2 * rng.NextDouble()) : High * (0.7 + 0.18 * rng.NextDouble());
        return Across(rng, y, depth, (70 + 40 * rng.NextDouble()) / (0.4 + 0.6 * depth), 0.04 + 0.1 * rng.NextDouble());
    }

    /// <summary>The next shard of metal: at any height, near or far, quicker the nearer.</summary>
    public static Drifter NextShard(Random rng) =>
        Across(rng, High * rng.NextDouble(), rng.NextDouble(), 0, 0.1 + 0.5 * rng.NextDouble());

    private static Drifter Across(Random rng, double y, double depth, double seconds, double spin)
    {
        if (seconds <= 0) seconds = (40 + 30 * rng.NextDouble()) / (0.3 + 0.7 * depth);
        return new Drifter(Wide + Margin, -Margin, y, y + (rng.NextDouble() - 0.5) * High * 0.15, seconds,
            (rng.NextDouble() < 0.5 ? -1 : 1) * spin, depth);
    }

    /// <summary>A shard's outline about its middle: a jagged scrap of a few corners, <paramref name="size"/> pixels across or so.</summary>
    public static (double X, double Y)[] ShardOf(Random rng, double size)
    {
        int corners = 4 + rng.Next(0, 4);
        var points = new (double X, double Y)[corners];
        for (int i = 0; i < corners; i++)
        {
            double angle = i * 2 * Math.PI / corners + rng.NextDouble() * 0.6, reach = size / 2 * (0.45 + 0.55 * rng.NextDouble());
            // Long and thin more often than round: torn strips.
            points[i] = (Math.Cos(angle) * reach * 1.6, Math.Sin(angle) * reach * 0.7);
        }
        return points;
    }

    /// <summary>A gap from <paramref name="least"/> to <paramref name="most"/> seconds.</summary>
    public static double Gap(Random rng, double least, double most) => least + (most - least) * rng.NextDouble();
}
