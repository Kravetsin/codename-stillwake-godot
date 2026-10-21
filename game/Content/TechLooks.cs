using System.Collections.Generic;
using static Stillwake.Game.IconLooks;

namespace Stillwake.Game;

/// <summary>
/// Each module technology's icon (0086), by its id (<c>Content.Modules</c>' <c>Tech</c>), as the user picked it on the
/// arc icon page (variant C, "Hexagon"): the hexagon every technology shares (<see cref="IconLooks.Hexagon"/>), and
/// inside it the technology's own glyph in its own colours, crackling on twos. It marks what takes a module, and what
/// fits, in place of words: the socket's ghost, the slot mark of a module that fits the machine open, the module's card.
/// </summary>
public static class TechLooks
{
    /// <summary>The arcs': a three-stroke bolt from top right to bottom left, violet-rimmed and white-hot.</summary>
    public static readonly Look Arc = new(Hexagon,
        [new Stroke(Fit((.3f, .2f, .4f, .6f), (.68f, 0f), (.2f, .56f), (.66f, .48f), (.3f, 1f)), Weight: 0.92f)],
        Core: 0xfcf6ff, Rim: 0xb284ff, Glow: 0xaa82ff, IconMotion.Crackle);

    public static readonly IReadOnlyDictionary<string, Look> All = new Dictionary<string, Look> { ["arc"] = Arc };

    /// <summary>Technology <paramref name="tech"/>'s icon; throws for one with none.</summary>
    public static Look Of(string tech) => All[tech];
}
