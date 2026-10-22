using System;

namespace Stillwake.Game.Features.Fauna;

/// <summary>
/// How one creature's body is drawn moving (0102), from what the sim decided and when it was struck: plain C#, tested. A hit
/// flashes it white and squashes it flat, springing back past round; it flinches back from the blow and its eye goes wide; for
/// a moment it shakes, never stopped: the sim moves it on, knocked and fleeing. Walking swings its legs by the way it goes, faster as it flees; landing squashes
/// it a little; idle, it breathes and blinks. Times in milliseconds, lengths in pixels of the world.
/// </summary>
public sealed class CreatureMotion
{
    /// <summary>How long a hit's flash, squash, flinch, wide eye and shake last.</summary>
    public const double FlashMs = 150, SquashMs = 380, FlinchMs = 320, StartleMs = 900, ShakeMs = 70;

    /// <summary>The squash at a hit, across and down: wider and flatter, as a ball struck.</summary>
    public const double SquashWide = 0.22, SquashFlat = 0.2;

    /// <summary>How far a flinch tilts the body back from the blow, in radians, and how far a struck body shakes, in pixels.</summary>
    public const double FlinchTilt = 0.38, ShakePixels = 1.2;

    /// <summary>Pixels walked for one swing of the legs, there and back.</summary>
    public const double Stride = 9;

    private double _flash, _squash, _flinch, _startle, _shake, _land, _clock, _blink;
    private double _nextBlink = 2400;

    /// <summary>The way it faces, +1 right: the way it last moved, or toward a hero it watches while still.</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>The way the last blow knocked it, +1 right.</summary>
    public int Away { get; private set; } = 1;

    /// <summary>How far through a stride its legs are, in turns.</summary>
    public double Phase { get; private set; }

    /// <summary>A blow landed, knocking it <paramref name="away"/>, <paramref name="share"/> of a whole hit (a drill's a fifth).</summary>
    public void Hit(int away, double share)
    {
        Away = away == 0 ? Away : Math.Sign(away);
        double whole = Math.Clamp(0.45 + 0.55 * share, 0, 1);
        _flash = Math.Max(_flash, whole);
        _squash = Math.Max(_squash, whole);
        _flinch = Math.Max(_flinch, whole);
        _startle = 1;
        _shake = Math.Max(_shake, ShakeMs * whole);
    }

    /// <summary>
    /// A frame of <paramref name="elapsedMs"/>: it moved <paramref name="dx"/> across (pixels), stands on the ground or not, lands
    /// as <paramref name="landed"/> says, and faces <paramref name="watch"/> while it stands still, a hero nearby, or none.
    /// </summary>
    public void Step(double elapsedMs, double dx, bool grounded, bool landed, int? watch)
    {
        _clock += elapsedMs;
        _flash = Math.Max(0, _flash - elapsedMs / FlashMs);
        _squash = Math.Max(0, _squash - elapsedMs / SquashMs);
        _flinch = Math.Max(0, _flinch - elapsedMs / FlinchMs);
        _startle = Math.Max(0, _startle - elapsedMs / StartleMs);
        _shake = Math.Max(0, _shake - elapsedMs);
        _land = landed ? 1 : Math.Max(0, _land - elapsedMs / 220);
        if (Math.Abs(dx) > 0.05) Facing = Math.Sign(dx);
        else if (watch is int w && w != 0 && _flinch == 0) Facing = w;
        if (grounded) Phase += Math.Abs(dx) / Stride;
        _blink = Math.Max(0, _blink - elapsedMs);
        if (_clock >= _nextBlink)
        {
            _blink = 120;
            _nextBlink = _clock + 2200 + (_clock * 7919) % 2600;
        }
    }

    /// <summary>Whether it shakes from a blow: only its drawing, never its place, which the sim keeps moving.</summary>
    public bool Shaking => _shake > 0;

    /// <summary>How white it is drawn, 0 to 1.</summary>
    public double Flash => _flash * _flash;

    /// <summary>
    /// Its scale across and up: a hit's squash springing back past round (a damped wobble), a landing's softer one, and its
    /// breath while it stands, <paramref name="fleeing"/> stretched long.
    /// </summary>
    public (double X, double Y) Scale(bool fleeing)
    {
        double t = 1 - _squash;
        // A spring: flat at the blow, past round at a third of the way, settling.
        double wobble = _squash > 0 ? Math.Exp(-4 * t) * Math.Cos(t * Math.PI * 2.6) : 0;
        double land = _land * _land * 0.08;
        double breath = 0.015 * Math.Sin(_clock / 520);
        double x = 1 + SquashWide * wobble + land + (fleeing ? 0.08 : 0);
        double y = 1 - SquashFlat * wobble - land + breath - (fleeing ? 0.06 : 0);
        return (x, y);
    }

    /// <summary>Its tilt in the world, radians clockwise: its top thrown the way a blow knocks it, leaning the way it flees.</summary>
    public double Tilt(bool fleeing) => Away * FlinchTilt * _flinch * _flinch + (fleeing ? Facing * 0.12 : 0);

    /// <summary>Its shake after a blow, in pixels across: drawn only.</summary>
    public double Shake => _shake > 0 ? ShakePixels * Math.Sin(_clock * 0.9) * (_shake / ShakeMs) : 0;

    /// <summary>How wide its eye is: 1 at rest, more startled, none blinking.</summary>
    public double Eye => _blink > 0 ? 0.1 : 1 + 0.45 * _startle;

    /// <summary>How flat its ears lie, 0 up to 1 pinned back: back while startled or fleeing.</summary>
    public double Ears(bool fleeing) => Math.Max(fleeing ? 1 : 0, _startle);
}
