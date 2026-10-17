using System.Text.Json.Serialization;

namespace Stillwake.Sim.Atmosphere;

/// <summary>The oxygen a tank or a generator's store holds, by its machine id.</summary>
internal sealed class TankAir
{
    public int Tank { get; set; }
    public int Oxygen { get; set; }
}

/// <summary>
/// A generator's work toward its next unit: a unit is <c>Air.LeakEvery × 1000</c>, and a generator adds
/// <c>Air.GeneratorRate</c> × its power's satisfaction a tick.
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
/// <param name="Oxygen">The oxygen on its vents' networks, each network counted once.</param>
/// <param name="Made">What the generators on those networks make every <c>Air.LeakEvery</c> ticks, together.</param>
/// <param name="Lasts">
/// Ticks its networks last at what this room alone draws: refilling its leak if aired, its vents' drain
/// if venting. Null otherwise, when nothing is drawn, or when a generator feeds it.
/// </param>
/// <param name="Problem">Why a room in vacuum gets no air, as an i18n key.</param>
public sealed record RoomAir(
    int Room, RoomStatus Status, int Aired, int Leak, int Oxygen, int Made, int? Lasts, string? Problem);

/// <summary>
/// The atmosphere's slice. Public so modules can be typed by it, its members internal: others go
/// through <see cref="IAtmosphereQueries"/> and <see cref="IAtmosphereOps"/>.
/// </summary>
public sealed class AtmosphereState
{
    /// <summary>The oxygen in each tank or generator holding any, by machine id, in id order.</summary>
    [JsonInclude] internal List<TankAir> Tanks { get; set; } = [];
    /// <summary>The cells holding air, a unit of oxygen each, sorted.</summary>
    [JsonInclude] internal List<int> Aired { get; set; } = [];
    /// <summary>Each generator's work toward its next unit, by machine id, in id order.</summary>
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
