using System.Text.Json.Serialization;

namespace Stillwake.Sim.Crew;

/// <summary>A hero, named by the peer that plays it: read by others, changed by crew alone.</summary>
public sealed class Hero
{
    /// <summary>The peer that plays it (TypeScript's <c>hero</c>).</summary>
    [JsonInclude] public int Id { get; internal set; }
    /// <summary>The cell its body is in, as its client last said.</summary>
    [JsonInclude] public int Cell { get; internal set; }
    /// <summary>The oxygen its suit holds, from nothing to <c>Crew.Suit</c>.</summary>
    [JsonInclude] public int Suit { get; internal set; }
    /// <summary>Ticks left passed out; 0 while conscious.</summary>
    [JsonInclude] public int Out { get; internal set; }
    /// <summary>Its suit colour, of <c>Crew.Colours</c>: what tells heroes apart.</summary>
    [JsonInclude] public int Colour { get; internal set; }
    /// <summary>Whether it runs or swims fast, as its client last said: it breathes <c>Crew.Exerted</c> a breath.</summary>
    [JsonInclude] public bool Exerting { get; internal set; }
}

/// <summary>
/// The crew's slice. Public so modules can be typed by it, its members internal: others go through
/// <see cref="ICrewQueries"/>.
/// </summary>
public sealed class CrewState
{
    /// <summary>Where heroes wake, from the map's S; the first cell when it has none.</summary>
    [JsonInclude] internal int Spawn { get; set; }
    /// <summary>Ticks run: breaths come every <c>Crew.BreathEvery</c> of them.</summary>
    [JsonInclude] internal int Clock { get; set; }
    /// <summary>Every hero, in id order.</summary>
    [JsonInclude] internal List<Hero> Heroes { get; set; } = [];
}
