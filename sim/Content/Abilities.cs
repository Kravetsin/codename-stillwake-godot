namespace Stillwake.Content;

/// <summary>What a hero's companion does with what is in hand, on the right button (companion-fights): a row by the item, only with a user.</summary>
public static class Abilities
{
    /// <summary>
    /// <paramref name="Item"/> thrown and hauled back by the drone: leaving at <paramref name="Out"/> a tick it slows to a stop
    /// <paramref name="Range"/> thousandths of a cell out; home it gains <paramref name="Pull"/> a tick each tick, up to
    /// <paramref name="Home"/>; a hit either way is the weapon's own. It spins up to <paramref name="Spin"/> tenths of a turn a second,
    /// at none flies point first. One that strikes <paramref name="Twice"/>, turned at its range by the thread and by no wall, strikes
    /// again home what it struck out, and rests two paces for it.
    /// </summary>
    public sealed record Throw(string Item, int Range, int Out, int Home, int Pull, int Spin, bool Twice = false);

    /// <summary>The fire axe's: leaving at 72 cells a second to stop at 18, hauled up to 90, some 0.9 s; it strikes both ways. Placeholder numbers, for a play pass.</summary>
    public static readonly Throw Axe = new(Weapons.Axe.Item, Range: 18000, Out: 3600, Home: 4500, Pull: 600, Spin: 32, Twice: true);

    /// <summary>The sledgehammer's: heavy, 36 cells a second to stop at 12, slowly hauled up to 45, some 1.2 s; one blow. Placeholder numbers, for a play pass.</summary>
    public static readonly Throw Sledgehammer = new(Weapons.Sledgehammer.Item, Range: 12000, Out: 1800, Home: 2250, Pull: 200, Spin: 14);

    /// <summary>The spear's: point first, 120 cells a second to stop at 26, hauled back by its butt, some 0.8 s; one hit. Placeholder numbers, for a play pass.</summary>
    public static readonly Throw Spear = new(Weapons.Spear.Item, Range: 26000, Out: 6000, Home: 6000, Pull: 1000, Spin: 0);

    public static readonly IReadOnlyList<Throw> Throws = [Axe, Sledgehammer, Spear];

    /// <summary>The throw of <paramref name="item"/>, or null for an item never thrown.</summary>
    public static Throw? ThrowOf(string? item)
    {
        for (int i = 0; i < Throws.Count; i++)
            if (Throws[i].Item == item) return Throws[i];
        return null;
    }
}
