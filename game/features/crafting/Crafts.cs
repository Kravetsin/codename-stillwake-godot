using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.Shared;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Crafting;

/// <summary>How a craft's slot looks.</summary>
public enum Look
{
    /// <summary>Known, and a unit is carried: a click queues it.</summary>
    Ready,
    /// <summary>Known, but short of an item: dimmed and grey; a click goes to what is missing.</summary>
    Short,
    /// <summary>Not known yet: a dark silhouette in its place.</summary>
    Locked,
}

/// <summary>A craft's fixed place in the crafts window: its tab among <see cref="Crafts.Tabs"/>, its row, its column.</summary>
public sealed record Spot(int Tab, int Row, int Column);

/// <summary>What the crafts window shows, without Godot: where each craft sits, how it looks, where a click on it goes, the pins.</summary>
public static class Crafts
{
    /// <summary>Units a shift-click queues; a plain click queues one.</summary>
    public const int Many = 5;

    /// <summary>The grid's columns, and its rows a page: as many as the inventory has.</summary>
    public const int Columns = Content.Crafts.RowMost, RowsShown = 3;

    /// <summary>The most crafts pinned over the tabs.</summary>
    public const int PinMost = 9;

    /// <summary>The tabs that hold any craft, in content's order.</summary>
    public static IReadOnlyList<CraftTab> Tabs { get; } = Content.Crafts.Tabs.Where(t => t.Rows.Any(r => r.Count > 0)).ToList();

    private static readonly Dictionary<string, Spot> Spots = Tabs
        .SelectMany((tab, t) => tab.Rows.SelectMany((row, r) => row.Select((craft, c) => (craft, new Spot(t, r, c)))))
        .ToDictionary(p => p.craft, p => p.Item2, StringComparer.Ordinal);

    /// <summary>Where <paramref name="craft"/> sits; null for an item no craft makes.</summary>
    public static Spot? SpotOf(string craft) => Spots.GetValueOrDefault(craft);

    /// <summary>A tab's pages, <see cref="RowsShown"/> rows each: one at least.</summary>
    public static int PagesOf(CraftTab tab) => Math.Max(1, (tab.Rows.Count + RowsShown - 1) / RowsShown);

    /// <summary>The page a row of its tab is on.</summary>
    public static int PageOf(int row) => row / RowsShown;

    /// <summary>The tab <paramref name="step"/> away from <paramref name="tab"/>, wrapping round: the wheel over the tabs.</summary>
    public static int Step(int tab, int step) => ((tab + step) % Tabs.Count + Tabs.Count) % Tabs.Count;

    /// <summary>What one unit of <paramref name="craft"/> costs, item by item, against what is <paramref name="carried"/>.</summary>
    public static IReadOnlyList<CostLine> Lines(string craft, IReadOnlyList<(string Item, int Count)> carried) =>
        CostLine.Of(Content.Crafts.Get(craft).Cost, carried);

    /// <summary>Whether a line is short: never in creative mode, where nothing is taken.</summary>
    public static bool Short(CostLine line, bool creative) => !creative && line.Have < line.Need;

    /// <summary>How a craft looks: <paramref name="known"/> is every one in creative mode, and nothing is short there.</summary>
    public static Look LookOf(bool known, IReadOnlyList<CostLine> lines, bool creative) =>
        !known ? Look.Locked : lines.Any(l => Short(l, creative)) ? Look.Short : Look.Ready;

    /// <summary>The first item a unit is short of that is itself crafted, where a click on a short craft goes; null when only raw items are short.</summary>
    public static string? MissingCraft(IReadOnlyList<CostLine> lines, bool creative) =>
        lines.FirstOrDefault(l => Short(l, creative) && Content.Crafts.TryGet(l.Item, out _))?.Item;

    /// <summary>Whether <paramref name="item"/> is raw: no craft makes it, so its tip says where it comes from.</summary>
    public static bool Raw(string item) => !Content.Crafts.TryGet(item, out _);

    /// <summary>How many units a click queues.</summary>
    public static int CountFor(bool shift) => shift ? Many : 1;

    /// <summary>
    /// <paramref name="pins"/> with <paramref name="craft"/> unpinned if it was, else pinned at the end
    /// while it is known and there is room.
    /// </summary>
    public static IReadOnlyList<string> Toggle(IReadOnlyList<string> pins, string craft, bool known)
    {
        if (pins.Contains(craft)) return pins.Where(p => p != craft).ToList();
        return known && pins.Count < PinMost ? [.. pins, craft] : pins;
    }

    /// <summary>Pins as saved, kept only for crafts there still are, each once, at most <see cref="PinMost"/>.</summary>
    public static IReadOnlyList<string> Loaded(IEnumerable<string> saved) =>
        saved.Where(p => SpotOf(p) != null).Distinct().Take(PinMost).ToList();

    /// <summary>How long a unit takes, in seconds: whole, or to a tenth.</summary>
    public static string Seconds(string craft) =>
        ((double)Content.Crafts.Get(craft).Ticks / World.TicksPerSecond).ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>How far the unit at work is, from 0 to 1: full while it waits for room.</summary>
    public static double Share(string recipe, int progress) =>
        Content.Crafts.TryGet(recipe, out var craft) ? (double)progress / craft.Ticks : 0;
}
