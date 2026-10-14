using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Codes = Stillwake.Sim.Grid.Structure;

namespace Stillwake.Game.Shared;

/// <summary>
/// The grid's doors and valves as this peer shows them (<c>predictedGrid</c> of session/predicted.ts):
/// the world with its own toggles still in flight applied, so a door opens on the frame E is pressed
/// rather than a round trip later (0014). <see cref="Key"/> changes when they do. The layers are null
/// while nothing is in flight: then the world's own serve, uncopied.
/// </summary>
public sealed record ShownGrid(string Key, int[]? Structure, int[]? Conduit)
{
    // One per world: a resync or a load brings another.
    private static readonly ConditionalWeakTable<World, ShownGrid> Made = new();

    /// <summary>The grid <paramref name="grid"/> of <paramref name="client"/>'s world as this peer shows it.</summary>
    public static ShownGrid Of(Client client, IGridQueries grid)
    {
        var toggles = client.InFlight.Where(c => c is ToggleDoor or ToggleValve).ToList();
        string key = $"{client.World.Tick}:{string.Join(",", toggles.Select(Name))}";
        if (Made.TryGetValue(client.World, out var known) && known.Key == key) return known;
        ShownGrid shown;
        if (toggles.Count == 0) shown = new ShownGrid(key, null, null);
        else
        {
            var (structure, conduit) = grid.AfterToggles(toggles);
            shown = new ShownGrid(key, structure, conduit);
        }
        Made.AddOrUpdate(client.World, shown);
        return shown;
    }

    private static string Name(Command c) => c switch
    {
        ToggleDoor d => $"door@{d.Cell}",
        ToggleValve v => $"valve@{v.Cell}",
        _ => "",
    };

    /// <summary>The structure code shown at <paramref name="cell"/>: the world's when nothing is in flight; empty off the grid.</summary>
    public int StructureAt(IGridQueries grid, int cell) =>
        Structure is { } s ? (cell >= 0 && cell < s.Length ? s[cell] : Codes.Empty) : grid.StructureAt(cell);
}
