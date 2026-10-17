using System.Text.Json.Serialization;

namespace Stillwake.Sim.Debris;

/// <summary>
/// One piece drifting by, its middle at <see cref="X"/>, <see cref="Y"/> in thousandths of a cell,
/// always on the grid. It drifts left by <see cref="Speed"/> a tick from <see cref="Start"/>, but while
/// <see cref="Reel"/> pulls it, and is gone once <see cref="X"/> is below <see cref="End"/>. Read by
/// others, changed by debris alone.
/// </summary>
public sealed class Piece
{
    /// <summary>Its id: they grow in spawn order and are never reused.</summary>
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>The zone it spawned in, by its id in <c>Debris.Zones</c>: the stream past the ship, or the far field round the heroes.</summary>
    [JsonInclude] public string Zone { get; internal set; } = "";
    /// <summary>The item it is: salvage caught as itself, a container that opens when caught, or wreckage broken with an axe.</summary>
    [JsonInclude] public string Item { get; internal set; } = "";
    [JsonInclude] public int X { get; internal set; }
    [JsonInclude] public int Y { get; internal set; }
    /// <summary>How far it drifts left a tick, in thousandths of a cell.</summary>
    [JsonInclude] public int Speed { get; internal set; }
    /// <summary>Where its way begins: <see cref="X"/> at its first tick, but for a piece a world begins with.</summary>
    [JsonInclude] public int Start { get; internal set; }
    /// <summary>It is gone once <see cref="X"/> is below this.</summary>
    [JsonInclude] public int End { get; internal set; }
    /// <summary>The hit points axes have taken off it: below its kind's <c>Wreckage</c> hit points, and none for a piece caught by hand.</summary>
    [JsonInclude] public int Damage { get; internal set; }
    /// <summary>A harpoon's line pulling it to a point instead of its drift, or null (0044).</summary>
    [JsonInclude] public Reeling? Reel { get; internal set; }
    /// <summary>The clock it was last in some hero's keep box, for a zone round the heroes; 0 for the stream's, never despawned by distance.</summary>
    [JsonInclude] public int SeenAt { get; internal set; }
}

/// <summary>
/// A piece reeled in by a harpoon (0044): it comes to (<see cref="ToX"/>, <see cref="ToY"/>), a point on
/// the grid in thousandths of a cell, in <see cref="Left"/> more ticks, then drifts on from there.
/// </summary>
public sealed class Reeling
{
    /// <summary>The peer whose harpoon reels it: the last to send <c>Reel</c> for it.</summary>
    [JsonInclude] public int By { get; internal set; }
    [JsonInclude] public int ToX { get; internal set; }
    [JsonInclude] public int ToY { get; internal set; }
    /// <summary>The ticks till it is there, above zero: the reel clears on the tick it lands.</summary>
    [JsonInclude] public int Left { get; internal set; }
}

/// <summary>A zone's beat: the clock at which it next spawns.</summary>
public sealed class Beat
{
    /// <summary>The zone's id in <c>Debris.Zones</c>.</summary>
    [JsonInclude] public string Zone { get; internal set; } = "";
    [JsonInclude] public int Next { get; internal set; }
}

/// <summary>
/// The debris slice. Public so it can be named, its members internal: others read
/// <see cref="DebrisModule.Pieces"/>.
/// </summary>
public sealed class DebrisState
{
    /// <summary>The pieces out, of every zone, in spawn order: so in id order.</summary>
    [JsonInclude] internal List<Piece> Pieces { get; set; } = [];
    /// <summary>The id the next piece takes: ids are never reused.</summary>
    [JsonInclude] internal int NextId { get; set; } = 1;
    /// <summary>Ticks this module has run.</summary>
    [JsonInclude] internal int Clock { get; set; }
    /// <summary>Each zone's beat, a zone's added the first tick it has none, due its <c>Every</c> on from then.</summary>
    [JsonInclude] internal List<Beat> Beats { get; set; } = [];
    /// <summary>Pieces put out at once on the first tick, the map's <c>Layout.DebrisAtStart</c>.</summary>
    [JsonInclude] internal int AtStart { get; set; }
}
