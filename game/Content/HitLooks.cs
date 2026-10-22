using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Render;

namespace Stillwake.Game;

/// <summary>What a hit strikes, as it looks struck (0094): the object decides, whatever the tool.</summary>
public enum Impact
{
    /// <summary>A wreck out in space: the axe's bite of 0075, nothing falling.</summary>
    Wreck,
    /// <summary>The ruins' alloy and their metal pieces, on a planet: the wreck's sparks and glint, falling.</summary>
    Alloy,
    Stone,
    Soil,
    /// <summary>The homes' plaster: chips and a lot of dust, no sparks.</summary>
    Plaster,
    Wood,
    /// <summary>A tree's trunk: its splinters and a leaf or two shaken down.</summary>
    Tree,
    Leaves,
    Crystal,
    Spores,
    Cloth,
    Glass,
    Ceramic,
    /// <summary>A creature's soft body (0102): tufts of its fur and warm motes, a white shock line; no blood.</summary>
    Flesh,
    /// <summary>A creature of a cool coat: its tufts lilac.</summary>
    CoolFlesh,
}

/// <summary>
/// <paramref name="Count"/> of <paramref name="Look"/> a whole hit throws (the axe's, times <see cref="Fx.Count"/>), at
/// <paramref name="Speed"/> pixels a second within <paramref name="Spread"/> radians either way of their heading: out of the
/// struck face, turned up by <paramref name="Up"/> (0 straight out, 1 halfway to straight up). <paramref name="Size"/> is a
/// mote's or a chip's own, else its look's.
/// </summary>
public sealed record Throw(Luminous Look, int Count, (double From, double To) Speed, double Spread, double Up = 0, (double From, double To)? Size = null);

/// <summary>
/// A hit's look on one kind of object: what it throws, and its key moments, a glint of <paramref name="Glint"/> pixels, a
/// faint shock line of <paramref name="Shock"/>'s alpha, a light flaring to <paramref name="Flash"/>; none at 0.
/// </summary>
public sealed record HitLook(Throw[] Throws, double Glint = 0, (int R, int G, int B) GlintColour = default, double Shock = 0,
    double Flash = 0, (int R, int G, int B) FlashColour = default);

/// <summary>
/// How a hit looks by what it strikes (0094), beside how it sounds (<see cref="Sound.Ground"/>, <see cref="Sound.Growths"/>):
/// one table by the natural wall's material, one by the growth's kind, and each object's look. A whole hit is the axe's; a
/// faster tool throws a share of it each hit (<c>Impacts.ShareOf</c>), so a second of it is as dense as a second of chopping.
/// Thrown out of a face into the air, none is drawn behind the walls (<see cref="Luminous.Low"/>): over them, a chip on the
/// ground's grass shows, as one a wreck's hit throws does.
/// </summary>
public static class HitLooks
{
    private static (int, int, int) Lighter((int R, int G, int B) c, int by) => (System.Math.Min(255, c.R + by), System.Math.Min(255, c.G + by), System.Math.Min(255, c.B + by));

    /// <summary>A material's chips, falling as matter does on a planet and bouncing, in its fleck and colour.</summary>
    private static Luminous ChipsOf(string material, double size = 1.4)
    {
        var look = GroundLooks.Of[material];
        return Luminous.Chip with { Ramp = [look.Fleck, look.Colour], Gravity = 420, Bounce = 0.25, Size = size };
    }

    /// <summary>A material's dust, a little paler than it, settling slowly.</summary>
    private static Luminous DustOf(string material) =>
        Luminous.HitDust with { Ramp = [Lighter(GroundLooks.Of[material].Colour, 50)], Gravity = 6, LifeMs = (900, 1700) };

    /// <summary>Sparks off metal on a planet: the wreck's, arcing down and bouncing.</summary>
    public static readonly Luminous FallingSpark = Luminous.HitSpark with { Gravity = 240, Bounce = 0.3 };

    /// <summary>A few short sparks off stone: shorter lived and finer than metal's, arcing down.</summary>
    public static readonly Luminous StoneSpark = Luminous.HitSpark with
    { LifeMs = (140, 340), Gravity = 260, Bounce = 0.3, Width = 0.22, Halo = 2.4, Longest = 6 };

    /// <summary>A splinter of wood: a thin dark streak of matter flicked out, arcing down, bouncing a little.</summary>
    public static readonly Luminous Splinter = new(Glimmer.Streak, [(196, 156, 108), (132, 96, 62)], (420, 800))
    { Matter = true, Bright = 0.9, Fade = 0.6, Gravity = 320, Drag = 0.8, Spin = 2, Stretch = 0.035, Shortest = 1.5, Longest = 4, Width = 0.4, Hot = 0, Bounce = 0.2 };

