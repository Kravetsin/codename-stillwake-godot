namespace Stillwake.Content;

/// <summary>What a hero's companion does with what is in hand, on the right button (companion-fights): a row by the item, only with a user.</summary>
public static class Abilities
{
    /// <summary>
    /// <paramref name="Item"/> thrown and hauled back by the drone: leaving at <paramref name="Out"/> a tick it slows to a stop
    /// <paramref name="Range"/> thousandths of a cell out; home it gains <paramref name="Pull"/> a tick each tick, up to
    /// <paramref name="Home"/>; a hit either way is the weapon's own.
    /// </summary>
    public sealed record Throw(string Item, int Range, int Out, int Home, int Pull);

    /// <summary>The fire axe's: leaving at 72 cells a second to stop at 18, hauled up to 90, some 0.9 s. Placeholder numbers, for a play pass.</summary>
    public static readonly Throw Axe = new(Weapons.Axe.Item, Range: 18000, Out: 3600, Home: 4500, Pull: 600);

    public static readonly IReadOnlyList<Throw> Throws = [Axe];

    /// <summary>The throw of <paramref name="item"/>, or null for an item never thrown.</summary>
    public static Throw? ThrowOf(string? item)
    {
        for (int i = 0; i < Throws.Count; i++)
            if (Throws[i].Item == item) return Throws[i];
        return null;
    }
}
