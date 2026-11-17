using Stillwake.Sim;

namespace Stillwake.Game.Tests;

/// <summary>A described vessel as built (vessel-wrecks): its class and seed through the shipyard, its chain forced empty, so intact.</summary>
internal static class Made
{
    public static Built Of(Vessel v) => Shipyard.Build(Content.Vessels.Class(v.Class)!, v.Seed, []);
}
