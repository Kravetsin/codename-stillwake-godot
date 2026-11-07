namespace Stillwake.Game;

/// <summary>A machine window's hologram colour, by the kind of station it is: its fire, its power, or neither (machine-holograms).</summary>
public enum HoloTone
{
    /// <summary>Neither fire nor power: crates, trade stations, the workbench, the arc bench, the lifepod, the drop pod.</summary>
    Neutral,
    /// <summary>A fire: furnaces and burning generators.</summary>
    Warm,
    /// <summary>On the power grid: it makes, draws or stores power.</summary>
    Cold,
}

/// <summary>One tone's colours, as hex: the light (ground wash, edge, beam), the ground under the contents, the name, headings and dim words.</summary>
public sealed record HoloTint(string Light, string Ground, string Name, string Heading, string Dim);

/// <summary>
/// How a machine window's hologram looks and moves, the user's pick D «Цвет станции» (machine-holograms). Pixels are
/// screen pixels, alphas 0..1; a cosmetic of the client, never read by the sim.
/// </summary>
public static class HoloLook
{
    /// <summary>Opening: the beam reaches the window, then the window grows from where it touches.</summary>
    public const double BeamS = 0.12, GrowS = 0.15;

    /// <summary>Closing: the ground shrinks into where the beam touches, then the beam pulls back.</summary>
    public const double ShrinkS = 0.09, RetractS = 0.06;

    /// <summary>Of the growth, the share spent drawing the near edge out from the touch point before it spreads across.</summary>
    public const double EdgeFirst = 0.42;

    /// <summary>The ground's alpha under the contents.</summary>
    public const float GroundAlpha = 0.87f;

    /// <summary>The light's wash on the ground at the edge nearest the machine, fading to none halfway across.</summary>
    public const float Wash = 0.11f;

    /// <summary>The 1 px edge in the light, its glow's alpha and size, and the edge nearest the machine.</summary>
    public const float Edge = 0.5f, Glow = 0.45f, NearEdge = 0.95f;

    /// <summary>The glow's reach round the edge, in pixels.</summary>
    public const int GlowSize = 6;

    /// <summary>The emitter's glow's radius on the machine, and its alpha at its middle.</summary>
    public const float EmitterRadius = 9, EmitterAlpha = 0.6f;

    public static readonly HoloTint Warm = new("ffac60", "160d08", "ffe2c2", "cf9f74", "8d6c52");
    public static readonly HoloTint Cold = new("b2d0ff", "080d18", "e4ecff", "a2b4d8", "6c7b9b");
    public static readonly HoloTint Neutral = new("ccd2da", "0e1014", "e6e9ee", "a9b0ba", "727a85");

    /// <summary>The colours of <paramref name="tone"/>.</summary>
    public static HoloTint Of(HoloTone tone) => tone switch
    {
        HoloTone.Warm => Warm,
        HoloTone.Cold => Cold,
        _ => Neutral,
    };
}
