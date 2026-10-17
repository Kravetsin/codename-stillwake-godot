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
    /// <summary>The oxygen its suit holds, from nothing to <c>Crew.Suit</c>.</summary>
    [JsonInclude] public int Suit { get; internal set; }
    /// <summary>Ticks left passed out; 0 while conscious.</summary>
    [JsonInclude] public int Out { get; internal set; }
    /// <summary>Its suit colour, of <c>Crew.Colours</c>: what tells heroes apart.</summary>
    [JsonInclude] public int Colour { get; internal set; }
    /// <summary>Whether it runs, as its client last said: it breathes <c>Crew.Exerted</c> a breath.</summary>
    [JsonInclude] public bool Exerting { get; internal set; }
    /// <summary>Whether it thrusts with its jetpack, as its client last said, and wears one, awake: the suit pays <c>Jetpack.TicksPerUnit</c> ticks a unit.</summary>
    [JsonInclude] public bool Thrusting { get; internal set; }
    /// <summary>Ticks of thrust toward the next unit the suit pays, kept between bursts so short taps add up.</summary>
    [JsonInclude] public int ThrustTicks { get; internal set; }
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
