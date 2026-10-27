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
/// <summary>Saved by number, so never renumber one.</summary>
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

/// <summary>What a <see cref="Ref"/> points at: nothing yet, or a hero by its peer. More kinds come with their first users.</summary>
public enum RefKind
{
    None = 0,
    Hero = 1,
}

/// <summary>Something a creature points at, resolved where it is used: one that resolves to nothing reads as none.</summary>
public readonly record struct Ref(RefKind Kind, int Id)
{
    public static readonly Ref None = new(RefKind.None, 0);

    public static Ref Hero(int peer) => new(RefKind.Hero, peer);
}

/// <summary>One number a rule keeps on a creature, by its key.</summary>
public sealed class Note
{
    [JsonInclude] public string Key { get; internal set; } = "";
    [JsonInclude] public int Value { get; internal set; }
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
    /// <summary>Its grudge or aim: the hero it means to strike, or none.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public Ref Target { get; internal set; }

    /// <summary>
    /// What its rules keep, a number a key, in ordinal order of the key (<see cref="string.CompareOrdinal(string, string)"/>), so a
    /// load lays them out as the save did: never a dictionary, whose order a load would not keep.
    /// </summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] internal List<Note>? Mem { get; set; }

    /// <summary>The clock it was last in some hero's keep box: <c>Creatures.Linger</c> past it, out of every one, it is gone.</summary>
    [JsonInclude] public int SeenAt { get; internal set; }

    /// <summary>What its rules keep under <paramref name="key"/>; 0 for nothing kept.</summary>
    internal int Recall(string key)
    {
        int at = Find(key);
        return at >= 0 ? Mem![at].Value : 0;
    }

    /// <summary>Keeps <paramref name="value"/> under <paramref name="key"/>, in its ordinal place.</summary>
    internal void Keep(string key, int value)
    {
        Mem ??= [];
        int at = Find(key);
        if (at >= 0) Mem[at].Value = value;
        else Mem.Insert(~at, new Note { Key = key, Value = value });
    }

