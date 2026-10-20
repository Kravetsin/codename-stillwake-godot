using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The two-handed hold (0078): an item with <see cref="HeldLook.Both"/> is held as a rifle is, close before the chest,
/// the near hand on its grip and the far one on its foregrip. Its <see cref="TwoHands.Mount"/> rests on the body's
/// pivot (<see cref="PivotAt"/>) and the item turns there toward what it aims at, its edge's line through the point, but
/// no further up than <see cref="Up"/> nor down than <see cref="Down"/>; both arms reach for their grips
/// (<see cref="Arms"/>). Pure math in the body's frame: pixels from the shoulder, +x the way the hero faces, +y down.
/// </summary>
public static class TwoHand
{
    /// <summary>Where the mount rests aimed straight ahead, from the shoulder: the item's tube across the middle of the chest.</summary>
    public static readonly Vec Pivot = new(2, 8);

    /// <summary>
    /// How far the pivot moves, in pixels a radian, as the item turns up and down: forward aimed up, so the near hand stays
    /// in reach as its grip swings down; up aimed down, so the near arm's elbow rises no higher than the shoulder.
    /// </summary>
    public static readonly Vec PivotUp = new(3, 0), PivotDown = new(0, -3);

    /// <summary>
    /// How far the item turns from straight ahead, in radians, up and down at most (about 34°): steeper up, its chuck would
    /// cross the helmet and the near hand outreach its arm; steeper down, the near elbow would rise past the shoulder and the
    /// far hand outreach its arm, and the bit of a long drill touch the floor. Past them it points as far as it turns.
    /// </summary>
    public const double Up = 0.6, Down = 0.6;

    /// <summary>The turn it rests at when aimed at nothing, a little down: a friend's hero holding it idle.</summary>
    public const double Rest = 0.3;

    /// <summary>An arm reaching for a grip: the fist's middle, <see cref="Rig.FistBack"/> short of the forearm's end, is what it puts there.</summary>
    private static readonly Segments ToFist = Rig.Arm with { Lower = Rig.Arm.Lower - Rig.FistBack };

    /// <summary>Where the mount rests with the item turned <paramref name="angle"/>, from the shoulder.</summary>
    public static Vec PivotAt(double angle)
    {
        var (by, turn) = angle < 0 ? (PivotUp, -angle) : (PivotDown, angle);
        return new Vec(Pivot.X + by.X * turn, Pivot.Y + by.Y * turn);
    }

    /// <summary>
    /// The turn of <paramref name="look"/>, clockwise from straight ahead, that puts its edge's line through a point
    /// (<paramref name="dx"/>, <paramref name="dy"/>) pixels from the shoulder, kept within <see cref="Up"/> and
    /// <see cref="Down"/>. A point nearer the pivot than the edge's line is aimed at straight.
    /// </summary>
    public static double AngleFor(HeldLook look, double dx, double dy)
    {
        var mount = look.Both!.Mount;
        double ux = look.EdgeTo.X - look.EdgeFrom.X, uy = look.EdgeTo.Y - look.EdgeFrom.Y, length = Math.Sqrt(ux * ux + uy * uy);
        // How far the edge's line passes beside the mount, and its turn in the picture: both fixed as the item turns.
        double beside = (ux * (look.EdgeTo.Y - mount.Y) - uy * (look.EdgeTo.X - mount.X)) / length;
        double angle = 0;
        // The pivot moves with the turn: eight rounds settle it within a thousandth of a pixel.
        for (int i = 0; i < 8; i++)
        {
            var pivot = PivotAt(angle);
            double tx = dx - pivot.X, ty = dy - pivot.Y, far = Math.Sqrt(tx * tx + ty * ty);
            double wanted = Math.Atan2(ty, tx) - Math.Atan2(uy, ux) - (far > Math.Abs(beside) ? Math.Asin(beside / far) : 0);
            angle = Math.Clamp(Math.IEEERemainder(wanted, 2 * Math.PI), -Up, Down);
        }
        return angle;
    }

    /// <summary>Where <paramref name="pixel"/> of <paramref name="look"/>'s picture is, from the shoulder, held turned <paramref name="angle"/>.</summary>
    public static Vec At(HeldLook look, double angle, (double X, double Y) pixel)
    {
        var (mount, pivot) = (look.Both!.Mount, PivotAt(angle));
        double x = pixel.X - mount.X, y = pixel.Y - mount.Y, cos = Math.Cos(angle), sin = Math.Sin(angle);
        return new Vec(pivot.X + x * cos - y * sin, pivot.Y + x * sin + y * cos);
    }

    /// <summary>
    /// The arms holding <paramref name="look"/> turned <paramref name="angle"/>: the near (front) arm's fist on its grip,
    /// the far (back) arm's on its foregrip, each elbow on the side below its hand, the near one back as a rifle's trigger arm.
    /// </summary>
    public static (Bend Near, Bend Far) Arms(HeldLook look, double angle) =>
        (Rig.Reach(At(look, angle, look.Grip), ToFist, 1), Rig.Reach(At(look, angle, look.Both!.Foregrip), ToFist, 1));
}
