using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How far the build tablet has slid up from the bottom edge and how far its screen has come on, frame by frame
/// (0073). B lifts it in <see cref="SlideIn"/>, easing out, and puts it away in <see cref="SlideOut"/>, easing in; turned
/// back halfway it goes on from where it is. Its screen fades in gently after <see cref="PowerDelay"/> over
/// <see cref="PowerFade"/>, never brighter than lit; taken out again within <see cref="Warm"/> of being put away, it
/// is lit at once, so B pressed often never blinks. Put away, the screen stays as it was until the tablet is gone.
/// </summary>
public sealed class TabletMotion
{
    /// <summary>Seconds to slide up, and to slide back down.</summary>
    public const double SlideIn = 0.13, SlideOut = 0.10;

    /// <summary>Seconds after it is taken out before the screen starts to come on, and how long it takes.</summary>
    public const double PowerDelay = 0.06, PowerFade = 0.08;

    /// <summary>Taken out again within this many seconds of being put away, the screen is still on.</summary>
    public const double Warm = 1.5;

    private bool _open, _warm;
    private double _openFor, _away = double.PositiveInfinity;

    /// <summary>How far up it is: 0 below the screen's edge, 1 docked.</summary>
    public double Up { get; private set; }

    /// <summary>How far the screen is on: 0 dark, 1 lit.</summary>
    public double Power { get; private set; } = 1;

    /// <summary>Whether any of it is on the screen: open, or still sliding away.</summary>
    public bool Shown => Up > 0;

    /// <summary>One frame of <paramref name="seconds"/>, the plan out or not.</summary>
    public void Step(bool open, double seconds)
    {
        if (open && !_open)
        {
            _warm = _away < Warm;
            _openFor = 0;
        }
        if (!open && _open) _away = 0;
        _open = open;
        if (open)
        {
            _openFor += seconds;
            // Along an ease-out from wherever it is now: the time at which the curve stands at Up, moved on.
            double t = Math.Min(1, 1 - Math.Cbrt(1 - Up) + seconds / SlideIn);
            Up = 1 - (1 - t) * (1 - t) * (1 - t);
            Power = _warm ? 1 : Math.Clamp((_openFor - PowerDelay) / PowerFade, 0, 1);
        }
        else
        {
            _away += seconds;
            double t = Math.Min(1, Math.Cbrt(1 - Up) + seconds / SlideOut);
            Up = 1 - t * t * t;
        }
    }
}
