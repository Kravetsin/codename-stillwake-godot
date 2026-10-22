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
    /// clouds, sun and land, <c>Render.PlanetSky</c>). <c>Tint</c>: its stone's colour by depth band, multiplied over the
    /// ground in its rows (0080), one a band, blended from one band's middle to the next, from <c>TintFrom</c> rows under the
    /// surface's middle over <c>TintOver</c> rows, so the land under the sky keeps its own colours.
    /// </summary>
    public sealed record Look(string Backdrop, IReadOnlyList<(float R, float G, float B)> Tint, int TintFrom, int TintOver);

    /// <summary>The pictures a backdrop set holds, <c>art/{Backdrop}/{name}.png</c>: the layers <c>Render.PlanetSky</c> draws.</summary>
    public static readonly IReadOnlyList<string> BackdropPictures = ["sky", "giant", "clouds", "sun", "far", "ridge", "hills"];

    /// <summary>
    /// The first planet's, the "Stillwake" style's (0079): its warm day; the coldest band a cool blue-grey, the garden as it is,
    /// the amber deep warmer, the ember band a warm red-brown.
    /// </summary>
    public static readonly Look First = new("planet", [(0.86f, 0.94f, 1f), (0.94f, 1f, 0.98f), (1f, 0.9f, 0.88f), (1f, 0.78f, 0.66f)], TintFrom: 80, TintOver: 120);

    /// <summary>By surface kind: every planet type, and the test strip, drawn under the first planet's rules and sky.</summary>
    public static readonly IReadOnlyDictionary<string, Look> Of = new Dictionary<string, Look>
    {
        [Stillwake.Content.PlanetTypes.First.Id] = First,
        [Stillwake.Content.PlanetTypes.Small.Id] = First,
        [Stillwake.Content.Surfaces.TestStrip.Id] = First,
    };
}
