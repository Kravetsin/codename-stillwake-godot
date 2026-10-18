using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
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
    /// <summary>Stuck in a piece or the ship's structure, the line slack, waiting for the press that reels or pulls.</summary>
    Hooked,
    /// <summary>Stuck in a piece the sim reels in, the line taut.</summary>
    Reeling,
    /// <summary>Stuck in the structure, the hero pulled toward it on the taut line (the grapple).</summary>
    Pulling,
    /// <summary>Stuck in the structure, the hero hanging where the pull stopped.</summary>
    Hanging,
    /// <summary>Come free, a miss or a line let go: snapping back to the muzzle.</summary>
    Returning,
}

/// <summary>What the rod's point meets: a piece, or the ship's structure with none; and where it sticks.</summary>
public readonly record struct RodHit(int? Piece, (double X, double Y) At);

/// <summary>
/// What the world says to the harpoon each frame, in thousandths of a cell: <see cref="Touches"/>, the first
/// thing along the rod's way from one point to another, a piece this hero could reel or a wall, floor or
/// closed door, and where it sticks; <see cref="PieceAt"/>, where a piece is drawn, none once it is gone;
/// <see cref="Reelable"/>, whether the sim would take this hero's reel of it; <see cref="Reeled"/>, whether
/// this hero's reel of it is still on, in flight or in the sim; <see cref="Grips"/>, whether the structure at
/// a point still holds a rod (a door opened or a wall taken down lets it go).
/// </summary>
public sealed record HarpoonWorld(
    Func<(double X, double Y), (double X, double Y), RodHit?> Touches,
    Func<int, (double X, double Y)?> PieceAt,
    Func<int, bool> Reelable,
    Func<int, bool> Reeled,
    Func<(double X, double Y), bool> Grips);

/// <summary>
/// The gun as the hero holds it this frame: whether it <see cref="Wields"/> it (in the hand, the hand free,
/// awake, not climbing), its <see cref="Muzzle"/> and the way its <see cref="Barrel"/> points, a unit vector,
/// as the figure last drew it (no muzzle before it has), the hero's body <see cref="Middle"/>, and whether the
/// player <see cref="LetsGo"/>: jumped or held a movement key in the steps since the last frame.
/// </summary>
public readonly record struct GunHold(
    bool Wields, (double X, double Y)? Muzzle, (double X, double Y) Barrel, (double X, double Y) Middle = default, bool LetsGo = false);

/// <summary>
/// What one frame of the harpoon did: the shot <see cref="Fired"/>, the piece the rod <see cref="Hooked"/>,
/// whether it <see cref="Gripped"/> the structure, the <see cref="Reel"/> to send, and whether the gun came
/// <see cref="Ready"/> again in the hand.
/// </summary>
public readonly record struct ShotStep(bool Fired = false, int? Hooked = null, Reel? Reel = null, bool Ready = false, bool Gripped = false);

/// <summary>
/// The hand harpoon (0044), this hero's, client only: one press fires the rod from the muzzle along the
/// barrel at <c>Harpoon.RodSpeed</c> out to <c>Harpoon.Range</c>, aboard or out in space; it sticks in the
/// first thing on its way, a piece the sim would let this hero reel or a wall, floor or closed door, else
/// snaps back at <c>Harpoon.ReturnSpeed</c>. In a piece, the next press sends <see cref="Reel"/> to the point
/// <c>Harpoon.Stop</c> before the muzzle toward the piece, and the rod rides the piece in till the sim's reel
/// is over, then snaps back; a press while reeling sends it again from where the hero is now, and so does the
/// hero moving the point before the muzzle more than <c>Harpoon.FollowBeyond</c> from the last sent, at most
/// every <c>Harpoon.FollowTicks</c>. In the structure (the grapple), the next press pulls the hero's middle
/// toward <see cref="PullTo"/>, <c>Harpoon.PullStop</c> short of the hook, and it hangs where the pull stops
/// till the player jumps, holds a movement key or presses again. The line comes free when the piece is gone
/// or the sim would refuse its reel, the structure lets go, or it is past the range unpulled and unreeled; put
/// away or passed out, the rod is back in the gun at once. The cooldown, <c>Harpoon.CooldownTicks</c> as real
/// time from the shot, hit or miss, is the hero's: one machine for every harpoon it holds. Presses in flight,
/// while pulled, on the way back or in the cooldown do nothing.
/// </summary>
public sealed class HarpoonShot
{
    /// <summary>The cooldown, in ms: <c>Harpoon.CooldownTicks</c> of the world's ticks.</summary>
    public static readonly double CooldownMs = Spec.CooldownTicks * 1000.0 / World.TicksPerSecond;

