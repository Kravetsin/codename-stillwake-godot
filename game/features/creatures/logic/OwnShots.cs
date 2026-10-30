using System.Collections.Generic;
using Stillwake.Sim.Creatures;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The creatures this player's own shots hit this tick (2026-10-29-kills-crumble): a shot is no strike sent, so its hits are
/// told apart by the sim's, each naming its shooter and the creature it met; heard as this player's own blows, from afar.
/// </summary>
public static class OwnShots
{
    /// <summary>Fills <paramref name="into"/> with the ids of the creatures <paramref name="hits"/> of peer <paramref name="peer"/>'s hero met.</summary>
    public static void Of(IReadOnlyList<ShotHit> hits, int peer, HashSet<int> into)
    {
        into.Clear();
        var mine = Ref.Hero(peer);
        foreach (var hit in hits)
            if (hit.What == ShotTarget.Creature && hit.By == mine) into.Add(hit.Id);
    }
}
