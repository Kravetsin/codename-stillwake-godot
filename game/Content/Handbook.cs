using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game;

/// <summary>A row of the Handbook's debris: where to look for its kinds, named by <c>handbook.row.&lt;Id&gt;</c>.</summary>
public sealed record DebrisRow(string Id, IReadOnlyList<string> Kinds);

/// <summary>
/// How the Handbook lays things out (0066): presentation only, the sim never reads it. Every kind of debris that
/// spawns in some zone (<c>Content.Debris.Zones</c>) is in exactly one row (<c>HandbookTests</c>).
/// </summary>
public static class Handbook
{
    /// <summary>The kinds of debris, a row by where to look for them, in the order shown.</summary>
    public static readonly IReadOnlyList<DebrisRow> DebrisRows =
    [
        new("salvage", ["scrapChunk", "hullPlate", "cargoCrate", "supplyPod"]),
        new("wrecks", ["hullSection", "cargoContainer", "machineBlock", "cableRack", "tankCluster", "shieldSlab", "soilClod"]),
        new("asteroids", ["asteroidSmall", "asteroid", "asteroidLarge", "asteroidRich"]),
        new("derelicts", ["engineBlock", "cargoHold", "podCluster"]),
    ];

    /// <summary>The row <paramref name="kind"/> is in, or null for a kind in none.</summary>
    public static DebrisRow? RowOf(string kind)
    {
        foreach (var row in DebrisRows)
            if (row.Kinds.Contains(kind)) return row;
        return null;
    }
}
