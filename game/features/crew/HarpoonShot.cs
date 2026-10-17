using System;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Spec = Stillwake.Content.Harpoon;

namespace Stillwake.Game.Features.Crew;

/// <summary>Where the harpoon's rod is (0044).</summary>
public enum Rod
{
    /// <summary>In the gun: fired again once the hero's cooldown is over.</summary>
    Seated,
    /// <summary>Flying out from the muzzle, paying out the line.</summary>
    Flying,
    /// <summary>Stuck in a piece, the line slack, waiting for the press that reels it in.</summary>
    Hooked,
    /// <summary>Stuck in a piece the sim reels in, the line taut.</summary>
    Reeling,
    /// <summary>Come free, a miss or a line let go: snapping back to the muzzle.</summary>
    Returning,
}

/// <summary>
/// What the world says to the harpoon each frame, in thousandths of a cell: <see cref="Touches"/>, the
/// first piece, in spawn order, the rod's point meets on its way from one point to another that this hero
/// could reel, and where it sticks; <see cref="PieceAt"/>, where a piece is drawn, none once it is gone;
/// <see cref="Reelable"/>, whether the sim would take this hero's reel of it; <see cref="Reeled"/>, whether
/// this hero's reel of it is still on, in flight or in the sim.
/// </summary>
public sealed record HarpoonWorld(
    Func<(double X, double Y), (double X, double Y), (int Piece, (double X, double Y) At)?> Touches,
    Func<int, (double X, double Y)?> PieceAt,
    Func<int, bool> Reelable,
    Func<int, bool> Reeled);

/// <summary>
/// The gun as the hero holds it this frame: whether it <see cref="Wields"/> it (in the hand, the hand free,
/// awake, not climbing), whether it <see cref="Floats"/> out in space, and its <see cref="Muzzle"/> and the
/// way its <see cref="Barrel"/> points, a unit vector, as the figure last drew it; no muzzle before it has.
/// </summary>
public readonly record struct GunHold(bool Wields, bool Floats, (double X, double Y)? Muzzle, (double X, double Y) Barrel);

/// <summary>
/// What one frame of the harpoon did: the shot <see cref="Fired"/>, the piece the rod <see cref="Hooked"/>,
/// the <see cref="Reel"/> to send, and whether the gun came <see cref="Ready"/> again in the hand.
/// </summary>
public readonly record struct ShotStep(bool Fired = false, int? Hooked = null, Reel? Reel = null, bool Ready = false);

/// <summary>
/// The hand harpoon (0044), this hero's, client only: one press fires the rod from the muzzle along the
/// barrel at <c>Harpoon.RodSpeed</c> out to <c>Harpoon.Range</c>; it sticks in the first piece it meets
/// that the sim would let this hero reel, else snaps back at <c>Harpoon.ReturnSpeed</c>. Hooked, the next
/// press sends <see cref="Reel"/> to the point <c>Harpoon.Stop</c> before the muzzle toward the piece, and
/// the rod rides the piece in till the sim's reel is over, then snaps back; a press while reeling sends it
/// again from where the hero is now. The line comes free when the piece is gone or the sim would refuse its
/// reel, drifts past the range unreeled, or the hero goes aboard; put away or passed out, the rod is back in
/// the gun at once. The cooldown, <c>Harpoon.CooldownTicks</c> as real time from the shot, hit or miss, is
/// the hero's: one machine for every harpoon it holds. Presses in flight, on the way back or in the
/// cooldown, and any aboard, do nothing and cost nothing.
/// </summary>
public sealed class HarpoonShot
{
    /// <summary>The cooldown, in ms: <c>Harpoon.CooldownTicks</c> of the world's ticks.</summary>
    public static readonly double CooldownMs = Spec.CooldownTicks * 1000.0 / World.TicksPerSecond;

    /// <summary>A frame longer than this counts as this long: a stall never flies the rod through half the sky at once.</summary>
    public const double MostPerFrameMs = 100;

    private static readonly double PerMs = World.TicksPerSecond / 1000.0;

    private double _cooldownMs;
    private (double X, double Y) _from, _stuck;
    private bool _wasReady = true;

    public Rod Rod { get; private set; }

    /// <summary>Where the rod's point is, in thousandths of a cell, while it is out of the gun.</summary>
    public (double X, double Y) Tip { get; private set; }

    /// <summary>The way the rod flew, a unit vector: it keeps it while it is stuck and unreeled.</summary>
    public (double X, double Y) Heading { get; private set; }

    /// <summary>The piece the rod is stuck in, hooked or reeling.</summary>
    public int? Piece { get; private set; }

    /// <summary>The ms of the hero's cooldown still to run.</summary>
    public double CooldownLeftMs => _cooldownMs;

    /// <summary>Whether the gun fires at the next press in space: the rod seated and the cooldown over. Drawn loaded.</summary>
    public bool Ready => Rod == Rod.Seated && _cooldownMs <= 0;

