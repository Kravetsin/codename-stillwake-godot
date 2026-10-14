using Stillwake.Content;

namespace Stillwake.Sim.Crafting;

public sealed partial class CraftingModule
{
    /// <summary>Every craft, and an item no craft makes: refused as unknown.</summary>
    private static readonly string[] FuzzRecipes = [.. Crafts.All.Select(c => c.Item), "scrap"];

    /// <summary>
    /// Crafts of every recipe, known or not, a few units at a time: refused as unknown, unpaid or full,
    /// paid mostly in creative mode, which the items fuzz switches on now and then. Cancels of the
    /// sender's entries, of ids that finished, and of ids never given.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        var queue = QueueOf(from);
        if (Below(3) == 0)
        {
            int id = queue is { Entries.Count: > 0 } && Below(2) == 0
                ? queue.Entries[Below(queue.Entries.Count)].Id
                : Below((queue?.Next ?? 1) + 2);
            return new Cancel(id) { From = from };
        }
        return new Craft(FuzzRecipes[Below(FuzzRecipes.Length)], 1 + Below(9)) { From = from };
    }
}
