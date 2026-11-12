using System.Text.Json.Serialization;

namespace Stillwake.Sim.Crew;

/// <summary>A hero, named by the peer that plays it: read by others, changed by crew alone.</summary>
public sealed class Hero
{
    /// <summary>The peer that plays it (TypeScript's <c>hero</c>).</summary>
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>
    /// The cell its body is in as its client last reported it (<see cref="Enter"/>): on entering another room,
    /// and else at most every <c>Crew.ReportEvery</c> ticks while it moves, so up to a second and a round trip
    /// behind the body. What it breathes, and what anything else reading where heroes are reads.
    /// </summary>
    [JsonInclude] public int Cell { get; internal set; }
    /// <summary>
    /// Its air, from nothing to its capacity: the worn suit's <c>Tank</c>, or <c>Crew.Lungs</c> bare (2026-10-27-suits-are-worn).
    /// </summary>
    [JsonInclude] public int Air { get; internal set; }
    /// <summary>
    /// Ticks left of its last breath, its air run dry, before it passes out; 0 while it has air or breathes room air
    /// (2026-11-05-the-last-breath).
    /// </summary>
    [JsonInclude] public int Gasp { get; internal set; }
    /// <summary>Ticks left passed out; 0 while conscious.</summary>
    [JsonInclude] public int Out { get; internal set; }
    /// <summary>Whether it runs, as its client last said: it breathes <c>Crew.Exerted</c> a breath.</summary>
    [JsonInclude] public bool Exerting { get; internal set; }
    /// <summary>Whether it thrusts with its jetpack, as its client last said, and wears one, awake: its air pays <c>Jetpack.TicksPerUnit</c> ticks a unit.</summary>
    [JsonInclude] public bool Thrusting { get; internal set; }
    /// <summary>Ticks of thrust toward the next unit its air pays, kept between bursts so short taps add up.</summary>
    [JsonInclude] public int ThrustTicks { get; internal set; }
    /// <summary>
    /// Thousandths of a unit its cape has spared short of a whole one, 0 to 999, kept for the next cold or blow so the cape's
    /// share is exact over time (2026-10-27-the-cape-is-worn).
    /// </summary>
    [JsonInclude] public int Spared { get; internal set; }
    /// <summary>
    /// Whether its suit's mode is switched on (<see cref="SuitMode"/>): the hero's, kept while no body with <c>Switch</c> is
    /// worn, working only while one is (<see cref="ICrewQueries.SuitOn"/>; 2026-11-03-the-regen-suit).
    /// </summary>
    [JsonInclude] public bool SuitOn { get; internal set; }
    /// <summary>How many hurts it has taken, each a blow, a shock, poison or a burn (2026-11-08-hurts-are-shown): the client shows each new one.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Hurts { get; internal set; }
    /// <summary>What hurt it last; <see cref="Harm.None"/> before anything has.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public Harm Hurt { get; internal set; }
    /// <summary>Its hidden wanted meter, the worth of what it broke and killed, 0 to <see cref="Need"/>: full, a catcher is sent (catcher-loot).</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Wanted { get; internal set; }
    /// <summary>What fills its meter, drawn after each reset; 0 while not drawn yet.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Need { get; internal set; }
    /// <summary>Breaths left of the alarm, drawn as its meter filled: at 0 with the meter full, its catcher hunts it.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Coming { get; internal set; }
    /// <summary>
    /// Ticks until it may breathe another canister, 0 when it may: ticks left, never a tick of the clock, as each surface
    /// keeps its own and a hero carries this across (food-and-the-body).
    /// </summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int CanisterIn { get; internal set; }
}

/// <summary>
/// The crew's slice. Public so modules can be typed by it, its members internal: others go through
/// <see cref="ICrewQueries"/>.
/// </summary>
public sealed class CrewState
{
    /// <summary>Where heroes are made: the lifepod's hatch, else the map's S; the first cell when it has neither.</summary>
    [JsonInclude] internal int Spawn { get; set; }
    /// <summary>The map's lifepod's top-left cell, whose air a hero near it draws into its suit; null for a map without one.</summary>
    [JsonInclude] internal int? Pod { get; set; }
    /// <summary>Ticks run: breaths come every <c>Crew.BreathEvery</c> of them.</summary>
    [JsonInclude] internal int Clock { get; set; }
    /// <summary>Every hero, in id order.</summary>
    [JsonInclude] internal List<Hero> Heroes { get; set; } = [];
}
