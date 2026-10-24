namespace Stillwake.Game;

public static partial class Sound
{
    public static readonly Cue Alarm = new(Shots("crew/alarm", 1), -14, Pitch: 0);

    /// <summary>The suit's alarm beeps this often, in seconds, while it runs low out of air.</summary>
    public const double AlarmEveryS = 1.6;

    /// <summary>
    /// How loud a hand tool is while it works nothing, its idle: the drill's motor running free (<see cref="DrillWhir"/>) and
    /// the axe's swing (<see cref="Swing"/>). Their files are levelled alike, a train of whir grains at their rate and each
    /// swing at −23.4 LUFS momentary, so this one value raises or lowers every idle tool sound together.
    /// </summary>
    public const float ToolIdleDb = -8;

    /// <summary>
    /// This player's own axe swung (0068), every swing, a wreck hit or nothing: a staff's whoosh, one of four, its pitch
    /// ±8%, on the Suit bus through the body as the chop is, so muffled the same in space. Each file's loudest moment lies
    /// <see cref="SwingPeakS"/> in, and it plays so that moment falls in the middle of the downstroke.
    /// </summary>
    public static readonly Cue Swing = new(Shots("crew/axe-swing", 4), ToolIdleDb, Pitch: 0.08f);
    public const double SwingPeakS = 0.065;

    /// <summary>
    /// This player's drill's motor (0059), heard at its bit on the Suit bus whenever it spins, on a piece or at nothing: grains of
    /// a cordless drill running free, 150 ms each, <see cref="DrillWhirGrainsPerSecond"/> of them a second at full speed (fewer
    /// and lower as it spins up or down), overlapping, each its pitch within ±<see cref="DrillWhirPitch"/> of the motor's speed.
    /// Never a long recording looped. 6 dB under the strikes, as the user chose it; at <see cref="ToolIdleDb"/>.
    /// </summary>
    public static readonly Cue DrillWhir = new(Shots("debris/drill-whir", 3), ToolIdleDb, Pitch: 0, Heard: new(10, 40));
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
    /// This player's jetpack thrusting (0047), through the suit: bright noise, high-passed at 700 Hz, low-passed at
    /// 4.8 kHz, its filters drifting ±150 cents, a 2 s loop rendered for Stillwake. It rises over
    /// <see cref="ThrustAttackS"/> as the thrust begins and falls over <see cref="ThrustReleaseS"/> as it ends. It only ever
    /// thrusts in space, where the Suit muffle takes most of a hiss: played loud (0059), it comes through dulled, about 6 dB
    /// under the bed of space.
    /// </summary>
    public static readonly Cue Thrust = new(["res://audio/crew/thrust-1.ogg"], 3, Loops: true);
    public const double ThrustAttackS = 0.04, ThrustReleaseS = 0.14;

    /// <summary>
    /// A hero touching lava (0087): a hiss of pressure, its suit burning, at each breath while it burns
    /// (<see cref="LavaBurnEveryS"/>), the first as it touches; about as loud as a chop. This player's own on Suit, a friend's on World.
    /// </summary>
    public static readonly Cue LavaBurn = new(Shots("crew/lava-burn", 1), -6, Heard: ToolReach);
    public static readonly double LavaBurnEveryS = (double)Stillwake.Content.Crew.BreathEvery / Stillwake.Sim.World.TicksPerSecond;

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
    /// A canister breathed into the suit (0047): air filling it, half a second of hiss rising fast and falling away, its
    /// band climbing as it fills. This player's own on the Helmet bus, in the helmet's air, so floating out in space never
    /// muffles it away (on Suit its band was all but cut there); a friend's on World where its hero is, a tool's reach.
    /// Played at about −20 dB RMS over 100 ms, the alarm's −17, well over the bed of space; a click peaks −15 dBFS. A
    /// placeholder synthesised in code until the audition's pick takes its file.
    /// </summary>
    public static readonly Cue AirFill = new(Shots("crew/air-fill", 1), -6, Heard: ToolReach);

    /// <summary>
    /// This player's hero breathing room air again, out of thin air or vacuum (0081): one soft inhale in the helmet, under
    /// the alarm, once as the air comes back, never each breath. A synthesised placeholder until a sound is picked.
    /// </summary>
    public static readonly Cue Inhale = new(Shots("crew/inhale", 1), -18);

    /// <summary>
    /// A ram's hit (0103), a placeholder from what is here till the audition: a heavy landing's thud, lower, this player's own
    /// on Suit, a friend's on World.
    /// </summary>
    public static Cue RamThud => CrewDerived.RamThud;

    /// <summary>The cues made from another file's, made when first read, after every static initializer has run.</summary>
    private static class CrewDerived
    {
        public static readonly Cue RamThud = LandHard with { VolumeDb = 0, Tune = 0.72f, Pitch = 0.08f, Heard = ToolReach };
    }
}
