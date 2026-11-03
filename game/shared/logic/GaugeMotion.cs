using System;
using System.Collections.Generic;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Shared;

/// <summary>
/// How a wrist gauge shows a value it reads four times a second (0081, the loss-shows-white ADR), as a share of full. A
/// loss falls at once and leaves its part lit white: it stays whole for <see cref="Look.TrailHoldS"/>, then drains, so a
/// steady drain trails a white band. A drop of <see cref="Look.BigDrop"/> or more between two reads is a
/// hit and flashes too. A rise slides at an even pace, a big one eases out. Plain C#: stepped by its gauge each frame.
/// </summary>
public sealed class GaugeMotion
{
    private double _target, _velocity, _clock;
    private bool _read, _easing;
    // What was shown over the last hold, oldest first: the lost part drains only down to the most of it.
    private readonly Queue<(double At, double Fill)> _shown = new();

    /// <summary>The share filled, as shown now.</summary>
    public double Fill { get; private set; }

    /// <summary>The lost part's far edge, as shown now; <see cref="Fill"/> while there is none. What the energy tab's number counts.</summary>
    public double Trail { get; private set; }

    /// <summary>A hit's flash: 1 as it lands, fading to 0 over <see cref="Look.FlashS"/>.</summary>
    public double Flash { get; private set; }

    /// <summary>The refill's light: 1 while the fill rises, fading to 0 over <see cref="Look.GainS"/> after.</summary>
    public double Gain { get; private set; }

    /// <summary>
    /// A read of the sim: <paramref name="share"/> of full. The first is shown at once. A drop of <paramref name="hit"/> or
    /// more is a hit, <see cref="Look.BigDrop"/> unless the reader's steady steps are as big.
    /// </summary>
    public void Read(double share, double hit = Look.BigDrop)
    {
        share = Math.Clamp(share, 0, 1);
        if (!_read)
        {
            _read = true;
            Fill = Trail = _target = share;
            return;
        }
        double drop = _target - share;
        if (drop == 0) return;
        _target = share;
        if (drop > 0)
        {
            // A rise still under way just stops lower; else what was shown is held from now and the fill falls to the read.
            if (Fill <= share) return;
            if (drop >= hit) Flash = 1;
            _shown.Enqueue((_clock, Fill));
            (Fill, _velocity, _easing) = (share, 0, false);
            return;
        }
        _easing = share - Fill >= Look.BigDrop;
        _velocity = (share - Fill) / Look.SlideS;
    }

    /// <summary>A share that changed with what is measured, not by a loss: a helmet put on or taken off. Shown at once, with nothing lost.</summary>
    public void Show(double share)
    {
        _read = true;
        Fill = Trail = _target = Math.Clamp(share, 0, 1);
        (_velocity, _easing, Flash) = (0, false, 0);
        _shown.Clear();
    }

    /// <summary>Moves what is shown on by <paramref name="seconds"/>.</summary>
    public void Step(double seconds)
    {
        _clock += seconds;
        if (Fill < _target)
        {
            double step = _easing ? Math.Max((_target - Fill) * (1 - Math.Exp(-seconds * Look.RiseEase)), seconds * Look.RiseFloor) : _velocity * seconds;
            Fill = Math.Min(_target, Fill + step);
            Gain = 1;
        }
        else Gain = Math.Max(0, Gain - seconds / Look.GainS);
        Flash = Math.Max(0, Flash - seconds / Look.FlashS);
        while (_shown.Count > 0 && _shown.Peek().At <= _clock - Look.TrailHoldS) _shown.Dequeue();
        double held = Fill;
        foreach (var (_, fill) in _shown) held = Math.Max(held, fill);
        if (Trail > held) Trail = Math.Max(held, Trail - seconds * (Look.TrailDrain + Look.TrailDrainPerGap * (Trail - Fill)));
        Trail = Math.Max(Trail, Fill);
    }
}
