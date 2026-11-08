using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The moment the suit's reads show room air breathed again, once (0081): as the hero comes out of thin air or vacuum into
/// room air with a suit to refill, at most once in <see cref="Look.AgainGapS"/> so a hero stepping to and fro over a door's
/// sill is not waved at each step. Plain C#: the suit gauge reads it.
/// </summary>
public sealed class AirCues
{
    private bool _read, _inAir;
    private double _sinceAgain = double.MaxValue;

    /// <summary>
    /// A read: the suit's units, its danger and whether the hero breathes room air (or the pod's), <paramref name="seconds"/>
    /// after the last; whether it breathes again. The first read only learns where the hero is.
    /// </summary>
    public bool Read(int suit, int capacity, SuitDanger danger, bool inAir, double seconds)
    {
        _sinceAgain += seconds;
        bool again = _read && inAir && !_inAir && suit < capacity && danger != SuitDanger.Out && _sinceAgain >= Look.AgainGapS;
        if (again) _sinceAgain = 0;
        (_read, _inAir) = (true, inAir);
        return again;
    }
}
