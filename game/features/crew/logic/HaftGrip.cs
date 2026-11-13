using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The far hand on a held haft (body-answers): the far arm, from the same shoulder as the near one, its fist on the haft
/// <see cref="BodyAnswers.GripBelow"/> pixels below the near fist, slid along it within <see cref="BodyAnswers.HaftFrom"/> to
/// <see cref="BodyAnswers.HaftTo"/> to where it reaches, the elbow out as <see cref="Rig.Reach"/> bends an arm, never quite straight.
/// </summary>
public static class HaftGrip
{
    /// <summary>The far arm holding the haft: its bend, how far along the haft below the near fist it holds, that point and the far fist, from the shoulder.</summary>
    public readonly record struct Hold(Bend Arm, double Along, Vec At, Vec Fist);

    /// <summary>
    /// The far arm holding the haft of <paramref name="look"/> held in the near arm <paramref name="arm"/>, its wrist turned
    /// <paramref name="wrist"/>; the far fist's reach is the arm less <see cref="Rig.FistBack"/>, <see cref="BodyAnswers.GripSlack"/> short of it.
    /// </summary>
    public static Hold ArmFor(Bend arm, HeldLook look, double wrist)
    {
        Vec HaftAt(double along) => Rig.HeldAt(arm, look, (look.Grip.X - along, look.Grip.Y), wrist);
        var fist = HaftAt(0);
        var next = HaftAt(1);
        var (vx, vy) = (next.X - fist.X, next.Y - fist.Y);
        var size = Rig.Arm with { Lower = Rig.Arm.Lower - Rig.FistBack };
        double fv = fist.X * vx + fist.Y * vy, ff = fist.X * fist.X + fist.Y * fist.Y, r = size.Upper + size.Lower - BodyAnswers.GripSlack;
        double d, room = fv * fv - ff + r * r;
        // Where along the haft the far fist reaches from the shoulder: the wanted point if it does, the nearest end of the reach if not.
        if (room >= 0)
        {
            double lo = Math.Max(BodyAnswers.HaftFrom, -fv - Math.Sqrt(room)), hi = Math.Min(BodyAnswers.HaftTo, -fv + Math.Sqrt(room));
            d = lo <= hi ? Math.Clamp(BodyAnswers.GripBelow, lo, hi) : hi < BodyAnswers.HaftFrom ? BodyAnswers.HaftFrom : BodyAnswers.HaftTo;
        }
        else d = Math.Clamp(-fv, BodyAnswers.HaftFrom, BodyAnswers.HaftTo);
        var at = HaftAt(d);
        double far = Math.Sqrt(at.X * at.X + at.Y * at.Y), reached = Math.Min(far, r);
        var bend = Rig.Reach(far > 0 ? new Vec(at.X * reached / far, at.Y * reached / far) : at, size, 1);
        // Turned the short way from the near arm, so it blends with the far arm hanging without a turn round.
        double toward = Math.Atan2(at.X, at.Y);
        bend = bend with { Upper = bend.Upper + arm.Upper + Poses.Apart(arm.Upper, toward) - toward };
        return new(bend, d, at, Rig.FistOf(bend));
    }
}
