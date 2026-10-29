using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.I18n;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// A wall's numbers on its build card (2026-10-29-meteor-wave): the oxygen it lets out a minute through each cell, and its strength
/// against meteors with, for a wall where meteors fall, the share of them that break it.
/// </summary>
public static class WallStats
{
    /// <summary>
    /// The lines for a wall (<paramref name="back"/> false) or back wall of <paramref name="material"/>, under the meteors of
    /// <paramref name="shower"/>; none for a place without meteors but the leak. A back wall is never struck, only blasted, so it
    /// shows its strength alone.
    /// </summary>
    public static IReadOnlyList<string> Of(Material material, bool back, Meteors.Shower? shower)
    {
        var lines = new List<string> { En.T("wall.leak", ("rate", LeakAMinute(material))) };
        if (shower == null) return lines;
        if (material.Armour == Material.Unbreakable) lines.Add(En.T("wall.meteorsNever"));
        else if (back) lines.Add(En.T("wall.resists", ("strength", material.Armour)));
        else lines.Add(En.T("wall.meteors", ("strength", material.Armour), ("share", BreakingShare(material.Armour, shower))));
        return lines;
    }

    /// <summary>The oxygen a cell of <paramref name="material"/> lets out a minute, to a tenth: its leak is counted every <c>Air.LeakEvery</c> ticks.</summary>
    public static string LeakAMinute(Material material) =>
        (Math.Round((double)material.Leak * 60 * Sim.World.TicksPerSecond / Air.LeakEvery, 1)).ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>The share of <paramref name="shower"/>'s meteors, in whole percent, strong enough to break a wall of <paramref name="strength"/>.</summary>
    public static int BreakingShare(int strength, Meteors.Shower shower)
    {
        int total = shower.Powers.Sum(p => p.Weight), breaking = shower.Powers.Where(p => p.Power >= strength).Sum(p => p.Weight);
        return (int)Math.Round(100.0 * breaking / total);
    }
}
