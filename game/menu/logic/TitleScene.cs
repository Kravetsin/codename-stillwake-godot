using System;

namespace Stillwake.Game.Menu;

/// <summary>Something drifting past the title's wreck, an astronaut or a piece of debris: its way across, in cells, its turn, how far off it is.</summary>
/// <param name="FromX">Where it comes in, in cells, off the view's left.</param>
/// <param name="ToX">Where it goes out, off its right.</param>
/// <param name="FromY">Its height coming in, in cells.</param>
/// <param name="ToY">Its height going out.</param>
/// <param name="Seconds">How long it takes to cross.</param>
/// <param name="Spin">How fast it turns over, in radians a second, either way.</param>
/// <param name="Depth">How far off it is: 1 beside the wreck, smaller farther, drawn smaller and dimmer.</param>
/// <param name="Lamp">An astronaut's: whether its helmet lamp still burns.</param>
public readonly record struct Drifter(double FromX, double ToX, double FromY, double ToY, double Seconds, double Spin, double Depth, bool Lamp)
{
    /// <summary>Where it is <paramref name="seconds"/> after coming in, in cells, and how far it has turned; null once it has crossed.</summary>
    public (double X, double Y, double Turn)? At(double seconds)
    {
        if (seconds < 0 || seconds > Seconds) return null;
        double t = seconds / Seconds;
        return (FromX + (ToX - FromX) * t, FromY + (ToY - FromY) * t, Spin * seconds);
    }
}

/// <summary>
/// The scene behind the title (0031): the wreck the game starts in, a world run but never saved, its hero sitting on the
/// edge of the torn deck looking out into space; debris and now and then another astronaut drifting past from where it looks,
/// left to right, as the wreck drifts on; the camera wandering slowly. Plain arithmetic; the client draws it.
/// </summary>
public static class TitleScene
{
    /// <summary>The camera's zoom: the hero small against the dark.</summary>
    public const float Zoom = 3;

    /// <summary>Where the hero sits on screen, as a share of it from the top-left: its gaze has the space to the left.</summary>
    public const float HeroAcross = 0.7f, HeroDown = 0.52f;

    /// <summary>How far the camera wanders, in cells, and how long each way takes, in seconds: slow enough to feel, never to see move.</summary>
    public const double SwayX = 0.9, SwayY = 0.5, SwayXSeconds = 37, SwayYSeconds = 23;

    /// <summary>How far in from the deck's edge the hips sit, in cells: the knees just over it.</summary>
    public const double SitIn = 0.7;

    /// <summary>The seconds between one astronaut and the next, at least and at most; the first comes sooner.</summary>
    public const double DrifterGapLeast = 16, DrifterGapMost = 34, FirstDrifter = 3;

    /// <summary>The seconds between one piece of debris and the next, at least and at most.</summary>
    public const double PieceGapLeast = 1.5, PieceGapMost = 5;

    /// <summary>How far off the view's sides things come in and go out, in cells: never seen popping in.</summary>
    private const double Margin = 10;

    /// <summary>
    /// The edge the hero sits on: the lowest solid cell open above it and to its left, with deck to its right, so its legs
    /// hang in space; null on a map with none. <paramref name="solid"/> says whether a cell, by its column and row, is solid.
    /// </summary>
    public static (int X, int Y)? Ledge(Func<int, int, bool> solid, int width, int height)
    {
        for (int y = height - 2; y > 0; y--)
            for (int x = 1; x < width - 1; x++)
                if (solid(x, y) && solid(x + 1, y) && !solid(x - 1, y) && !solid(x, y - 1) && !solid(x - 1, y + 1)) return (x, y);
        return null;
    }

    /// <summary>Where the hips rest, in cells, on the ledge at <paramref name="ledge"/>: on its top, <see cref="SitIn"/> in from its edge.</summary>
    public static (double X, double Y) Seat((int X, int Y) ledge) => (ledge.X + SitIn, ledge.Y);

    /// <summary>
    /// The point the camera centres, in cells, <paramref name="seconds"/> into the title, for a hero seated at
    /// <paramref name="seat"/> on a screen <paramref name="screenCells"/> wide and high at <see cref="Zoom"/>.
    /// </summary>
    public static (double X, double Y) Camera((double X, double Y) seat, (double W, double H) screenCells, double seconds) =>
        (seat.X + (0.5 - HeroAcross) * screenCells.W + SwayX * Math.Sin(seconds / SwayXSeconds * 2 * Math.PI),
         seat.Y + (0.5 - HeroDown) * screenCells.H + SwayY * Math.Sin(seconds / SwayYSeconds * 2 * Math.PI));

    /// <summary>
    /// The next astronaut to drift past a view <paramref name="view"/> (its left, top, width and height in cells): above the
    /// wreck or below it, where space is open, slowly, near or far, a few with their lamps still burning.
    /// </summary>
    public static Drifter NextDrifter(Random rng, (double Left, double Top, double W, double H) view)
    {
        double depth = 0.45 + 0.55 * rng.NextDouble();
        double share = rng.NextDouble() < 0.5 ? 0.06 + 0.16 * rng.NextDouble() : 0.66 + 0.24 * rng.NextDouble();
        return Across(rng, view, view.Top + view.H * share, depth, (55 + 30 * rng.NextDouble()) / (0.5 + 0.5 * depth),
            0.05 + 0.12 * rng.NextDouble(), rng.NextDouble() < 0.35);
    }

    /// <summary>The next piece of debris to drift past <paramref name="view"/>: at any height, near or far, quicker than an astronaut.</summary>
    public static Drifter NextPiece(Random rng, (double Left, double Top, double W, double H) view)
    {
        double depth = 0.3 + 0.9 * rng.NextDouble();
        return Across(rng, view, view.Top + view.H * rng.NextDouble(), depth, (30 + 30 * rng.NextDouble()) / (0.4 + 0.6 * depth),
            0.1 + 0.5 * rng.NextDouble(), false);
    }

    private static Drifter Across(Random rng, (double Left, double Top, double W, double H) view, double y, double depth, double seconds, double spin, bool lamp) =>
        new(view.Left - Margin, view.Left + view.W + Margin, y, y + (rng.NextDouble() - 0.5) * view.H * 0.2, seconds,
            (rng.NextDouble() < 0.5 ? -1 : 1) * spin, depth, lamp);

    /// <summary>A gap from <paramref name="least"/> to <paramref name="most"/> seconds.</summary>
    public static double Gap(Random rng, double least, double most) => least + (most - least) * rng.NextDouble();
}
