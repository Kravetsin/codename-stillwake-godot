using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// One slot of a kind's loot in the Handbook (0066): an item, how many come at once and how often, and whether one
/// has come out of the kind yet. Unseen, it shows only its chance; seen, its range in the corner too.
/// </summary>
public sealed record LootSlot(string Item, int Min, int Max, int? OneIn, bool Seen)
{
    /// <summary>How many come at once: "8–12", or one number when it is always the same.</summary>
    public string Range => Min == Max ? $"{Min}" : $"{Min}–{Max}";

    /// <summary>What the slot's corner says: its range once seen, nothing for exactly one, nothing while unseen.</summary>
    public string Corner => !Seen || Max == 1 ? "" : Range;

    /// <summary>What the slot's top-left corner says of its roll: "25%", a hundred over one in n rounded to the nearest; none with no roll.</summary>
    public string? Percent => OneIn is int n ? $"{PercentOf(n)}%" : null;

    /// <summary>"25% (1 in 4)", or "Always" for an entry with no roll.</summary>
    public string Chance => OneIn is int n ? T("handbook.chance", ("p", PercentOf(n)), ("n", n)) : T("handbook.always");

    /// <summary>100 / <paramref name="oneIn"/>, a half rounded up: 1 in 3 is 33, 1 in 8 is 13.</summary>
    private static int PercentOf(int oneIn) => (200 + oneIn) / (2 * oneIn);

    /// <summary>The one tooltip's words: the item, its range and its chance; unseen, "???", its chance and that none has come out yet.</summary>
    public Tip Tip => Seen
        ? new Tip(T($"item.{Item}"), new TipLine(Range), new TipLine(Chance, Tone.Good))
        : new Tip(T("handbook.unknown"), new TipLine(Chance), new TipLine(T("handbook.notDropped"), Tone.Dim));
}

/// <summary>
/// One look of a kind in its entry's strip (0066), from 0 of <paramref name="Count"/>: one of the pictures a piece of it
/// may wear, seen once a piece wearing it has been caught or broken.
/// </summary>
public sealed record LookSlot(int Look, int Count, bool Seen)
{
    /// <summary>"Look 2 of 5"; unseen, "???" and that none wearing it has been had yet.</summary>
    public Tip Tip => Seen
        ? new Tip(T("handbook.look", ("n", Look + 1), ("all", Count)))
        : new Tip(T("handbook.unknown"), new TipLine(T("handbook.notCaught"), Tone.Dim));
}

/// <summary>
/// What the Handbook's debris section shows of the world's record (<c>DebrisModule.Found</c>, 0066): which kinds are
/// found, each kind's loot slot by slot, revealed item by item, and what the one tooltip says of each.
/// </summary>
public static class Finds
{
    /// <summary>
    /// The loot slots of <paramref name="kind"/>, one an entry of its <c>Loot</c> in order, or itself for a piece caught
    /// as itself; each seen when <paramref name="seen"/>, the items seen come out of it, holds its item. None found: null.
    /// </summary>
    public static IReadOnlyList<LootSlot> SlotsOf(string kind, IReadOnlyCollection<string>? seen)
    {
        bool Seen(string item) => seen?.Contains(item) == true;
        return Content.Loot.Of(kind) is { } loot
            ? loot.Select(e => new LootSlot(e.Item, e.Min, e.Max, e.OneIn, Seen(e.Item))).ToList()
            : [new LootSlot(kind, 1, 1, null, Seen(kind))];
    }

    /// <summary>How many of the Handbook's kinds are among <paramref name="found"/>, and how many there are: "10/18".</summary>
    public static (int Found, int All) Tally(IReadOnlyCollection<string> found)
    {
        var kinds = Stillwake.Game.Handbook.DebrisRows.SelectMany(r => r.Kinds).ToList();
        return (kinds.Count(found.Contains), kinds.Count);
    }

    /// <summary>The first kind found, in the rows' order: the one shown when none is chosen; null with none found.</summary>
    public static string? FirstFound(IReadOnlyCollection<string> found) =>
        Stillwake.Game.Handbook.DebrisRows.SelectMany(r => r.Kinds).FirstOrDefault(found.Contains);

