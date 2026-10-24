using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stillwake.Game;

/// <summary>
/// What is heard (ADR 0033): each cue's files under <c>game/audio</c>, one-shots normalised to −3 dBFS
/// peak (the chop's and the break's variants then matched by loudness, the louder turned down) and loops
/// to about −20 LUFS, and how loud each plays in the mix; the buses by name, the contract settings will set
/// volumes by; and how the world falls silent round a hero floating in space. Swapping a placeholder
/// for another file is a change here only.
/// </summary>
/// <remarks>
/// A cue only one feature uses is in that feature's <c>logic/&lt;Name&gt;.Sound.cs</c>; here are the types, the buses, the reaches,
/// the cues several features use and the ground tables built on them. Static initializers of partial files run in no defined
/// order, so none reads a field of another file: a cue made from another file's is built on first use, in a nested class, and
/// <see cref="All"/> is listed in the static constructor, which runs after every initializer.
/// </remarks>
public static partial class Sound
{
    /// <summary>
    /// A cue: its files, any one of them each time a one-shot plays, its pitch varied by up to
    /// <see cref="Pitch"/> either way around <see cref="Tune"/>, its files' own pitch times this; a loop is one
    /// file, looped on import. <see cref="VolumeDb"/> is its place in the mix. Placed in the world, it is heard
    /// at full volume within <see cref="Reach"/>'s <c>Full</c> cells of the listener, fading to silence at its
    /// <c>Silent</c> cells.
    /// </summary>
    public sealed record Cue(IReadOnlyList<string> Files, float VolumeDb, float Pitch = 0.06f, bool Loops = false, Reach? Heard = null, float Tune = 1);

    /// <summary>
    /// What a hero's boots stand on (0093): anything built, a hull wall, a floor or a ladder, is the ship's deck; on a planet's
    /// ground, grass where moss grows on soil under the sky (the drawing's rule), moss on a cave's rock, bare soil, stone, alloy.
    /// </summary>
    public enum Footing { Deck, Grass, Moss, Soil, Stone, Alloy }

    /// <summary>
    /// A motor's sound (0059): a loop that spins up over <paramref name="SpinUpS"/> and down over <paramref name="SpinDownS"/>,
    /// linearly, its pitch scale rising from <paramref name="Idle"/> at a standstill to 1 at speed and its volume with it,
    /// full from <c>1/<paramref name="Swell"/></c> of its speed. Under load it sags: its pitch scale times 1 − <paramref name="Sag"/>
    /// and its volume <paramref name="LoadedDb"/> more, the load eased in over a time constant of <paramref name="LoadInS"/> and
    /// out over <paramref name="LoadOutS"/>; a blow (a press stroke) loads it at once.
    /// </summary>
    public sealed record Motor(double SpinUpS, double SpinDownS, double Idle, double Sag, double LoadInS, double LoadOutS, float LoadedDb = 0, double Swell = 1);

    /// <summary>How far a sound in the world carries, in cells: full volume up to <paramref name="Full"/>, none from <paramref name="Silent"/>.</summary>
    public sealed record Reach(float Full, float Silent)
    {
        /// <summary>The share of full volume heard <paramref name="cells"/> away: fading as the square of what is left of the way to silence.</summary>
        public float Share(float cells)
        {
            if (cells <= Full) return 1;
            float left = 1 - System.Math.Min(1, (cells - Full) / (Silent - Full));
            return left * left;
        }
    }

    /// <summary>The reaches, in a class of their own: made when first read, whichever file's cue reads one first.</summary>
    private static class Reaches
    {
        public static readonly Reach Station = new(4, 20), Tool = new(10, 40), Break = new(12, 56);
    }

    /// <summary>
    /// How every station's sound carries while it works (0059): the Former's, the smelter's, a furnace's or burner
    /// generator's fire, a charging station's hum. All on the World bus, all as far: full within 4 cells, gone at 20. Their
    /// volumes are set by ear and meter so each station at work is about as loud as the next, about −32 LUFS at its loudest
    /// 400 ms within 4 cells as Godot mixes it: it tells that it works, under a door, a step or a tool.
    /// </summary>
    public static Reach StationReach => Reaches.Station;

    /// <summary>
    /// How a hand tool's sound carries where it is placed: the axe's chop, and a friend's harpoon heard on World,
    /// its shot, its drum and its rod biting (0065). Full within 10 cells, gone at 40.
    /// </summary>
    public static Reach ToolReach => Reaches.Tool;

    /// <summary>How far something breaking carries: a wreck, a cell of ground, a tree or a ruin's piece. Full within 12 cells, gone at 56.</summary>
    public static Reach BreakReach => Reaches.Break;

