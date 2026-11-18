namespace Stillwake.Game.Features.Crew;

/// <summary>
/// When the drill hits (0054), on the world's ticks: the tick it starts running, then every <c>Pace</c> ticks
/// while it runs, so a peer drawing frames slower than the ticks still hits at the drill's pace. A stall owes
/// no burst: once a whole <c>Pace</c> more has gone by, it starts afresh; a tick gone back (a load) too.
/// </summary>
public struct DrillPace
{
    private int? _last;

    /// <summary>
    /// A run tool's <paramref name="pace"/> in ticks as the body works it at <paramref name="work"/> thousandths
    /// (<c>BodyHeat.SpeedOf</c>, body-temperature): pace × 1000 / work, rounded up.
    /// </summary>
    public static int Slowed(int pace, int work) => (pace * 1000 + work - 1) / work;

    /// <summary>Whether the drill hits on <paramref name="tick"/>, running or not as <paramref name="runs"/> says, a hit due every <paramref name="pace"/> ticks.</summary>
    public bool Due(int tick, bool runs, int pace)
    {
        if (!runs)
        {
            _last = null;
            return false;
        }
        if (_last is int last && tick >= last && tick - last < pace) return false;
        _last = _last is int was && tick >= was && tick - was < 2 * pace ? was + pace : tick;
        return true;
    }
}