    /// <summary>The binary search of <paramref name="key"/> in <see cref="Mem"/>: its place, or the complement of where it would go.</summary>
    private int Find(string key)
    {
        if (Mem == null) return ~0;
        int lo = 0, hi = Mem.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2, order = string.CompareOrdinal(Mem[mid].Key, key);
            if (order == 0) return mid;
            if (order < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    /// <summary>Killed this tick: gone to every command, query and cap, swept out before the creatures next act. Never saved.</summary>
    [JsonIgnore] internal bool Dead { get; set; }

    /// <summary>How it shows what it does (<see cref="Creatures.Posture"/>).</summary>
    [JsonIgnore] public Posture Posture => RuleBook.PostureOf(this);

    /// <summary>Whether it is in flight: steered as one, at its run, turned and leaping alike.</summary>
    [JsonIgnore] public bool Fleeing => RuleBook.FleeingOf(this);

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
    /// <summary>The newest debris piece that has drawn whether a guard comes with it (<c>Surfaces.Kind.Guards</c>): those after it draw next.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int Watermark { get; set; }
    /// <summary>The shots in flight, in id order (shots-are-the-sims).</summary>
    [JsonIgnore] internal List<Shot> Shots { get; set; } = [];

    /// <summary>The shots as saved: none written while none fly, so a world that never fired saves as before shots.</summary>
    [JsonInclude, JsonPropertyName("shots"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Shot>? SavedShots { get => Shots.Count > 0 ? Shots : null; set => Shots = value ?? []; }
    /// <summary>The arcs struck and still taking heroes' word they were crossed, in id order (2026-10-27-sentry-arc).</summary>
    [JsonIgnore] internal List<Arc> Arcs { get; set; } = [];

    /// <summary>The arcs as saved: none written while none linger.</summary>
    [JsonInclude, JsonPropertyName("arcs"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Arc>? SavedArcs { get => Arcs.Count > 0 ? Arcs : null; set => Arcs = value ?? []; }

    /// <summary>The shots ever fired and arcs ever struck: the next one's id is one more. Ids are never reused.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int Fired { get; set; }
}

/// <summary>
/// A shot in flight (shots-are-the-sims): what flies (<see cref="Content.Shots.Projectile"/>), who fired it, its point and speed in
/// thousandths of a cell, always on the grid, and the ticks of flight it has left.
/// </summary>
public sealed class Shot
{
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>Its projectile, by its id in <c>Shots.Projectiles</c>.</summary>
    [JsonInclude] public string Projectile { get; internal set; } = "";
    /// <summary>The gun it was fired from, by its item; none for a creature's, whose damage is its projectile's alone.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Gun { get; internal set; }
    /// <summary>Its shooter: a hero by its peer, or none.</summary>
    [JsonInclude] public Ref By { get; internal set; }
    [JsonInclude] public int X { get; internal set; }
    [JsonInclude] public int Y { get; internal set; }
    [JsonInclude] public int VX { get; internal set; }
    [JsonInclude] public int VY { get; internal set; }
    [JsonInclude] public int Left { get; internal set; }
    /// <summary>The things it flew on through (2026-10-27-shots-pierce), broken, killed or, for a gun that pierces all, still standing, its damages kept through each; none written while none.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Pierced { get; internal set; }
    /// <summary>
    /// What a shot that pierces everything hit and flew on through that still stands, in the order hit (2026-10-27-the-railgun): never
    /// hit again, a body spanning cells and ticks. Pierced counts these and what broke.
    /// </summary>
    [JsonIgnore] internal List<ShotPass> Passed { get; set; } = [];

    /// <summary>What it passed, as saved: none written while none, so a shot that passed nothing saves as before.</summary>
    [JsonInclude, JsonPropertyName("passed"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<ShotPass>? SavedPassed { get => Passed.Count > 0 ? Passed : null; set => Passed = value ?? []; }

    internal ShotMotion Motion => new(X, Y, VX, VY, Left);
}

/// <summary>
/// A shot that ended this tick: its id, its projectile, its gun (none for a creature's), the cell it met or last flew in, and
/// whether it met something there or ran out of reach or grid.
/// </summary>
public readonly record struct ShotEnd(int Id, string Projectile, string? Gun, int Cell, bool Met);

/// <summary>What a shot hits in a cell: what is solid there, a creature, or a wreck. Saved by number in what a shot passed, so never renumber one.</summary>
public enum ShotTarget
{
    Solid = 0,
    Creature = 1,
    Wreck = 2,
}

/// <summary>
/// A hit of a shot this tick (2026-10-27-shots-pierce): the shot by its id, its shooter, projectile and gun; the cell it hit
/// <see cref="What"/> in, from (<see cref="From"/>), the shot's point as the tick began, in thousandths of a cell; and
/// what it took (block damage off what is solid) and whether it broke or killed it, the shot then flying on. One with nothing
/// left to take is listed too, taking nothing.
/// </summary>
public readonly record struct ShotHit(int Shot, Ref By, string Projectile, string? Gun, int Cell, (int X, int Y) From, ShotTarget What, int Damage, bool Broke);

/// <summary>
/// An arc a creature struck (2026-10-27-sentry-arc): what it carries (<see cref="Content.Shots.Projectile"/>), from its creature's
/// middle (<see cref="X0"/>, <see cref="Y0"/>) to where it struck (<see cref="X1"/>, <see cref="Y1"/>), in thousandths of a cell;
/// the ticks it still takes a hero's word it crossed them, and the heroes, by peer, it has hurt.
/// </summary>
public sealed class Arc
{
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>Its projectile, by its id in <c>Shots.Projectiles</c>.</summary>
    [JsonInclude] public string Projectile { get; internal set; } = "";
    [JsonInclude] public int X0 { get; internal set; }
    [JsonInclude] public int Y0 { get; internal set; }
    [JsonInclude] public int X1 { get; internal set; }
    [JsonInclude] public int Y1 { get; internal set; }
    [JsonInclude] public int Left { get; internal set; }
    /// <summary>The peers whose heroes it has hurt, in order; none written while none.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] internal List<int>? Paid { get; set; }
}
