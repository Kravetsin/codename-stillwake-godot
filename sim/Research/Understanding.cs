using Stillwake.Content;

namespace Stillwake.Sim.Research;

/// <summary>What it takes to understand a part (docs/plans/board.md): how much of it the crew must have obtained.</summary>
public static class Understanding
{
    /// <summary>How many of <paramref name="item"/> are enough: <c>EnoughWorth</c> credits of its worth, rounded up, at most <c>EnoughMost</c>; one of what has no worth.</summary>
    public static int Enough(string item)
    {
        int worth = Market.WorthOf(item);
        return worth == 0 ? 1 : Math.Min(Content.Research.EnoughMost, (Content.Research.EnoughWorth + worth - 1) / worth);
    }
}
