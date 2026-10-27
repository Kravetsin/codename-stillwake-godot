using Stillwake.Sim.Creatures;

namespace Stillwake.Game.Shared;

/// <summary>
/// Which drawing of a shot a peer shows (shots-are-the-sims, 2026-10-27-shots-pierce): the host's own shots are the sim's, flown on
/// the tick its <c>Fire</c> applies, the next, so a copy of them would only be stepped on a world its own hits already changed; a
/// guest flies a copy of its own, a round trip ahead of the sim's, and the sim's of them are never drawn there.
/// </summary>
public static class ShotDraws
{
    /// <summary>Whether this peer, hosting or not (<paramref name="hosts"/>), flies copies of its own shots: a guest only.</summary>
    public static bool FliesOwn(bool hosts) => !hosts;

    /// <summary>Whether the sim's shot fired by <paramref name="by"/>, and its hits, are drawn by peer <paramref name="own"/>.</summary>
    public static bool DrawsSims(Ref by, int own, bool hosts) => hosts || by != Ref.Hero(own);
}
