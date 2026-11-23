using System;
using Stillwake.Sim;
using Spec = Stillwake.Content.Harpoon;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// A body as the rope pulls it, in thousandths of a cell and seconds: its middle, its motion a second, its spin in radians a second, its
/// mass (<see cref="PieceShape.Mass"/>) and its inertia about its middle.
/// </summary>
public readonly record struct RopeBody(double X, double Y, double Vx, double Vy, double Spin, double Mass, double Inertia);

/// <summary>An impulse, mass times thousandths of a cell a second, put on a body at a point (<see cref="AtX"/>, <see cref="AtY"/>) from its middle.</summary>
public readonly record struct RopeImpulse(double X, double Y, double AtX, double AtY);

/// <summary>
/// A harpoon's line as the host player's physics pulls with it (debris-physics, the prototype's rope.gd): anchored at its holder's body
/// middle, hooked at a point of a body. Slack inside its length; taut, one impulse at the hook point takes away the point's speed away
/// from the anchor, so the piece swings and turns as a weight on a string, and only the hero pulls, never pulled back. The drum is a
/// brake: a pull above <c>Harpoon.Brake</c> slips and pays the line out, the brake hardening as it goes (<c>Harpoon.BrakeRamp</c>), up
/// to <c>Harpoon.PayOut</c>; a heavy piece needs more for the same change of speed, so it pays out sooner. Winding takes in what was
/// paid out first, then the line, at <c>Harpoon.ReelSpeed</c> down to <c>Harpoon.Stop</c>, where it does nothing.
/// </summary>
public sealed class PieceRope(double length)
{
    /// <summary>The share of an overstretch taken back a step, and the fastest it is taken back, in thousandths of a cell a second (the prototype's 240 px/s).</summary>
    public const double Bias = 0.2, BiasMost = 30_000;

    /// <summary>How fast the drum winds, in thousandths of a cell a second.</summary>
    public static readonly double WindSpeed = Spec.ReelSpeed * (double)World.TicksPerSecond;

    /// <summary>What the drum is wound to, in thousandths of a cell: the line's length the sim holds.</summary>
    public double Wound { get; private set; } = length;

    /// <summary>What the drum has paid out under load past <see cref="Wound"/>, in thousandths of a cell; the host player's alone.</summary>
    public double Paid { get; private set; }

    /// <summary>The line's whole length: wound and paid out.</summary>
    public double Length => Wound + Paid;

    /// <summary>Whether the line spanned its whole length at the last step.</summary>
    public bool Taut { get; private set; }

    /// <summary>
    /// Moves the rope on by <paramref name="dt"/> seconds: winds it if it <paramref name="winds"/>, then the impulse that keeps the hook
    /// point at <paramref name="hook"/> of <paramref name="body"/> within the line's length of <paramref name="anchor"/>, moving at
    /// <paramref name="anchorVelocity"/>; none while slack.
    /// </summary>
    public RopeImpulse? Step(double dt, (double X, double Y) anchor, (double X, double Y) anchorVelocity, (double X, double Y) hook, RopeBody body, bool winds)
    {
        if (dt <= 0) return null;
        double windRate = 0;
        if (winds)
        {
            double take = WindSpeed * dt, fromPaid = Math.Min(take, Paid);
            Paid -= fromPaid;
            take -= fromPaid;
            double before = Wound;
            // Never longer: a line already shorter than the stop stays as it is.
            Wound = Math.Max(Wound - take, Math.Min(Wound, Spec.Stop));
            windRate = (fromPaid + before - Wound) / dt;
        }
        double dx = hook.X - anchor.X, dy = hook.Y - anchor.Y, d = Math.Sqrt(dx * dx + dy * dy), length = Length;
        Taut = d >= length - 62.5;
        if (d <= length || d < 1) return null;
        double nx = dx / d, ny = dy / d;
        double rx = hook.X - body.X, ry = hook.Y - body.Y;
        // How fast the hook point moves away from the anchor.
        double vx = body.Vx - body.Spin * ry, vy = body.Vy + body.Spin * rx;
        double away = (vx - anchorVelocity.X) * nx + (vy - anchorVelocity.Y) * ny;
        double want = -windRate - Math.Min(Bias * (d - length) / dt, BiasMost);
        if (away <= want) return null;
        double rn = rx * ny - ry * nx;
        double j = (away - want) / (1 / body.Mass + rn * rn / body.Inertia);
        if (Paid < Spec.PayOut)
        {
            double f = Paid / Spec.PayOut, limit = Spec.Brake * (1 + Spec.BrakeRamp * f * f) * dt;
            if (j > limit)
            {
                j = limit;
                Paid = Math.Min(Math.Max(Paid, d - Wound), Spec.PayOut);
            }
        }
        return new RopeImpulse(-nx * j, -ny * j, rx, ry);
    }
}
