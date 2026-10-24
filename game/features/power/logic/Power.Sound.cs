namespace Stillwake.Game;

public static partial class Sound
{
    /// <summary>
    /// A charging station's hum while it charges (0053): a warm 50 Hz with its harmonics under a 500 Hz low-pass,
    /// swaying ±4% twice a second, a 4 s loop rendered for Stillwake; a station's sound (<see cref="StationReach"/>). Faded in
    /// over <see cref="HumInS"/> and out over <see cref="HumOutS"/>.
    /// </summary>
    public static readonly Cue ChargerHum = Looped("power/charger-hum", -10, StationReach);
    public const double HumInS = 0.4, HumOutS = 0.5;

    /// <summary>A battery in a charging station come full: one soft blip, where the station stands.</summary>
    public static readonly Cue ChargeFull = new(Shots("power/charge-full", 1), -10, Pitch: 0, Heard: new(8, 32));
}