    /// <summary>A chip of wood, tumbling.</summary>
    public static readonly Luminous WoodChip = Luminous.Chip with { Ramp = [(168, 128, 88), (104, 76, 52)], Gravity = 380, Bounce = 0.2, Size = 1.2 };

    /// <summary>A leaf knocked off, fluttering down: a felled plant's, over the walls.</summary>
    public static readonly Luminous Leaf = FloraLooks.Leaf with { Low = false };

    /// <summary>A glassy shard: a pale chip that catches the light as it turns, falling and bouncing.</summary>
    public static readonly Luminous Shard = Luminous.Chip with { Ramp = [(236, 250, 255), (150, 196, 224)], Gravity = 380, Bounce = 0.35, Size = 1.1 };

    /// <summary>Glitter off crystal or glass: tiny cold streaks of light, arcing down, each lighting its spot a little.</summary>
    public static readonly Luminous Glitter = new(Glimmer.Streak, [(250, 254, 255), (190, 230, 255), (120, 180, 240)], (160, 360))
    { Fade = 1.2, Gravity = 200, Drag = 1.4, Spin = 2, Stretch = 0.04, Shortest = 0.5, Longest = 4, Width = 0.22, Hot = 0.8, Halo = 2.2, Light = true };

    /// <summary>A spore puffed off a cap: a fine soft dot drifting up and curling (the caves' spores, 0080).</summary>
    public static readonly Luminous Spore = new(Glimmer.Mote, [(236, 255, 246), (160, 236, 214)], (1400, 2600))
    { Bright = 0.75, Fade = 0.9, Gravity = -4, Drag = 1.2, Turbulence = 10, NoiseScale = 0.05, Size = 0.55 };

    /// <summary>A fibre of cloth: a hair-thin pale streak of matter, drifting down and curling.</summary>
    public static readonly Luminous Fibre = new(Glimmer.Streak, [(222, 210, 186), (170, 156, 132)], (600, 1100))
    { Matter = true, Bright = 0.8, Fade = 0.7, Gravity = 22, Drag = 3, Spin = 3, Turbulence = 26, NoiseScale = 0.1, Stretch = 0.06, Shortest = 1, Longest = 3, Width = 0.2, Hot = 0 };

    /// <summary>A chip of glazed crockery, tumbling.</summary>
    public static readonly Luminous CeramicChip = Luminous.Chip with { Ramp = [(226, 214, 196), (168, 108, 80)], Gravity = 400, Bounce = 0.3, Size = 1.2 };

    /// <summary>Fine pale dust off wood, cloth or crockery.</summary>
    public static readonly Luminous PaleDust = Luminous.HitDust with { Ramp = [(206, 198, 186)], Gravity = 6, LifeMs = (800, 1500) };

    /// <summary>A white slash of a hit on a creature: short, bright and gone at once, the crisp line of the strike.</summary>
    public static readonly Luminous Slash = new(Glimmer.Streak, [(255, 255, 255), (255, 236, 220)], (70, 140))
    { Fade = 2, Drag = 6, Stretch = 0.06, Shortest = 2, Longest = 7, Width = 0.35, Hot = 1 };

    /// <summary>A tuft of fur knocked loose: a short soft streak of matter, drifting down and curling.</summary>
    public static readonly Luminous Tuft = new(Glimmer.Streak, [(236, 214, 176), (190, 160, 120)], (500, 900))
    { Matter = true, Bright = 0.85, Fade = 0.8, Gravity = 60, Drag = 2.4, Spin = 3, Turbulence = 22, NoiseScale = 0.1, Stretch = 0.05, Shortest = 1, Longest = 2.5, Width = 0.35, Hot = 0 };

    /// <summary>A tuft of a cool coat (the tapir's, the sixleg's, the fluff's: the savanna palette's lilac fur), as <see cref="Tuft"/>.</summary>
    public static readonly Luminous CoolTuft = Tuft with { Ramp = [(206, 198, 222), (143, 134, 180)] };

    /// <summary>A soft warm puff off a struck body.</summary>
    public static readonly Luminous Puff = Luminous.HitDust with { Ramp = [(240, 222, 200)], Gravity = -4, LifeMs = (500, 1000) };

    private static readonly (int, int, int) Warm = (255, 214, 150), WarmLight = (255, 220, 170), Cold = (214, 240, 255);

