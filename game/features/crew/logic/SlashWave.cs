using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A struck tool's hit sent out as a wave (axe-wave, a prototype): born where the blade is as it hits, from where the hero stood
/// then, it runs out along the stroke's fan to the weapon's range over <see cref="WorkLooks.WaveMs"/>, striking each cell, wreck
/// and creature once as it reaches it; gone at the range.
/// </summary>
public sealed class SlashWave(Hero from, int heading, string tool, int range)
{
    private readonly HashSet<int> _cells = [], _wrecks = [], _creatures = [];
    private bool _grounded;

    /// <summary>The hero as it struck: the wave runs from its shoulder then, wherever it goes after.</summary>
    public Hero From { get; } = from;

    /// <summary>The fan's heading, as the stroke aimed it.</summary>
    public int Heading { get; } = heading;

    /// <summary>The tool that sent it.</summary>
    public string Tool { get; } = tool;

    /// <summary>How long it has run, in ms.</summary>
    public double Ms { get; private set; }

    /// <summary>Whether it has reached the range and is gone.</summary>
    public bool Done { get; private set; }

    /// <summary>How far out it is <paramref name="ms"/> after the hit, thousandths of a cell: from the blade to <paramref name="range"/>, slowing.</summary>
    public static int ReachAt(int range, double ms)
    {
        double p = Math.Clamp(ms / WorkLooks.WaveMs, 0, 1), eased = 1 - (1 - p) * (1 - p);
        return (int)Math.Round(range * (WorkLooks.WaveFrom + (1 - WorkLooks.WaveFrom) * eased));
    }

    /// <summary>How far out it is now.</summary>
    public int Reach => ReachAt(range, Ms);

    /// <summary>On <paramref name="elapsedMs"/>; past its time it has made its last reach and is done.</summary>
    public void Advance(double elapsedMs)
    {
        if (Ms >= WorkLooks.WaveMs) Done = true;
        Ms = Math.Min(WorkLooks.WaveMs, Ms + elapsedMs);
    }

    /// <summary>
    /// What of <paramref name="hit"/>, the fan out to <see cref="Reach"/>, the wave has not struck yet, and marks it struck: the
    /// first ground touch only the first time it touches ground.
    /// </summary>
    public EdgeHit Fresh(EdgeHit hit)
    {
        var cells = hit.Cells.Where(_cells.Add).ToList();
        var wrecks = hit.Wrecks.Where(p => _wrecks.Add(p.Id)).ToList();
        var creatures = hit.Creatures.Where(c => _creatures.Add(c.Id)).ToList();
        var touches = hit.Touches.Where(t => t.Wreck is { } w ? wrecks.Any(p => p.Id == w.Id) : !_grounded && t.Cell is int c && cells.Contains(c)).ToList();
        if (touches.Any(t => t.Cell != null)) _grounded = true;
        return new EdgeHit(cells, wrecks, creatures, touches, hit.End);
    }
}
