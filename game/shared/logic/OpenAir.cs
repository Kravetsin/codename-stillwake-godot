using System;
using Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where a thing by a hero that flies through walls is in the hero's open air: the one rule for where a light drone shines
/// from, what its beam glints on and where it perches. Pixels of the world.
/// </summary>
public static class OpenAir
{
    /// <summary>How far off a wall's face the point keeps: more than a lamp's bulb, so none of its light starts in the wall.</summary>
    public const double Off = 3.5;

    private const double StepPx = 2, Inside = 1e-6;
    private const int Round = 2;

    /// <summary>Whether the point (<paramref name="x"/>, <paramref name="y"/>) lies where light stops: in a wall or a closed doorway. Past the grid it is open.</summary>
    public static bool Blocks(Func<int, int> structureAt, int width, int height, int cell, double x, double y)
    {
        int cx = (int)Math.Floor(x / cell), cy = (int)Math.Floor(y / cell);
        if (cx < 0 || cy < 0 || cx >= width || cy >= height) return false;
        return Structures.Is(structureAt(cy * width + cx), Trait.BlocksLight);
    }

    /// <summary>
    /// The point nearest <paramref name="wish"/> that is open, <see cref="Off"/> from a wall's face and in sight of <paramref name="from"/>
    /// inside the hero, out of the circle <paramref name="clear"/> where there is room: so never in a wall, nor on its far side;
    /// <paramref name="from"/> where none is.
    /// </summary>
    public static Vec Nearest(Vec wish, Vec from, Func<double, double, bool> blocks, (Vec At, double R)? clear = null)
    {
        if (blocks(from.X, from.Y)) return blocks(wish.X, wish.Y) ? from : wish;
        bool Kept(Vec p) => clear is not var (middle, r) || Apart(p, middle) >= r - Inside;
        if (Kept(wish) && !blocks(wish.X, wish.Y) && !blocks(wish.X - Off, wish.Y) && !blocks(wish.X + Off, wish.Y)
            && !blocks(wish.X, wish.Y - Off) && !blocks(wish.X, wish.Y + Off) && InSight(from, wish, blocks))
            return wish;

        const double cell = Camera.Cell;
        int left = (int)Math.Floor(Math.Min(wish.X, from.X) / cell) - Round, right = (int)Math.Floor(Math.Max(wish.X, from.X) / cell) + Round;
        int top = (int)Math.Floor(Math.Min(wish.Y, from.Y) / cell) - Round, bottom = (int)Math.Floor(Math.Max(wish.Y, from.Y) / cell) + Round;
        Vec? best = null;
        double nearest = double.PositiveInfinity;
        for (int row = top; row <= bottom; row++)
            for (int column = left; column <= right; column++)
            {
                double x = column * cell, y = row * cell, mx = x + cell / 2, my = y + cell / 2;
                if (blocks(mx, my)) continue;
                // Slid along the cell to the wish, off each face that is a wall's.
                var at = new Vec(
                    Math.Clamp(wish.X, x + (blocks(mx - cell, my) ? Off : 0), x + cell - (blocks(mx + cell, my) ? Off : Inside)),
                    Math.Clamp(wish.Y, y + (blocks(mx, my - cell) ? Off : 0), y + cell - (blocks(mx, my + cell) ? Off : Inside)));
                if (!Kept(at) && clear is var (middle, r))
                {
                    double out_ = Apart(at, middle);
                    at = out_ < Inside ? new Vec(middle.X, middle.Y - r)
                        : new Vec(middle.X + (at.X - middle.X) * r / out_, middle.Y + (at.Y - middle.Y) * r / out_);
                    if (blocks(at.X, at.Y)) continue;
                }
                double far = Apart(at, wish);
                if (far < nearest && InSight(from, at, blocks)) (best, nearest) = (at, far);
            }
        return best ?? (clear == null ? from : Nearest(wish, from, blocks));
    }

    private static bool InSight(Vec from, Vec to, Func<double, double, bool> blocks)
    {
        int steps = (int)(Apart(from, to) / StepPx) + 1;
        for (int i = 1; i <= steps; i++)
            if (blocks(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps)) return false;
        return true;
    }

    private static double Apart(Vec a, Vec b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
