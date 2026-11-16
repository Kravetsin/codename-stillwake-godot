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

    /// <summary>The picked mockup's four (derelict-looks).</summary>
    public static readonly IReadOnlyList<Livery> Liveries =
    [
        new(0x7b828b, 0xc58a33, 0xe6dfcf),
        new(0x6c7a7e, 0x2f7480, 0xdfe4dc),
        new(0x77716b, 0x93412c, 0xded3c0),
        new(0x848991, 0x2d4569, 0xe3e3dd),
    ];

    /// <summary>A liner's three, light (big-vessels, the picked mockup's): hull pale, stripe and paint dark.</summary>
    public static readonly IReadOnlyList<Livery> LinerLiveries =
    [
        new(0xb9bab4, 0x2d4a7a, 0x1f2a36),
        new(0xb6b2a8, 0x8a2f2a, 0x2a2626),
        new(0xaeb6b7, 0x2f6f6a, 0x1d272c),
    ];

    /// <summary>A tanker's three (big-vessels, the picked mockup's): hull dark, a bold stripe, its name in pale paint.</summary>
    public static readonly IReadOnlyList<Livery> TankerLiveries =
    [
        new(0x6f6a62, 0xb8452f, 0xe2d9c6),
        new(0x5f6b6a, 0xd0a23a, 0xe6e2d6),
        new(0x6d6c73, 0x2f6f5f, 0xe0e0da),
    ];

    /// <summary>The liveries a vessel of class <paramref name="cls"/> is painted in: a liner's and a tanker's own, the derelicts' for the rest.</summary>
    public static IReadOnlyList<Livery> LiveriesOf(string cls) => cls switch { "liner" => LinerLiveries, "tanker" => TankerLiveries, _ => Liveries };

    /// <summary>Names painted on a vessel's side, in Latin for every player: content, not i18n keys.</summary>
    public static readonly IReadOnlyList<string> Names =
    [
        "SISYPHUS", "PELICAN", "GUILLEMOT", "BOREAS", "LADOGA", "WANDERER", "WORMWOOD",
        "NORTHSTAR", "LOON", "PILGRIM", "HAZEL", "HEATHER", "GYRFALCON", "HUSH",
    ];

    /// <summary>An owner's two letters before a vessel's number: <c>KT-0412</c>.</summary>
    public static readonly IReadOnlyList<string> CodePrefixes = ["SZ", "KT", "GR", "TN", "VL", "OK"];

    /// <summary>A tanker's names (big-vessels, the picked mockup's) and its owner's letters before its number.</summary>
    public static readonly IReadOnlyList<string> TankerNames = ["BULWARK", "ATLAS", "GRAVITAS", "OBELISK", "KRAKEN", "TITAN", "HAULMARK", "MONOLITH"];

    public const string TankerPrefix = "TK";

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

    /// <summary>How many signs of abandonment a vessel of each class shows inside, fewest and most; fewer where no room fits one.</summary>
    public static readonly IReadOnlyDictionary<string, (int Least, int Most)> Signs =
        new Dictionary<string, (int, int)>(System.StringComparer.Ordinal) { ["shuttle"] = (2, 3), ["freighter"] = (3, 5), ["liner"] = (4, 6), ["tanker"] = (4, 6) };

    /// <summary>What soot may lie over, by its first look: a burnt console or machine, never a crate or a pod.</summary>
    public static readonly IReadOnlyList<string> SootOver = ["salvageConsole", "salvageMachine", "salvageEngine", "salvageRack", "salvageTanks", "salvageVending",
        Stillwake.Content.Vessels.Console, Stillwake.Content.Vessels.Machine, Stillwake.Content.Vessels.Rack, Stillwake.Content.Vessels.LifeTank];

    /// <summary>
    /// The sparking cable throws a burst every <see cref="SparkEveryMs"/>, <see cref="SparkCount"/> sparks up to <see cref="SparkAcross"/>
    /// pixels a second either way, <see cref="SparkUp"/> up and <see cref="SparkDown"/> down, falling at <see cref="SparkFall"/>, their
    /// light flashing for <see cref="SparkFlashMs"/> (<c>Light.SparkFlash</c>); the mockup's.
    /// </summary>
    public static readonly (double From, double To) SparkEveryMs = (700, 3200);
    public const double SparkAcross = 56, SparkUp = 48, SparkDown = 16, SparkFall = 208;
    public static readonly (int From, int To) SparkCount = (10, 18);
    public const double SparkFlashMs = 160;

    /// <summary>
    /// The burst pipe puffs gas every <see cref="PuffEveryMs"/>: each drifts its way at <see cref="PuffSpeed"/> and sinks at
    /// <see cref="PuffSink"/> pixels a second, keeping <see cref="PuffDrag"/> of its speed a second, swelling from <see cref="PuffSize"/> by
    /// <see cref="PuffGrow"/> pixels a second, fading from <see cref="PuffAlpha"/> over its life, clipped to its room; unlit, a pale mist
    /// seen in the dark as the mockup's.
    /// </summary>
    public const double PuffEveryMs = 70, PuffDrag = 0.16, PuffSize = 2, PuffGrow = 8, PuffAlpha = 0.22;
    public static readonly (double From, double To) PuffSpeed = (18, 34), PuffSink = (3, 13), PuffLifeMs = (1200, 1900);

    /// <summary>The gas's colour, 0xRRGGBB: a pale cold grey.</summary>
    public const uint PuffColour = 0xd6e2e8;

    /// <summary>Where in the hanging cable's picture its frayed end throws its sparks, and in the burst pipe's where its gas comes out, in pixels.</summary>
    public static readonly (float X, float Y) CableTip = (15, 21.5f), BurstAt = (32, 7.5f);

    /// <summary>Why a look is drawn, mixed into its draw so two looks of one cell differ.</summary>
    public enum Salt : uint { Livery = 1, Name, Prefix, Number, Panel, Stripe, Lamp, Dying, Console, Glitch, Jam, Phase, Flicker, SignCount, SignKinds, Sign, SignDir }

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

    /// <summary>The livery of the vessel of class <paramref name="cls"/> and <paramref name="seed"/>, by its place in <see cref="LiveriesOf"/>.</summary>
    public static int LiveryOf(string cls, uint seed) => Below(seed, Salt.Livery, 0, LiveriesOf(cls).Count);

    /// <summary>The name painted on the vessel of class <paramref name="cls"/> and <paramref name="seed"/>: a tanker's from its own names.</summary>
    public static string NameOf(string cls, uint seed)
    {
        var names = cls == "tanker" ? TankerNames : Names;
        return names[Below(seed, Salt.Name, 0, names.Count)];
    }

    /// <summary>The code painted by its stern: two letters and four digits, a tanker's letters always its own.</summary>
    public static string CodeOf(string cls, uint seed) =>
        (cls == "tanker" ? TankerPrefix : CodePrefixes[Below(seed, Salt.Prefix, 0, CodePrefixes.Count)]) + "-"
        + Below(seed, Salt.Number, 0, CodeNumbers).ToString("0000", CultureInfo.InvariantCulture);
}
