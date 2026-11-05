using Stillwake.Game.Render;

namespace Stillwake.Game;

/// <summary>
/// How damage is drawn (damage-looks), as the user picked it on the page: a wreck's cracks in ink, a dent where each blow lands
/// and pen lines out of it ("penDent"); a dug block's one split across it that opens into a gap ("fault"). Sizes in the world's pixels.
/// </summary>
public static class DamageLooks
{
    /// <summary>A blow's cracks grow over this share of the wreck's hit points after it lands.</summary>
    public const double DentGrow = 0.45;

    /// <summary>The pen lines out of a blow, the longest's length, their width; the dent's widest radius past its least, half a pixel.</summary>
    public const int DentLines = 6;
    public const double DentLongest = 11, DentWidth = 0.45, DentRadius = 2.2, DentLeast = 0.5;

    /// <summary>How far below each line its lit line runs: the far wall's edge, the light from above catching it.</summary>
    public const double DentLitDrop = 0.4;

    public static readonly Ink.Tone DentInk = Ink.Tone.Of(16, 17, 24, 0.88f), DentLit = Ink.Tone.Of(214, 222, 232, 0.45f), Dent = Ink.Tone.Of(20, 20, 28, 0.55f);

    /// <summary>A blow nearer an earlier one than this deepens it rather than starting its own; a wreck shows at most <see cref="MostBlows"/>.</summary>
    public const double BlowsApart = 3;
    public const int MostBlows = 8;

    /// <summary>The fault's kinks; its gap and its line's width at each of the three stages.</summary>
    public const int FaultKinks = 5;
    public static readonly double[] FaultGap = [0, 0.25, 0.75], FaultWidth = [0.22, 0.38, 0.5];

    /// <summary>A material's fault: its ink, the gap's depth, the lit lower lip.</summary>
    public readonly record struct FaultInk(Ink.Tone Ink, Ink.Tone Deep, Ink.Tone Lit);

    /// <summary>A planet's stone, and anything built: a ship's scrap, alloy, a pile.</summary>
    public static readonly FaultInk Stone = new(Ink.Tone.Of(12, 13, 18, 0.9f), Ink.Tone.Of(10, 11, 15, 0.85f), Ink.Tone.Of(206, 214, 226, 0.55f)),
        Scrap = new(Ink.Tone.Of(18, 12, 10, 0.9f), Ink.Tone.Of(14, 9, 8, 0.85f), Ink.Tone.Of(236, 220, 196, 0.55f));
}
