using Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>The mafia's reputation raised in a test, as capsules would (2026-11-08-mafia-reputation).</summary>
internal static class Mafia
{
    /// <summary><paramref name="count"/> of <paramref name="item"/> sold for what it is worth, straight into the counts.</summary>
    public static void Sell(Scenario w, string item, int count) => w.M.Knowledge.Sell(item, count, (long)count * Market.WorthOf(item));

    /// <summary>Each level up to <paramref name="level"/> reached by selling its order, a tick between: what is sold before a level opens counts for nothing.</summary>
    public static void Reach(Scenario w, int level)
    {
        while (w.M.Knowledge.Level < level)
        {
            foreach (var (item, count) in w.M.Research.Standing().Next!.Order) Sell(w, item, count);
            w.Run();
        }
    }
}
