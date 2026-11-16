using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// How a stack's picture lies on the floor (2026-11-16-loot-pieces): on a flat side of its outline, the one nearest how it landed,
/// turned so that side is down. Plain C#, so the geometry is tested. Points are in pixels about the picture's middle, y down; an
/// angle turns a point as Godot's <c>Rotation</c> does.
/// </summary>
public static class LootPose
{
    /// <summary>The convex hull of <paramref name="points"/>, in order round it; as many as are given for fewer than three.</summary>
    public static IReadOnlyList<(float X, float Y)> Hull(IReadOnlyList<(float X, float Y)> points)
    {
        var p = new List<(float X, float Y)>(points);
        if (p.Count < 3) return p;
        p.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        var hull = new List<(float X, float Y)>();
        // Andrew's monotone chain: the lower run, then the upper.
        for (int pass = 0; pass < 2; pass++)
        {
            int start = hull.Count;
            for (int i = 0; i < p.Count; i++)
            {
                var q = pass == 0 ? p[i] : p[p.Count - 1 - i];
                while (hull.Count >= start + 2 && Cross(hull[^2], hull[^1], q) <= 0) hull.RemoveAt(hull.Count - 1);
                hull.Add(q);
            }
            hull.RemoveAt(hull.Count - 1);
        }
        return hull;
    }

    /// <summary>
    /// The angle <paramref name="hull"/> lies at, turned from <paramref name="landed"/> by the least: one of its sides at least
    /// <see cref="LootLooks.LeastSide"/> of its longest, flat underneath, its middle over that side, so it would not tip off.
    /// <paramref name="landed"/> itself for no such side.
    /// </summary>
    public static float RestAngle(IReadOnlyList<(float X, float Y)> hull, float landed)
    {
        if (hull.Count < 3) return landed;
        float longest = 0;
        for (int i = 0; i < hull.Count; i++) longest = MathF.Max(longest, Length(hull[i], hull[(i + 1) % hull.Count]));
        float best = landed, turn = float.MaxValue;
        for (int i = 0; i < hull.Count; i++)
        {
            var (a, b) = (hull[i], hull[(i + 1) % hull.Count]);
            float length = Length(a, b);
            if (length < longest * LootLooks.LeastSide) continue;
            // The middle (the origin) must lie over the side: its foot on the side's line falls between the ends.
            float along = -(a.X * (b.X - a.X) + a.Y * (b.Y - a.Y)) / (length * length);
            if (along <= 0 || along >= 1) continue;
            // The side's outward normal turned to point straight down.
            float nx = b.Y - a.Y, ny = -(b.X - a.X);
            if (nx * (a.X + b.X) + ny * (a.Y + b.Y) < 0) (nx, ny) = (-nx, -ny);
            float angle = MathF.PI / 2 - MathF.Atan2(ny, nx);
            float d = Wrap(angle - landed);
            if (MathF.Abs(d) < turn) (best, turn) = (landed + d, MathF.Abs(d));
        }
        return best;
    }

    /// <summary>How far below the middle <paramref name="hull"/>'s lowest point lies turned by <paramref name="angle"/>.</summary>
    public static float Lowest(IReadOnlyList<(float X, float Y)> hull, float angle)
    {
        float sin = MathF.Sin(angle), cos = MathF.Cos(angle), lowest = 0;
        for (int i = 0; i < hull.Count; i++) lowest = MathF.Max(lowest, hull[i].X * sin + hull[i].Y * cos);
        return lowest;
    }

    /// <summary>How far <paramref name="hull"/>'s farthest point lies from its middle: below the middle by that, it clears the line it turns over at any angle.</summary>
    public static float Radius(IReadOnlyList<(float X, float Y)> hull)
    {
        float most = 0;
        for (int i = 0; i < hull.Count; i++) most = MathF.Max(most, hull[i].X * hull[i].X + hull[i].Y * hull[i].Y);
        return MathF.Sqrt(most);
    }

    /// <summary>How many objects a stack of <paramref name="count"/> shows: one, two or three.</summary>
    public static int Objects(int count) => count >= LootLooks.Three ? 3 : count >= LootLooks.Two ? 2 : 1;

    /// <summary><paramref name="angle"/> brought within a half turn of none.</summary>
    public static float Wrap(float angle)
    {
        angle %= 2 * MathF.PI;
        return angle > MathF.PI ? angle - 2 * MathF.PI : angle < -MathF.PI ? angle + 2 * MathF.PI : angle;
    }

    private static float Cross((float X, float Y) o, (float X, float Y) a, (float X, float Y) b) =>
        (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

    private static float Length((float X, float Y) a, (float X, float Y) b) => MathF.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
}
