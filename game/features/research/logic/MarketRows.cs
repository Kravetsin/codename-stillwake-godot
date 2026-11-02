using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>What a good on the market looks like, from what the sim says of it.</summary>
public enum MarketCard
{
    /// <summary>Its order is not met: garbage, and no pick.</summary>
    Sealed,

    /// <summary>Open, and the credits are short.</summary>
    Dear,

    /// <summary>Open, and it can be bought.</summary>
    Affordable,

    /// <summary>Already had.</summary>
    Bought,
}

/// <summary>Where the market's rows and cards lie in the view, and what a card's state is ([market-decrypt](../../../../docs/decisions/2026-11-02-market-decrypt.md)).</summary>
public static class MarketRows
{
    /// <summary>A row's height at least, the tall row's, the gap between rows and between cards, and the width of a row's head.</summary>
    public const int Small = 64, Big = 122, Gap = 6, HeadWide = 256;

    /// <summary>The state of a good from the sim's answers: whether it is bought, reached, and why it cannot be bought.</summary>
    public static MarketCard StateOf(bool done, bool reached, string? problem) =>
        done ? MarketCard.Bought : !reached ? MarketCard.Sealed : problem == null ? MarketCard.Affordable : MarketCard.Dear;

    /// <summary>
    /// Each row's top and height in a view <paramref name="tall"/> high, <paramref name="weights"/> saying how far each is the tall row: what is spare is shared out, so
    /// the rows fill the view with or without a tall one.
    /// </summary>
    public static IReadOnlyList<(float Top, float Tall)> Of(float tall, IReadOnlyList<double> weights)
    {
        int count = weights.Count;
        float spare = (tall - Gap * (count - 1) - count * Small - (float)weights.Sum() * (Big - Small)) / count, top = 0;
        var rows = new (float, float)[count];
        for (int row = 0; row < count; row++)
        {
            float height = Small + (float)weights[row] * (Big - Small) + spare;
            rows[row] = (top, height);
            top += height + Gap;
        }
        return rows;
    }

    /// <summary>The left of card <paramref name="card"/> of <paramref name="count"/> and every card's width, in cards <paramref name="wide"/> across.</summary>
    public static (float Left, float Wide) Card(float wide, int count, int card)
    {
        float each = (wide - Gap * (count - 1)) / count;
        return (card * (each + Gap), each);
    }
}
