namespace Stillwake.Game;

public static partial class Sound
{
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
}
