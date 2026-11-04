using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where a line led from one point toward another stops short: walked back from the far end, the first cell that will do. A cable
/// held on the way past its reach, a pipe jointed past its span or cut short of the tubes in hand.
/// </summary>
public static class WayBack
{
    /// <summary>How finely the way back is walked, in cells.</summary>
    private const double Step = 0.25;

    /// <summary>
    /// The cell nearest <paramref name="to"/> on the straight way back to <paramref name="from"/>, points in cells, that
    /// <paramref name="holds"/>; nothing when no cell on the way does. <paramref name="from"/>'s own cell is never tried.
    /// </summary>
    public static int? Nearest((double X, double Y) from, (double X, double Y) to, int width, Func<int, bool> holds)
    {
        double length = Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y));
        int steps = (int)Math.Ceiling(length / Step);
        for (int i = 0; i < steps; i++)
        {
            double t = 1 - (double)i / steps;
            int cell = (int)Math.Floor(from.Y + (to.Y - from.Y) * t) * width + (int)Math.Floor(from.X + (to.X - from.X) * t);
            if (holds(cell)) return cell;
        }
        return null;
    }
}
