using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>The colour of a tip's line: the few the one tooltip speaks in.</summary>
public enum Tone
{
    /// <summary>Plain words, light grey.</summary>
    Plain,
    /// <summary>What matters less: a hint, a use.</summary>
    Dim,
    /// <summary>Done, enough, opened: green.</summary>
    Good,
    /// <summary>Short of something: red.</summary>
    Bad,
    /// <summary>Not analysed yet: violet, as the slot's "?" is.</summary>
    Unknown,
}

/// <summary>A line under a tip's name: its words in their tone, after the icon of item <paramref name="Icon"/> when it names one.</summary>
/// <param name="Large">A cost row under a blueprint: the icon twice its size and the words larger, read at a glance.</param>
/// <param name="Faded">The icon faded: a row with nothing left to do.</param>
public sealed record TipLine(string Text, Tone Tone = Tone.Plain, string? Icon = null, bool Large = false, bool Faded = false);

/// <summary>
/// What the one tooltip says (<c>PointerTip</c>): a name in bold, then lines in colour. Equal by
/// what it says, so a tip set every frame redraws nothing.
/// </summary>
/// <param name="Name">The bold first line; null for a tip of lines only, a cost under the pointer.</param>
public sealed record Tip(string? Name, IReadOnlyList<TipLine> Lines)
{
    public Tip(string name, params TipLine[] lines) : this(name, (IReadOnlyList<TipLine>)lines) { }

    /// <summary>An item's card, shown in place of the name and lines.</summary>
    public ItemCard? Card { get; init; }

    /// <summary>What a placement looks like built, drawn before the name: a blueprint's, so its tinted look never hides what was planned.</summary>
    public Stillwake.Sim.Grid.Placement? Picture { get; init; }

    /// <summary>
    /// What the tip tells of on the screen, its left, top, width and height: the slot under the pointer, set by
    /// <c>TipSpots</c>. An item's card is pinned beside it (<see cref="TipPlace.Beside"/>); none follows the pointer.
    /// </summary>
    public (float X, float Y, float W, float H)? Anchor { get; init; }

    public bool Equals(Tip? other) =>
        other is not null && Name == other.Name && Lines.SequenceEqual(other.Lines) && Equals(Card, other.Card) && Picture == other.Picture && Anchor == other.Anchor;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        hash.Add(Card);
        hash.Add(Picture);
        hash.Add(Anchor);
        foreach (var line in Lines) hash.Add(line);
        return hash.ToHashCode();
    }
}
