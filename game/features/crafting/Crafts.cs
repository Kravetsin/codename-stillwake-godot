using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crafting;

/// <summary>How a recipe's slot looks.</summary>
public enum Look
{
    /// <summary>Known, and a unit is carried: a click queues it.</summary>
    Ready,
    /// <summary>Known, but short of an item: dimmed and grey; a click goes to what is missing.</summary>
    Short,
    /// <summary>Not known yet: a dark silhouette in its place.</summary>
    Locked,
}

/// <summary>What a queued station is doing, as its screen says it.</summary>
public enum Doing
{
    /// <summary>Nothing queued: READY on its screen.</summary>
    Ready,
    /// <summary>A unit at work: lines running on its screen.</summary>
    Making,
    /// <summary>A unit done with no room in the outputs: it waits there, its slot full.</summary>
    OutputFull,
    /// <summary>A unit at work with no power for it: its screen dark.</summary>
    NoPower,
    /// <summary>A first entry fed from the inputs, waiting for them: idle, resting, READY on its screen.</summary>
    Waiting,
}

/// <summary>What a queued station's panel shows, without Godot: where each recipe sits, how it looks, where a click on it goes, how far the unit at work is.</summary>
public static class Crafts
{
    /// <summary>Units a shift-click queues; a plain click queues one.</summary>
    public const int Many = 5;

    /// <summary>The recipes' columns.</summary>
    public const int Columns = Content.Stations.RowMost;

    /// <summary>The fewest rows of recipes shown, empty places dim: the panel keeps its size as recipes come.</summary>
    public const int RowsLeast = 2;

    /// <summary>
    /// Every place of <paramref name="station"/>'s recipes' grid, row by row: a craft in the place content gives it,
    /// or null for an empty one; whole rows, <see cref="RowsLeast"/> at least.
    /// </summary>
    public static IReadOnlyList<string?> Places(Content.Station station) => Enumerable.Range(0, Math.Max(RowsLeast, station.Rows.Count))
        .SelectMany(r => Enumerable.Range(0, Columns).Select(c =>
            r < station.Rows.Count && c < station.Rows[r].Count ? station.Rows[r][c] : null))
        .ToList();

    /// <summary>What one unit of <paramref name="craft"/> costs, item by item, against what is <paramref name="carried"/>.</summary>
    public static IReadOnlyList<CostLine> Lines(string craft, IReadOnlyList<(string Item, int Count)> carried) =>
        CostLine.Of(Content.Crafts.Get(craft).Cost, carried);

    /// <summary>Whether a line is short: never in creative mode, where nothing is taken.</summary>
    public static bool Short(CostLine line, bool creative) => !creative && line.Have < line.Need;

    /// <summary>How a recipe looks: <paramref name="known"/> is every one in creative mode, and nothing is short there.</summary>
    public static Look LookOf(bool known, IReadOnlyList<CostLine> lines, bool creative) =>
        !known ? Look.Locked : lines.Any(l => Short(l, creative)) ? Look.Short : Look.Ready;

    /// <summary>Whether <paramref name="station"/> makes <paramref name="item"/>: it is in its rows.</summary>
    public static bool Makes(Content.Station station, string item) => station.Rows.Any(r => r.Contains(item));

    /// <summary>
    /// The first item a unit is short of that <paramref name="station"/> makes itself, where a click on a short recipe goes
    /// (its place in the same grid); null when what is short comes from elsewhere.
    /// </summary>
    public static string? MissingCraft(IReadOnlyList<CostLine> lines, bool creative, Content.Station station) =>
        lines.FirstOrDefault(l => Short(l, creative) && Makes(station, l.Item))?.Item;

