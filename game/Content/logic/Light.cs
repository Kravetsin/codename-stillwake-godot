using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game;

/// <summary>
/// Light, drawn only (ADR 0019, 0029, 0070): a colour per cell, red, green and blue from 0 up, spread from
/// what gives it off. Starlight is worked out cell by cell: crossing a cell of air it keeps
/// <see cref="StarKeep"/> of it, of a machine <see cref="StarMachine"/>; going into a wall or a closed door
/// it keeps <see cref="WallIn"/>, into each further wall cell <see cref="WallDeep"/>, and coming out
/// <see cref="WallOut"/>, so a wall is lit where light falls on it, darker deeper in, and a wall a cell
/// thick lets next to none through. It spreads in <see cref="Rounds"/> sweeps each way across and down.
/// As it is drawn, a cell's light is blended with its neighbours' of its own side only, walls with walls
/// and rooms with rooms, so light is smooth and blocks keep their edges. Tuned at the 16 px cell: what is a reach or a count of cells
/// here is twice it, and what a cell keeps the square root of it, so light falls off over the same pixels (0104).
///
/// What gives it off, in colours that may pass 1 so a room round it is lit fully: the stars in every open
/// cell with no back wall, as much as its sky is open (<see cref="StarHole"/> to whole), a lamp's bulb in
/// proportion to the power it gets, a working furnace's fire, and each glowing particle at
/// <see cref="Particle"/> of its glow's colour. What is shown is <see cref="Ambient"/> more, so no corner
/// is quite black.
///
/// Here lights add where the TypeScript game took the brightest, each colour stopping at whole, so a
/// lamp gives at most <see cref="LampMost"/> of a colour, warm and dim, leaving a fire's warmth and a
/// screen's glow room to show over it. A fire and a glowing particle give what they gave there but
/// fall off steeply, keeping <see cref="FireKeep"/> and <see cref="ParticleKeep"/> a cell: the fire's
/// light pools at the furnace's mouth, an ember's at the ember. A particle's light is a spot added over
/// all the rest, past whole by at most <see cref="ParticleMost"/> a colour, so an ember lights the wall by a fire.
/// </summary>
public static class Light
{
    public const int Margin = 48;
    public const int Rounds = 2;
    /// <summary>
    /// Starlight crossing a cell keeps this much of itself, a machine's <see cref="StarMachine"/>: through a
    /// hole or a doorway it falls off within a few cells, and only open space is lit by the stars whole.
    /// </summary>
    public const float StarKeep = 0.775f, StarMachine = 0.693f;

    /// <summary>
    /// Under a planet's sky (0077), what daylight keeps crossing a cell of air in from the open: it dies a few cells into a
    /// cave mouth or a shaft, so tunnels ahead are black but for what lamps, fires and glows light; and no ambient light
    /// there (<see cref="Ambient"/> is the ship's alone), so unlit ground is black too.
    /// </summary>
    public const float GroundKeep = 0.548f;

    /// <summary>Under a planet's sky, what daylight keeps falling a cell down through air: down a shaft or a cave mouth it lights
    /// some twelve to sixteen cells, fading, while round a bend it dies within two to four (<see cref="GroundKeep"/>).</summary>
    public const float GroundDownKeep = 0.894f;

    /// <summary>
    /// Under a planet's sky, what daylight keeps going down a cell through its ground (0079): the top few cells of soil under the
    /// sun are lit, fading, as the "Stillwake" style shows them, where sideways and up a wall keeps only <see cref="WallDeep"/>.
    /// </summary>
    public const float GroundSoak = 0.849f;
    /// <summary>
    /// The stars' light in a lone hole in a back wall, against whole in open space: a cell's sky is the cells
    /// within <see cref="StarSky"/> of it, and with <see cref="StarFullSky"/> of them open to the stars, itself
    /// among them, it is lit whole — beside a flat hull too; between, as many as are open.
    /// </summary>
    public const float StarHole = 0.5f;
    public const int StarSky = 4, StarFullSky = 39;
    public const float WallIn = 0.7f;
    public const float WallDeep = 0.548f;
    public const float WallOut = 0.06f;
    public static readonly (float R, float G, float B) Star = (0.62f, 0.66f, 0.8f);

    /// <summary>
    /// What shines through the open sky of a surface of <paramref name="kind"/> (0076): its sky's light, a planet's
    /// daylight, from content's thousandths; the <see cref="Star"/>s where it names none, as aboard.
    /// </summary>
    public static (float R, float G, float B) SkyOf(Stillwake.Content.Surfaces.Kind kind) =>
        kind.Sky is { } sky ? (sky.R / 1000f, sky.G / 1000f, sky.B / 1000f) : Star;

