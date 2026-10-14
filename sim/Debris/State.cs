using System.Text.Json.Serialization;

namespace Stillwake.Sim.Debris;

/// <summary>
/// One piece drifting past, its middle at <see cref="X"/>, <see cref="Y"/> in thousandths of a cell,
/// always on the grid. It drifts left by <see cref="Speed"/> a tick from <see cref="Start"/> and is gone
/// once <see cref="X"/> is below <see cref="End"/>. Read by others, changed by debris alone.
/// </summary>
public sealed class Piece
{
    /// <summary>Its id: they grow in spawn order and are never reused.</summary>
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>The item it is: salvage caught as itself, or a container that opens when caught.</summary>
    [JsonInclude] public string Item { get; internal set; } = "";
    [JsonInclude] public int X { get; internal set; }
    [JsonInclude] public int Y { get; internal set; }
    /// <summary>How far it drifts left a tick, in thousandths of a cell.</summary>
    [JsonInclude] public int Speed { get; internal set; }
    /// <summary>Where it spawned: <see cref="X"/> at its first tick.</summary>
    [JsonInclude] public int Start { get; internal set; }
    /// <summary>It is gone once <see cref="X"/> is below this.</summary>
    [JsonInclude] public int End { get; internal set; }
}

/// <summary>
/// The debris slice. Public so it can be named, its members internal: others read
/// <see cref="DebrisModule.Pieces"/>.
/// </summary>
public sealed class DebrisState
{
    /// <summary>The pieces out, in spawn order.</summary>
    [JsonInclude] internal List<Piece> Pieces { get; set; } = [];
    /// <summary>The id the next piece takes: ids are never reused.</summary>
    [JsonInclude] internal int NextId { get; set; } = 1;
    /// <summary>Ticks this module has run.</summary>
    [JsonInclude] internal int Clock { get; set; }
    /// <summary>The clock at which the next piece is due.</summary>
    [JsonInclude] internal int NextSpawn { get; set; } = Content.Debris.Every;
}