    /// <summary>
    /// The buses (<c>game/default_bus_layout.tres</c>): UI, Ambient, Music and Effects into Master; into
    /// Effects, World (every hero's steps, machines, air, doors: silenced while floating), Suit (what this
    /// player's hero itself touches: an action's sound, its own axe's chop and drill, a wreck its hit breaks, its harpoon, its
    /// jetpack, a canister breathed; heard through the body, dulled while floating, a little louder there) and Helmet (the
    /// suit's alarm, in the helmet's air: always clear). Settings will set Master, Effects, Ambient, UI and Music; World, Suit and Helmet are the
    /// hearing's (0033, 0042). Ambient waits for the first ambience: the ship's hum comes from its vents
    /// and fires, in the world.
    /// </summary>
    public const string Master = "Master", Ui = "UI", Ambient = "Ambient", Effects = "Effects", World = "World", Suit = "Suit", Helmet = "Helmet", Music = "Music";
    public static readonly IReadOnlyList<string> Buses = [Master, Ui, Ambient, Effects, World, Suit, Helmet, Music];

    /// <summary>
    /// The action's sound while this player's hero floats in space (0042): a glove's low thump in place of the
    /// click, which has almost nothing the muffle lets through; on the Suit bus, muffled as all there is. Placed on World
    /// where a friend's harpoon rod bites a crate or pod (0065).
    /// </summary>
    public static readonly Cue ActInSpace = new(Shots("hands/act-space", 1), -12, Heard: ToolReach);

    /// <summary>
    /// This player's own tool biting into a wreck (0034, 0042), heard where the wreck is: on the Suit bus, through the
    /// glove, as a wreck is only ever out in space; another hero's hit there is silent. A wreck sounds hit as this whatever
    /// hits it (0093): the axe's swing, the drill's hits as often as it hits, <see cref="DrillHitDb"/> quieter.
    /// </summary>
    public static readonly Cue Chop = new(Shots("debris/chop", 3), -2, Pitch: 0.1f, Heard: ToolReach);

    /// <summary>A wreck breaking apart under this player's last hit, in place of its chop: on the Suit bus as the chop is, and heard farther.</summary>
    public static readonly Cue Break = new(Shots("debris/break", 3), -2, Heard: BreakReach);

    /// <summary>
    /// A planet's ground and what grows on it, hit and broken (0093): each file levelled as the audition played it under the axe's
    /// swing, so played 1 dB over its level, as loud by the swing as the user heard it; a hit about as loud as a wreck's chop, a
    /// break as a wreck's. Hits ±10% in pitch, falls ±6%. Any tool's hit on soil and stone; a cell of either broken out.
    /// </summary>
    public static readonly Cue HitSoil = new(Shots("building/hit-soil", 5), 1, Pitch: 0.1f, Heard: ToolReach);
    public static readonly Cue HitStone = new(Shots("building/hit-stone", 5), 1, Pitch: 0.1f, Heard: ToolReach);
    public static readonly Cue BreakSoil = new(Shots("building/break-soil", 3), 1, Heard: BreakReach);
    public static readonly Cue BreakStone = new(Shots("building/break-stone", 3), 1, Heard: BreakReach);

    /// <summary>The axe in an acacia's trunk, and the tree falling as its last hit fells it.</summary>
    public static readonly Cue ChopWood = new(Shots("flora/chop-wood", 2), 1, Pitch: 0.1f, Heard: ToolReach);
    public static readonly Cue TreeFall = new(Shots("flora/tree-fall", 1), 1, Heard: BreakReach);
    /// <summary>A bush hit; the caves' threads and roots, quieter.</summary>
    public static readonly Cue Bush = new(Shots("flora/bush", 1), 1, Pitch: 0.1f, Heard: ToolReach);
    public static readonly Cue BushSoft = Bush with { VolumeDb = -5 };
    /// <summary>A tuft of grass or a bell flower cut, a sickle through grass; a herb or a cobweb too.</summary>
    public static readonly Cue Grass = new(Shots("flora/grass", 4), 1, Pitch: 0.1f, Heard: ToolReach);
    /// <summary>A crystal, an icicle, a geode or a vein of ice breaking: icicles snapping.</summary>
    public static readonly Cue Crystal = new(Shots("flora/crystal", 4), 1, Heard: ToolReach);
    /// <summary>A glowcap or a hanging cap felled: a squelch.</summary>
    public static readonly Cue Glowcap = new(Shots("flora/glowcap", 1), 1, Pitch: 0.1f, Heard: ToolReach);

    /// <summary>A ruin's piece felled (0091), by what it is made of: wood, cloth, glass, metal or crockery.</summary>
    public static readonly Cue WoodBreak = new(Shots("ruins/wood-break", 1), 1, Heard: BreakReach);
    public static readonly Cue ClothTear = new(Shots("ruins/cloth-tear", 1), 1, Heard: BreakReach);
    public static readonly Cue GlassBreak = new(Shots("ruins/glass-break", 1), 1, Heard: BreakReach);
    public static readonly Cue MetalBreak = new(Shots("ruins/metal-break", 2), 1, Heard: BreakReach);
    public static readonly Cue CeramicBreak = new(Shots("ruins/ceramic-break", 1), 1, Heard: BreakReach);

