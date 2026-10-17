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

/// <summary>What the Fabricator is doing, as its screen says it.</summary>
public enum Doing
{
    /// <summary>Nothing queued: READY on its screen.</summary>
    Ready,
    /// <summary>A unit at work: lines running on its screen.</summary>
    Making,
    /// <summary>A unit done with no room in the outputs: it waits there, its slot full.</summary>
    OutputFull,
}

/// <summary>What the Fabricator's panel shows, without Godot: where each recipe sits, how it looks, where a click on it goes, how far the unit at work is.</summary>
public static class Crafts
{
    /// <summary>Units a shift-click queues; a plain click queues one.</summary>
    public const int Many = 5;

    /// <summary>The recipes' columns.</summary>
    public const int Columns = Content.Crafts.RowMost;

    /// <summary>The fewest rows of recipes shown, empty places dim: the panel keeps its size as recipes come.</summary>
    public const int RowsLeast = 2;

    /// <summary>
    /// Every place of the recipes' grid, row by row: a craft in the place content gives it, or null for an
    /// empty one; whole rows, <see cref="RowsLeast"/> at least.
    /// </summary>
    public static IReadOnlyList<string?> Places { get; } = Enumerable.Range(0, Math.Max(RowsLeast, Content.Crafts.Rows.Count))
        .SelectMany(r => Enumerable.Range(0, Columns).Select(c =>
            r < Content.Crafts.Rows.Count && c < Content.Crafts.Rows[r].Count ? Content.Crafts.Rows[r][c] : null))
        .ToList();

    /// <summary>What one unit of <paramref name="craft"/> costs, item by item, against what is <paramref name="carried"/>.</summary>
    public static IReadOnlyList<CostLine> Lines(string craft, IReadOnlyList<(string Item, int Count)> carried) =>
        CostLine.Of(Content.Crafts.Get(craft).Cost, carried);

    /// <summary>Whether a line is short: never in creative mode, where nothing is taken.</summary>
    public static bool Short(CostLine line, bool creative) => !creative && line.Have < line.Need;

    /// <summary>How a recipe looks: <paramref name="known"/> is every one in creative mode, and nothing is short there.</summary>
    public static Look LookOf(bool known, IReadOnlyList<CostLine> lines, bool creative) =>
        !known ? Look.Locked : lines.Any(l => Short(l, creative)) ? Look.Short : Look.Ready;

    /// <summary>The first item a unit is short of that is itself crafted, where a click on a short recipe goes; null when only raw items are short.</summary>
    public static string? MissingCraft(IReadOnlyList<CostLine> lines, bool creative) =>
        lines.FirstOrDefault(l => Short(l, creative) && Content.Crafts.TryGet(l.Item, out _))?.Item;

    /// <summary>Whether <paramref name="item"/> is raw: no craft makes it, so its tip says where it comes from.</summary>
    public static bool Raw(string item) => !Content.Crafts.TryGet(item, out _);

    /// <summary>How many units a click queues.</summary>
    public static int CountFor(bool shift) => shift ? Many : 1;

    /// <summary>Work a unit of <paramref name="recipe"/> takes, in thousandths of a tick at full speed: the sim's measure of progress.</summary>
    public static int WorkOf(string recipe) => Content.Crafts.Get(recipe).Ticks * 1000;

    /// <summary>How long a unit takes at the pod, in seconds: whole, or to a tenth.</summary>
    public static string Seconds(string craft) =>
        ((double)WorkOf(craft) / Content.Crafts.PodSpeed / World.TicksPerSecond).ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>
    /// What a recipe adds to its item's card: "Craft" and a unit's seconds at the pod; each item of its cost
    /// against what is <paramref name="carried"/>, where a short raw one comes from; and what a click does,
    /// queue it (shift, <see cref="Many"/>) or go to what is missing.
    /// </summary>
    public static StationBlock Block(string craft, IReadOnlyList<(string Item, int Count)> carried, bool creative)
    {
        var lines = Lines(craft, carried);
        var rows = lines.Select(l =>
        {
            bool isShort = Short(l, creative);
            return new CraftRow(l.Item, T($"item.{l.Item}"), l.Have, l.Need, isShort, isShort && Raw(l.Item) ? T($"crafts.from.{l.Item}") : null);
        }).ToList();
        IReadOnlyList<CardKey> keys = !rows.Any(r => r.Short)
            ? [new(T("key.lmb"), T("card.craftIt")), new(T("key.shift"), T("card.many", ("n", Many)))]
            : MissingCraft(lines, creative) != null ? [new(T("key.lmb"), T("card.goTo"))] : [];
        return new StationBlock(T("time.seconds", ("n", Seconds(craft))), rows, keys);
    }

    /// <summary>How far the unit at work is, from 0 to 1: full while it waits for room.</summary>
    public static double Share(string recipe, int progress) =>
        Content.Crafts.TryGet(recipe, out _) ? (double)progress / WorkOf(recipe) : 0;

    /// <summary>What a machine with <paramref name="queue"/> and the first entry's <paramref name="progress"/> is doing.</summary>
    public static Doing DoingOf(IReadOnlyList<Entry> queue, int progress) =>
        queue.Count == 0 ? Doing.Ready : progress >= WorkOf(queue[0].Recipe) ? Doing.OutputFull : Doing.Making;
}
