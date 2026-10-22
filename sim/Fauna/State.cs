using System.Text.Json.Serialization;

namespace Stillwake.Sim.Fauna;

/// <summary>What a creature is doing: saved by number, so never renumber one.</summary>
public enum Mode
{
    /// <summary>Standing about, still.</summary>
    Idle = 0,
    /// <summary>Walking its way at its species' wander.</summary>
    Wander = 1,
    /// <summary>Bolting its way, away from the nearest hero when it began, at its species' flee.</summary>
    Flee = 2,
    /// <summary>Knocked by a hit and flinching, steering nothing: flight follows.</summary>
    Hurt = 3,
    /// <summary>Crouched to ram the nearest hero, standing (0103): a charge follows.</summary>
    WindUp = 4,
    /// <summary>Charging at the nearest hero as its wind-up ended, along the ground: a hit, passing it, a wall or a drop end it.</summary>
    Charge = 5,
    /// <summary>Struck and fleeing a moment before it turns back to ram the hero (0103): a flight, as <see cref="Flee"/>.</summary>
    Retreat = 6,
    /// <summary>Fleeing after its ram, hit or missed: a flight that never rams, till it calms or is struck again (0103).</summary>
    Escape = 7,
}

/// <summary>
/// One creature (0102): its body <see cref="Content.Fauna.Species"/> cells across and tall, its bottom middle at <see cref="X"/>,
/// <see cref="Y"/> in thousandths of a cell, always on the grid and out of what is solid. It faces the way it moves, the sign of <see cref="VX"/>;
/// still, the way it last moved, which only the client keeps. Read by others, changed by fauna alone.
/// </summary>
public sealed class Creature
{
    /// <summary>Its id: they grow in spawn order and are never reused.</summary>
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>Its species, by its id in <c>Fauna.All</c>.</summary>
    [JsonInclude] public string Species { get; internal set; } = "";
    [JsonInclude] public int X { get; internal set; }
    [JsonInclude] public int Y { get; internal set; }
    /// <summary>Its speed across a tick, in thousandths of a cell, right while positive: 0 standing still.</summary>
    [JsonInclude] public int VX { get; internal set; }
    /// <summary>Its speed down a tick, up while negative.</summary>
    [JsonInclude] public int VY { get; internal set; }
    /// <summary>The hit points weapons have taken off it: below its species' hit points.</summary>
    [JsonInclude] public int Damage { get; internal set; }
    [JsonInclude] public Mode Mode { get; internal set; }
    /// <summary>Ticks left in its mode.</summary>
    [JsonInclude] public int ModeLeft { get; internal set; }
    /// <summary>The clock it was last in some hero's keep box: <c>Fauna.Linger</c> past it, out of every one, it is gone.</summary>
    [JsonInclude] public int SeenAt { get; internal set; }
}

/// <summary>The fauna slice. Public so it can be named, its members internal: others read <see cref="FaunaModule.Creatures"/>.</summary>
public sealed class FaunaState
{
    /// <summary>The creatures, in spawn order: so in id order.</summary>
    [JsonInclude] internal List<Creature> Creatures { get; set; } = [];
    /// <summary>The id the next creature takes: ids are never reused.</summary>
    [JsonInclude] internal int NextId { get; set; } = 1;
    /// <summary>Ticks this module has run on a surface with life: spawns come when it is a multiple of <c>Fauna.Beat</c>.</summary>
    [JsonInclude] internal int Clock { get; set; }
}
