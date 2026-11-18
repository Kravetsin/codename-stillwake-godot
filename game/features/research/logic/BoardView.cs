using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Research;

/// <summary>How a frame on the board shows (docs/plans/board-islands.md).</summary>
public enum BoardState
{
    /// <summary>A dim «?»: nothing of it had yet.</summary>
    Unknown,
    /// <summary>A silhouette with a progress bar and what it still needs.</summary>
    Almost,
    /// <summary>Its picture and its name: obtained or granted.</summary>
    Known,
}

/// <summary>What the board reads of the world.</summary>
public sealed class BoardRead
{
    private readonly HashSet<string> _known;

    /// <param name="found">What was obtained, in the order first obtained.</param>
    /// <param name="granted">The blueprints and crafts known.</param>
    /// <param name="has">Whether an item was ever obtained.</param>
    /// <param name="stands">Whether a machine of a kind stands on any surface.</param>
    public BoardRead(IReadOnlyList<string> found, IReadOnlyCollection<string> granted, Func<string, bool> has, Func<string, bool> stands)
    {
        (Found, Granted, Has, Stands) = (found, granted, has, stands);
        _known = [.. found, .. granted];
    }

    /// <summary>What was obtained, in the order first obtained.</summary>
    public IReadOnlyList<string> Found { get; }

    public IReadOnlyCollection<string> Granted { get; }

    public Func<string, bool> Has { get; }

    public Func<string, bool> Stands { get; }

    /// <summary>Whether <paramref name="id"/> is known: obtained or granted.</summary>
    public bool Knows(string id) => _known.Contains(id);

    /// <summary>
    /// What the board reads of <paramref name="world"/>: what is obtained and granted, and a machine standing on any of its surfaces, as research grants by
    /// the machines of the surface it stands on.
    /// </summary>
    public static BoardRead Of(World world)
    {
        var surfaces = Open(world).Select(s => Registry.On(world, s)).ToList();
        var knowledge = surfaces[0].Knowledge;
        return new BoardRead(knowledge.Obtained.Select(t => t.Item).ToList(), knowledge.GrantedIds, item => knowledge.ObtainedOf(item) > 0,
            kind => surfaces.Any(s => s.Grid.MachinesOf(kind).Count > 0));
    }

    /// <summary>What moves when anything the board reads of <paramref name="world"/> may have: its knowledge's version and every surface's grid's.</summary>
    public static string KeyOf(World world)
    {
        var knowledge = Registry.On(world, 0).Knowledge;
        return $"{world.Id}|{knowledge.GetHashCode()}|{knowledge.Version}|" + string.Join(',', Open(world).Select(s => $"{s}:{Registry.On(world, s).Grid.Version}"));
    }

    // A closed surface (a planet left behind) runs no modules.
    private static IEnumerable<int> Open(World world) => world.Surfaces.Where(s => !s.Closed).Select(s => s.Id);
}

/// <summary>Decides how each frame shows (docs/plans/board-islands.md): known if obtained or granted, almost if its pencil has started, else unknown.</summary>
public static class BoardView
{
    public static BoardState StateOf(string id, BoardRead read) =>
        read.Knows(id) ? BoardState.Known : PencilOf(id, read) > 0 ? BoardState.Almost : BoardState.Unknown;

    /// <summary>
    /// How far <paramref name="id"/> has come, 0 to 1: a recipe's parts had and a machine making it standing (<see cref="BoardPencil"/>); a smelted item's source
    /// had and a machine working it standing. Never anything of a market good, which its parts never bring nearer, nor of an item only found.
    /// </summary>
    public static double PencilOf(string id, BoardRead read)
    {
        if (BoardThings.Market(id)) return 0;
        if (BoardThings.Of(id) is { } thing) return BoardPencil.Of(thing, read.Has, read.Stands);
        if (BoardThings.SourcesOf(id) is not { Count: > 0 } sources) return 0;
        var workers = TechLinks.Of(id).MadeAt;
        return BoardPencil.Of([sources.Any(read.Has)], workers.Count == 0 ? null : workers.Any(read.Stands));
    }

    /// <summary>
    /// Everything <paramref name="id"/> still needs: each part never had and a machine to make it if none stands; of a smelted item one source if none
    /// was had and a machine to work it. Empty when nothing is missing and it only waits to be made.
    /// </summary>
    public static IReadOnlyList<string> MissingOf(string id, BoardRead read)
    {
        var missing = new List<string>();
        if (BoardThings.Of(id) is { } thing)
        {
            missing.AddRange(thing.Parts.Where(p => !read.Has(p)));
            if (thing.Stations is { Count: > 0 } stations && !stations.Any(read.Stands)) missing.Add(stations[0]);
            return missing;
        }
        var sources = BoardThings.SourcesOf(id);
        if (sources.Count > 0 && !sources.Any(read.Has)) missing.Add(sources[0]);
        var workers = TechLinks.Of(id).MadeAt;
        if (workers.Count > 0 && !workers.Any(read.Stands)) missing.Add(workers[0]);
        return missing;
    }

    /// <summary>How many of <paramref name="id"/>'s parts or sources are had, of how many, for its progress bar: a source had counts as all of one.</summary>
    public static (int Had, int Of) PartsOf(string id, BoardRead read)
    {
        if (BoardThings.Of(id) is { Parts.Count: > 0 } thing) return (thing.Parts.Count(read.Has), thing.Parts.Count);
        var sources = BoardThings.SourcesOf(id);
        return sources.Count > 0 ? (sources.Any(read.Has) ? 1 : 0, 1) : (0, 1);
    }
}