    /// <summary>A frame longer than this counts as this long: a stall never flies the rod through half the sky at once.</summary>
    public const double MostPerFrameMs = 100;

    /// <summary>Pulled this near the point it is pulled to, in thousandths of a cell, the hero is there.</summary>
    private const double There = 20;

    private static readonly double PerMs = World.TicksPerSecond / 1000.0;

    private double _cooldownMs;
    private (double X, double Y) _from, _stuck;
    private bool _wasReady = true;
    /// <summary>Reeling: the point the last <see cref="Reel"/> went to, and the ms since it went.</summary>
    private (int X, int Y) _sentTo;
    private double _sinceSentMs;
    /// <summary>Pulled: where the hero's middle was last frame; none on the frame the pull began.</summary>
    private (double X, double Y)? _pulledFrom;

    /// <summary>The ms a reel waits at least before it is sent again to follow the hero: <c>Harpoon.FollowTicks</c>.</summary>
    public static readonly double FollowMs = Spec.FollowTicks * 1000.0 / World.TicksPerSecond;

    public Rod Rod { get; private set; }

    /// <summary>Where the rod's point is, in thousandths of a cell, while it is out of the gun.</summary>
    public (double X, double Y) Tip { get; private set; }

    /// <summary>The way the rod flew, a unit vector: it keeps it while it is stuck and unreeled.</summary>
    public (double X, double Y) Heading { get; private set; }

    /// <summary>The piece the rod is stuck in, hooked or reeling; none in the structure.</summary>
    public int? Piece { get; private set; }

    /// <summary>
    /// Pulled or hanging, where the hero's middle is pulled to, in whole thousandths of a cell:
    /// <c>Harpoon.PullStop</c> short of the hook on the way from where the pull began. None otherwise: the
    /// hero moves as it will.
    /// </summary>
    public Point? PullTo { get; private set; }

    /// <summary>The ms of the hero's cooldown still to run.</summary>
    public double CooldownLeftMs => _cooldownMs;

    /// <summary>Whether the gun fires at the next press: the rod seated and the cooldown over. Drawn loaded.</summary>
    public bool Ready => Rod == Rod.Seated && _cooldownMs <= 0;

    /// <summary>Whether a rod where <paramref name="rod"/> says draws its line wound taut: reeling, pulled, hanging or on the way back.</summary>
    public static bool Taut(Rod rod) => rod is Rod.Reeling or Rod.Pulling or Rod.Hanging or Rod.Returning;

    /// <summary>Whether the drum turns with a rod where <paramref name="rod"/> says: reeling a piece in or pulling the hero.</summary>
    public static bool Drums(Rod rod) => rod is Rod.Reeling or Rod.Pulling;

    /// <summary>The rod as others see it (0065): none in the gun; else its state and the one point that holds through it.</summary>
    public RodSeen? Seen => Rod switch
    {
        Rod.Seated => null,
        Rod.Flying => new RodSeen(Rod, Whole((_from.X + Heading.X * Spec.Range, _from.Y + Heading.Y * Spec.Range))),
        Rod.Returning => new RodSeen(Rod),
        _ when Piece is int piece => new RodSeen(Rod, Whole(_stuck), piece),
        _ => new RodSeen(Rod, Whole(Tip)),
    };