    /// <summary>
    /// A natural wall hit by any tool and broken out, by its material: soil and stone their own, the homes' plaster as stone, the
    /// ruins' alloy as a wreck's metal (<see cref="Chop"/>, <see cref="Break"/>). Bedrock and lava never break, so never sound.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (Cue Hit, Cue Break)> Ground = new Dictionary<string, (Cue, Cue)>
    {
        ["soil"] = (HitSoil, BreakSoil),
        ["stone"] = (HitStone, BreakStone),
        ["plaster"] = (HitStone, BreakStone),
        ["alloy"] = (Chop, Break),
    };

    /// <summary>
    /// Growths by what they sound like (0079, 0080, 0091): each kind's hit, none for one any hit fells alone, and its fall. A
    /// surface rock and a stalagmite or a stalactite are stone; a ruin's piece its material. What lies in the rock and goes with
    /// it — moss, mica, a copper vein, the heat's cracks, a fossil, rubble — has no sound of its own: its stone's break speaks.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (Cue? Hit, Cue Fall)> Growths = Sounding(
        (["acacia"], ChopWood, TreeFall),
        (["bush"], Bush, Bush),
        (["rock", "stalagmite", "stalactite"], HitStone, BreakStone),
        (["bellTeal", "bellViolet", "bellAmber", "tuft", "herbs", "cobweb"], null, Grass),
        (["threadTeal", "threadViolet", "roots"], null, BushSoft),
        (["crystalRose", "crystalIce", "crystalAmber", "crystalEmber", "crystalIceHang", "crystalAmberHang", "geode", "veinIce"], null, Crystal),
        (["glowcapViolet", "glowcapTeal", "hangcap"], null, Glowcap),
        (["table", "chair", "stool", "bench", "bed", "cradle", "wardrobe", "chest", "frame", "clock", "basket", "toys", "desk",
            "bookshelf", "wallShelf", "toolRack", "heightMarks"], ChopWood, WoodBreak),
        (["banner", "tapestry", "rug", "curtain", "coatPegs", "drawings"], null, ClothTear),
        (["ceilLamp", "sconce", "floorLamp", "lantern", "arcCandle", "lightWindow", "garland", "brokenLamp", "shelfJars", "holoPortrait",
            Stillwake.Content.Ruins.SpireLamp, Stillwake.Content.Ruins.Niche], null, GlassBreak),
        (["hearth", "kettle", "utensils", "musicBox", Stillwake.Content.Ruins.Resonator, "fallenBeam", Stillwake.Content.Ruins.SpireCache,
            Stillwake.Content.Ruins.Cradle], Chop, MetalBreak),
        (["dishes", "pots", "basin", "plantAlive", "plantDead"], HitStone, CeramicBreak));

    /// <summary>The growths with no sound of their own: in the rock, gone with their stone; or fixed, never felled.</summary>
    public static readonly IReadOnlyList<string> Unheard = ["moss", "mica", "veinCopper", "heatCrack", "fossil", "rubble", "mural", "crack"];

    private static Dictionary<string, (Cue? Hit, Cue Fall)> Sounding(params (string[] Kinds, Cue? Hit, Cue Fall)[] groups) =>
        groups.SelectMany(g => g.Kinds.Select(kind => (kind, g.Hit, g.Fall))).ToDictionary(k => k.kind, k => (k.Hit, k.Fall));

    /// <summary>
    /// Every cue of the game, the sound board's list: each public cue of every file of this class, one for each file and tune
    /// (<c>StepMoss</c> and <c>BushSoft</c> play <c>StepGrass</c>'s and <c>Bush</c>'s files quieter: the board has those).
    /// </summary>
    public static readonly IReadOnlyList<Cue> All;

    static Sound()
    {
        const BindingFlags Statics = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        All = typeof(Sound).GetFields(Statics).Where(f => f.FieldType == typeof(Cue)).Select(f => (Cue)f.GetValue(null)!)
            .Concat(typeof(Sound).GetProperties(Statics).Where(p => p.PropertyType == typeof(Cue)).Select(p => (Cue)p.GetValue(null)!))
            .DistinctBy(cue => (cue.Files[0], cue.Tune))
            .ToList();
    }

    private static string[] Shots(string cue, int count) =>
        Enumerable.Range(1, count).Select(n => $"res://audio/{cue}-{n}.wav").ToArray();

    private static Cue Looped(string cue, float volumeDb, Reach heard) => new([$"res://audio/{cue}-1.ogg"], volumeDb, Loops: true, Heard: heard);
}