    /// <summary>
    /// Moves the harpoon on by <paramref name="elapsedMs"/>: <paramref name="pressed"/> is a fresh press of the
    /// left button, never a hold.
    /// </summary>
    public ShotStep Step(double elapsedMs, bool pressed, GunHold hold, HarpoonWorld world)
    {
        double ms = Math.Min(Math.Max(0, elapsedMs), MostPerFrameMs);
        _cooldownMs = Math.Max(0, _cooldownMs - ms);
        var step = new ShotStep();
        if (!hold.Wields) Seat();
        else if (hold.Muzzle is { } muzzle) step = Rod switch
        {
            Rod.Seated => pressed && hold.Floats && _cooldownMs <= 0 ? Fire(muzzle, hold.Barrel) : step,
            Rod.Flying => hold.Floats ? Fly(ms, world) : Free(step),
            Rod.Hooked => Hooked(pressed, hold.Floats, muzzle, world),
            Rod.Reeling => Reeling(pressed, hold.Floats, muzzle, world),
            _ => Return(ms, muzzle, step),
        };
        bool ready = Ready;
        step = step with { Ready = ready && !_wasReady && hold.Wields };
        _wasReady = ready;
        return step;
    }

    private ShotStep Fire((double X, double Y) muzzle, (double X, double Y) barrel)
    {
        double length = Math.Sqrt(barrel.X * barrel.X + barrel.Y * barrel.Y);
        if (length == 0) return default;
        (Rod, Tip, _from, Heading, _cooldownMs) = (Rod.Flying, muzzle, muzzle, (barrel.X / length, barrel.Y / length), CooldownMs);
        return new ShotStep(Fired: true);
    }

    private ShotStep Fly(double ms, HarpoonWorld world)
    {
        double flown = Distance(_from, Tip) + Spec.RodSpeed * PerMs * ms;
        bool spent = flown >= Spec.Range;
        if (spent) flown = Spec.Range;
        var next = (_from.X + Heading.X * flown, _from.Y + Heading.Y * flown);
        if (world.Touches(Tip, next) is var (piece, at) && world.PieceAt(piece) is { } middle)
        {
            (Rod, Piece, Tip, _stuck) = (Rod.Hooked, piece, at, (at.X - middle.X, at.Y - middle.Y));
            return new ShotStep(Hooked: piece);
        }
        Tip = next;
        return spent ? Free(default) : default;
    }

    private ShotStep Hooked(bool pressed, bool floats, (double X, double Y) muzzle, HarpoonWorld world)
    {
        if (!floats || Piece is not int piece || world.PieceAt(piece) is not { } middle || !world.Reelable(piece)) return Free(default);
        Tip = (middle.X + _stuck.X, middle.Y + _stuck.Y);
        if (!pressed) return Distance(muzzle, Tip) > Spec.Range ? Free(default) : default;
        Rod = Rod.Reeling;
        return new ShotStep(Reel: ReelOf(piece, muzzle, middle));
    }

    private ShotStep Reeling(bool pressed, bool floats, (double X, double Y) muzzle, HarpoonWorld world)
    {
        if (!floats || Piece is not int piece || world.PieceAt(piece) is not { } middle) return Free(default);
        Tip = (middle.X + _stuck.X, middle.Y + _stuck.Y);
        // Pressed again, the reel goes again from where the hero is now; the last reel wins (0044).
        if (pressed) return new ShotStep(Reel: ReelOf(piece, muzzle, middle));
        return world.Reeled(piece) ? default : Free(default);
    }

    private ShotStep Return(double ms, (double X, double Y) muzzle, ShotStep step)
    {
        double left = Distance(Tip, muzzle), by = Spec.ReturnSpeed * PerMs * ms;
        if (left <= by) Seat();
        else Tip = (Tip.X + (muzzle.X - Tip.X) / left * by, Tip.Y + (muzzle.Y - Tip.Y) / left * by);
        return step;
    }

    private ShotStep Free(ShotStep step)
    {
        (Rod, Piece) = (Rod.Returning, null);
        return step;
    }

    private void Seat() => (Rod, Piece) = (Rod.Seated, null);

    /// <summary>
    /// The reel of <paramref name="piece"/>, drawn at <paramref name="middle"/>, to the point
    /// <c>Harpoon.Stop</c> before <paramref name="muzzle"/> on the way to it, in whole thousandths of a cell.
    /// </summary>
    public static Reel ReelOf(int piece, (double X, double Y) muzzle, (double X, double Y) middle)
    {
        double dx = middle.X - muzzle.X, dy = middle.Y - muzzle.Y, far = Math.Sqrt(dx * dx + dy * dy);
        (double X, double Y) to = far == 0 ? muzzle : (muzzle.X + dx / far * Spec.Stop, muzzle.Y + dy / far * Spec.Stop);
        return new Reel(piece, (int)Math.Round(to.X), (int)Math.Round(to.Y));
    }

    /// <summary>
    /// The arm's angle from straight ahead of the shoulder, down positive, that puts the line of
    /// <paramref name="look"/>'s barrel through a point (<paramref name="dx"/>, <paramref name="dy"/>) pixels
    /// from the shoulder: the arm straight, the gun held as the figure draws it (<see cref="AxeSwing.EdgeAt"/>).
    /// A point nearer than the barrel's line is aimed at straight.
    /// </summary>
    public static double ArmFor(HeldLook look, double dx, double dy)
    {
        var (from, to) = AxeSwing.EdgeAt(look, 0);
        double ux = to.X - from.X, uy = to.Y - from.Y, length = Math.Sqrt(ux * ux + uy * uy);
        // The barrel's turn from the arm, and how far its line passes beside the shoulder: both fixed as the arm turns.
        double turn = Math.Atan2(uy, ux), beside = (ux * to.Y - uy * to.X) / length, far = Math.Sqrt(dx * dx + dy * dy);
        return Math.Atan2(dy, dx) - turn - (far > Math.Abs(beside) ? Math.Asin(beside / far) : 0);
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
