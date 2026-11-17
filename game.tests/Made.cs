using Stillwake.Sim;

namespace Stillwake.Game.Tests;

/// <summary>A described vessel as built (vessel-wrecks): its class and seed through the shipyard, intact while no contact is a wreck.</summary>
internal static class Made
{
    public static Built Of(Vessel v) => Shipyard.Build(Content.Vessels.Class(v.Class)!, v.Seed);
}
