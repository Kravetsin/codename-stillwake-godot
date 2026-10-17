using Stillwake.Content;

namespace Stillwake.Sim.Crafting;

public sealed partial class CraftingModule
{
    /// <summary>Every craft, and an item no craft makes: refused as unknown.</summary>
    private static readonly string[] FuzzRecipes = [.. Content.Crafts.All.Select(c => c.Item), "scrap"];

    /// <summary>
    /// Crafts and cancels mostly at the map's stations, queued and fed alike (a fed one refuses both as not
    /// here), now and then at another machine or one that is not there, refused as no station. Crafts of every
    /// recipe, known or not, a few units at a time: refused as unknown, not made there, unpaid or full, paid
    /// mostly in creative mode, which the items fuzz switches on now and then. Cancels of the station's
    /// entries, of ids that finished, and of ids never given.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        int Below(int n) => (int)rng.Below((ulong)n);
        var machines = _grid.Machines;
        var stations = machines.Where(m => Stations.TryGet(m.Kind, out _)).ToList();
        // One station is taken without a draw: a map with only the pod draws what it drew before stations.
        int machine = stations.Count > 0 && Below(8) != 0 ? stations[stations.Count == 1 ? 0 : Below(stations.Count)].Id
            : machines.Count > 0 && Below(2) == 0 ? machines[Below(machines.Count)].Id
            : Below(50);
        var queue = QueueOf(machine);
        if (Below(3) == 0)
        {
            int id = queue is { Entries.Count: > 0 } && Below(2) == 0
                ? queue.Entries[Below(queue.Entries.Count)].Id
                : Below((queue?.Next ?? 1) + 2);
            return new Cancel(machine, id) { From = from };
        }
        return new Craft(machine, FuzzRecipes[Below(FuzzRecipes.Length)], 1 + Below(9)) { From = from };
    }
}
