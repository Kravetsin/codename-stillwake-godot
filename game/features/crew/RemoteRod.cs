using System;
using Stillwake.Sim;
using Spec = Stillwake.Content.Harpoon;

namespace Stillwake.Game.Features.Crew;

/// <summary>What one frame of another hero's rod did, for its sounds and its puff: it was <see cref="Fired"/>, it <see cref="Bit"/> where it stuck.</summary>
public readonly record struct RodCues(bool Fired = false, bool Bit = false);

/// <summary>
/// Another hero's harpoon as this peer plays it (0065), from the rod it sends (<see cref="RodSeen"/>), taken
/// as drawn, 150 ms behind with its hero: fired, the rod flies from the gun drawn here toward the end of the
/// range sent, at <c>Harpoon.RodSpeed</c>; stuck, it sits where it was sent to stick, riding its piece as this
/// peer draws it, so the line ends on the piece the sim reels; come free, or its piece gone here, it snaps back
/// to the gun at <c>Harpoon.ReturnSpeed</c>, the shooter's own rules; sent back in the gun, it is there at
/// once. The gun is drawn loaded only <c>Harpoon.CooldownTicks</c> after the shot this peer saw.
/// </summary>
public sealed class RemoteRod
{
    private static readonly double PerMs = World.TicksPerSecond / 1000.0;

    private RodSeen? _heard;
    private (double X, double Y) _from;
    private double _sinceFiredMs = double.PositiveInfinity;

    /// <summary>Where the rod is drawn: in the gun, flying, stuck (hooked, reeling, pulling, hanging) or on its way back.</summary>
    public Rod Rod { get; private set; }

    /// <summary>Where the rod's point is drawn, in thousandths of a cell, while it is out of the gun.</summary>
    public (double X, double Y) Tip { get; private set; }

    /// <summary>The way the rod flew, a unit vector; for a rod first seen stuck, the way from the gun to it.</summary>
    public (double X, double Y) Heading { get; private set; } = (1, 0);

    /// <summary>The piece the rod is stuck in, by id; none in flight, on the way back or in the structure.</summary>
    public int? Piece { get; private set; }

    /// <summary>Whether the gun is drawn with its rod in: back in the gun and the cooldown since the shot seen here over.</summary>
    public bool Loaded => Rod == Rod.Seated && _sinceFiredMs >= HarpoonShot.CooldownMs;

    /// <summary>Whether the line is drawn wound taut: reeling, pulled, hanging or on the way back; slack in flight and hooked.</summary>
    public bool Taut => HarpoonShot.Taut(Rod);

    /// <summary>Whether its drum turns: reeling a piece in or pulling its hero, as the shooter hears it.</summary>
    public bool Drums => HarpoonShot.Drums(Rod);

    /// <summary>
    /// Where the gun is aimed this frame, in thousandths of a cell, given <paramref name="sent"/>, before the figure
    /// is drawn: flying, the end of its range; stuck, the rod; on its way back, where it is; none in the gun.
    /// </summary>
    public (double X, double Y)? AimAt(RodSeen? sent, Func<int, (double X, double Y)?> pieceAt)
    {
        if (sent is not { } now) return null;
        if (now is { Rod: Rod.Flying, At: { } toward }) return (toward.X, toward.Y);
        if (now.Rod != Rod.Returning && StuckAt(now, pieceAt) is { } stuck) return stuck;
        return Rod == Rod.Seated ? null : Tip;
    }

    /// <summary>
    /// Moves the rod on by <paramref name="elapsedMs"/> as <paramref name="sent"/> says, from the gun's
    /// <paramref name="muzzle"/> as just drawn (none: no gun drawn, the rod in it), its piece where
    /// <paramref name="pieceAt"/> draws it, none once gone.
    /// </summary>
    public RodCues Step(double elapsedMs, RodSeen? sent, (double X, double Y)? muzzle, Func<int, (double X, double Y)?> pieceAt)
    {
        double ms = Math.Min(Math.Max(0, elapsedMs), HarpoonShot.MostPerFrameMs);
        _sinceFiredMs += ms;
        var was = _heard;
        _heard = sent;
        if (sent is not { } now || muzzle is not { } gun)
        {
            Seat();
            return default;
        }
        // A rod out where none was is a shot: one heard first on its way back is not.
        bool fired = was == null && now.Rod != Rod.Returning;
        if (fired) _sinceFiredMs = 0;
        bool bit = false;
        switch (now.Rod)
        {
            case Rod.Flying:
                if (Rod != Rod.Flying) Launch(gun, now.At);
                Fly(ms);
                break;
            case Rod.Returning:
                if (Rod != Rod.Seated) Return(ms, gun);
                break;
            default:
                // Its piece gone here, it comes free here, before its shooter says so.
                if (StuckAt(now, pieceAt) is not { } at)
                {
                    if (Rod != Rod.Seated) Return(ms, gun);
                    break;
                }
                bit = was is null or { Rod: Rod.Flying };
                // Its flight unseen here, it points the way from the gun.
                if (Rod is Rod.Seated or Rod.Returning) Heading = Toward(gun, at) ?? Heading;
                (Rod, Tip, Piece) = (now.Rod, at, now.Piece);
                break;
        }
        return new RodCues(fired, bit);
    }

    private void Launch((double X, double Y) gun, Point? toward)
    {
        (Rod, Piece, _from, Tip) = (Rod.Flying, null, gun, gun);
        if (toward is { } end) Heading = Toward(gun, (end.X, end.Y)) ?? Heading;
    }

    private void Fly(double ms)
    {
        double flown = Math.Min(Distance(_from, Tip) + Spec.RodSpeed * PerMs * ms, Spec.Range);
        Tip = (_from.X + Heading.X * flown, _from.Y + Heading.Y * flown);
    }

    private void Return(double ms, (double X, double Y) gun)
    {
        (Rod, Piece) = (Rod.Returning, null);
        double left = Distance(Tip, gun), by = Spec.ReturnSpeed * PerMs * ms;
        if (left <= by) Seat();
        else Tip = (Tip.X + (gun.X - Tip.X) / left * by, Tip.Y + (gun.Y - Tip.Y) / left * by);
    }

    private void Seat() => (Rod, Piece) = (Rod.Seated, null);

    /// <summary>Where a stuck rod sent as <paramref name="sent"/> is drawn here; none without a point or once its piece is gone.</summary>
    private static (double X, double Y)? StuckAt(RodSeen sent, Func<int, (double X, double Y)?> pieceAt)
    {
        if (sent.At is not { } at) return null;
        if (sent.Piece is not int piece) return (at.X, at.Y);
        return pieceAt(piece) is { } middle ? (middle.X + at.X, middle.Y + at.Y) : null;
    }

    private static (double X, double Y)? Toward((double X, double Y) from, (double X, double Y) to)
    {
        double far = Distance(from, to);
        return far > 0 ? ((to.X - from.X) / far, (to.Y - from.Y) / far) : null;
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