    /// <summary>The brightest colour any sky shines in, the stars' among them: what the faintest texel of starlight is judged by.</summary>
    public static readonly float BrightestSky = Stillwake.Content.Surfaces.All.Select(SkyOf).Append(Star)
        .Max(c => System.Math.Max(c.R, System.Math.Max(c.G, c.B)));
    /// <summary>
    /// A starter lamp: dim and warm, keeping <see cref="LampKeep"/> a cell out to <see cref="LampReach"/>
    /// cells, so a room is lit to walk and its corners dark, and fires, screens and glows read in it.
    /// </summary>
    public static readonly (float R, float G, float B) Lamp = (0.8f, 0.64f, 0.42f);
    /// <summary>A lifepod's own light, pale and cold, needing no power: what shows the first room (the-scoop).</summary>
    public static readonly (float R, float G, float B) Pod = (0.5f, 0.72f, 0.62f);
    public const float LampKeep = 0.86f;
    public const int LampReach = 16;
    public const float LampMost = 1f;

    /// <summary>
    /// An arc makers' lamp hung aboard (0091): their cold white-blue, brighter than the starter lamp and reaching further, as their
    /// homes' lamps are (the page's arc light, a fifth wider, three tenths stronger).
    /// </summary>
    public static readonly (float R, float G, float B) ArcLamp = (0.56f, 0.78f, 1f);
    public const float ArcLampKeep = 0.883f, ArcLampStrength = 1.3f;
    public const int ArcLampReach = 20;

    /// <summary>
    /// How a glowing machine (<c>MachineTrait.Glow</c>, 0113) lights, by its kind: its light's colour, strength at full power,
    /// reach and keep; its halo's colour, as <c>0xRRGGBBAA</c>; and whether the arc makers' glass is drawn over it (<c>ArcLampLooks</c>).
    /// </summary>
    public sealed record LampLight((float R, float G, float B) Colour, float Strength, int Reach, float Keep, uint Halo, bool Glass = false);

    /// <summary>Every glowing machine's light, by its kind.</summary>
    public static readonly IReadOnlyDictionary<string, LampLight> Lamps = new Dictionary<string, LampLight>(System.StringComparer.Ordinal)
    {
        ["lamp"] = new(Lamp, 1, LampReach, LampKeep, 0xffd9a0ff),
        // The hold's red battery lamp (the-scoop): enough to find the piles by, not to work by comfortably.
        ["emergencyLamp"] = new((1f, 0.22f, 0.16f), 0.75f, LampReach, LampKeep, 0xff3a2cff),
        // An arc makers' lamp (0091): their cold light, brighter and further.
        ["arcLamp"] = new(ArcLamp, ArcLampStrength, ArcLampReach, ArcLampKeep, 0xb4dcffff, Glass: true),
    };
    public static readonly (float R, float G, float B) Fire = (1f, 0.48f, 0.14f);
    public const float FireKeep = 0.849f;
    // An ember's light is a bright point, not a wash: the furnace's fire lights the room round it anyway.
    public const float Particle = -1.2f;
    public const float ParticleKeep = 0.447f;
    public const float ParticleMost = 1f;
    /// <summary>A planner's screen, keeping this a cell: softer than an ember's.</summary>
    public const float ScreenKeep = 0.707f;

    /// <summary>
    /// How wide a lamp's bulb and a fire's mouth are, in pixels: a shadow's edge is soft over as much as
    /// the light seen from it is wide, sharp by what casts it, wider further off. Glows are points.
    /// </summary>
    public const float LampSize = 6f, FireSize = 10f;

    /// <summary>
    /// The helmet lamp (0077): a beam of warm light the way the head looks, <see cref="HeadLampHalfAngle"/> either side of
    /// it (60° across), keeping <see cref="HeadLampKeep"/> a cell out to <see cref="HeadLampReach"/> cells, its bulb small;
    /// and a faint glow round the lamp, <see cref="HeadGlow"/> of it out to <see cref="HeadGlowReach"/> cells, so the wearer
    /// is never pitch black behind it.
    /// </summary>
    public static readonly (float R, float G, float B) HeadLamp = (0.95f, 0.82f, 0.6f);
    public const float HeadLampKeep = 0.894f, HeadLampSize = 3f, HeadLampHalfAngle = 0.5236f;
    public const int HeadLampReach = 18;
    public const float HeadGlow = 0.4f, HeadGlowKeep = 0.707f;
    public const int HeadGlowReach = 4;
    public static readonly (float R, float G, float B) Ambient = (0.04f, 0.045f, 0.06f);
}
