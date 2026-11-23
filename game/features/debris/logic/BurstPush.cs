using System;
using Stillwake.Sim.Debris;
using Spec = Stillwake.Content.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// A burst's push on a body of the host player's physics (debris-bursts): <c>Debris.BurstPush</c> for each point of its power, falling off
/// to none at its push reach, away from its middle along the line to the body's box's nearest point and put on there, so a light piece
/// flies far, a heavy one barely moves and one struck off its middle turns; never faster than <c>Debris.BurstFastest</c>. In thousandths
/// of a cell and radians, as <see cref="RopeBody"/>.
/// </summary>
public static class BurstPush
{
    /// <summary>
    /// The impulse a burst of <paramref name="power"/> pushing within <paramref name="push"/> cells from (<paramref name="cx"/>,
    /// <paramref name="cy"/>) puts on a body at (<paramref name="x"/>, <paramref name="y"/>) turned <paramref name="angle"/>,
    /// <paramref name="shape"/>'s box; none at its reach or past it. A burst inside the box pushes the middle away from it, from where it
    /// went off; one at the middle, up.
    /// </summary>
    public static RopeImpulse? Of(double cx, double cy, int power, int push, double x, double y, double angle, PieceShape shape)
    {
        if (push <= 0 || power <= 0) return null;
        double cos = Math.Cos(angle), sin = Math.Sin(angle), dx = cx - x, dy = cy - y;
        // The burst in the box's own frame, and the box's point nearest it.
        double lx = dx * cos + dy * sin, ly = -dx * sin + dy * cos;
        double qx = Math.Clamp(lx, -shape.HalfW, shape.HalfW), qy = Math.Clamp(ly, -shape.HalfH, shape.HalfH);
        double atX = qx * cos - qy * sin, atY = qx * sin + qy * cos;
        double gap = Math.Sqrt((lx - qx) * (lx - qx) + (ly - qy) * (ly - qy)), most = push * (double)DebrisModule.Unit;
        if (gap >= most) return null;
        // Outside, from the burst to the nearest point; inside, from the burst to the middle.
        double nx = atX - dx, ny = atY - dy;
        if (gap == 0) (nx, ny) = (-dx, -dy);
        double length = Math.Sqrt(nx * nx + ny * ny);
        (nx, ny) = length > 0 ? (nx / length, ny / length) : (0, -1);
        double j = Math.Min(power * (double)Spec.BurstPush * (1 - gap / most), Spec.BurstFastest * shape.Mass);
        return new RopeImpulse(nx * j, ny * j, atX, atY);
    }
}
