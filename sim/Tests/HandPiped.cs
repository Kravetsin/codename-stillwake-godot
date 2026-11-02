namespace Stillwake.Sim.Tests;

/// <summary>
/// The sealed rooms the crew's, suits' and hazards' tests draw, a vent over a tank they pipe by hand, made the liquid-oxygen
/// chain. <see cref="AirTesting.Vaporized"/> wants the tank's pipe drawn rightwards; these lay the vent's gas pipe by hand.
/// </summary>
internal static class HandPiped
{
    /// <summary>
    /// <paramref name="map"/>, drawn a character a 16 px cell, its one tank <c>TT</c> made a vaporizer <c>GG</c>, the tank's id
    /// kept; under the hull, past a row of space, an empty tank and a solar panel cabled to the vaporizer, ids after every
    /// other, the liquid drawn up through the floor from the tank's port to the vaporizer's input. Doubled as the map's own.
    /// </summary>
    public static Layout Chained(string map)
    {
        var drawn = Layout.Parse(map);
        int height = drawn.Height, ty = drawn.Rows.ToList().FindIndex(r => r.Contains('T')), tx = drawn.Rows[ty].IndexOf('T');
        string At(string chars) => new string(' ', tx) + chars;
        var rows = drawn.Rows.Select(r => r.Replace('T', 'G')).Concat(["", At("TTPPP"), At("TT")]).ToArray();
        // From the vaporizer's bottom row, its input's, down to the tank's, its port's.
        var under = Enumerable.Range(0, height + 3).Select(y => y > ty ? At("-") : "").ToArray();
        return Layout.Parse(rows, under, [((tx + 2, height + 1), (tx, ty))]);
    }

    /// <summary>The oxygen the chain holds: the tank's liquid and the vaporizer's gas, which only draws and leaks lessen.</summary>
    public static int Held(Scenario w) => w.M.Grid.Machines.Sum(m => w.M.Atmosphere.OxygenIn(m.Id));
}
