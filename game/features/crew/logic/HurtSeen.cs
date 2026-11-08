using Stillwake.Sim.Crew;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A hero's hurts as the client sees them (2026-11-08-hurts-are-shown): the sim counts each with its kind, and a count risen
/// since the last read is a hurt to show. The first read, and a count that fell (a world loaded, a hero made anew), show nothing.
/// </summary>
public sealed class HurtSeen
{
    private int? _hurts;

    /// <summary>The kind of the newest hurt if the hero has taken one since the last read; none with no hero.</summary>
    public Harm? Read((int Hurts, Harm Hurt)? hero)
    {
        if (hero is not var (hurts, hurt))
        {
            _hurts = null;
            return null;
        }
        bool more = _hurts is int was && hurts > was;
        _hurts = hurts;
        return more ? hurt : null;
    }
}
