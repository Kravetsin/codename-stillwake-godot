namespace Stillwake.Game;

public static partial class Sound
{
    /// <summary>
    /// A drip off a stalactite landing in a cave (0080), where it splashes: a water drop, at most one every
    /// <see cref="CaveDripLeastS"/> to <see cref="CaveDripMostS"/> s however many fall, up to <see cref="CaveDripLowDb"/> under its cue's.
    /// </summary>
    public static readonly Cue CaveDrip = new(Shots("flora/cave-drip", 2), -9, Pitch: 0.08f, Heard: new(8, 32));
    public const double CaveDripLeastS = 0.5, CaveDripMostS = 2.2;
    public const float CaveDripLowDb = -10;
}
