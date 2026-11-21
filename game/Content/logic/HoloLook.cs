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
    /// <summary>Not a machine's: the bag the paired drone projects (drone-companion), in its hologram blue.</summary>
    Drone,
}

/// <summary>One tone's colours, as hex: the light (frame, pens, beams), the ground under the contents, the name, headings and dim words.</summary>
public sealed record HoloTint(string Light, string Ground, string Name, string Heading, string Dim);

/// <summary>
/// How a hologram window looks and moves: the look «Перо» (two laser pens trace its frame) and the motion «Отставание» (it
/// trails the hero), the user's picks for every hologram window; the tones from machine-holograms. Pixels are screen pixels,
/// alphas 0..1, times seconds or shares of the opening; a cosmetic of the client, never read by the sim.
/// </summary>
public static class HoloLook
{
    /// <summary>A whole opening, and a whole closing, which plays the opening back.</summary>
    public const double OpenS = 0.5, CloseS = 0.3;

    /// <summary>Of the opening, where the first beam has reached the window's near edge.</summary>
    public const double ReachEnd = 0.12;

    /// <summary>Of the opening, where the pens set off from the touch point, and how long they take to meet across the window.</summary>
    public const double TraceFrom = 0.12, TraceFor = 0.46;

    /// <summary>Of the opening, where the dark ground starts to fade in under the pens, and how long it takes.</summary>
    public const double GroundFrom = 0.25, GroundFor = 0.5;

    /// <summary>Of the opening, where the slot grid starts to wipe in from the near edge, and how long it takes.</summary>
    public const double GridFrom = 0.42, GridFor = 0.36;

    /// <summary>Of the opening, where the contents start to fade in; whole at its end.</summary>
    public const double ContentsFrom = 0.62;

    /// <summary>Of the opening, where the pens' beams start to slide to the near corners, and how long it takes.</summary>
    public const double SettleFrom = 0.6, SettleFor = 0.18;

    /// <summary>
    /// The window's controls' alpha behind the grid's wipe, before the contents fade in whole: a slot and its item are one
    /// control here, so the items come in with the grid, half lit.
    /// </summary>
    public const float GridAlpha = 0.5f;

    /// <summary>The ground's alpha under the contents.</summary>
    public const float GroundAlpha = 0.8f;

    /// <summary>The touch point's least distance from a corner along its edge, so both pens have an edge to run.</summary>
    public const float CornerInset = 14;

    /// <summary>The frame: its core line's width, alpha and paleness (toward white), and its soft glow's width and alpha.</summary>
    public const float FrameCore = 1, FrameCoreAlpha = 0.85f, FramePale = 0.35f, FrameGlow = 3, FrameGlowAlpha = 0.14f;

    /// <summary>Once traced, each edge's alpha buzzes between the low and the low plus the depth.</summary>
    public const float BuzzLow = 0.62f, BuzzDepth = 0.25f;

    /// <summary>A beam: its core's width, alpha and paleness, its glow's width and alpha, before the beam's own alpha.</summary>
    public const float BeamCore = 1, BeamCoreAlpha = 0.65f, BeamPale = 0.35f, BeamGlow = 4, BeamGlowAlpha = 0.1f;

    /// <summary>The first beam's alpha while it reaches; the pens' beams' flicker, between the low and the low plus the depth.</summary>
    public const float ReachAlpha = 0.9f, FlickerLow = 0.75f, FlickerDepth = 0.25f;

    /// <summary>The glow at a beam's end: radius and alpha while the pens run or the beams slide, then settled; its paleness.</summary>
    public const float TipRadius = 7, TipAlpha = 0.8f, TipSettledRadius = 4.5f, TipSettledAlpha = 0.45f, TipPale = 0.4f;

    /// <summary>The grid's scan line: its core's width, alpha and paleness, its glow's width and alpha.</summary>
    public const float ScanCore = 1.5f, ScanCoreAlpha = 0.7f, ScanPale = 0.5f, ScanGlow = 7, ScanGlowAlpha = 0.12f;

    /// <summary>How far past the far edge the grid's wipe runs, so its scan line leaves the window whole.</summary>
    public const float WipePast = 4;

    /// <summary>The emitter's glow's radius on the machine or the drone, and its alpha at its middle.</summary>
    public const float EmitterRadius = 9, EmitterAlpha = 0.6f;

    /// <summary>The sway's high-pass: how fast, a second, its smoothed velocity catches the hero's, so a steady run settles.</summary>
    public const double SwayFollow = 3;

    /// <summary>The sway's target per screen pixel a second of the hero's unsettled velocity, across and up.</summary>
    public const double SwayGainX = 0.06, SwayGainY = 0.02;

    /// <summary>The farthest the window is carried off its place, across and up.</summary>
    public const float SwayReachX = 6, SwayReachY = 4.5f;

    /// <summary>The spring carrying it: its stiffness, and its damping as a share of critical.</summary>
    public const double SwayStiffness = 70, SwayDamping = 0.55;

    /// <summary>The sway's share while the pointer is in a hologram window or a stack is carried, and how fast it eases there and back, a second.</summary>
    public const double SwayCalm = 0.15, SwayCalmIn = 22, SwayCalmOut = 5;

    public static readonly HoloTint Warm = new("ffac60", "160d08", "ffe2c2", "cf9f74", "8d6c52");
    public static readonly HoloTint Cold = new("b2d0ff", "080d18", "e4ecff", "a2b4d8", "6c7b9b");
    public static readonly HoloTint Neutral = new("ccd2da", "0e1014", "e6e9ee", "a9b0ba", "727a85");
    /// <summary>The drone's, the bag-as-hologram pick's HOLO, rgb(140,196,255).</summary>
    public static readonly HoloTint Drone = new("8cc4ff", "07101c", "dcecff", "9cbfe6", "62809f");

    /// <summary>The colours of <paramref name="tone"/>.</summary>
    public static HoloTint Of(HoloTone tone) => tone switch
    {
        HoloTone.Warm => Warm,
        HoloTone.Cold => Cold,
        HoloTone.Drone => Drone,
        _ => Neutral,
    };
}
