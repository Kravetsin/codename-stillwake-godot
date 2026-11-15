using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Where a swung blade's trail lies on a wreck (blade-sparks): of the stretch it swept last, from its edge as it was to its edge
/// as it is, the points inside the wreck and the way the blade went through them. A struck wreck throws its sparks from there,
/// along the swing, rather than from its side facing the hero: off the haze, as a running tool's do (drill-contact).
/// </summary>
public static class BladeBites
{
    /// <summary>
    /// The points of the stretch from <paramref name="was"/> to <paramref name="now"/> (the edge's points, one to one, in pixels of
    /// the world) that <paramref name="inside"/> says are in a wreck, each edge point and the middle of its sweep, and the unit way
    /// they went; none while none is inside.
    /// </summary>
    public static (List<(double X, double Y)> Points, double Dx, double Dy)? Of(
        IReadOnlyList<(double X, double Y)> was, IReadOnlyList<(double X, double Y)> now, Func<double, double, bool> inside)
    {
        if (was.Count != now.Count) return null;
        List<(double X, double Y)>? points = null;
        double dx = 0, dy = 0;
        for (int i = 0; i < now.Count; i++)
        {
            var (a, b) = (was[i], now[i]);
            var middle = ((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            bool hit = false;
            foreach (var p in new[] { middle, b })
            {
                if (!inside(p.Item1, p.Item2)) continue;
                (points ??= []).Add(p);
                hit = true;
            }
            if (hit) (dx, dy) = (dx + b.X - a.X, dy + b.Y - a.Y);
        }
        double l = Math.Sqrt(dx * dx + dy * dy);
        return points == null || l <= 0 ? null : (points, dx / l, dy / l);
    }
}
