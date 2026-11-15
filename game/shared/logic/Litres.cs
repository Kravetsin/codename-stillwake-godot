using System;
using System.Globalization;
using Air = Stillwake.Content.Air;
using Crew = Stillwake.Content.Crew;
using Water = Stillwake.Content.Water;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Shared;

/// <summary>
/// Oxygen and water as a player reads them (industrial-oxygen): a unit of the sim is a millilitre, shown in litres to
/// two decimals, rounded down, 1523 as 1.52 (liquid-volumes). Only the client: the sim keeps its whole units.
/// </summary>
public static class Litres
{
    /// <summary><paramref name="units"/> in litres to the hundredth, rounded down: what is not quite full never reads full.</summary>
    public static string Of(long units) => (units / 10 / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>
    /// The millilitres one of <paramref name="item"/> holds: a canister's oxygen, a bottle's water, 3 litres each
    /// (liquid-volumes); null for an item that holds no liquid.
    /// </summary>
    public static int? In(string item) => item == Crew.Canister ? Air.Canister : item == Water.Item ? Water.Bottle : null;

    /// <summary>
    /// A rate of units every <c>Air.LeakEvery</c> ticks as litres a minute, to a tenth of a millilitre rounded down: a wall
    /// of glass leaks 1.5 units a minute, 0.0015.
    /// </summary>
    public static string PerMinute(long perPeriod)
    {
        long tenths = perPeriod * 60 * World.TicksPerSecond * 10 / Air.LeakEvery;
        return (tenths / 10000m).ToString("0.000#", CultureInfo.InvariantCulture);
    }
}
