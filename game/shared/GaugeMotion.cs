using System;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Shared;

/// <summary>
/// How a wrist gauge shows a value it reads four times a second (0081), as a share of full: never in steps. A small
/// change slides at an even pace (<see cref="Look.SlideS"/>), so a steady drain moves steadily and leaves no trail. A drop
/// of <see cref="Look.BigDrop"/> or more between two reads is a hit: the fill falls at once, the lost part stays and
/// flashes, then drains. A big refill eases out. Plain C#: stepped by its gauge each frame.
/// </summary>
public sealed class GaugeMotion
{
    private double _target, _velocity, _hold;
    private bool _read, _easing, _trailing;

    /// <summary>The share filled, as shown now.</summary>
    public double Fill { get; private set; }

    /// <summary>The lost part's far edge after a hit, as shown now; <see cref="Fill"/> while there is none. What a readout counts.</summary>
    public double Trail { get; private set; }

    /// <summary>A hit's flash: 1 as it lands, fading to 0 over <see cref="Look.FlashS"/>.</summary>
    public double Flash { get; private set; }

    /// <summary>The refill's light: 1 while the fill rises, fading to 0 over <see cref="Look.GainS"/> after.</summary>
    public double Gain { get; private set; }

    /// <summary>A read of the sim: <paramref name="share"/> of full. The first is shown at once.</summary>
    public void Read(double share)
    {
        share = Math.Clamp(share, 0, 1);
        if (!_read)
        {
            _read = true;
            Fill = Trail = _target = share;
            return;
        }
        double drop = _target - share;
        // The same read again keeps the slide's pace: it arrives as the sim's next breath does.
        if (drop == 0) return;
        _target = share;
        if (drop >= Look.BigDrop)
        {
            Trail = _trailing ? Math.Max(Trail, Fill) : Fill;
            Fill = share;
            (_velocity, _hold, _easing, _trailing, Flash) = (0, Look.TrailHoldS, false, true, 1);
            return;
        }
        _easing = share - Fill >= Look.BigDrop;
        _velocity = Math.Abs(share - Fill) / Look.SlideS;
    }

    /// <summary>Moves what is shown on by <paramref name="seconds"/>.</summary>
    public void Step(double seconds)
    {
        if (Fill < _target)
        {
            double step = _easing ? Math.Max((_target - Fill) * (1 - Math.Exp(-seconds * Look.RiseEase)), seconds * Look.RiseFloor) : _velocity * seconds;
            Fill = Math.Min(_target, Fill + step);
            Gain = 1;
        }
        else
        {
            if (Fill > _target) Fill = Math.Max(_target, Fill - _velocity * seconds);
            Gain = Math.Max(0, Gain - seconds / Look.GainS);
        }
        Flash = Math.Max(0, Flash - seconds / Look.FlashS);
        if (!_trailing)
        {
            Trail = Fill;
            return;
        }
        if (_hold > 0) _hold -= seconds;
        else
        {
            double gap = Trail - Fill;
            Trail -= Math.Min(gap, seconds * (Look.TrailDrain + Look.TrailDrainPerGap * gap));
        }
        if (Trail > Fill) return;
        Trail = Fill;
        _trailing = false;
    }
}
