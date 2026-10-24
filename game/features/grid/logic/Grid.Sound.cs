namespace Stillwake.Game;

public static partial class Sound
{
    public static readonly Cue DoorOpen = new(Shots("grid/door-open", 1), -8, Heard: new(12, 48));
    public static readonly Cue DoorClose = new(Shots("grid/door-close", 1), -8, Heard: new(12, 48));

    /// <summary>
    /// The planet's lava (0087, 0093), on World, a station's reach: a deep rumble, a 4 s loop synthesised to close on itself, at
    /// the open lava nearest the listener, as loud as a station at work; over it its bubbles popping at random moments,
    /// <see cref="LavaBubblesPerSecond"/> a second on average, each at an open cell in reach, its pitch within ±<see cref="LavaBubblePitch"/>
    /// and up to <see cref="LavaBubbleLowDb"/> under its cue's, the loudest 1 dB over the rumble.
    /// </summary>
    public static readonly Cue LavaBed = Looped("grid/lava-bed", -6, StationReach);
    public static readonly Cue LavaBubble = new(Shots("grid/lava-bubble", 6), -8, Pitch: 0, Heard: StationReach);
    public const float LavaBubblesPerSecond = 2, LavaBubblePitch = 0.1f, LavaBubbleLowDb = -8;
}
