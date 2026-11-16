using System;
using Stillwake.Content;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// Where the radar's map puts things (solar-system, its picked look): the system seen from above, the sun in the middle and its
/// planets on their orbits, zoomed from the whole system (0) down to one planet with the ship's orbit round it (1), where the
/// contacts lie. Positions are in map units round the sun; <see cref="ToScreen"/> turns them into the screen's pixels.
/// </summary>
public static class NavMap
{
    /// <summary>How many times larger the map is at full zoom than with the whole system in view.</summary>
    public const float Deep = 12;

    /// <summary>The map's reach from the sun that fits the screen's shorter side at zoom 0.</summary>
    public const float Extent = 250;

    /// <summary>A planet's radius, and how far over its surface the ship's orbit runs, in map units.</summary>
    public const float PlanetRadius = 9, OrbitGap = 6;

    /// <summary>The angle of each orbit's planet, in degrees: spread round the sun so no two stand in a line.</summary>
    private static readonly float[] Angles = [200, 320, 62, 148, 262, 24];

    /// <summary>Planet <paramref name="i"/> of the system: on its own orbit, each wider than the last.</summary>
    public static (float X, float Y) Planet(int i)
    {
        float radius = Orbit(i), angle = (Angles[i % Angles.Length] + 17 * (i / Angles.Length)) * MathF.PI / 180;
        return (MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
    }

    /// <summary>The radius of planet <paramref name="i"/>'s orbit round the sun.</summary>
    public static float Orbit(int i) => 55 + 35.4f * i;

    /// <summary>How many times larger than at zoom 0 the map is at <paramref name="zoom"/>: even steps as the wheel turns.</summary>
    public static float Scale(float zoom) => MathF.Exp(Math.Clamp(zoom, 0, 1) * MathF.Log(Deep));

    /// <summary>Pixels a map unit at zoom 0 on a screen <paramref name="width"/> by <paramref name="height"/>.</summary>
    public static float Fit(float width, float height) => MathF.Min(width, height) / (2 * Extent);

    /// <summary>
    /// Where map point <paramref name="at"/> is on a screen <paramref name="width"/> by <paramref name="height"/> at
    /// <paramref name="zoom"/>: the sun in the middle at 0, <paramref name="focus"/> there at 1, slid by <paramref name="pan"/> pixels.
    /// </summary>
    public static (float X, float Y) ToScreen((float X, float Y) at, (float X, float Y) focus, float zoom, float width, float height, (float X, float Y) pan)
    {
        float s = Scale(zoom), toward = (s - 1) / (Deep - 1), k = Fit(width, height) * s;
        return (width / 2 + (at.X - focus.X * toward) * k + pan.X, height / 2 + (at.Y - focus.Y * toward) * k + pan.Y);
    }

    /// <summary>The ship's mark at planet <paramref name="i"/>: on its orbit straight over it.</summary>
    public static (float X, float Y) Ship(int i)
    {
        var (x, y) = Planet(i);
        return (x, y - PlanetRadius - OrbitGap);
    }

    /// <summary>
    /// A contact at <paramref name="offset"/> on the ship's orbit round planet <paramref name="i"/>: the ship at its top, a contact at
    /// the radar's reach a quarter of the way round, left or right as its offset says.
    /// </summary>
    public static (float X, float Y) Contact(int i, int offset)
    {
        var (x, y) = Planet(i);
        float angle = -MathF.PI / 2 + Math.Clamp(offset / (float)Contacts.Reach, -1, 1) * 1.5f, radius = PlanetRadius + OrbitGap;
        return (x + MathF.Cos(angle) * radius, y + MathF.Sin(angle) * radius);
    }

    /// <summary>A course from <paramref name="from"/> to <paramref name="to"/>, <paramref name="t"/> of the way along: bowed away from the sun, never through it.</summary>
    public static (float X, float Y) Course((float X, float Y) from, (float X, float Y) to, (float X, float Y) sun, float t)
    {
        float mx = (from.X + to.X) / 2, my = (from.Y + to.Y) / 2, length = MathF.Sqrt(Sq(to.X - from.X) + Sq(to.Y - from.Y));
        float nx = mx - sun.X, ny = my - sun.Y, d = MathF.Sqrt(nx * nx + ny * ny);
        if (d < 1) (nx, ny, d) = (from.Y - to.Y, to.X - from.X, MathF.Max(length, 1));
        float cx = mx + nx / d * length * 0.22f, cy = my + ny / d * length * 0.22f, u = 1 - t;
        return (u * u * from.X + 2 * u * t * cx + t * t * to.X, u * u * from.Y + 2 * u * t * cy + t * t * to.Y);
    }

    /// <summary>
    /// Where a callout <paramref name="wide"/> by <paramref name="tall"/> stands beside a thing at (<paramref name="x"/>,
    /// <paramref name="y"/>) of radius <paramref name="r"/>: on its side away from the middle, where it covers least of what the
    /// thing stands round, else the other, kept <paramref name="margin"/> inside the screen.
    /// </summary>
    public static (float X, float Y) Callout(float x, float y, float r, float wide, float tall, float width, float height, float margin = 16)
    {
        const float Gap = 34;
        float right = x + r + Gap, left = x - r - Gap - wide;
        bool fitsRight = right + wide <= width - margin, fitsLeft = left >= margin;
        left = x < width / 2 ? (fitsLeft ? left : right) : (fitsRight ? right : left);
        return (Math.Clamp(left, margin, MathF.Max(margin, width - margin - wide)), Math.Clamp(y - 44, margin, MathF.Max(margin, height - margin - tall)));
    }

    private static float Sq(float v) => v * v;
}
