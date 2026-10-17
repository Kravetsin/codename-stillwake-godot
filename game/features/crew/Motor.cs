using Stillwake.Sim;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// An electric tool in the hand (0055): it runs while the left button holds it, touching anything or nothing,
/// and the sim draws its power for every tick it runs. What this client wants of it, and the <c>Run</c> that
/// tells the sim, sent while the two differ as <c>Thrust</c> is.
/// </summary>
public static class Motor
{
    /// <summary>
    /// The electric item this hero runs: <paramref name="held"/>, what the hands can use now, when it has a
    /// <c>Draw</c>, while the left button <paramref name="holdsLeft"/> (not a hold begun by a grab,
    /// <paramref name="grabHeld"/>) and while peer <paramref name="peer"/>'s belt can pay a tick of its draw, as the
    /// sim has it (always in creative mode). None once let go, put away or flat.
    /// </summary>
    public static string? Wanted(IItemsQueries items, int peer, string? held, bool holdsLeft, bool grabHeld)
    {
        if (held == null || !holdsLeft || grabHeld || Content.Items.Get(held).Draw is not int draw) return null;
        return items.CanDrain(peer, draw) ? held : null;
    }

    /// <summary>
    /// <c>Run</c> when what the sim says peer <paramref name="peer"/> runs differs from <paramref name="wanted"/>:
    /// on for what it wants, off for what the sim runs; null while they agree. Sent again each frame until the
    /// host releases it.
    /// </summary>
    public static Command? RunOf(IItemsQueries items, int peer, string? wanted)
    {
        var running = items.Running(peer);
        if (running == wanted) return null;
        return wanted != null ? new Run(wanted, true) : new Run(running!, false);
    }
}
