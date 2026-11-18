using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>A thing's upstream chain: what it rests on, down to raw, and the ties between them, each from what goes in to what comes out.</summary>
public sealed record BoardChainOf(IReadOnlyList<string> Ids, IReadOnlyList<(string From, string To)> Ties);

/// <summary>
/// What a thing rests on, down to raw (docs/plans/board-islands.md): a recipe's parts and one machine making it, a smelted item's one source and one machine
/// working it; one that was had or stands, else the shallowest. What the pinned goal shows and the "whole chain" toggle lights.
/// </summary>
public static class BoardChain
{
    /// <summary>The chain under <paramref name="goal"/>, the goal itself left out, nearest first.</summary>
    public static BoardChainOf Of(string goal, BoardRead read)
    {
        var ids = new List<string>();
        var ties = new List<(string, string)>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { goal };
        var next = new Queue<string>([goal]);
        while (next.Count > 0)
        {
            string id = next.Dequeue();
            foreach (string input in Inputs(id, read))
            {
                ties.Add((input, id));
                if (!seen.Add(input)) continue;
                ids.Add(input);
                next.Enqueue(input);
            }
        }
        return new BoardChainOf(ids, ties);
    }

    /// <summary>What of <paramref name="goal"/>'s chain is still missing: an item never had, a machine standing nowhere, a blueprint not known.</summary>
    public static IReadOnlyList<string> Missing(string goal, BoardRead read) => Of(goal, read).Ids.Where(id => !Done(id, read)).ToList();

    /// <summary>Whether <paramref name="id"/> of a chain is in hand: a machine standing, an item had, a blueprint known.</summary>
    public static bool Done(string id, BoardRead read) =>
        BoardThings.Machine(id) ? read.Stands(id) : Content.Items.TryGet(id, out _) ? read.Has(id) : read.Knows(id);

    /// <summary>What <paramref name="id"/> is made of one step up: its parts and a machine, or a source and a machine; only things with a frame.</summary>
    public static IReadOnlyList<string> Inputs(string id, BoardRead read)
    {
        var inputs = new List<string>();
        if (BoardThings.Of(id) is { } thing)
        {
            inputs.AddRange(thing.Parts);
            if (One(thing.Stations ?? [], read.Stands) is { } station) inputs.Add(station);
        }
        else if (One(BoardThings.SourcesOf(id), read.Has) is { } source)
        {
            inputs.Add(source);
            if (One(TechLinks.Of(id).MadeAt, read.Stands) is { } worker) inputs.Add(worker);
        }
        return inputs.Where(i => i != id && BoardDistricts.Of(i) != null).Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>The one of <paramref name="ids"/> on the board <paramref name="had"/> says is in hand, else the shallowest; ties in the order given.</summary>
    private static string? One(IReadOnlyList<string> ids, Func<string, bool> had)
    {
        var onBoard = ids.Where(i => BoardDistricts.Of(i) != null).ToList();
        return onBoard.FirstOrDefault(had) ?? onBoard.OrderBy(BoardLayout.DepthOf).FirstOrDefault();
    }
}
