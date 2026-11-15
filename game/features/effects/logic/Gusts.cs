using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Effects;

/// <summary>
/// A push of air an action gives its surroundings (air-gusts), in cells: from (<paramref name="X"/>, <paramref name="Y"/>), toward
/// (<paramref name="Dx"/>, <paramref name="Dy"/>), a unit vector, or every way at zero; <paramref name="Strength"/> 1 a railgun's
/// shot, felt out to <paramref name="Radius"/> cells.
/// </summary>
public readonly record struct Gust(double X, double Y, double Dx, double Dy, double Strength, double Radius);

/// <summary>
/// What a gust stirs round it (air-gusts): the floor's dust blown out along it, the colour of the ground it lies on, and, from a
/// strong one, grit shaken off the walls and ceiling near. The surroundings answer, not the tool: a stroke low over the floor
/// raises it, one up in the air raises nothing. Only the cells within its radius are looked at, once, as it happens.
/// </summary>
public static class Gusts
{
    /// <summary>The push under which a column's dust or a face's grit stays where it lies.</summary>
    public const double Still = 0.06;

    /// <summary>The push a face must feel to shed grit: a heavy gust's, never a stroke's.</summary>
    public const double Shakes = 0.35;

    private static readonly Dictionary<(int, int, int), (Luminous Dust, Luminous Veil, Luminous Grain, Luminous Grit)> Looks = [];

    /// <summary>
    /// Throws what <paramref name="gust"/> stirs into <paramref name="stage"/>, from the ground <paramref name="grid"/> holds;
    /// none where <paramref name="airless"/> says a cell has no air to carry dust, grit floating where <paramref name="afloat"/>.
    /// The particles thrown.
    /// </summary>
    public static int Throw(Stage stage, IGridQueries grid, Func<int, bool> airless, Func<int, bool> afloat, Gust gust)
    {
        double strength = gust.Strength, r = gust.Radius;
        if (strength <= 0 || r <= 0) return 0;
        var glims = stage.Glims;
        int thrown = 0, w = grid.Width, h = grid.Height;
        int left = Math.Max(0, (int)Math.Floor(gust.X - r)), right = Math.Min(w - 1, (int)Math.Floor(gust.X + r));
        int top = Math.Max(0, (int)Math.Floor(gust.Y - r)), bottom = Math.Min(h - 1, (int)Math.Floor(gust.Y + r));
        // Looked for from just over it: a stroke landing on the floor's top lands in the floor's cell.
        int from = Math.Clamp((int)Math.Floor(gust.Y - 0.05), 0, h - 1);
        for (int c = left; c <= right; c++)
        {
            // The floor down this column from the gust's row: the top of the first solid cell under an open one.
            int? floor = null;
            for (int row = from; row <= bottom; row++)
                if (grid.Solid(row * w + c))
                {
                    if (row > from || !grid.Solid(from * w + c)) floor = row;
                    break;
                }
            if (floor is not int f || f == 0 || airless((f - 1) * w + c)) continue;
            // Pushed down, air spreads along the floor: the floor is felt as if half as far under as it is.
            double p = strength * Felt(gust, c + 0.5, gust.Y + (f - gust.Y) / 2);
            if (p < Still || grid.WallAt(f * w + c) is not { } ground) continue;
            var look = LookOf(ground);
            double side = Math.Abs(c + 0.5 - gust.X) < 0.3 ? Math.Sign(gust.Dx) : Math.Sign(c + 0.5 - gust.X);
            double x0 = c * Camera.Cell, y = f * Camera.Cell;
            for (int i = 0; i < Math.Round(p * 16 * Fx.Count); i++, thrown++)
                glims.Add(look.Dust, x0 + glims.Between(0, Camera.Cell), y - 0.5, side * glims.Between(25, 120) * p + glims.Between(-6, 6),
                    -glims.Between(8, 60) * p - glims.Between(0, 8), y, glims.Between(0.7, 1), glims.Between(0.8, 1.5));
            // Grains skittering out along it, as a landing's do.
            for (int i = 0; i < Math.Round(p * 5 * Fx.Count); i++, thrown++)
                glims.Add(look.Grain, x0 + glims.Between(0, Camera.Cell), y - 0.5, side * glims.Between(40, 140) * p, -glims.Between(2, 14), y);
            // A cloud of it rolling out and up, the most of what is seen.
            for (int i = 0; i < Math.Round(p * 3 * Fx.Count); i++, thrown++)
                glims.Add(look.Veil, x0 + glims.Between(1, Camera.Cell - 1), y - glims.Between(1, 3), side * glims.Between(10, 45) * p, -glims.Between(4, 16) * p);
        }
        if (strength < Shakes) return thrown;
        for (int row = top; row <= bottom; row++)
            for (int c = left; c <= right; c++)
            {
                int cell = row * w + c;
                if (grid.WallAt(cell) is not { } wall) continue;
                // The face it sheds from: a ceiling's underside first, else a side open to the air.
                int below = row + 1 < h ? cell + w : -1;
                (double X, double Y, double Nx)? face =
                    below >= 0 && !grid.Solid(below) ? (c + glims.Between(0.1, 0.9), row + 1, 0)
                    : c > 0 && !grid.Solid(cell - 1) ? (c, row + glims.Between(0.2, 0.9), -1)
                    : c + 1 < w && !grid.Solid(cell + 1) ? (c + 1, row + glims.Between(0.2, 0.9), 1)
                    : null;
                if (face is not var (fx, fy, nx)) continue;
                double p = strength * Felt(gust, fx, fy);
                if (p < Shakes * 0.5 || glims.Between(0, 1) > p) continue;
                var look = LookOf(wall);
                int open = nx == 0 ? below : cell + (int)nx;
                var grit = afloat(open) ? look.Grit with { Gravity = 0, Bounce = 0 } : look.Grit;
                double px = fx * Camera.Cell, py = fy * Camera.Cell;
                for (int i = 0; i < 1 + (int)(p * 2); i++, thrown++)
                    glims.Add(grit, px, py, nx * glims.Between(10, 50) + glims.Between(-10, 10), glims.Between(0, 30), size: glims.Between(0.5, 1.1));
                if (!airless(open) && glims.Add(look.Dust with { Gravity = 14 }, px, py, nx * glims.Between(4, 14), glims.Between(4, 16)) != null) thrown++;
            }
        return thrown;
    }

