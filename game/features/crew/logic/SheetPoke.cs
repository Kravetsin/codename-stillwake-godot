using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The near finger poking the drone's hologram (drone-holo) as its player works the window or places plans, the far hand holding
/// it: up to the band and a short jab into it per poke; quick pokes keep the finger up, sliding to the next spot, and jab again
/// no oftener than <see cref="RestartMs"/>. A spot is along the band, -1 its top and 1 its bottom, and across it, -1 nearest the
/// eyes. Client only. Lengths in world px.
/// </summary>
public sealed class SheetPoke
{
    /// <summary>A jab's length, how soon another may start, how long the finger stays up after the last poke.</summary>
    public const double JabMs = 220, RestartMs = 200, HoldMs = 600;

    /// <summary>How long the arm takes to come up to the band and to go down again.</summary>
    public const double InMs = 120, OutMs = 250;

    /// <summary>How far short of the band the fingertip waits, how far past it a jab goes, the finger's length past the hand's end.</summary>
    public const double Hover = 2.5, Past = 0.4, Finger = 2.5;

    /// <summary>How quickly the finger slides to a new spot: the time it takes to go two thirds of the way.</summary>
    public const double SlideMs = 40;

    private double _wantAlong, _wantAcross, _sinceJab = double.PositiveInfinity, _sincePoke = double.PositiveInfinity;

    /// <summary>The spot the finger is at.</summary>
    public double Along { get; private set; }

    public double Across { get; private set; }

    /// <summary>How far up the arm is, 0 hanging to 1 at the band.</summary>
    public double Weight { get; private set; }

    /// <summary>How far into a jab the finger is, 0 waiting to 1 at its deepest.</summary>
    public double Depth
    {
        get
        {
            if (_sinceJab >= JabMs) return 0;
            double t = _sinceJab / JabMs;
            return t < 0.35 ? t / 0.35 : 1 - (t - 0.35) / 0.65;
        }
    }

    /// <summary>Pokes the band at (<paramref name="along"/>, <paramref name="across"/>), each clamped to -1..1.</summary>
    public void Poke(double along, double across)
    {
        (_wantAlong, _wantAcross) = (Math.Clamp(along, -1, 1), Math.Clamp(across, -1, 1));
        // From hanging the finger goes straight to the spot, not sliding there from the last one.
        if (Weight == 0) (Along, Across) = (_wantAlong, _wantAcross);
        if (_sinceJab >= RestartMs) _sinceJab = 0;
        _sincePoke = 0;
    }

    /// <summary>Plays on by <paramref name="elapsedMs"/>; while the band <paramref name="shows"/> not, the arm goes down at once.</summary>
    public void Step(double elapsedMs, bool shows)
    {
        _sinceJab += elapsedMs;
        _sincePoke += elapsedMs;
        if (!shows)
        {
            (Weight, _sincePoke, _sinceJab) = (0, double.PositiveInfinity, double.PositiveInfinity);
            return;
        }
        bool up = _sincePoke < HoldMs;
        Weight = up ? Math.Min(1, Weight + elapsedMs / InMs) : Math.Max(0, Weight - elapsedMs / OutMs);
        double slide = 1 - Math.Exp(-elapsedMs / SlideMs);
        Along += (_wantAlong - Along) * slide;
        Across += (_wantAcross - Across) * slide;
    }

    /// <summary>Where the hand's end goes for the spot <paramref name="spot"/> from the shoulder: short of it by the finger and the hover, past it in a jab.</summary>
    public (double X, double Y) HandFor((double X, double Y) spot)
    {
        double length = Math.Sqrt(spot.X * spot.X + spot.Y * spot.Y);
        if (length < 1e-6) return spot;
        double back = Finger + Hover * (1 - Depth) - Past * Depth;
        return (spot.X - spot.X / length * back, spot.Y - spot.Y / length * back);
    }
}
