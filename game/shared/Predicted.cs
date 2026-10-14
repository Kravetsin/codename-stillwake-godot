using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Shared;

/// <summary>This peer's own actions shown before their tick runs (session/predicted.ts, 0014).</summary>
public static class Predicted
{
    /// <summary>The pieces of debris this peer has a grab in flight for: gone from its view, and passed over by its aim, at once.</summary>
    public static IReadOnlySet<int> Grabbing(IReadOnlyList<Command> inFlight) =>
        inFlight.OfType<Grab>().Select(g => g.Piece).ToHashSet();
}
