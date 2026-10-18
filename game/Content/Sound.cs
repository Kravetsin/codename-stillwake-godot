using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game;

/// <summary>
/// What is heard (ADR 0033): each cue's files under <c>game/audio</c>, one-shots normalised to −3 dBFS
/// peak (the chop's and the break's variants then matched by loudness, the louder turned down) and loops
/// to about −20 LUFS, and how loud each plays in the mix; the buses by name, the contract settings will set
/// volumes by; and how the world falls silent round a hero floating in space. Swapping a placeholder
/// for another file is a change here only.
/// </summary>
public static class Sound
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

    /// <summary>
    /// How every station's sound carries while it works (0059): the Former's, the smelter's, a furnace's or burner
    /// generator's fire, a charging station's hum. All on the World bus, all as far: full within 2 cells, gone at 10. Their
    /// volumes are set by ear and meter so each station at work is about as loud as the next, about −32 LUFS at its loudest
    /// 400 ms within 2 cells as Godot mixes it: it tells that it works, under a door, a step or a tool.
    /// </summary>
    public static readonly Reach StationReach = new(2, 10);

    /// <summary>
    /// How a hand tool's sound carries where it is placed: the axe's chop, and a friend's harpoon heard on World,
    /// its shot, its drum and its rod biting (0065). Full within 5 cells, gone at 20.
    /// </summary>
    public static readonly Reach ToolReach = new(5, 20);

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

    /// <summary>A boot on the hollow metal deck, at each half stride walking or running.</summary>
    public static readonly Cue Step = new(Shots("crew/step", 6), -12, Heard: new(4, 18));
    /// <summary>A landing: the same boot on the same deck, louder and a little lower for the body's weight.</summary>
    public static readonly Cue Land = Step with { VolumeDb = -8, Tune = 0.85f };
    /// <summary>The boots pushing off the deck as the hero jumps; walking off an edge makes none.</summary>
    public static readonly Cue Jump = new(Shots("crew/jump", 4), -12, Heard: new(4, 18));
    public static readonly Cue Alarm = new(Shots("crew/alarm", 1), -14, Pitch: 0);
    public static readonly Cue Screwdriver = new(Shots("building/screwdriver", 5), -16, Heard: new(4, 14));
    public static readonly Cue Hiss = Looped("atmosphere/hiss", -8, new(6, 24));
    public static readonly Cue Click = new(Shots("ui/click", 2), -12);
    public static readonly Cue VentHum = Looped("atmosphere/vent-hum", -12, new(3, 10));
    public static readonly Cue DoorOpen = new(Shots("grid/door-open", 1), -8, Heard: new(6, 24));
    public static readonly Cue DoorClose = new(Shots("grid/door-close", 1), -8, Heard: new(6, 24));
    public static readonly Cue Valve = new(Shots("grid/valve", 3), -12, Heard: new(4, 14));
    /// <summary>The one soft click an action makes: a crate opened, a canister loaded, debris caught, an item picked up.</summary>
    public static readonly Cue Act = new(Shots("hands/act", 1), -10);
    /// <summary>
    /// The action's sound while this player's hero floats in space (0042): a glove's low thump in place of the
    /// click, which has almost nothing the muffle lets through; on the Suit bus, muffled as all there is. Placed on World
    /// where a friend's harpoon rod bites a crate or pod (0065).
    /// </summary>
    public static readonly Cue ActInSpace = new(Shots("hands/act-space", 1), -12, Heard: ToolReach);
    /// <summary>A furnace's or burner generator's fire crackling while it burns, a station's sound (<see cref="StationReach"/>).</summary>
    public static readonly Cue Fire = Looped("furnace/fire", -13, StationReach);
    /// <summary>
    /// This player's own axe biting into a wreck (0034, 0042), heard where the wreck is: on the Suit bus,
    /// through the glove, as a wreck is only ever out in space; another hero's hit there is silent.
    /// </summary>
    public static readonly Cue Chop = new(Shots("debris/chop", 3), -2, Pitch: 0.1f, Heard: ToolReach);
    /// <summary>
    /// This player's own drill working on a wreck (0054), heard at its bit on the Suit bus as the chop is: a pickaxe on
    /// metal, a low heavy tuk, cut to 50 ms so <see cref="DrillStrikesPerSecond"/> of them buzz as a hammer drill does
    /// (its 4000 to 5500 blows a minute are a buzz, not clicks), apart from the damage's pace. Each strike's pitch and
    /// volume are varied by the player (<see cref="DrillStrikePitch"/>, <see cref="DrillStrikeVolumeDb"/>), not the cue.
    /// </summary>
    public static readonly Cue DrillStrike = new(Shots("debris/drill-strike", 1), -10, Pitch: 0, Heard: new(5, 20));
    /// <summary>Strikes a second while the drill works on a piece, and how far each one's pitch scale and volume stray either way.</summary>
    public const float DrillStrikesPerSecond = 18, DrillStrikePitch = 0.12f, DrillStrikeVolumeDb = 1.5f;

    /// <summary>
    /// This player's drill's motor (0059), heard at its bit on the Suit bus whenever it spins, on a piece or at nothing: grains of
    /// a cordless drill running free, 150 ms each, <see cref="DrillWhirGrainsPerSecond"/> of them a second at full speed (fewer
    /// and lower as it spins up or down), overlapping, each its pitch within ±<see cref="DrillWhirPitch"/> of the motor's speed.
    /// Never a long recording looped. 6 dB under the strikes, as the user chose it.
    /// </summary>
    public static readonly Cue DrillWhir = new(Shots("debris/drill-whir", 3), -8, Pitch: 0, Heard: new(5, 20));
    public const float DrillWhirGrainsPerSecond = 9, DrillWhirPitch = 0.02f;

    /// <summary>
    /// The drill's motor: spun up and down as its picture is (<see cref="HeldLook.SpinUpMs"/>), from 0.35 of its pitch; with its bit
    /// on a piece it sags 8% and quietens 3 dB under the strikes, eased in over 60 ms and out over 150 ms.
    /// </summary>
    public static readonly Motor DrillMotor = new(HeldLook.SpinUpMs / 1000, HeldLook.SpinDownMs / 1000, Idle: 0.35, Sag: 0.08, LoadInS: 0.06, LoadOutS: 0.15, LoadedDb: -3, Swell: 1.5);

    /// <summary>
    /// How long the drill counts as on a piece after its bit last was, in seconds: grazing a piece's edge, where the bit is on and
    /// off it from frame to frame, keeps striking rather than breaking into bursts.
    /// </summary>
    public const double DrillTouchHoldS = 0.1;

    /// <summary>
    /// The Former's press (0059), where it stands, on the World bus: a 10-ton punch press's stroke, once each time its head comes
    /// down in its pictures (every 0.44 s at its 4 × 110 ms), each its pitch scale within ±<see cref="FormerStrokePitch"/> and its
    /// volume within ±<see cref="FormerStrokeVolumeDb"/>. Soft, 3 dB over its motor, so it tells the press works without pounding.
    /// </summary>
    public static readonly Cue FormerStroke = new(Shots("crafting/former-stroke", 1), -15, Pitch: 0, Heard: StationReach);
    public const float FormerStrokePitch = 0.15f, FormerStrokeVolumeDb = 1.5f;

    /// <summary>
    /// The Former's motor while it works: a flywheel's belt and bearing, swaying 2.25 times a second, once a stroke, 3 dB under the
    /// strokes; spun up over 0.6 s from half its pitch, run down over 0.8 s, and dipping 3% at each stroke, back over 120 ms.
    /// </summary>
    public static readonly Cue FormerMotor = Looped("crafting/former-motor", -18, StationReach);
    public static readonly Motor FormerSpin = new(0.6, 0.8, Idle: 0.5, Sag: 0.03, LoadInS: 0, LoadOutS: 0.12);

    /// <summary>
    /// The electric smelter's coils while it works (0059): a 100 Hz hum with a hiss of hot metal over it, risen over <see cref="SmelterHumInS"/> and fallen over <see cref="SmelterHumOutS"/>.
    /// </summary>
    public static readonly Cue SmelterHum = Looped("crafting/smelter-hum", -14, StationReach);
    public const double SmelterHumInS = 0.3, SmelterHumOutS = 0.6;

    /// <summary>
    /// The melt bubbling in the smelter while it works: six low pops, played at random moments, <see cref="SmelterCracklesPerSecond"/>
    /// a second on average, each its pitch within ±<see cref="SmelterCracklePitch"/> and its volume up to
    /// <see cref="SmelterCrackleLowDb"/> under its cue's.
    /// </summary>
    public static readonly Cue SmelterCrackle = new(Shots("crafting/smelter-crackle", 6), -10, Pitch: 0, Heard: StationReach);
    public const float SmelterCracklesPerSecond = 2, SmelterCracklePitch = 0.1f, SmelterCrackleLowDb = -10;

    /// <summary>
    /// This player's jetpack thrusting (0047), through the suit: bright noise, high-passed at 700 Hz, low-passed at
    /// 4.8 kHz, its filters drifting ±150 cents, a 2 s loop rendered for Stillwake. It rises over
    /// <see cref="ThrustAttackS"/> as the thrust begins and falls over <see cref="ThrustReleaseS"/> as it ends. It only ever
    /// thrusts in space, where the Suit muffle takes most of a hiss: played loud (0059), it comes through dulled, about 6 dB
    /// under the bed of space.
    /// </summary>
    public static readonly Cue Thrust = new(["res://audio/crew/thrust-1.ogg"], 3, Loops: true);
    public const double ThrustAttackS = 0.04, ThrustReleaseS = 0.14;

    /// <summary>
    /// A charging station's hum while it charges (0053): a warm 50 Hz with its harmonics under a 500 Hz low-pass,
    /// swaying ±4% twice a second, a 4 s loop rendered for Stillwake; a station's sound (<see cref="StationReach"/>). Faded in
    /// over <see cref="HumInS"/> and out over <see cref="HumOutS"/>.
    /// </summary>
    public static readonly Cue ChargerHum = Looped("power/charger-hum", -10, StationReach);
    public const double HumInS = 0.4, HumOutS = 0.5;

    /// <summary>
    /// A drone's rotors (0062), one loop a drone where it flies, on the World bus, a station's reach: air through a fan, noise
    /// in a band about 450 Hz chopped 37.5 times a second over a low rustle and a faint 112.5 Hz (the audition's synth #2,
    /// "Fan"), 4 dB under a station at work (−36 LUFS within 2 cells, standing). Its motor (<see cref="DroneMotor"/>) spins up
    /// as it takes off and down once it touches down; flying, it rises <see cref="DroneSpeedPitch"/> in pitch and
    /// <see cref="DroneSpeedDb"/> in volume at full speed, wavers ±<see cref="DroneWobbleCents"/> cents and
    /// ±<see cref="DroneWobbleDb"/> dB on three slow sines (<see cref="DroneWobble"/>), and each drone is tuned within
    /// ±<see cref="DroneSwarm"/> by its id, so a few never hum in unison.
    /// </summary>
    public static readonly Cue DroneWhir = Looped("logistics/drone-whir", -16, StationReach);
    /// <summary>A drone's motor: spun up over 0.5 s from 0.4 of its pitch and down over 0.7 s, full volume from two thirds of its spin.</summary>
    public static readonly Motor DroneMotor = new(0.5, 0.7, Idle: 0.4, Sag: 0, LoadInS: 0, LoadOutS: 0, Swell: 1.5);
    public const double DroneSpeedPitch = 0.06, DroneSpeedDb = 2, DroneWobbleCents = 12, DroneWobbleDb = 0.5, DroneSwarm = 0.04;
    /// <summary>The wobble's three sines: each its rate, Hz, and its share of the whole.</summary>
    public static readonly IReadOnlyList<(double Hz, double Share)> DroneWobble = [(0.7, 0.5), (1.9, 0.3), (4.3, 0.2)];

    /// <summary>
    /// A drone touching down on its pad, and lifting off one (the user's pick: the same sound both ways), where the pad is: a
    /// soft thud, three variants, a small latch clicking 60 ms after it, 6 dB under; 1 dB under a station (−33 LUFS).
    /// </summary>
    public static readonly Cue DroneDock = new(Shots("logistics/drone-dock", 3), -14, Heard: StationReach);

    /// <summary>A drone's clamp taking a stack, or putting it in: one of three small plastic switches, 3 dB under a station (−35 LUFS).</summary>
    public static readonly Cue DroneGrab = new(Shots("logistics/drone-grab", 3), -10, Heard: StationReach);

    /// <summary>A battery in a charging station come full: one soft blip, where the station stands.</summary>
    public static readonly Cue ChargeFull = new(Shots("power/charge-full", 1), -10, Pitch: 0, Heard: new(4, 16));
    /// <summary>A wreck breaking apart under this player's last hit, in place of its chop: on the Suit bus as the chop is, and heard farther.</summary>
    public static readonly Cue Break = new(Shots("debris/break", 3), -2, Heard: new(6, 28));

    /// <summary>
    /// This player's harpoon (0044), on the Suit bus, through the glove: its rod fired, a dart trap's click and
    /// whoosh (<see cref="Shot"/>); its drum winding the line in, a fishing reel, looped from the press that reels
    /// till the piece is in (<see cref="Drum"/>); and a bolt worked home as the hero's cooldown ends, ready to fire
    /// again (<see cref="Ready"/>). The rod biting plays <see cref="Chop"/>, or <see cref="ActInSpace"/> on a crate or pod.
    /// A friend's harpoon plays the same, but Ready, on the World bus where it is (0065).
    /// </summary>
    public static readonly Cue Shot = new(Shots("crew/harpoon-shot", 1), -6, Heard: ToolReach);
    public static readonly Cue Drum = new(["res://audio/crew/harpoon-reel-1.ogg"], -10, Loops: true, Heard: ToolReach);
    public static readonly Cue Ready = new(Shots("crew/harpoon-ready", 1), -10);

    /// <summary>
    /// A canister breathed into the suit (0047), heard through it: a short hiss of air, a placeholder cut from
    /// <see cref="Hiss"/> until a sound is picked.
    /// </summary>
    public static readonly Cue Breathe = new(["res://audio/crew/breathe-1.ogg"], -8);

    /// <summary>The title's music (0035), on the Music bus, faded in with the title and out on leaving it.</summary>
    public static readonly Cue Title = new(["res://audio/music/title-1.ogg"], -2, Loops: true);

    /// <summary>How long the title's music takes to fade in or out, in seconds.</summary>
    public const double MusicFadeS = 2.5;

    /// <summary>
    /// The quiet bed that fills the silence of space (0042), on the Music bus: faded in over
    /// <see cref="SpaceInS"/> seconds as this player's hero leaves the ship, out over <see cref="SpaceOutS"/>
    /// back aboard; 3 dB lower since 0059, under what the hero touches. A placeholder, the synthesised drone "Wreck", until the
    /// team's own track.
    /// </summary>
    public static readonly Cue Space = new(["res://audio/music/space-1.ogg"], -13, Loops: true);

    public const double SpaceInS = 2.5, SpaceOutS = 2;

    public static readonly IReadOnlyList<Cue> All = [Title, Space, Step, Land, Jump, Alarm, Screwdriver, Hiss, Click, VentHum, DoorOpen, DoorClose, Valve, Act, ActInSpace, Fire, Chop, DrillStrike, Break, Shot, Drum, Ready, Breathe, Thrust, ChargerHum, ChargeFull, DrillWhir, FormerStroke, FormerMotor, SmelterHum, SmelterCrackle, DroneWhir, DroneDock, DroneGrab];

    /// <summary>The suit's alarm beeps this often, in seconds, while it runs low out of air.</summary>
    public const double AlarmEveryS = 1.6;

    /// <summary>
    /// Floating in space the world goes silent over <see cref="MuffleMs"/>: the World bus's low-pass falls from
    /// <see cref="ClearHz"/> to <see cref="MuffledHz"/> as the bus fades out. What the hero touches, on the
    /// Suit bus, dulls over the same time to a thud through glove and helmet (0042): its low-pass falls to
    /// <see cref="SuitMuffledHz"/> and its lows below <see cref="SuitLowsHz"/> rise to <see cref="SuitLowsGain"/>
    /// (Godot's shelf gain, the lows' amplitude times its square: 1.25 is about +4 dB); and the bus rises <see cref="SuitFloatingDb"/>,
    /// so what the hero touches stands out of the bed of space (0059).
    /// </summary>
    public const float ClearHz = 20000, MuffledHz = 300, MuffleMs = 600, SuitMuffledHz = 900, SuitLowsHz = 300, SuitLowsGain = 1.25f, SuitFloatingDb = 6;

    private static string[] Shots(string cue, int count) =>
        Enumerable.Range(1, count).Select(n => $"res://audio/{cue}-{n}.wav").ToArray();

    private static Cue Looped(string cue, float volumeDb, Reach heard) => new([$"res://audio/{cue}-1.ogg"], volumeDb, Loops: true, Heard: heard);
}
