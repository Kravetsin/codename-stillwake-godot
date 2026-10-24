using System.Collections.Generic;

namespace Stillwake.Game;

public static partial class Sound
{
    /// <summary>
    /// A drone's rotors (0062), one loop a drone where it flies, on the World bus, a station's reach: air through a fan, noise
    /// in a band about 450 Hz chopped 37.5 times a second over a low rustle and a faint 112.5 Hz (the audition's synth #2,
    /// "Fan"), 4 dB under a station at work (−36 LUFS within 4 cells, standing). Its motor (<see cref="DroneMotor"/>) spins up
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
}
