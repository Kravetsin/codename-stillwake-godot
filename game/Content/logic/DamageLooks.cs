using Stillwake.Game.Render;

namespace Stillwake.Game;

/// <summary>
/// How damage is drawn (damage-looks, cracks-to-end), as the user picked it on the pages: a wreck's cracks in ink, a dent where each
/// blow lands and one network out of the first that covers the wreck by its break («Трещины до конца»); a dug block's one split
/// across it that opens into a gap ("fault"). Sizes in the world's pixels.
/// </summary>
public static class DamageLooks
{
    /// <summary>A dent's radius: its least, more by this for each doubling of the blows landed there, and by this times the share lost.</summary>
    public const double DentLeast = 0.6, DentPerHits = 0.5, DentRadius = 1.2;

    /// <summary>
    /// The crack network: its trunks fanned by these radians about the way to the wreck's middle, each this many pixels long at
    /// most, turning by up to <see cref="NetWander"/> a step, forking every <see cref="NetEvery"/> steps or so to <see cref="NetDepth"/>
    /// forks deep. It runs as the share lost to the power <see cref="NetPower"/>, whole by <see cref="NetFull"/> lost; its lines
    /// <see cref="NetWidths"/> wide by fork depth, widening by half as they run on.
    /// </summary>
    public static readonly double[] NetFan = [0, 0.5, -0.48, 1.0, -0.95], NetWidths = [0.5, 0.38, 0.3, 0.24];
    public const double NetLength = 90, NetWander = 0.2, NetPower = 0.45, NetFull = 0.92;
    public const int NetEvery = 11, NetDepth = 3;

    /// <summary>How far below each line its lit line runs: the far wall's edge, the light from above catching it.</summary>
    public const double DentLitDrop = 0.4;

    public static readonly Ink.Tone DentInk = Ink.Tone.Of(16, 17, 24, 0.9f), DentLit = Ink.Tone.Of(214, 222, 232, 0.5f), Dent = Ink.Tone.Of(20, 20, 28, 0.55f);

    /// <summary>
    /// How a wreck's cracks are inked: each line turning by up to <paramref name="Wander"/> a step; its <paramref name="Under"/> line
    /// <paramref name="UnderWidth"/> of its width, <paramref name="Drop"/> below it, then its <paramref name="Over"/> line
    /// <paramref name="OverWidth"/> of it; its dents in <paramref name="Dented"/>.
    /// </summary>
    public sealed record CrackInk(double Wander, Ink.Tone Under, double UnderWidth, double Drop, Ink.Tone Over, double OverWidth, Ink.Tone Dented);

    /// <summary>Every wreck's: dark ink wandering, its far wall's lit edge under it.</summary>
    public static readonly CrackInk Wreck = new(NetWander, DentLit, 0.55, DentLitDrop, DentInk, 1, Dent);

    /// <summary>
    /// The ice belt's druse (docs/plans/stretches.md, the pick «Друза»): cracks along straight planes, a dark line under a bright one,
    /// white where it was struck.
    /// </summary>
    public static readonly CrackInk Ice = new(0, Ink.Tone.Of(46, 74, 96, 0.85f), 1.5, 0, Ink.Tone.Of(242, 248, 251, 0.9f), 0.6, Ink.Tone.Of(232, 244, 250, 0.5f));

    /// <summary>The cracks of wreck <paramref name="item"/>: the ice belt's druse its own, every other the same.</summary>
    public static CrackInk InkOf(string item) => item == StretchLooks.IceBlock ? Ice : Wreck;

    /// <summary>A blow nearer an earlier one than this deepens it rather than starting its own dent.</summary>
    public const double BlowsApart = 3;

    /// <summary>A picture's pixel this opaque, or more, is the wreck's own: where its cracks run.</summary>
    public const float Opaque = 0.43f;

    /// <summary>The fault's kinks; its gap and its line's width at each of the three stages.</summary>
    public const int FaultKinks = 5;
    public static readonly double[] FaultGap = [0, 0.25, 0.75], FaultWidth = [0.22, 0.38, 0.5];

    /// <summary>A material's fault: its ink, the gap's depth, the lit lower lip.</summary>
    public readonly record struct FaultInk(Ink.Tone Ink, Ink.Tone Deep, Ink.Tone Lit);

    /// <summary>A planet's stone, and anything built: a ship's scrap, alloy, a pile.</summary>
    public static readonly FaultInk Stone = new(Ink.Tone.Of(12, 13, 18, 0.9f), Ink.Tone.Of(10, 11, 15, 0.85f), Ink.Tone.Of(206, 214, 226, 0.55f)),
        Scrap = new(Ink.Tone.Of(18, 12, 10, 0.9f), Ink.Tone.Of(14, 9, 8, 0.85f), Ink.Tone.Of(236, 220, 196, 0.55f));
}
