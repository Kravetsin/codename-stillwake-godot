using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;

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
    public static ShownGrid Grid(Client client, IGridQueries grid, IPipesQueries pipes)
    {
        // Asked a few times a frame: with no toggle in flight, the tick's grid as last made, nothing built.
        if (!Any<SetDoor, SetValve>(client.InFlight) && _quiet.World == client.World && _quiet.Tick == client.World.Tick
            && Made.TryGetValue(client.World, out var quiet) && quiet == _quiet.Shown)
            return quiet;
        var toggles = client.InFlight.Where(c => c is SetDoor or SetValve).ToList();
        string key = $"{client.World.Tick}:{string.Join(",", toggles.Select(Name))}";
        if (Made.TryGetValue(client.World, out var known) && known.Key == key) return known;
        ShownGrid shown;
        if (toggles.Count == 0) shown = new ShownGrid(key, null, null);
        else
        {
            shown = new ShownGrid(key, grid.AfterToggles(toggles), pipes.AfterToggles(toggles));
        }
        Made.AddOrUpdate(client.World, shown);
        _quiet = toggles.Count == 0 ? (client.World, client.World.Tick, shown) : default;
        return shown;
    }

    /// <summary>The world, tick and grid last made with no toggle in flight.</summary>
    private static (World? World, int Tick, ShownGrid? Shown) _quiet;

    private static bool Any<TA, TB>(IReadOnlyList<Command> commands)
    {
        for (int i = 0; i < commands.Count; i++) if (commands[i] is TA or TB) return true;
        return false;
    }

    private static readonly IReadOnlySet<int> None = new HashSet<int>();

    private static string Name(Command c) => c switch
    {
        SetDoor d => $"door@{d.Cell}={d.Open}",
        SetValve v => $"valve@{v.Cell}={v.Open}",
        _ => "",
    };

    /// <summary>The pieces of debris this peer has a grab in flight for: gone from its view, and passed over by its aim, at once.</summary>
    public static IReadOnlySet<int> Grabbing(IReadOnlyList<Command> inFlight) =>
        Any<Grab, Grab>(inFlight) ? inFlight.OfType<Grab>().Select(g => g.Piece).ToHashSet() : None;
}

/// <summary>
/// The grid's doors and valves as this peer shows them (<see cref="Predicted.Grid"/>). <see cref="Key"/>
/// changes when they do. <see cref="Structure"/> holds the codes of the cells its toggles in flight move and
/// <see cref="Valves"/> whether the valves they turn are open, the rest as the world has them; both are null while
/// nothing is in flight.
/// </summary>
public sealed record ShownGrid(string Key, IReadOnlyDictionary<int, int>? Structure, IReadOnlyDictionary<int, bool>? Valves)
{
    /// <summary>The structure code shown at <paramref name="cell"/>: the world's but where a toggle in flight moved it; empty off the grid.</summary>
    public int StructureAt(IGridQueries grid, int cell) =>
        Structure != null && Structure.TryGetValue(cell, out int code) ? code : grid.StructureAt(cell);

    /// <summary>Whether the valve at <paramref name="cell"/> shows open: the world's but where a toggle in flight turned it; null for no valve.</summary>
    public bool? ValveOpenAt(IPipesQueries pipes, int cell) =>
        pipes.ValveAt(cell) is not { } valve ? null : Valves != null && Valves.TryGetValue(cell, out bool open) ? open : valve.Open;
}
