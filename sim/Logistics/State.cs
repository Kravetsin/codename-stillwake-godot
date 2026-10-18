using System.Text.Json.Serialization;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Logistics;

/// <summary>What a crate is to drones (0062): storage by default, a provider they take from first, or a requester they bring to. Saved as its number: never renumber.</summary>
public enum Role { Storage = 0, Provider = 1, Requester = 2 }

/// <summary>A machine asks drones for <c>Count</c> of <c>Item</c>: they bring what it holds and what is on its way short of that.</summary>
public sealed record Request(string Item, int Count);

/// <summary>Where a flying drone is going: to take its job's items, to put them in, or home to its bay. Saved as its number: never renumber.</summary>
public enum Leg { Source = 0, Target = 1, Home = 2 }

/// <summary>
/// A drone in flight, a sim entity (0062): the item it is while docked (<see cref="Stack"/>, all it holds of its own
/// with it), its bay, where it is, and its job: <c>Count</c> of <c>Item</c> from slot <c>Slot</c> of machine
/// <c>Source</c> to machine <c>Target</c>, and the <see cref="Cargo"/> it carries. Nothing about its path is kept.
/// </summary>
public sealed class Drone
{
    [JsonInclude] public int Id { get; internal set; }
    [JsonInclude] public int Bay { get; internal set; }
    [JsonInclude] public Stack Stack { get; internal set; } = null!;
    /// <summary>Its middle, in thousandths of a cell: a cell's centre, or on the way between two side by side.</summary>
    [JsonInclude] public int X { get; internal set; }
    [JsonInclude] public int Y { get; internal set; }
    /// <summary>The cell whose centre it last reached, and the one it flies to: the same while it stands there.</summary>
    [JsonInclude] public int Cell { get; internal set; }
    [JsonInclude] public int To { get; internal set; }
    [JsonInclude] public Leg Leg { get; internal set; }
    [JsonInclude] public int Source { get; internal set; }
    [JsonInclude] public int Slot { get; internal set; }
    [JsonInclude] public int Target { get; internal set; }
    [JsonInclude] public string Item { get; internal set; } = "";
    [JsonInclude] public int Count { get; internal set; }
    /// <summary>What it carries, from its source to its target; none on the way there and home.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Stack? Cargo { get; internal set; }
}

/// <summary>A crate's role other than storage.</summary>
internal sealed class CrateRole
{
    public int Machine { get; set; }
    public Role Role { get; set; }
}

/// <summary>What one machine requests, in the order set.</summary>
internal sealed class Requesting
{
    public int Machine { get; set; }
    public List<Request> Requests { get; set; } = [];
}

/// <summary>The logistics slice. Public so modules can be typed by it; its members internal.</summary>
public sealed class LogisticsState
{
    /// <summary>The crates that are not storage, by machine id, in id order: storage, the default, is not saved.</summary>
    [JsonInclude] internal List<CrateRole> Roles { get; set; } = [];
    /// <summary>The machines requesting anything, by machine id, in id order; kept for a machine taken apart, never read.</summary>
    [JsonInclude] internal List<Requesting> Requests { get; set; } = [];
    /// <summary>Drones in flight, in id order.</summary>
    [JsonInclude] internal List<Drone> Drones { get; set; } = [];
    [JsonInclude] internal int NextId { get; set; } = 1;
    /// <summary>Ticks since the world began, counted here: when bays look.</summary>
    [JsonInclude] internal int Ticks { get; set; }
}