    /// <summary>Each object's whole hit.</summary>
    public static readonly IReadOnlyDictionary<Impact, HitLook> Of = new Dictionary<Impact, HitLook>
    {
        // The axe's bite on a wreck as 0075 had it: 16 sparks back toward the hero, 14 motes of metal dust, 3 dark chips.
        [Impact.Wreck] = new([
            new(Luminous.HitSpark, 16, (60, 175), 0.8),
            new(Luminous.HitDust, 14, (5, 26), 1.8, Size: (0.4, 0.7)),
            new(Luminous.Chip, 3, (22, 40), 1.1),
        ], Glint: 10, GlintColour: Warm, Shock: 0.35, Flash: 0.25, FlashColour: WarmLight),
        [Impact.Alloy] = new([
            new(FallingSpark, 12, (60, 160), 0.8, Up: 0.3),
            new(DustOf("alloy"), 6, (4, 20), 1.6, Up: 0.4, Size: (0.4, 0.65)),
            new(ChipsOf("alloy", 1.2), 3, (30, 70), 0.7, Up: 0.6),
        ], Glint: 8, GlintColour: Warm, Shock: 0.25, Flash: 0.2, FlashColour: WarmLight),
        [Impact.Stone] = new([
            new(StoneSpark, 4, (50, 130), 0.9, Up: 0.3),
            new(ChipsOf("stone"), 5, (30, 75), 0.7, Up: 0.7),
            new(DustOf("stone"), 5, (3, 16), 1.6, Up: 0.5, Size: (0.4, 0.7)),
        ], Flash: 0.06, FlashColour: WarmLight),
        [Impact.Soil] = new([
            new(ChipsOf("soil", 1.3), 6, (30, 75), 0.7, Up: 0.7),
            new(DustOf("soil"), 7, (3, 18), 1.7, Up: 0.5, Size: (0.45, 0.75)),
        ]),
        [Impact.Plaster] = new([
            new(ChipsOf("plaster", 1.3), 4, (30, 75), 0.7, Up: 0.7),
            new(DustOf("plaster"), 9, (3, 20), 1.8, Up: 0.5, Size: (0.45, 0.8)),
        ]),
        [Impact.Wood] = new([
            new(Splinter, 6, (40, 90), 0.8, Up: 0.4),
            new(WoodChip, 2, (25, 55), 0.7, Up: 0.6),
            new(PaleDust, 3, (3, 14), 1.6, Up: 0.5, Size: (0.4, 0.6)),
        ]),
        [Impact.Tree] = new([
            new(Splinter, 6, (40, 90), 0.8, Up: 0.4),
            new(WoodChip, 1, (25, 55), 0.7, Up: 0.6),
            new(Leaf, 3, (20, 50), 1.4, Up: 1.5),
        ]),
        [Impact.Leaves] = new([
            new(Leaf, 4, (18, 48), 1.3, Up: 1),
            new(PaleDust with { Ramp = [(170, 210, 150)] }, 2, (3, 12), 1.6, Up: 0.6, Size: (0.35, 0.55)),
        ]),
        [Impact.Crystal] = new([
            new(Glitter, 5, (40, 110), 1, Up: 0.4),
            new(Shard, 3, (25, 60), 0.7, Up: 0.7),
        ], Glint: 6, GlintColour: Cold, Flash: 0.08, FlashColour: Cold),
        [Impact.Spores] = new([
            new(Spore, 7, (6, 22), 1.8, Up: 1),
        ]),
        [Impact.Cloth] = new([
            new(Fibre, 6, (18, 50), 1.2, Up: 0.5),
            new(PaleDust, 2, (3, 12), 1.6, Up: 0.5, Size: (0.35, 0.55)),
        ]),
        [Impact.Glass] = new([
            new(Shard, 4, (30, 70), 0.7, Up: 0.6),
            new(Glitter, 4, (40, 110), 1, Up: 0.4),
        ], Glint: 6, GlintColour: Cold, Flash: 0.06, FlashColour: Cold),
        [Impact.Ceramic] = new([
            new(CeramicChip, 4, (30, 75), 0.7, Up: 0.7),
            new(PaleDust, 3, (3, 16), 1.6, Up: 0.5, Size: (0.4, 0.65)),
        ]),
        // Crisp rather than gory: a few hot white slash streaks, tufts of fur and a soft warm puff, the shock line bright.
        [Impact.Flesh] = new([
            new(Slash, 4, (110, 220), 0.5),
            new(Tuft, 5, (25, 70), 1.1, Up: 0.6),
            new(Puff, 4, (4, 18), 1.8, Up: 0.4, Size: (0.5, 0.85)),
        ], Glint: 7, GlintColour: (255, 255, 255), Shock: 0.55, Flash: 0.08, FlashColour: (255, 236, 220)),
        [Impact.CoolFlesh] = new([
            new(Slash, 4, (110, 220), 0.5),
            new(CoolTuft, 5, (25, 70), 1.1, Up: 0.6),
            new(Puff, 4, (4, 18), 1.8, Up: 0.4, Size: (0.5, 0.85)),
        ], Glint: 7, GlintColour: (255, 255, 255), Shock: 0.55, Flash: 0.08, FlashColour: (236, 232, 255)),
    };

