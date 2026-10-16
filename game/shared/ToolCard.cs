using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>An effect on a tool's card: a chip with its name, and a line telling it, in its colours, "rrggbb".</summary>
public sealed record CardEffect(string Name, string About, string Colour, string Ground);

/// <summary>
/// What the one tooltip shows over a tool (<c>PointerTip</c>), the plaque of the card mockup: its icon
/// large, its name and kind; damage a second, damage and seconds between hits in three tiles, as written;
/// its effects, what it works on and how it is used; and a violet band while it is not analysed. Equal by
/// what it says, as <see cref="Tip"/> is.
/// </summary>
/// <param name="Item">The item, whose icon heads the card.</param>
/// <param name="Key">The key cap of <paramref name="Hint"/>: the button that uses it.</param>
/// <param name="Unknown">Not analysed yet: the band at the bottom.</param>
public sealed record ToolCard(
    string Item, string Name, string Kind, string Dps, string Damage, string Hit,
    IReadOnlyList<CardEffect> Effects, IReadOnlyList<string> Uses, string Key, string Hint, bool Unknown)
{
    public bool Equals(ToolCard? other) =>
        other is not null && Item == other.Item && Name == other.Name && Kind == other.Kind && Dps == other.Dps
        && Damage == other.Damage && Hit == other.Hit && Effects.SequenceEqual(other.Effects)
        && Uses.SequenceEqual(other.Uses) && Key == other.Key && Hint == other.Hint && Unknown == other.Unknown;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Item);
        hash.Add(Name);
        hash.Add(Dps);
        hash.Add(Unknown);
        foreach (var effect in Effects) hash.Add(effect);
        foreach (var use in Uses) hash.Add(use);
        return hash.ToHashCode();
    }
}
