using System;
using System.Globalization;
using Air = Stillwake.Content.Air;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Shared;

/// <summary>
/// Oxygen and water as a player reads them (industrial-oxygen): a unit of the sim is a millilitre, shown in litres to
/// three decimals, 1523 as 1.523. Only the client: the sim keeps its whole units.
/// </summary>
public static class Litres
{
    /// <summary><paramref name="units"/> in litres, to the millilitre.</summary>
    public static string Of(long units) => (units / 1000m).ToString("0.000", CultureInfo.InvariantCulture);

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
