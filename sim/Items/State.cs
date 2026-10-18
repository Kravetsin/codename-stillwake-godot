using System.Text.Json.Serialization;

namespace Stillwake.Sim.Items;

/// <summary>
/// Some of one item in a slot: <c>Count</c> from 1 to the item's stack limit. What one item of it holds of its
/// own, <c>Charge</c> (a battery's, from 0 to its <c>Capacity</c>), is on the stack, only on items of a stack of
/// one; zero for anything else, and unwritten in a save while zero. A copy made with <c>with</c> keeps it.
/// </summary>
public sealed record Stack(
    string Item,
    int Count,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int Charge = 0);

/// <summary>Where a stack is: a slot of machine <c>Crate</c>, or of the sender's own inventory when there is none.</summary>
public sealed record Place(int Slot, int? Crate = null);

/// <summary>Where a move goes: a slot as a <see cref="Place"/>, or, with no slot, that side as a whole.</summary>
public sealed record Target(int? Slot = null, int? Crate = null)
{
    public static implicit operator Target(Place p) => new(p.Slot, p.Crate);
}

/// <summary>
/// What a hero left where it died (0061): the peer it was and the items module's tick it died on. Every floor
/// stack it left carries one, alike, so the stacks of one death are one body and stacks of two never merge.
/// </summary>
public sealed record Body(int Peer, int Died);

/// <summary>A stack lying on the floor, flying where it was thrown, or floating in space, for anyone to pick up.</summary>
public sealed class FloorStack
{
    /// <summary><see cref="By"/> for a stack no peer dropped, which anyone may pick up at once: peer ids are never negative.</summary>
    public const int Nobody = -1;

    [JsonInclude] public int Id { get; internal set; }
    [JsonInclude] public string Item { get; internal set; } = "";
    /// <summary>From 1 to the item's stack limit.</summary>
    [JsonInclude] public int Count { get; internal set; }
    /// <summary>What its one item holds of its own, as <see cref="Stack.Charge"/>: kept from the slot it was dropped from to the one it is picked into.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Charge { get; internal set; }
    /// <summary>The cell it is in, kept with its position.</summary>
    [JsonInclude] public int Cell { get; internal set; }
    /// <summary>Its bottom middle, and its speed, in thousandths of a cell and per tick.</summary>
    [JsonInclude] public int X { get; internal set; }
    [JsonInclude] public int Y { get; internal set; }
    [JsonInclude] public int Vx { get; internal set; }
    [JsonInclude] public int Vy { get; internal set; }
    /// <summary>The peer that dropped it (or <see cref="Nobody"/>), and on which of the module's ticks: its own may not take it back at once.</summary>
    [JsonInclude] public int By { get; internal set; }
    [JsonInclude] public int At { get; internal set; }
    /// <summary>Whose body it is part of, when a hero left it where it died (0061); none for anything else, and unwritten in a save.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Body? Body { get; internal set; }

    /// <summary>What it holds as a slot would hold it, every value of its own with it.</summary>
    internal Stack AsStack() => new(Item, Count, Charge);
}

/// <summary>A peer's inventory, or what a machine holds: the slots, each a stack or null.</summary>
internal sealed class Held
{
    /// <summary>The peer's id, or the machine's.</summary>
    public int Id { get; set; }
    public Stack?[] Slots { get; set; } = [];
}

/// <summary>An electric item a peer's hero runs (0055): it draws its <c>Draw</c> from that peer's belt every tick.</summary>
internal sealed class Runner
{
    public int Peer { get; set; }
    public string Item { get; set; } = "";
}

/// <summary>The item an input slot of a fed station or a fire remembers (0062): what drones keep it full of, held or not.</summary>
public sealed record Kept(int Machine, int Slot, string Item);

/// <summary>The items' slice. Public so modules can be typed by it; its members internal.</summary>
public sealed class ItemsState
{
    /// <summary>The inventories changed at least once, by peer id, in id order; one never changed is empty.</summary>
    [JsonInclude] internal List<Held> Inventories { get; set; } = [];
    /// <summary>The machines holding something, by machine id, in id order; an empty one has no entry.</summary>
    [JsonInclude] internal List<Held> Crates { get; set; } = [];
    /// <summary>Creative mode: nothing is ever taken. A world setting, saved with the world.</summary>
    [JsonInclude] internal bool Creative { get; set; }
    /// <summary>Stacks on the floor, in id order.</summary>
    [JsonInclude] internal List<FloorStack> Floor { get; set; } = [];
    [JsonInclude] internal int NextFloorId { get; set; } = 1;
    /// <summary>The electric items heroes run, one at most a peer, in peer order.</summary>
    [JsonInclude] internal List<Runner> Running { get; set; } = [];
    /// <summary>Ticks since the world began, counted here: the module's own clock.</summary>
    [JsonInclude] internal int Ticks { get; set; }
    /// <summary>What input slots remember, by machine and slot, in that order: the last item put in, until taken out by hand.</summary>
    [JsonInclude] internal List<Kept> Kept { get; set; } = [];
}
