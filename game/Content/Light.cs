namespace Stillwake.Game;

/// <summary>
/// Light, drawn only (ADR 0019): a colour per cell, red, green and blue from 0 up, spread from what gives
/// it off. Crossing a cell of air it keeps <see cref="Air"/> of it, of a machine <see cref="Machine"/>;
/// going into a wall or a closed door it keeps <see cref="WallIn"/>, into each further wall cell
/// <see cref="WallDeep"/>, and coming out <see cref="WallOut"/>, so a wall is lit where light falls on it,
/// darker deeper in, and a wall a cell thick lets next to none through. It spreads in
/// <see cref="Rounds"/> sweeps each way across and down, and is worked out over the screen and
/// <see cref="Margin"/> cells around it. As it is drawn, a cell's light is blended with its neighbours'
/// of its own side only, walls with walls and rooms with rooms, so light is smooth and blocks keep their
/// edges.
///
/// What gives it off, in colours that may pass 1 so a room round it is lit fully: the stars through
/// every open cell with no back wall, a lamp's bulb in proportion to the power it gets, a working
/// furnace's fire, and each glowing particle at <see cref="Particle"/> of its glow's colour. What is
/// shown is <see cref="Ambient"/> more, so no corner is quite black.
///
/// Here lights add where the TypeScript game took the brightest, each colour stopping at whole, so a
/// lamp gives at most <see cref="LampMost"/> of a colour, white by the bulb and below whole, leaving a
/// fire's warmth room to show over it. A fire and a glowing particle give what they gave there but
/// fall off steeply, keeping <see cref="FireKeep"/> and <see cref="ParticleKeep"/> a cell: the fire's
/// light pools at the furnace's mouth, an ember's at the ember.
/// </summary>
public static class Light
{
    public const int Margin = 24;
    public const int Rounds = 2;
    public const float Air = 0.88f;
    public const float Machine = 0.72f;
    public const float WallIn = 0.7f;
    public const float WallDeep = 0.3f;
    public const float WallOut = 0.06f;
    public static readonly (float R, float G, float B) Star = (0.62f, 0.66f, 0.8f);
    public static readonly (float R, float G, float B) Lamp = (1.5f, 1.3f, 0.95f);
    public const float LampMost = 1f;
    public static readonly (float R, float G, float B) Fire = (1f, 0.48f, 0.14f);
    public const float FireKeep = 0.72f;
    public const float Particle = 0.5f;
    public const float ParticleKeep = 0.5f;
    public static readonly (float R, float G, float B) Ambient = (0.05f, 0.055f, 0.07f);

    /// <summary>
    /// Going into a closed door's cell in the cells mode (0026) light keeps this, as into a wall
    /// (<see cref="WallIn"/>, the TypeScript game's door); deeper and out of it as through a wall.
    /// </summary>
    public const float DoorIn = 0.7f;

    /// <summary>
    /// What gives off light in the cells mode (0026), one entry a kind: its colour and strength, the light
    /// it gives the cell holding most of its glowing pixels (a cell with fewer gets its share by their
    /// count), and which of its pixels glow: those at least <see cref="Emission.Glowing"/> bright, 0 for
    /// every opaque one. A lamp's, a fire's and a particle's are the TypeScript game's.
    /// </summary>
    public static class Emits
    {
        /// <summary>A lamp's bulb, by the power it gets: its white pixels, not its shade or bracket.</summary>
        public static readonly Emission Lamp = new(Light.Lamp, 1, Glowing: 0.8f);
        /// <summary>A furnace's fire, by how brightly it burns: its flames, not the bars of its grate or the dim embers under them.</summary>
        public static readonly Emission Fire = new(Light.Fire, 1, Glowing: 0.3f);
        /// <summary>An ember rising from a fire and a spark off work on a plan, as they fade: the colours of their glows.</summary>
        public static readonly Emission Ember = new((1f, 0.48f, 0.125f), Particle);
        public static readonly Emission Spark = new((1f, 0.63f, 0.25f), Particle);
        /// <summary>The stars, in every open cell with no back wall and past the grid's edge.</summary>
        public static readonly Emission Star = new(Light.Star, 1);
    }
}

/// <summary>
/// How a kind of thing gives off light (<see cref="Light.Emits"/>): <paramref name="Colour"/> times
/// <paramref name="Strength"/> in its brightest cell, from its pixels at least <paramref name="Glowing"/>
/// bright (their luminance, 0 to 1).
/// </summary>
public sealed record Emission((float R, float G, float B) Colour, float Strength, float Glowing = 0);
