using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// How each planet type looks (0095), keyed by its id, and every other surface kind under a sky: the sim names no look. What
/// grows on it and its ground's materials look as their own catalogs say (<see cref="FloraLooks"/>, <see cref="CaveLooks"/>,
/// <see cref="GroundLooks"/>); this is what is the type's alone.
/// </summary>
public static class PlanetLooks
{
    /// <summary>
    /// A type's look. <c>Backdrop</c>: the set of pictures of the sky behind its ground (<c>art/{Backdrop}/sky</c>, its giant,
    /// clouds and sun, <c>Render.PlanetSky</c>; its land is composed from its own pictures). <c>StrataFrom</c>: how many rows under the
    /// surface's middle its stone is drawn by depth band (<c>Features.Grid.Strata</c>, 2026-11-08-planet-layers), plain above, so the
    /// land under the sky keeps its own grey. <c>Fog</c>: the colour of the fog
    /// at its sides (fog-edges), the haze of its far land.
    /// </summary>
    public sealed record Look(string Backdrop, int StrataFrom, (float R, float G, float B) Fog)
    {
        /// <summary>
        /// Its dusk and night (2026-11-07-night-chills), as <see cref="Daylight"/> fades through them: what the light through the
        /// open sky is multiplied by, and what the backdrop and the fog at its sides are. By default the user's pick for the
        /// first planet, "ochre and slate": an ochre dusk into a cold blue-grey night.
        /// </summary>
        public (float R, float G, float B) DuskSky { get; init; } = (0.88f, 0.64f, 0.4f);
        public (float R, float G, float B) NightSky { get; init; } = (0.09f, 0.105f, 0.14f);
        public (float R, float G, float B) DuskBackdrop { get; init; } = (0.9f, 0.66f, 0.44f);
        public (float R, float G, float B) NightBackdrop { get; init; } = (0.16f, 0.19f, 0.26f);
    }

    /// <summary>The pictures a backdrop set holds, <c>art/{Backdrop}/{name}.png</c>: the sky <c>Render.PlanetSky</c> draws; the land is composed (<c>Features.Backdrop</c>).</summary>
    public static readonly IReadOnlyList<string> BackdropPictures = ["sky", "giant", "clouds", "sun"];

    /// <summary>
    /// The first planet's, the "Stillwake" style's (0079): its warm day; its bands' own stone from 100 rows under the surface's
    /// middle, under nearly every valley.
    /// </summary>
    public static readonly Look First = new("planet", StrataFrom: 100, Fog: (0.74f, 0.79f, 0.72f));

    /// <summary>By surface kind: every planet type, and the test strip, drawn under the first planet's rules and sky.</summary>
    public static readonly IReadOnlyDictionary<string, Look> Of = new Dictionary<string, Look>
    {
        [Stillwake.Content.PlanetTypes.First.Id] = First,
        [Stillwake.Content.PlanetTypes.Small.Id] = First,
        [Stillwake.Content.PlanetTypes.SmallSpire.Id] = First,
        [Stillwake.Content.Surfaces.TestStrip.Id] = First,
    };
}