    /// <summary>
    /// Where <paramref name="item"/> comes from, for the tip of a recipe at <paramref name="station"/> short of it: null where
    /// the station makes it itself; its own words for one salvaged or smelted, or made besides elsewhere
    /// (<c>crafts.from.&lt;item&gt;</c>); else the stations that make it, by name.
    /// </summary>
    public static string? From(string item, Content.Station station)
    {
        if (Makes(station, item)) return null;
        if (Table.ContainsKey($"crafts.from.{item}")) return T($"crafts.from.{item}");
        var makers = Content.Stations.All.Where(s => Makes(s, item)).Select(s => T($"station.{s.Machine}"));
        return T("crafts.madeAt", ("stations", string.Join(", ", makers)));
    }

    /// <summary>Whether <paramref name="station"/> takes items in, so a craft may be fed from its inputs: the Former and the assembler, not the pod.</summary>
    public static bool HasInputs(Content.Station station) => Content.Machines.Get(station.Machine) is var spec && (spec.Inputs ?? 0) > 0 && !spec.Tunes;

    /// <summary>How many units a click queues.</summary>
    public static int CountFor(bool shift) => shift ? Many : 1;

    /// <summary>Work a unit of <paramref name="recipe"/> takes, in thousandths of a tick at full speed: the sim's measure of progress.</summary>
    public static int WorkOf(string recipe) => Content.Crafts.Get(recipe).Ticks * 1000;

    /// <summary>How long a unit takes at <paramref name="station"/> at full power, in seconds: whole, or to a tenth.</summary>
    public static string Seconds(string craft, Content.Station station) =>
        ((double)WorkOf(craft) / station.Speed / World.TicksPerSecond).ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>
    /// What a recipe adds to its item's card: "Craft" and a unit's seconds at <paramref name="station"/>; each item of its cost
    /// against what is <paramref name="carried"/>, where a short raw one comes from; and what a click does,
    /// queue it (shift, <see cref="Many"/>) or go to what is missing; at a station with inputs, the right button queues it
    /// from them, whatever is carried.
    /// </summary>
    public static StationBlock Block(string craft, Content.Station station, IReadOnlyList<(string Item, int Count)> carried, bool creative)
    {
        var lines = Lines(craft, carried);
        var rows = lines.Select(l =>
        {
            bool isShort = Short(l, creative);
            return new CraftRow(l.Item, T($"item.{l.Item}"), l.Have, l.Need, isShort, isShort ? From(l.Item, station) : null);
        }).ToList();
        var keys = new List<CardKey>();
        if (!rows.Any(r => r.Short)) keys.Add(new(T("key.lmb"), T("card.craftIt")));
        else if (MissingCraft(lines, creative, station) != null) keys.Add(new(T("key.lmb"), T("card.goTo")));
        if (HasInputs(station)) keys.Add(new(T("key.rmb"), T("card.fromInputs")));
        if (!rows.Any(r => r.Short) || HasInputs(station)) keys.Add(new(T("key.shift"), T("card.many", ("n", Many))));
        return new StationBlock(T("time.seconds", ("n", Seconds(craft, station))), rows, keys);
    }

    /// <summary>How far the unit at work is, from 0 to 1: full while it waits for room.</summary>
    public static double Share(string recipe, int progress) =>
        Content.Crafts.TryGet(recipe, out _) ? (double)progress / WorkOf(recipe) : 0;

    /// <summary>
    /// What a station with <paramref name="queue"/> and the first entry's <paramref name="progress"/> is doing;
    /// <paramref name="works"/> whether the sim says it works now (<c>CraftingModule.Works</c>): false for a unit with no power;
    /// <paramref name="waiting"/> whether its first entry waits for its inputs (<c>CraftingModule.Lacks</c>).
    /// </summary>
    public static Doing DoingOf(IReadOnlyList<Entry> queue, int progress, bool works = true, bool waiting = false) =>
        queue.Count == 0 ? Doing.Ready
        : waiting ? Doing.Waiting
        : progress >= WorkOf(queue[0].Recipe) ? Doing.OutputFull
        : works ? Doing.Making : Doing.NoPower;
}
