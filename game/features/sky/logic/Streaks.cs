using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// What a weather blows across the open sky in view (docs/plans/surface-storms.md), as the pick page drew it: streaks, each from its
/// own numbers and the time alone, wrapping round a box of the pick's size; the world tiled by such boxes, each with its own numbers,
/// so the same streak is in the same place for any camera and nothing is kept between frames. Only what is in view is laid, and only
/// over open sky as <c>open</c> says. Plain C#.
/// </summary>
public static class Streaks
{
    /// <summary>A streak: tapering from its head, <see cref="Width"/> wide, to its tail; in its weather's colour <see cref="Colour"/>; a speck of grit by its head, or none.</summary>
    public readonly record struct Line(float HeadX, float HeadY, float TailX, float TailY, float Width, int Colour, (float X, float Y)? Speck);

    /// <summary>A wisp: picture <see cref="Picture"/> of its weather's, its middle at <see cref="X"/>, <see cref="Y"/>, drawn at <see cref="Alpha"/>.</summary>
    public readonly record struct Puff(float X, float Y, int Picture, float Alpha);

    /// <summary>The box streaks wrap round, in pixels of the world: the pick's view and its margin.</summary>
    public const float Wide = StretchLooks.Weather.BoxWide, Tall = StretchLooks.Weather.BoxTall;

    /// <summary>How high over the ground low streaks reach, thinning to none: the pick's ground halfway down its view.</summary>
    public const float LowReach = 70;

    /// <summary>
    /// The streaks of <paramref name="weather"/> at <paramref name="storm"/>'s height in <paramref name="view"/> (pixels of the world),
    /// <paramref name="seconds"/> on, into <paramref name="into"/>: each whose head and tail are both <paramref name="open"/>; a low
    /// weather's thinning with the height over <paramref name="ground"/>, the ground's top at a pixel across.
    /// </summary>
    public static void Fill(StretchLooks.Weather weather, Storm storm, (float Left, float Top, float Right, float Bottom) view, double seconds,
        Func<float, float, bool> open, Func<float, float> ground, List<Line> into)
    {
        int count = storm.Streaks(weather);
        if (count <= 0) return;
        var spec = weather.Streaks;
        float dx = -MathF.Cos(spec.Angle), dy = MathF.Sin(spec.Angle), longest = spec.Length.Most + 1;
        int left = (int)MathF.Floor((view.Left - longest) / Wide), right = (int)MathF.Floor((view.Right + longest) / Wide);
        int top = (int)MathF.Floor((view.Top - longest) / Tall), bottom = (int)MathF.Floor((view.Bottom + longest) / Tall);
        for (int tx = left; tx <= right; tx++)
            for (int ty = top; ty <= bottom; ty++)
                for (int i = 0; i < count; i++)
                {
                    float q0 = Hash(tx, ty, i, 0), q1 = Hash(tx, ty, i, 1), q2 = Hash(tx, ty, i, 2), q3 = Hash(tx, ty, i, 3), q4 = Hash(tx, ty, i, 4), q5 = Hash(tx, ty, i, 5);
                    float speed = Lerp(spec.Speed.Least, spec.Speed.Most, q2), length = Lerp(spec.Length.Least, spec.Length.Most, q3);
                    float jitter = spec.Low ? MathF.Sin(Turn(seconds * 7) + q4 * 6) * 0.8f : 0;
                    float hx = tx * Wide + Wrap(Wide * q0 + dx * seconds * speed, Wide), hy = ty * Tall + Wrap(Tall * q1 + dy * seconds * speed + jitter, Tall);
                    if (hx < view.Left - longest || hx > view.Right + longest || hy < view.Top - longest || hy > view.Bottom + longest) continue;
                    if (spec.Low && Hash(tx, ty, i, 6) >= Thick(ground(hx) - hy)) continue;
                    float tailX = hx - dx * length, tailY = hy - dy * length;
                    if (!open(hx, hy) || !open(tailX, tailY)) continue;
                    (float, float)? speck = spec.Low && q4 < 0.35f ? (hx + 6 * q5, hy + 3 * (q3 - 0.5f)) : null;
                    into.Add(new(hx, hy, tailX, tailY, spec.Width * (0.7f + q5 * 0.6f), i % spec.Colours.Count, speck));
                }
    }

    /// <summary>
    /// The wisps of <paramref name="weather"/> at <paramref name="storm"/>'s height in <paramref name="view"/>, <paramref name="seconds"/>
    /// on, into <paramref name="into"/>: a row of them along the ground, across the boxes in view, each up to 42 pixels over
    /// <paramref name="ground"/>, its middle <paramref name="open"/>. None for a weather without.
    /// </summary>
    public static void Puffs(StretchLooks.Weather weather, Storm storm, (float Left, float Top, float Right, float Bottom) view, double seconds,
        Func<float, float, bool> open, Func<float, float> ground, List<Puff> into)
    {
        if (weather.Wisps is not { } wisps) return;
        int count = (int)MathF.Round(wisps.Count * storm.Share);
        float reach = 30;
        int left = (int)MathF.Floor((view.Left - reach) / Wide), right = (int)MathF.Floor((view.Right + reach) / Wide);
        for (int tx = left; tx <= right; tx++)
            for (int i = 0; i < count; i++)
            {
                float q0 = Hash(tx, 0, i, 10), q1 = Hash(tx, 0, i, 11), q2 = Hash(tx, 0, i, 12), q3 = Hash(tx, 0, i, 13), q4 = Hash(tx, 0, i, 14);
                float x = tx * Wide + Wrap(Wide * q0 - seconds * Lerp(wisps.Speed.Least, wisps.Speed.Most, q2), Wide);
                float y = ground(x) - (42 - 56 * q1) + MathF.Sin(Turn(seconds * 2) + q4 * 6) * 1.5f;
                if (x < view.Left - reach || x > view.Right + reach || y < view.Top - reach || y > view.Bottom + reach || !open(x, y)) continue;
                into.Add(new(x, y, i % wisps.Pictures.Count, wisps.Alpha * storm.Share * (0.6f + 0.4f * q3)));
            }
    }

    /// <summary>Of a low weather's streaks, the share kept <paramref name="height"/> pixels over the ground: all at it and below, none at <see cref="LowReach"/>.</summary>
    public static float Thick(float height) => Math.Clamp(1 - height / LowReach, 0, 1);

    /// <summary>A number from 0 to 1 for streak <paramref name="i"/> of box (<paramref name="x"/>, <paramref name="y"/>), its <paramref name="k"/>-th.</summary>
    public static float Hash(int x, int y, int i, int k)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u ^ (uint)y * 668265263u ^ (uint)i * 2246822519u ^ (uint)k * 3266489917u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            h *= 2654435761u;
            h ^= h >> 15;
            return (h >> 8) / (float)(1 << 24);
        }
    }

    /// <summary>
    /// <paramref name="v"/> wrapped into 0 to <paramref name="span"/>: in doubles, so a world hours old still moves its streaks smoothly.
    /// </summary>
    private static float Wrap(double v, float span) => (float)(((v % span) + span) % span);

    private static float Lerp(float a, float b, float k) => a + (b - a) * k;

    /// <summary>An angle of <paramref name="radians"/> taken within a turn, so a sine of hours of seconds keeps its precision.</summary>
    private static float Turn(double radians) => (float)(radians % Math.Tau);
}
