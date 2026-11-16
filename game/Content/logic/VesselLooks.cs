using System.Collections.Generic;
using System.Globalization;

namespace Stillwake.Game;

/// <summary>
/// How an abandoned vessel looks (derelict-looks), drawn only: its livery, its name and code painted on its side, how often its
/// plates rust or pit. Every look is drawn from <see cref="LookRng"/> of its seed, never the sim's draws, so every client paints a
/// vessel alike while the sim never sees its looks. Colours are 0xRRGGBB.
/// </summary>
public static class VesselLooks
{
    /// <summary>An owner's paint: the hull's tone, its stripe along the bottom armour, the paint its name and code are in.</summary>
    public sealed record Livery(uint Hull, uint Stripe, uint Paint);

    /// <summary>The mockup's four (derelict-looks-mockup.html, LIVERIES).</summary>
    public static readonly IReadOnlyList<Livery> Liveries =
    [
        new(0x7b828b, 0xc58a33, 0xe6dfcf),
        new(0x6c7a7e, 0x2f7480, 0xdfe4dc),
        new(0x77716b, 0x93412c, 0xded3c0),
        new(0x848991, 0x2d4569, 0xe3e3dd),
    ];

    /// <summary>Names painted on a vessel's side, in Latin for every player: content, not i18n keys.</summary>
    public static readonly IReadOnlyList<string> Names =
    [
        "SISYPHUS", "PELICAN", "GUILLEMOT", "BOREAS", "LADOGA", "WANDERER", "WORMWOOD",
        "NORTHSTAR", "LOON", "PILGRIM", "HAZEL", "HEATHER", "GYRFALCON", "HUSH",
    ];

    /// <summary>An owner's two letters before a vessel's number: <c>KT-0412</c>.</summary>
    public static readonly IReadOnlyList<string> CodePrefixes = ["SZ", "KT", "GR", "TN", "VL", "OK"];

    /// <summary>The numbers a code takes, below this: four digits.</summary>
    public const int CodeNumbers = 10_000;

    /// <summary>A plate of the hull shows rust one time in this many, pits one in <see cref="PitOneIn"/> of the rest.</summary>
    public const int RustOneIn = 7, PitOneIn = 5;

    /// <summary>A cell of the livery stripe shows a scuff one time in this many.</summary>
    public const int StripeScuffOneIn = 6;

    /// <summary>
    /// The grey the hull's, the stripe's and the letters' pictures are painted in (0..255): a livery's colour over it is the tint that
    /// paints them its colour, so one atlas serves every livery.
    /// </summary>
    public const int HullGrey = 196, StripeGrey = 222, PaintGrey = 232;

    /// <summary>Cells from a module's left to its painted name, and from the stern module's to the code.</summary>
    public const int NameInset = 3, CodeInset = 3;

    /// <summary>
    /// The painted letters' sheets (<c>vessel_letters</c>, <c>vessel_letters_small</c>): a slot for each of <see cref="Glyphs"/> in
    /// order, its pixels across and down; the name in the large, the code in the small.
    /// </summary>
    public const string Glyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

    public static readonly (int W, int H) LargeGlyph = (7, 12), SmallGlyph = (6, 10);

    /// <summary>
    /// Of a hundred lamps inside, but the airlock's (always red) and the one dying, how many still burn red on their battery; the
    /// rest are dead.
    /// </summary>
    public const int LampRedPercent = 45;

    /// <summary>A console's screen is dark one time in this many; of those lit, one in <see cref="GlitchOneIn"/> glitches.</summary>
    public const int ConsoleDarkOneIn = 4, GlitchOneIn = 4;

    /// <summary>A door stamped open is jammed one time in this many, drawn <see cref="JammedOpen"/> open while it stands open.</summary>
    public const int JamOneIn = 3;

    /// <summary>How far open a jammed door is drawn, a share of its doorway: its panel stuck a little past half-way down (the mockup's).</summary>
    public const double JammedOpen = 0.45;

    /// <summary>Why a look is drawn, mixed into its draw so two looks of one cell differ.</summary>
    public enum Salt : uint { Livery = 1, Name, Prefix, Number, Panel, Stripe, Lamp, Dying, Console, Glitch, Jam, Phase, Flicker }

    /// <summary>
    /// A look's draw (SplitMix64, fixed here): the same <paramref name="seed"/>, <paramref name="salt"/> and <paramref name="cell"/>
    /// give the same 64 bits on every machine.
    /// </summary>
    public static ulong LookRng(uint seed, Salt salt, int cell)
    {
        ulong z = ((ulong)seed << 32 | (uint)salt) ^ (uint)cell * 0xD1B54A32D192ED03UL;
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A draw below <paramref name="n"/>.</summary>
    public static int Below(uint seed, Salt salt, int cell, int n) => (int)(LookRng(seed, salt, cell) % (ulong)n);

    /// <summary>The livery of the vessel of <paramref name="seed"/>, by its place in <see cref="Liveries"/>.</summary>
    public static int LiveryOf(uint seed) => Below(seed, Salt.Livery, 0, Liveries.Count);

    /// <summary>The name painted on the vessel of <paramref name="seed"/>.</summary>
    public static string NameOf(uint seed) => Names[Below(seed, Salt.Name, 0, Names.Count)];

    /// <summary>The code painted by its stern: two letters and four digits.</summary>
    public static string CodeOf(uint seed) =>
        CodePrefixes[Below(seed, Salt.Prefix, 0, CodePrefixes.Count)] + "-" + Below(seed, Salt.Number, 0, CodeNumbers).ToString("0000", CultureInfo.InvariantCulture);
}