    /// <summary>A creature struck, by its species (0102), as <see cref="Sound.Creatures"/> hears it: its tufts its coat's colour.</summary>
    public static readonly IReadOnlyDictionary<string, Impact> Creatures = new Dictionary<string, Impact>
    {
        [Stillwake.Content.Fauna.Loper.Id] = Impact.Flesh,
        [Stillwake.Content.Fauna.Tapir.Id] = Impact.CoolFlesh,
        [Stillwake.Content.Fauna.Sixleg.Id] = Impact.CoolFlesh,
        [Stillwake.Content.Fauna.Fluff.Id] = Impact.CoolFlesh,
    };

    /// <summary>The tuft a creature struck with <paramref name="impact"/> throws dying, every way.</summary>
    public static Luminous TuftOf(Impact impact) => impact == Impact.CoolFlesh ? CoolTuft : Tuft;

    /// <summary>A natural wall hit, by its material, as <see cref="Sound.Ground"/> hears it: bedrock and lava never break, so never are.</summary>
    public static readonly IReadOnlyDictionary<string, Impact> Ground = new Dictionary<string, Impact>
    {
        ["soil"] = Impact.Soil,
        ["stone"] = Impact.Stone,
        ["plaster"] = Impact.Plaster,
        ["alloy"] = Impact.Alloy,
    };

    /// <summary>
    /// A growth hit, by its kind, as <see cref="Sound.Growths"/> hears it; what lies in the rock (<see cref="Sound.Unheard"/>) is
    /// struck as its stone. A ruin's piece by its material.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Impact> Growths = Looking(
        (["acacia"], Impact.Tree),
        (["bush", "bellTeal", "bellViolet", "bellAmber", "tuft", "herbs"], Impact.Leaves),
        (["roots"], Impact.Wood),
        (["rock", "stalagmite", "stalactite"], Impact.Stone),
        (["cobweb", "threadTeal", "threadViolet"], Impact.Cloth),
        (["crystalRose", "crystalIce", "crystalAmber", "crystalEmber", "crystalIceHang", "crystalAmberHang", "geode", "veinIce",
            Stillwake.Content.Ruins.Cradle], Impact.Crystal),
        (["glowcapViolet", "glowcapTeal", "hangcap"], Impact.Spores),
        (["table", "chair", "stool", "bench", "bed", "cradle", "wardrobe", "chest", "frame", "clock", "basket", "toys", "desk",
            "bookshelf", "wallShelf", "toolRack", "heightMarks"], Impact.Wood),
        (["banner", "tapestry", "rug", "curtain", "coatPegs", "drawings"], Impact.Cloth),
        (["ceilLamp", "sconce", "floorLamp", "lantern", "arcCandle", "lightWindow", "garland", "brokenLamp", "shelfJars", "holoPortrait",
            Stillwake.Content.Ruins.SpireLamp, Stillwake.Content.Ruins.Niche], Impact.Glass),
        (["hearth", "kettle", "utensils", "musicBox", Stillwake.Content.Ruins.Resonator, "fallenBeam", Stillwake.Content.Ruins.SpireCache],
            Impact.Alloy),
        (["dishes", "pots", "basin", "plantAlive", "plantDead"], Impact.Ceramic));

    private static Dictionary<string, Impact> Looking(params (string[] Kinds, Impact Impact)[] groups) =>
        groups.SelectMany(g => g.Kinds.Select(kind => (kind, g.Impact))).ToDictionary(k => k.kind, k => k.Impact);

    /// <summary>A plant's hit lands this share of a cell from its cell's middle toward the hero: its picture fills its cell unevenly.</summary>
    public const double PlantInset = 0.25;

    /// <summary>A hit's point on a face is kept this share of the face from either corner, off the edge of the next.</summary>
    public const double FaceMargin = 0.15;

    /// <summary>How far under a hit chips fall to a floor at most, in cells; past it they fall on out of sight.</summary>
    public const int FloorRows = 6;
}
