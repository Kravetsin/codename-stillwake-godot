using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Codes = Stillwake.Sim.Grid.Structure;
using Conduits = Stillwake.Sim.Grid.Conduit;

namespace Stillwake.Game.Shared;

/// <summary>This peer's own actions shown before their tick runs (session/predicted.ts, 0014).</summary>
public static class Predicted
{
    // One per world: a resync or a load brings another.
    private static readonly ConditionalWeakTable<World, ShownGrid> Made = new();

    /// <summary>
    /// The grid <paramref name="grid"/> of <paramref name="client"/>'s world as this peer shows it
    /// (<c>predictedGrid</c>): its own door and valve toggles still in flight already made, so a door
    /// opens on the frame E is pressed rather than a round trip later.
    /// </summary>
    public static ShownGrid Grid(Client client, IGridQueries grid)
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

    /// <summary>The pieces of debris this peer has a grab in flight for: gone from its view, and passed over by its aim, at once.</summary>
    public static IReadOnlySet<int> Grabbing(IReadOnlyList<Command> inFlight) =>
        inFlight.OfType<Grab>().Select(g => g.Piece).ToHashSet();
}

/// <summary>
/// The grid's doors and valves as this peer shows them (<see cref="Predicted.Grid"/>). <see cref="Key"/>
/// changes when they do. The layers are null while nothing is in flight: then the world's own serve, uncopied.
/// </summary>
public sealed record ShownGrid(string Key, int[]? Structure, int[]? Conduit)
{
    /// <summary>The structure code shown at <paramref name="cell"/>: the world's when nothing is in flight; empty off the grid.</summary>
    public int StructureAt(IGridQueries grid, int cell) =>
        Structure is { } s ? (cell >= 0 && cell < s.Length ? s[cell] : Codes.Empty) : grid.StructureAt(cell);

    /// <summary>The conduit code shown at <paramref name="cell"/>: the world's when nothing is in flight; none off the grid.</summary>
    public int ConduitAt(IGridQueries grid, int cell) =>
        Conduit is { } c ? (cell >= 0 && cell < c.Length ? c[cell] : Conduits.None) : grid.ConduitAt(cell);
}
