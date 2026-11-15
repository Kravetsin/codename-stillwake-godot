namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A share from 0 to 1 that follows its target as the arm's hand-over does (<see cref="SwingArm"/>): a new target eases in from
/// wherever the share is, in and out over the hand-over's time, so what turns with the arm (an aimed item laid across the fist,
/// <see cref="HeldLook.Rest"/>) starts and stops with it.
/// </summary>
public struct HandOver
{
    private double _from, _to, _sinceMs, _ms;

    /// <summary>The share now.</summary>
    public double Share { get; private set; }

    /// <summary>The share <paramref name="elapsedMs"/> after the last, heading for <paramref name="to"/>; a new target eases over <paramref name="ms"/>.</summary>
    public double Next(double to, double elapsedMs, double ms)
    {
        if (to != _to) (_from, _to, _sinceMs, _ms) = (Share, to, 0, ms);
        else _sinceMs += elapsedMs;
        Share = _ms > 0 ? _to + (_from - _to) * SwingArm.Left(_sinceMs, _ms) : _to;
        return Share;
    }
}
