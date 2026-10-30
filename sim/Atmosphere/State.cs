using System.Text.Json.Serialization;

namespace Stillwake.Sim.Atmosphere;

/// <summary>The liquid oxygen a tank holds, or the gas a vaporizer's store holds, by its machine id: told apart by <c>MachineTrait.Gas</c>.</summary>
internal sealed class TankAir
{
    public int Tank { get; set; }
    public int Oxygen { get; set; }
}

/// <summary>
/// A vaporizer's work toward its next unit: a unit is <c>Air.LeakEvery × 1000</c>, and a vaporizer adds
/// <c>Air.GeneratorRate</c> × its power's satisfaction a tick while it has room and liquid behind it.
/// </summary>
internal sealed class GeneratorWork
{
    public int Generator { get; set; }
    public int Part { get; set; }
}

/// <summary>A room's air, read from its cells and its vents' networks every tick.</summary>
public enum RoomStatus { Vacuum, Filling, Aired, Venting }

/// <summary>A room's air, as the air overlay shows it.</summary>
/// <param name="Aired">Its cells holding air, of its volume.</param>
/// <param name="Leak">The cells it loses every <c>Air.LeakEvery</c> ticks through what its air touches.</param>
/// <param name="Oxygen">
/// The liquid in the tanks behind the vaporizers feeding its vents' networks, each tank counted once; null when no
/// vaporizer feeds it. The stores' gas is not counted.
/// </param>
/// <param name="Made">What the vaporizers feeding those networks make every <c>Air.LeakEvery</c> ticks at their power, together.</param>
/// <param name="Lasts">
/// Ticks that liquid lasts, leaving at the smaller of what this room alone draws (refilling its leak if aired, its
/// vents' drain if venting) and what its vaporizers make. Null when nothing feeds it, nothing is drawn or made.
/// </param>
/// <param name="Problem">Why a room in vacuum gets no air, as an i18n key.</param>
public sealed record RoomAir(
    int Room, RoomStatus Status, int Aired, int Leak, int? Oxygen, int Made, int? Lasts, string? Problem);

/// <summary>
/// The atmosphere's slice. Public so modules can be typed by it, its members internal: others go
/// through <see cref="IAtmosphereQueries"/> and <see cref="IAtmosphereOps"/>.
/// </summary>
public sealed class AtmosphereState
{
    /// <summary>The liquid in each tank, and the gas in each vaporizer's store, holding any, by machine id, in id order.</summary>
    [JsonInclude] internal List<TankAir> Tanks { get; set; } = [];
    /// <summary>The cells holding air, a unit of oxygen each, sorted.</summary>
    [JsonInclude] internal List<int> Aired { get; set; } = [];
    /// <summary>Each vaporizer's work toward its next unit, by machine id, in id order.</summary>
    [JsonInclude] internal List<GeneratorWork> Making { get; set; } = [];
    /// <summary>Ticks the air has run: fill and leak beats are this modulo their rates.</summary>
    [JsonInclude] internal int Clock { get; set; }
    /// <summary>Derived: each room's cells in the order its air lies in, by room id; only looked up.</summary>
    [JsonIgnore] internal Dictionary<int, int[]> Orders { get; set; } = new();
    /// <summary>Derived: the rooms per cell the orders were found from.</summary>
    [JsonIgnore] internal int[] OrderedRooms { get; set; } = [];
    /// <summary>Derived: the vents' cells, in machine id order, the orders were found from.</summary>
    [JsonIgnore] internal List<int> OrderedVents { get; set; } = [];
    /// <summary>Derived: the grid's and the rooms' versions the orders were found at.</summary>
    [JsonIgnore] internal int OrderedGrid { get; set; }
    [JsonIgnore] internal int OrderedVersion { get; set; }
}
