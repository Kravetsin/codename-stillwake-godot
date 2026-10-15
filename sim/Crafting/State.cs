using System.Text.Json.Serialization;

namespace Stillwake.Sim.Crafting;

/// <summary>
/// A craft queued: <c>Count</c> units of <c>Recipe</c>, the item made, left to make, the one at work
/// included; <c>By</c> the peer that queued it, whose suit colour marks it.
/// </summary>
public sealed record Entry(int Id, string Recipe, int Count, int By);

/// <summary>A crafting machine's queue, shared by the crew: its entries in the order queued, the first at work.</summary>
internal sealed class Queue
{
    /// <summary>The machine that crafts, and into whose outputs what is made goes.</summary>
    public int Machine { get; set; }
    /// <summary>The id the next entry gets: ids only grow, so one keeps its id while those before it finish.</summary>
    public int Next { get; set; } = 1;
    /// <summary>Work on the first entry's unit, in thousandths of a tick at full speed: at its craft's ticks × 1000 it waits for room.</summary>
    public int Progress { get; set; }
    public List<Entry> Entries { get; set; } = [];
}

/// <summary>The crafting slice. Public so modules can be typed by it; its members internal.</summary>
public sealed class CraftingState
{
    /// <summary>The queues of every machine ever crafted at, in machine id order; an empty one keeps its ids counting.</summary>
    [JsonInclude] internal List<Queue> Queues { get; set; } = [];
}
