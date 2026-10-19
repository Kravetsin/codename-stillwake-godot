using System.Text.Json.Serialization;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Logistics;

/// <summary>Where a flying drone is going: to take its job's items, to put them in, or home to its bay. Saved as its number: never renumber.</summary>
public enum Leg { Source = 0, Target = 1, Home = 2 }

/// <summary>
/// A drone in flight, a sim entity (0062): the item it is while docked (<see cref="Stack"/>, its charge with it), its
/// bay, where it is, and its job: <c>Count</c> of <c>Item</c> from machine <c>Source</c>, from every slot of it there, to machine
/// <c>Target</c>, and the <see cref="Cargo"/> it carries. Nothing about its path is kept.
/// </summary>
public sealed class Drone
{
    [JsonInclude] public int Id { get; internal set; }
    [JsonInclude] public int Bay { get; internal set; }
    /// <summary>The drone as an item, a stack of one: its <c>Charge</c> is what it has left to fly on.</summary>
    [JsonInclude] public Stack Stack { get; internal set; } = null!;
    /// <summary>Its middle, in thousandths of a cell: a cell's centre, or on the way between two side by side.</summary>
    [JsonInclude] public int X { get; internal set; }
    [JsonInclude] public int Y { get; internal set; }
    /// <summary>The cell whose centre it last reached, and the one it flies to: the same while it stands there.</summary>
    [JsonInclude] public int Cell { get; internal set; }
    [JsonInclude] public int To { get; internal set; }
    [JsonInclude] public Leg Leg { get; internal set; }
    [JsonInclude] public int Source { get; internal set; }
    [JsonInclude] public int Target { get; internal set; }
    [JsonInclude] public string Item { get; internal set; } = "";
    [JsonInclude] public int Count { get; internal set; }
    /// <summary>Whether it flies home for want of charge: work was left that it could not cover, and the way home after.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool Low { get; internal set; }
    /// <summary>What it carries, from its source to its target; none on the way there, and home but when turned home low.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Stack? Cargo { get; internal set; }
}

/// <summary>The logistics slice. Public so modules can be typed by it; its members internal.</summary>
public sealed class LogisticsState
{
    /// <summary>The machines drones are told to leave alone, by id, sorted; kept for a machine taken apart, never read.</summary>
    [JsonInclude] internal List<int> Off { get; set; } = [];
    /// <summary>Drones in flight, in id order.</summary>
    [JsonInclude] internal List<Drone> Drones { get; set; } = [];
    [JsonInclude] internal int NextId { get; set; } = 1;
    /// <summary>Ticks since the world began, counted here: when bays look.</summary>
    [JsonInclude] internal int Ticks { get; set; }
}
