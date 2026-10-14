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
public sealed record TipLine(string Text, Tone Tone = Tone.Plain, string? Icon = null);

/// <summary>
/// What the one tooltip says (<c>PointerTip</c>): a name in bold, then lines in colour. Equal by
/// what it says, so a tip set every frame redraws nothing.
/// </summary>
/// <param name="Name">The bold first line; null for a tip of lines only, a cost under the pointer.</param>
public sealed record Tip(string? Name, IReadOnlyList<TipLine> Lines)
{
    public Tip(string name, params TipLine[] lines) : this(name, (IReadOnlyList<TipLine>)lines) { }

    public bool Equals(Tip? other) => other is not null && Name == other.Name && Lines.SequenceEqual(other.Lines);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        foreach (var line in Lines) hash.Add(line);
        return hash.ToHashCode();
    }
}
