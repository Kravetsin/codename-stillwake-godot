using CrewSpec = Stillwake.Content.Crew;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Features.Crew;

/// <summary>What a read of the suit set off: breathing room air again, and the danger letting go.</summary>
public readonly record struct AirBack(bool BreathesAgain, bool LetsGo);

/// <summary>
/// The moments the suit's reads show air coming back, each once (0081): <see cref="AirBack.BreathesAgain"/> as the hero
/// comes out of thin air or vacuum into room air with a suit to refill, at most once in <see cref="Look.AgainGapS"/> so a
/// hero stepping to and fro over a door's sill is not waved at each step; <see cref="AirBack.LetsGo"/> as a low suit stops
/// being in danger without passing out, by a canister or by room air. Plain C#: the suit gauge reads it.
/// </summary>
public sealed class AirCues
{
    private bool _read, _inAir;
    private SuitDanger _danger;
    private double _sinceAgain = double.MaxValue;

    /// <summary>
    /// A read: the suit's units, its danger and whether the hero breathes room air (or the pod's), <paramref name="seconds"/>
    /// after the last. The first read only learns where the hero is.
    /// </summary>
    public AirBack Read(int suit, SuitDanger danger, bool inAir, double seconds)
    {
        _sinceAgain += seconds;
        bool again = _read && inAir && !_inAir && suit < CrewSpec.Suit && danger != SuitDanger.Out && _sinceAgain >= Look.AgainGapS;
        bool letsGo = _read && _danger == SuitDanger.Low && danger == SuitDanger.None;
        if (again) _sinceAgain = 0;
        (_read, _inAir, _danger) = (true, inAir, danger);
        return new AirBack(again, letsGo);
    }
}