    /// <summary>
    /// How much of a gust is felt at (<paramref name="x"/>, <paramref name="y"/>), in cells, 0 to 1: falling off to its radius,
    /// and a gust with a way mostly felt along it.
    /// </summary>
    public static double Felt(Gust gust, double x, double y)
    {
        double dx = x - gust.X, dy = y - gust.Y, d = Math.Sqrt(dx * dx + dy * dy);
        if (d >= gust.Radius) return 0;
        // Falling off evenly, so a stroke across the room still stirs the floor under it; one driven down stirs it twice as much.
        double felt = 1 - d / gust.Radius;
        if ((gust.Dx != 0 || gust.Dy != 0) && d > 1e-6) felt *= 0.5 + 0.5 * Math.Max(0, (dx * gust.Dx + dy * gust.Dy) / d);
        return felt;
    }

    /// <summary>A ground's dust, veil, grains and grit, its colour a natural one's, a built one's the hull's.</summary>
    private static (Luminous Dust, Luminous Veil, Luminous Grain, Luminous Grit) LookOf(Stillwake.Content.Material ground)
    {
        var colour = ground.Natural && GroundLooks.Of.TryGetValue(ground.In ?? ground.Id, out var natural) ? natural.Colour : (120, 126, 136);
        var key = (colour.Item1, colour.Item2, colour.Item3);
        if (Looks.TryGetValue(key, out var look)) return look;
        (int R, int G, int B) Paler(int by) => (Math.Min(255, key.Item1 + by), Math.Min(255, key.Item2 + by), Math.Min(255, key.Item3 + by));
        // Over the walls, not behind them as a landing's is: thrown up off the floor, it rises over its grass.
        look = (Luminous.LandDust with { Ramp = [Paler(60), Paler(40)], Low = false }, Luminous.LandVeil with { Ramp = [Paler(70)], Low = false, Alpha = 0.32, Size = 2.6, Grow = 4, LifeMs = (800, 1500) },
            Luminous.DustGrain with { Ramp = [Paler(50), Paler(20)], Low = false },
            Luminous.Chip with { Ramp = [Paler(10)], Gravity = 260, Bounce = 0.25, LifeMs = (700, 1100) });
        Looks[key] = look;
        return look;
    }
}
