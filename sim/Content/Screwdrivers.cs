namespace Stillwake.Content;

/// <summary>
/// An item that builds plans and takes marks apart (0107): held in the hand, the left button held on a plan works it
/// <paramref name="Work"/> ticks of its work for every tick held, the client's pace, as a weapon's is (0034).
/// </summary>
public sealed record Screwdriver(string Item, int Work);

/// <summary>Everything that builds, by its item. A better one only works faster.</summary>
public static class Screwdrivers
{
    /// <summary>The screwdriver every hero comes aboard with: a tick of work a tick, as building always went.</summary>
    public static readonly Screwdriver Basic = new("screwdriver", Work: 1);

    public static readonly IReadOnlyList<Screwdriver> All = [Basic];

    /// <summary>The screwdriver that is <paramref name="item"/>, or null for an item that builds nothing.</summary>
    public static Screwdriver? Of(string? item)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Item == item) return All[i];
        return null;
    }
}
