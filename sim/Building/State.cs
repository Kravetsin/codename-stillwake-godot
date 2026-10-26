using System.Text.Json.Serialization;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>
/// A plan, with the ticks of work done on it: a blueprint, to <c>Place</c> something, with what was
/// <c>Delivered</c> to it so far, an item for each part of its work begun (ADR 0063); or a mark, to <c>Clear</c>
/// what a layer of a cell holds. A machine's is kept at its top-left cell, whichever cell it was
/// marked from. Exactly one of <c>Place</c> and <c>Clear</c> is set.
/// </summary>
/// <param name="Delivered">What a blueprint was given so far, each item once, in the order first given; none for a mark.</param>
public sealed record Plan(int Id, int Cell, int Progress, Placement? Place, string? Clear, IReadOnlyList<Stack> Delivered)
{
    /// <summary>A blueprint as it is placed: nothing delivered, no work done.</summary>
    public static Plan Blueprint(int id, int cell, Placement place) => new(id, cell, 0, place, null, []);

    /// <summary>A mark as it is made, on <paramref name="layer"/> of <paramref name="cell"/>.</summary>
    public static Plan Mark(int id, int cell, string layer) => new(id, cell, 0, null, layer, []);
}

/// <summary>
/// A natural wall at <c>Cell</c> dug part way (0077): <c>Damage</c> of its <c>Strength</c> taken by weapons' hits, the last on
/// tick <c>At</c> of the building's clock; <c>Builds.CrackHeals</c> ticks after it, healed. <c>Back</c>: its back wall's (0107).
/// </summary>
public sealed record Crack(int Cell, int Damage, int At, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Back = false);

/// <summary>
/// The building slice. Public so modules can be typed by it, its members internal: others read it
/// through <see cref="BuildingModule"/>'s queries.
/// </summary>
public sealed class BuildingState
{
    /// <summary>The plans in the order they were made.</summary>
    [JsonInclude] internal List<Plan> Plans { get; set; } = [];
    [JsonInclude] internal int NextPlanId { get; set; } = 1;
    /// <summary>The natural walls dug part way, by cell, ascending (0077): each below its material's strength.</summary>
    [JsonInclude] internal List<Crack> Cracks { get; set; } = [];
    /// <summary>Ticks run: when a crack was last hit, for it to heal (0077).</summary>
    [JsonInclude] internal int Clock { get; set; }
    /// <summary>The cells of the plants felled since the ground grew (0079), ascending: the rest of its plants grow again from the seed.</summary>
    [JsonInclude] internal List<int> Felled { get; set; } = [];
    /// <summary>Instant building (dev panel): a blueprint placed is built at once. A world setting, kept per surface; off, not written.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal bool Instant { get; set; }
}
