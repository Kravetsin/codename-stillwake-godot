namespace Stillwake.Content;

/// <summary>
/// An item that builds plans and takes marks apart (0107): held in the hand, the left button held on a plan works it
/// <paramref name="Work"/> ticks of its work for every tick held, the client's pace, as a weapon's is (0034). While it runs
/// (an electric one, on the belt's charge), it works every plan in the <paramref name="Area"/> by <paramref name="Area"/>
/// cells under the pointer; else the one cell's (power-driver).
/// </summary>
public sealed record Screwdriver(string Item, int Work, int Area);

/// <summary>Everything that builds, by its item. A better one works faster or wider.</summary>
public static class Screwdrivers
{
    /// <summary>The screwdriver every hero comes aboard with: a tick of work a tick on one cell, as building always went.</summary>
    public static readonly Screwdriver Basic = new("screwdriver", Work: 1, Area: 1);

    /// <summary>
    /// The power driver, bought on the black market at <c>order2</c> (power-driver): the screwdriver's pace on each plan, but the
    /// 2×2 square under the pointer while it runs on the belt's charge; flat, one cell, as the screwdriver.
    /// </summary>
    public static readonly Screwdriver PowerDriver = new("powerDriver", Work: 1, Area: 2);

    public static readonly IReadOnlyList<Screwdriver> All = [Basic, PowerDriver];

    /// <summary>The screwdriver that is <paramref name="item"/>, or null for an item that builds nothing.</summary>
    public static Screwdriver? Of(string? item)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Item == item) return All[i];
        return null;
    }
}
