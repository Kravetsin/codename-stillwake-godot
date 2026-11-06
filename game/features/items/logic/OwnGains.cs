using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// This peer's new gains in a surface's items journal (<see cref="ItemsModule.Gains"/>, 2026-11-06-head-tally), each once. Every
/// surface has its own module, its sequence from 1, so a mark is kept per module: back on the ship, nothing old shows again; a
/// module first seen starts at what it holds, a new one after a resync (emptied by its load) from nothing.
/// </summary>
public sealed class OwnGains
{
    private sealed class Mark
    {
        public long Seq;
    }

    private readonly ConditionalWeakTable<object, Mark> _marks = new();
    private readonly List<Gain> _new = [];

    /// <summary>The gains for <paramref name="peer"/> in <paramref name="module"/>'s <paramref name="gains"/> since the last read of it, oldest first; live until the next read.</summary>
    public IReadOnlyList<Gain> Read(object module, IReadOnlyList<Gain> gains, int peer)
    {
        _new.Clear();
        long newest = gains.Count > 0 ? gains[^1].Seq : 0;
        if (!_marks.TryGetValue(module, out var mark))
        {
            _marks.Add(module, new Mark { Seq = newest });
            return _new;
        }
        foreach (var gain in gains)
            if (gain.Seq > mark.Seq && gain.Peer == peer) _new.Add(gain);
        mark.Seq = Math.Max(mark.Seq, newest);
        return _new;
    }
}
