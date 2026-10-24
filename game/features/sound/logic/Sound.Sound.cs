using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class Sound
{
    /// <summary>A boot on the hollow metal deck, at each half stride walking or running.</summary>
    public static readonly Cue Step = new(Shots("crew/step", 6), -12, Heard: new(8, 36));
    /// <summary>A landing: the same boot on the same deck, louder and a little lower for the body's weight.</summary>
    public static readonly Cue Land = Step with { VolumeDb = -8, Tune = 0.85f };
    /// <summary>The boots pushing off the deck as the hero jumps; walking off an edge makes none.</summary>
    public static readonly Cue Jump = new(Shots("crew/jump", 4), -12, Heard: new(8, 36));

    /// <summary>
    /// Boots on a planet's ground (0093), at each half stride as on the deck: on grass (soil under the open sky), bare soil, stone
    /// (bedrock, lava's crust and the homes' plaster floors too) and the ruins' alloy. Levelled 13 dB under their files: the
    /// audition's 8 stood out over everything else in play, so the user had them 5 dB quieter. A jump off the ground pushes
    /// off with the step of what it stood on. Bare soil's fifth take hissed in its tail and is gone.
    /// </summary>
    public static readonly Cue StepGrass = new(Shots("crew/step-grass", 6), -13, Heard: new(8, 36));
    public static readonly Cue StepSoil = new(Shots("crew/step-soil", 4), -13, Heard: new(8, 36));
    public static readonly Cue StepStone = new(Shots("crew/step-stone", 6), -13, Heard: new(8, 36));
    public static readonly Cue StepAlloy = new(Shots("crew/step-alloy", 5), -13, Heard: new(8, 36));
    /// <summary>Moss on a cave's rock underfoot: the grass step, quieter (the audition's proposal).</summary>
    public static readonly Cue StepMoss = StepGrass with { VolumeDb = -17 };

    /// <summary>
    /// A landing on natural ground (0093): a light one on <see cref="LandSoft"/>, one from <c>HeroMotion.Landing.Heavy</c> on
    /// <see cref="LandHard"/>, as the figure's landing pose turns heavy (0090). Louder and lower the harder the fall: from
    /// <see cref="LandSoftestDb"/> and its own pitch at a fall of <see cref="LandSoftestFall"/> or less, to its full volume at
    /// <see cref="LandHardestTune"/> of its pitch from <see cref="LandHardestFall"/>, the audition's hop, jump and long fall.
    /// On the ruins' alloy it lands as on the deck, its step louder and lower (<see cref="LandAlloy"/>).
    /// </summary>
    public static readonly Cue LandSoft = new(Shots("crew/land-soft", 1), -6, Heard: new(8, 36));
    public static readonly Cue LandHard = new(Shots("crew/land-hard", 3), -6, Heard: new(8, 36));
    public static readonly Cue LandAlloy = StepAlloy with { VolumeDb = -4, Tune = 0.85f };
    public const int LandSoftestFall = 500, LandHardestFall = 1800;
    public const float LandSoftestDb = -6, LandHardestTune = 0.88f;

    /// <summary>
    /// What each step plays, by what the boots stand on (<see cref="Footing"/>): the deck's own step on anything built.
    /// </summary>
    public static readonly IReadOnlyDictionary<Footing, Cue> Steps = new Dictionary<Footing, Cue>
    {
        [Footing.Deck] = Step,
        [Footing.Grass] = StepGrass,
        [Footing.Moss] = StepMoss,
        [Footing.Soil] = StepSoil,
        [Footing.Stone] = StepStone,
        [Footing.Alloy] = StepAlloy,
    };

    /// <summary>
    /// Every tool's hit sounds as what it hits (0093), the ground or a wreck: a drill plays the axe's own hit sounds, every
    /// one it hits (the drill's 10 a second), each <see cref="DrillHitDb"/> under the axe's, so a run of
    /// them, overlapping, is about as loud as the axe's hits one by one. Its motor's whir stays its own.
    /// </summary>
    public const float DrillHitDb = -4;

    public static readonly Cue Click = new(Shots("ui/click", 2), -12);

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

    /// <summary>
    /// Floating in space the world goes silent over <see cref="MuffleMs"/>: the World bus's low-pass falls from
    /// <see cref="ClearHz"/> to <see cref="MuffledHz"/> as the bus fades out. What the hero touches, on the
    /// Suit bus, dulls over the same time to a thud through glove and helmet (0042): its low-pass falls to
    /// <see cref="SuitMuffledHz"/> and its lows below <see cref="SuitLowsHz"/> rise to <see cref="SuitLowsGain"/>
    /// (Godot's shelf gain, the lows' amplitude times its square: 1.25 is about +4 dB); and the bus rises <see cref="SuitFloatingDb"/>,
    /// so what the hero touches stands out of the bed of space (0059).
    /// </summary>
    public const float ClearHz = 20000, MuffledHz = 300, MuffleMs = 600, SuitMuffledHz = 900, SuitLowsHz = 300, SuitLowsGain = 1.25f, SuitFloatingDb = 6;
}
