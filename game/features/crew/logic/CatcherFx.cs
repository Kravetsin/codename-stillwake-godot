using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The catcher's fight as the user picked it (catcher-combat), its numbers as its page drew them, in pixels of the world, y down, a
/// figure's feet at 0: its aim «Красная нить», a thin red line from its muzzle wobbling on its quarry, still and brightening in the tell;
/// its blink «Красная помеха», its figure cut into bands jittering sideways under a red copy, its ghost in bands ahead, a red tear where it
/// was; its salvo's marks «Столбы света», a pale column over each landing spot, narrowing to a thread as its shell nears.
/// </summary>
public static class CatcherFx
{
    /// <summary>The page's hash: a number from 0 to 1 for <paramref name="n"/>, the same each time.</summary>
    public static double Hash(int n)
    {
        uint h = unchecked((uint)(n ^ 0x5bd1e995) * 2654435761u);
        h = unchecked((h ^ (h >> 15)) * 2246822519u);
        h ^= h >> 13;
        return h / 4294967296.0;
    }

    private static double Ease(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    // The aim.

    /// <summary>How long the bolt burns, then its faint ghost lingers, then the line takes the time to come back, in ms.</summary>
    public const double BoltMs = 140, AfterMs = 220, BackMs = 300;

    /// <summary>The line's red, its halo's in the tell, the dot's white-pink core; the bolt's ghost.</summary>
    public static readonly (int R, int G, int B) Line = (255, 70, 50), Halo = (255, 60, 40), DotCore = (255, 200, 190), Ghost = (190, 40, 40);

    /// <summary>
    /// The aim line as drawn: its <paramref name="Alpha"/> and <paramref name="Width"/>, how far its end wobbles (<paramref name="Sway"/>,
    /// 1 a whole wobble), its halo's alpha (none outside the tell), the dot on its end and the light at the muzzle.
    /// </summary>
    public readonly record struct Thread(double Alpha, double Width, double Sway, double Halo, double DotRadius, double DotAlpha, double MuzzleRadius);

    /// <summary>
    /// The aim line <paramref name="tell"/> of the way through the tell (null while it only tracks), <paramref name="back"/> of the way
    /// back after a bolt, <paramref name="ms"/> into the clock (its flicker).
    /// </summary>
    public static Thread AimOf(double? tell, double back, double ms)
    {
        double flicker = 0.92 + 0.08 * Math.Sin(ms / 40);
        if (tell is not double k) return new(0.3 * back * flicker, 0.4, 1, 0, 2.4, 0.45 * back, 1.8);
        k = Math.Clamp(k, 0, 1);
        return new((0.35 + 0.6 * k) * flicker, 0.4 + 0.35 * k, 1 - k, 0.2 * k, 2.4 + 3.2 * k, 0.55 + 0.45 * k, 1.8 + 2.5 * k);
    }

    /// <summary>Where the aim's end wobbles to, <paramref name="ms"/> into the clock, at <paramref name="sway"/> of a whole wobble.</summary>
    public static (double X, double Y) Wobble(double ms, double sway) => (Math.Sin(ms / 170) * 1.4 * sway, Math.Sin(ms / 230 + 1) * 1.6 * sway);

    /// <summary>
    /// The aim's share back after a bolt <paramref name="sinceMs"/> ago: none while the bolt and its ghost show, then up over
    /// <see cref="BackMs"/>.
    /// </summary>
    public static double BackOf(double sinceMs) => Math.Clamp((sinceMs - BoltMs - AfterMs) / BackMs, 0, 1);

    /// <summary>
    /// The points of a crooked line from (<paramref name="x0"/>, <paramref name="y0"/>) to (<paramref name="x1"/>, <paramref name="y1"/>):
    /// one every 8 pixels, at least five, each strayed across it by up to <paramref name="amp"/>, most in the middle; how many filled.
    /// </summary>
    public static int Crooked(double x0, double y0, double x1, double y1, int seed, double amp, Span<(double X, double Y)> into)
    {
        double dx = x1 - x0, dy = y1 - y0, length = Math.Max(1e-9, Math.Sqrt(dx * dx + dy * dy)), nx = -dy / length, ny = dx / length;
        int n = Math.Min(into.Length - 1, Math.Max(4, (int)Math.Round(length / 8)));
        for (int i = 0; i <= n; i++)
        {
            double u = (double)i / n, off = (Hash(seed * 131 + i) - 0.5) * 2 * amp * Math.Sin(Math.PI * u);
            into[i] = (x0 + dx * u + nx * off, y0 + dy * u + ny * off);
        }
        return n + 1;
    }

    // The blink.

    /// <summary>A figure's bands, its ghost's; where they start over its feet and how much of it they span.</summary>
    public const int Bands = 7, GhostBands = 9;
    public const double Top = -50, Tall = 52;

    /// <summary>How long each jitter holds, in ms: the page's 45.</summary>
    public const double FrameMs = 45;

    /// <summary>
    /// After the blink, in ms: its bands twitch for <see cref="TwitchMs"/>; its tear burns <see cref="TearMs"/>; its arrival's red copy and
    /// line <see cref="ArriveMs"/>; specks where it was <see cref="SpeckMs"/>.
    /// </summary>
    public const double TwitchMs = 450, TearMs = 170, ArriveMs = 260, SpeckMs = 320;

    /// <summary>The red it glitches in, and the pale one among its specks.</summary>
    public static readonly (int R, int G, int B) Red = (255, 52, 40), Pale = (255, 200, 190);

    /// <summary>The jitter's frame <paramref name="ms"/> into the clock.</summary>
    public static int FrameOf(double ms) => (int)Math.Floor(ms / FrameMs);

    /// <summary>
    /// The figure's own bands, <paramref name="gather"/> through its gather, or <paramref name="spentMs"/> after its blink (null while
    /// gathering): over half of them still, the rest moved sideways by up to as much as it has gathered, fading out after.
    /// </summary>
    public static void Body(int frame, double gather, double? spentMs, Span<float> shift)
    {
        double amp = spentMs is double e ? 2.4 * Math.Max(0, 1 - e / TwitchMs) : 0.4 + 2.8 * gather;
        for (int i = 0; i < Bands; i++)
            shift[i] = Hash(frame * 7 + i) < 0.55 ? 0 : (float)((Hash(frame * 11 + i) - 0.5) * 2 * amp);
    }

    /// <summary>
    /// The red copy over the figure, added: gathering, a pixel off and torn wider as it gathers, a quarter of its bands faint; just
    /// arrived (<paramref name="spentMs"/> under <see cref="ArriveMs"/>), its bands jumping and dropping out as it fades; else none.
    /// </summary>
    public static void RedCopy(int frame, double gather, double? spentMs, Span<float> shift, Span<float> alpha)
    {
        for (int i = 0; i < Bands; i++)
        {
            if (spentMs is double e)
            {
                double fade = Math.Max(0, 1 - e / ArriveMs);
                shift[i] = (float)((Hash(frame * 41 + i) - 0.5) * 3 * fade);
                alpha[i] = (float)(Hash(frame * 43 + i) < 0.6 ? 0.45 * fade : 0);
                continue;
            }
            shift[i] = (float)(1.2 + (Hash(frame * 7 + i) < 0.55 ? 0 : (Hash(frame * 13 + i) - 0.5) * 5 * gather));
            alpha[i] = (float)((0.08 + 0.2 * gather) * (0.6 + 0.4 * Hash(frame)) * (Hash(frame * 37 + i) < 0.5 ? 1 : 0.25));
        }
    }

    /// <summary>The ghost where it will be, red, flickering in <see cref="GhostBands"/> bands, about a quarter missing, brighter as it gathers.</summary>
    public static void Ghosting(int frame, double gather, Span<float> shift, Span<float> alpha)
    {
        double a = (0.12 + 0.3 * gather) * (0.6 + 0.4 * Hash(frame + 9));
        for (int i = 0; i < GhostBands; i++)
        {
            bool gone = Hash(frame * 3 + i * 5) < 0.25;
            shift[i] = gone ? 0 : (float)((Hash(frame * 5 + i) - 0.5) * 3);
            alpha[i] = gone ? 0 : (float)a;
        }
    }

    /// <summary>
    /// Speck <paramref name="i"/> of those round a figure: from its middle across and over its feet, a short line across, every third pale,
    /// its share of <paramref name="alpha"/>.
    /// </summary>
    public static (double X, double Y, double Length, bool Pale, double Alpha) Speck(int frame, int i, double alpha) =>
        ((Hash(frame * 19 + i) - 0.5) * 26, -2 - Hash(frame * 23 + i) * 46, 0.8 + Hash(frame * 29 + i) * 2.6, i % 3 == 0,
            alpha * (0.4 + 0.6 * Hash(frame * 31 + i)));

    /// <summary>How many specks fly round it <paramref name="gather"/> through its gather, round its ghost, and where it was <paramref name="ms"/> after.</summary>
    public static int SpecksGathering(double gather) => (int)Math.Round(4 + 14 * gather);

    public static int SpecksGhost(double gather) => (int)Math.Round(3 + 6 * gather);

    public static int SpecksLeft(double ms) => ms < SpeckMs ? (int)Math.Round(10 * (1 - ms / SpeckMs)) : 0;

    // The salvo.

    /// <summary>A mark's column: its height, its light, its thread's and the glow on the ground.</summary>
    public const double PillarHeight = 48;

    public static readonly (int R, int G, int B) Pillar = (255, 236, 190), PillarThread = (255, 250, 235), PillarGlow = (255, 230, 170);

    /// <summary>
    /// A mark's column <paramref name="k"/> of the way through its shell's flight: 6 pixels wide narrowing to 1.4, brighter as it nears,
    /// its thread lit as much, its glow on the ground.
    /// </summary>
    public static (double Width, double Alpha, double Thread, double Glow) PillarOf(double k)
    {
        k = Math.Clamp(k, 0, 1);
        return (6 + (1.4 - 6) * Ease(k), 0.32 + 0.45 * k, 0.8 * k, 0.35 + 0.3 * k);
    }
}
