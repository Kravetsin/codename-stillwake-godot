namespace Stillwake.Content;

/// <summary>What a hero's companion does with what is in hand, on the right button (companion-fights): a row by the item, only with a user.</summary>
public static class Abilities
{
    /// <summary>
    /// <paramref name="Item"/> thrown and brought back by the drone: <paramref name="Range"/> thousandths of a cell out at
    /// <paramref name="Out"/> a tick, home to the hero at <paramref name="Home"/>; a hit either way is the weapon's own.
    /// </summary>
    public sealed record Throw(string Item, int Range, int Out, int Home);

    /// <summary>The fire axe's: 18 cells at 45 a second out and 60 home, some 0.7 s. Placeholder numbers, for a play pass.</summary>
    public static readonly Throw Axe = new(Weapons.Axe.Item, Range: 18000, Out: 2250, Home: 3000);

    public static readonly IReadOnlyList<Throw> Throws = [Axe];

    /// <summary>The throw of <paramref name="item"/>, or null for an item never thrown.</summary>
    public static Throw? ThrowOf(string? item)
    {
        for (int i = 0; i < Throws.Count; i++)
            if (Throws[i].Item == item) return Throws[i];
        return null;
    }
}