    /// <summary>
    /// A kind's slot's words: found, its name, how it is had (its hit points, or caught by hand), how many of its looks
    /// are found (<paramref name="looks"/>) when it has more than one, and how many of its items have not come out yet;
    /// not found, "???" and where to look, its row.
    /// </summary>
    public static Tip KindTip(string kind, IReadOnlyCollection<string>? seen, IReadOnlyCollection<int>? looks = null)
    {
        if (seen == null)
        {
            var row = Stillwake.Game.Handbook.RowOf(kind);
            return row == null ? new Tip(T("handbook.unknown")) : new Tip(T("handbook.unknown"), new TipLine(T($"handbook.row.{row.Id}"), Tone.Dim));
        }
        var lines = new List<TipLine> { new(Content.Wreckage.HpOf(kind) is int hp ? T("handbook.hp", ("hp", hp)) : T("handbook.byHand")) };
        int count = Content.Looks.CountOf(kind);
        if (count > 1) lines.Add(new TipLine(T("handbook.looks", ("n", looks?.Count ?? 0), ("all", count))));
        int missing = SlotsOf(kind, seen).Count(s => !s.Seen);
        if (missing > 0) lines.Add(new TipLine(T("handbook.missing", ("n", missing)), Tone.Unknown));
        return new Tip(T($"item.{kind}"), lines);
    }

    /// <summary>Every look of <paramref name="kind"/> in order, each seen when <paramref name="seen"/>, the looks found, holds it.</summary>
    public static IReadOnlyList<LookSlot> LooksOf(string kind, IReadOnlyCollection<int>? seen)
    {
        int count = Content.Looks.CountOf(kind);
        return Enumerable.Range(0, count).Select(look => new LookSlot(look, count, seen?.Contains(look) == true)).ToList();
    }

    /// <summary>The strip's count: how many of <paramref name="looks"/> are found of all, "2/5".</summary>
    public static string LookCount(IReadOnlyList<LookSlot> looks) => $"{looks.Count(l => l.Seen)}/{looks.Count}";

    /// <summary>
    /// The look shown big: <paramref name="chosen"/> once it is found, else the first found (the one its slot in the
    /// rows wears), else the first.
    /// </summary>
    public static int ShownLook(IReadOnlyList<int>? seen, int? chosen) =>
        chosen is int c && seen?.Contains(c) == true ? c : seen is { Count: > 0 } ? seen[0] : 0;

    /// <summary>How many looks of the Handbook's kinds are among <paramref name="looks"/>, by kind, and how many there are: "23/81".</summary>
    public static (int Found, int All) LookTally(IReadOnlyDictionary<string, int[]> looks)
    {
        var kinds = Stillwake.Game.Handbook.DebrisRows.SelectMany(r => r.Kinds).ToList();
        return (kinds.Sum(k => looks.TryGetValue(k, out var seen) ? seen.Length : 0), kinds.Sum(Content.Looks.CountOf));
    }

    /// <summary>The counter's words: the kinds found and the looks found, each of all there are.</summary>
    public static Tip TallyTip((int Found, int All) kinds, (int Found, int All) looks) => new(T("handbook.found"),
        new TipLine(T("handbook.kinds", ("n", kinds.Found), ("all", kinds.All))),
        new TipLine(T("handbook.looks", ("n", looks.Found), ("all", looks.All))));

    /// <summary>The words of an entry's hit points, or of a kind caught by hand.</summary>
    public static Tip HowTip(string kind) => Content.Wreckage.HpOf(kind) is int hp
        ? new Tip(T("handbook.hp", ("hp", hp)), new TipLine(T("handbook.anyWeapon"), Tone.Dim))
        : new Tip(T("handbook.byHand"), new TipLine(T("handbook.byHarpoon"), Tone.Dim));

    /// <summary>
    /// How much a picture <paramref name="w"/> × <paramref name="h"/> is scaled to fit <paramref name="maxW"/> ×
    /// <paramref name="maxH"/>: by a whole number up to <paramref name="most"/> when it fits at least once, so its pixels
    /// stay square and a scrap chunk is not blown up to a blur of blocks; else down to fit, a big derelict in a small slot.
    /// </summary>
    public static float Fit(int w, int h, int maxW, int maxH, int most = 4)
    {
        float fit = Math.Min((float)maxW / w, (float)maxH / h);
        return fit >= 1 ? Math.Min(most, MathF.Floor(fit)) : fit;
    }
}