    private static Point Whole((double X, double Y) at) => new((int)Math.Round(at.X), (int)Math.Round(at.Y));

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
            Rod.Seated => pressed && _cooldownMs <= 0 ? Fire(muzzle, hold.Barrel) : step,
            Rod.Flying => Fly(ms, world),
            Rod.Hooked when Piece == null => Gripping(pressed, muzzle, hold.Middle, world),
            Rod.Hooked => Hooked(pressed, muzzle, world),
            Rod.Reeling => Reeling(ms, pressed, muzzle, world),
            Rod.Pulling or Rod.Hanging => Pulled(pressed, hold, world),
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
        switch (world.Touches(Tip, next))
        {
            case { Piece: null, At: var wall }:
                (Rod, Piece, Tip) = (Rod.Hooked, null, wall);
                return new ShotStep(Gripped: true);
            case { Piece: int piece, At: var at } when world.PieceAt(piece) is { } middle:
                (Rod, Piece, Tip, _stuck) = (Rod.Hooked, piece, at, (at.X - middle.X, at.Y - middle.Y));
                return new ShotStep(Hooked: piece);
        }
        Tip = next;
        return spent ? Free(default) : default;
    }

    private ShotStep Hooked(bool pressed, (double X, double Y) muzzle, HarpoonWorld world)
    {
        if (Piece is not int piece || world.PieceAt(piece) is not { } middle || !world.Reelable(piece)) return Free(default);
        Tip = (middle.X + _stuck.X, middle.Y + _stuck.Y);
        if (!pressed) return Distance(muzzle, Tip) > Spec.Range ? Free(default) : default;
        Rod = Rod.Reeling;
        return Send(ReelOf(piece, muzzle, middle));
    }

    /// <summary>Hooked in the structure: the next press pulls the hero toward it.</summary>
    private ShotStep Gripping(bool pressed, (double X, double Y) muzzle, (double X, double Y) middle, HarpoonWorld world)
    {
        if (!world.Grips(Tip)) return Free(default);
        if (!pressed) return Distance(muzzle, Tip) > Spec.Range ? Free(default) : default;
        (Rod, PullTo, _pulledFrom) = (Rod.Pulling, PullOf(middle, Tip), null);
        return default;
    }

    /// <summary>
    /// Pulled toward the hook, then hanging there: there once the hero's middle is at <see cref="PullTo"/> or
    /// stopped short of it, by a wall or the tether; let go by a jump, a movement key or a press while hanging.
    /// </summary>
    private ShotStep Pulled(bool pressed, GunHold hold, HarpoonWorld world)
    {
        if (!world.Grips(Tip) || PullTo is not { } to) return Free(default);
        if (Rod == Rod.Hanging) return pressed || hold.LetsGo ? Free(default) : default;
        var now = hold.Middle;
        bool there = Distance(now, (to.X, to.Y)) <= There;
        bool stopped = _pulledFrom is { } was && Distance(now, was) < 1;
        _pulledFrom = now;
        if (there || stopped) Rod = Rod.Hanging;
        return default;
    }

    /// <summary>
    /// Where a hero whose middle is at <paramref name="middle"/> is pulled to by a rod stuck at
    /// <paramref name="hook"/>: <c>Harpoon.PullStop</c> short of it on the way, in whole thousandths of a cell;
    /// a middle nearer than that stays where it is.
    /// </summary>
    public static Point PullOf((double X, double Y) middle, (double X, double Y) hook)
    {
        double dx = hook.X - middle.X, dy = hook.Y - middle.Y, far = Math.Sqrt(dx * dx + dy * dy);
        (double X, double Y) to = far <= Spec.PullStop ? middle : (hook.X - dx / far * Spec.PullStop, hook.Y - dy / far * Spec.PullStop);
        return new Point((int)Math.Round(to.X), (int)Math.Round(to.Y));
    }

    private ShotStep Reeling(double ms, bool pressed, (double X, double Y) muzzle, HarpoonWorld world)
    {
        if (Piece is not int piece || world.PieceAt(piece) is not { } middle) return Free(default);
        Tip = (middle.X + _stuck.X, middle.Y + _stuck.Y);
        _sinceSentMs += ms;
        var reel = ReelOf(piece, muzzle, middle);
        // Pressed again, the reel goes again from where the hero is now; the last reel wins (0044).
        if (pressed) return Send(reel);
        if (!world.Reeled(piece)) return Free(default);
        // The hero swum on: the reel follows it, the point before the gun sent again once it is a cell off.
        double dx = reel.ToX - _sentTo.X, dy = reel.ToY - _sentTo.Y;
        bool moved = dx * dx + dy * dy > (double)Spec.FollowBeyond * Spec.FollowBeyond;
        return moved && _sinceSentMs >= FollowMs ? Send(reel) : default;
    }

    private ShotStep Send(Reel reel)
    {
        (_sentTo, _sinceSentMs) = ((reel.ToX, reel.ToY), 0);
        return new ShotStep(Reel: reel);
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
        (Rod, Piece, PullTo) = (Rod.Returning, null, null);
        return step;
    }

    private void Seat() => (Rod, Piece, PullTo) = (Rod.Seated, null, null);

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
    /// The puff of air a shot blows from the muzzle (0044), as bursts of particles: most along the barrel,
    /// pointing <paramref name="barrel"/>, one off either side; braked aboard, freer out in space.
    /// </summary>
    public static IReadOnlyList<(Effect Effect, int Count, double Heading)> Puff((double X, double Y) barrel, bool inSpace)
    {
        var effect = inSpace ? Effect.PuffInSpace : Effect.Puff;
        double along = Math.Atan2(barrel.Y, barrel.X);
        return [(effect, 7, along), (effect, 1, along - Math.PI / 2), (effect, 1, along + Math.PI / 2)];
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
