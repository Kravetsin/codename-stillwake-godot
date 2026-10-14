using System.Text.Json.Serialization;

namespace Stillwake.Sim.Crafting;

/// <summary>A craft queued: <c>Count</c> units of <c>Recipe</c>, the item made, left to make, the one at work included.</summary>
public sealed record Entry(int Id, string Recipe, int Count);

/// <summary>A peer's queue: its entries in the order queued, the first at work.</summary>
internal sealed class Queue
{
    /// <summary>The peer whose hero crafts, and into whose inventory what is made goes.</summary>
    public int Peer { get; set; }
    /// <summary>The id the next entry gets: ids only grow, so one keeps its id while those before it finish.</summary>
    public int Next { get; set; } = 1;
    /// <summary>Ticks of work on the first entry's unit: at its craft's ticks it waits for room.</summary>
    public int Progress { get; set; }
    public List<Entry> Entries { get; set; } = [];
}

/// <summary>The crafting slice. Public so modules can be typed by it; its members internal.</summary>
public sealed class CraftingState
{
    /// <summary>The queues of every peer that ever crafted, in peer order; an empty one keeps its ids counting.</summary>
    [JsonInclude] internal List<Queue> Queues { get; set; } = [];
}
