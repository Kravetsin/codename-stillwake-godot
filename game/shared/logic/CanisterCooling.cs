using System;
using Stillwake.Sim;
using CrewSpec = Stillwake.Content.Crew;

namespace Stillwake.Game.Shared;

/// <summary>
/// A canister's cooldown on the HUD (canister-mark, as picked): how much of it has passed, counted down evenly between reads,
/// and a short glow as it runs out. A cooldown cleared at once, by passing out, glows nothing. The suit gauge's ring and the
/// canister's slots each keep one. Plain C#: read four times a second, stepped each frame.
/// </summary>
public sealed class CanisterCooling
{
    /// <summary>How long the ready glow lasts.</summary>
    public const double ReadyS = 0.8;
    /// <summary>Ticks left a read may lag the count by: a drop to none from no more is the count running out, more is a clear.</summary>
    public const int Slack = 10;

    private double _left, _ready = -1;
    private bool _read;

    /// <summary>A read: the hero's ticks left before another canister, the sim's <c>CanisterIn</c>.</summary>
    public void Read(int ticksLeft)
    {
        if (!_read)
        {
            (_left, _read) = (ticksLeft, true);
            return;
        }
        if (ticksLeft == 0)
        {
            if (_left > 0 && _left <= Slack) _ready = 0;
            _left = 0;
        }
        // The count ran out a moment before the sim did: a read lagging behind it starts nothing again.
        else if (_left > 0 || ticksLeft > Slack) _left = ticksLeft;
    }

    public void Step(double seconds)
    {
        if (_ready >= 0) _ready = _ready + seconds < ReadyS ? _ready + seconds : -1;
        if (_left <= 0) return;
        _left = Math.Max(0, _left - seconds * World.TicksPerSecond);
        if (_left == 0) _ready = 0;
    }

    /// <summary>Whether a canister would be refused now for cooling.</summary>
    public bool Cooling => _left > 0;

    /// <summary>How much of the cooldown has passed, 0 as a canister is breathed to 1 as another may be.</summary>
    public double Passed => Math.Clamp(1 - _left / CrewSpec.CanisterCooldown, 0, 1);

    /// <summary>The ready glow, 1 as the cooldown runs out, easing to 0 over <see cref="ReadyS"/>.</summary>
    public double Ready => _ready < 0 ? 0 : Math.Pow(1 - _ready / ReadyS, 2);
}
