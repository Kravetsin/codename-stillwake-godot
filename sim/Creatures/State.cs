using System.Text.Json.Serialization;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// How a creature shows what it does, derived from its drive and act in charge, never saved: what the client and the tests read
/// while looks are still keyed by it (2026-10-26-creatures-are-composed). New looks read the drive, act and phase themselves.
/// </summary>
public enum Posture
{
    /// <summary>Standing about, still: grazing.</summary>
    Idle,
    /// <summary>Walking its way: grazing.</summary>
    Wander,
    /// <summary>Bolting from what struck it.</summary>
    Flee,
    /// <summary>Struck and fleeing a moment before it turns back on the hero (0103).</summary>
    Retreat,
    /// <summary>Crouched for its move: a lunge's wind-up.</summary>
    WindUp,
    /// <summary>Its move under way: a lunge's charge.</summary>
    Charge,
    /// <summary>Fleeing after its move, hit or missed: a flight that never turns back.</summary>
    Escape,
}

/// <summary>The phase of an act: crouched for it, then under way.</summary>
public enum ActPhase
{
    WindUp = 0,
    Active = 1,
}

/// <summary>An act under way (0103): which of its species' moves, its phase and the ticks left in it.</summary>
public sealed class Act
{
    [JsonInclude] public int Move { get; internal set; }
    [JsonInclude] public ActPhase Phase { get; internal set; }
    [JsonInclude] public int Left { get; internal set; }
}

/// <summary>
/// One creature (0102): its body <see cref="Content.Creatures.Species"/> cells across and tall, its bottom middle at <see cref="X"/>,
/// <see cref="Y"/> in thousandths of a cell, always on the grid and out of what is solid. It faces the way it moves, the sign of <see cref="VX"/>;
/// still, the way it last moved, which only the client keeps. Read by others, changed by creatures alone.
/// </summary>
public sealed class Creature
{
    /// <summary>Its id: they grow in spawn order and are never reused.</summary>
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>Its species, by its id in <c>Creatures.All</c>.</summary>
    [JsonInclude] public string Species { get; internal set; } = "";
    [JsonInclude] public int X { get; internal set; }
    [JsonInclude] public int Y { get; internal set; }
    /// <summary>Its speed across a tick, in thousandths of a cell, right while positive: 0 standing still.</summary>
    [JsonInclude] public int VX { get; internal set; }
    /// <summary>Its speed down a tick, up while negative.</summary>
    [JsonInclude] public int VY { get; internal set; }
    /// <summary>The hit points weapons have taken off it: below its species' hit points.</summary>
    [JsonInclude] public int Damage { get; internal set; }
    /// <summary>The drive in charge, by its place in its species' brain.</summary>
    [JsonInclude] public int Drive { get; internal set; }
    /// <summary>Ticks left to the drive in charge; frozen while an act is under way.</summary>
    [JsonInclude] public int DriveLeft { get; internal set; }
    /// <summary>Where the drive in charge is within itself: a grazer idling (0) or wandering (1).</summary>
    [JsonInclude] public int Step { get; internal set; }
    /// <summary>The act under way, null for none.</summary>
    [JsonInclude] public Act? Act { get; internal set; }
    /// <summary>The clock it was last in some hero's keep box: <c>Creatures.Linger</c> past it, out of every one, it is gone.</summary>
    [JsonInclude] public int SeenAt { get; internal set; }

    /// <summary>Killed this tick: gone to every command, query and cap, swept out before the creatures next act. Never saved.</summary>
    [JsonIgnore] internal bool Dead { get; set; }

    /// <summary>How it shows what it does (<see cref="Creatures.Posture"/>).</summary>
    [JsonIgnore] public Posture Posture => RuleBook.PostureOf(this);

    /// <summary>The ticks left to what it does: its act's while one is under way, else its drive's.</summary>
    [JsonIgnore] public int Left => Act?.Left ?? DriveLeft;
}

/// <summary>The creatures slice. Public so it can be named, its members internal: others read <see cref="CreaturesModule.Creatures"/>.</summary>
public sealed class CreaturesState
{
    /// <summary>The creatures, in spawn order: so in id order.</summary>
    [JsonInclude] internal List<Creature> Creatures { get; set; } = [];
    /// <summary>The id the next creature takes: ids are never reused.</summary>
    [JsonInclude] internal int NextId { get; set; } = 1;
    /// <summary>Ticks this module has run on a surface with life: spawns come when it is a multiple of <c>Creatures.Beat</c>.</summary>
    [JsonInclude] internal int Clock { get; set; }
}
