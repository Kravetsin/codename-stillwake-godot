namespace Stillwake.Game;

/// <summary>
/// How drones look and move (0062), the user's picks: drone A, the steel quad (<c>art/drone_*.png</c>), and the motion
/// "lively"; the bay's points are <c>BayInk</c>'s (salvage-drones). Points are in pixels of their pictures, from the
/// top-left; a cosmetic of the client, never read by the sim.
/// </summary>
public static class DroneLook
{
    /// <summary>
    /// How a flying drone follows the sim (its "rig"): <paramref name="Lift"/> pixels over its cell's centre, lower under a
    /// ceiling so its body keeps <paramref name="Clear"/> pixels from it (0104: the body is taller than a cell); bobbing <paramref name="BobAmp"/> pixels every
    /// <paramref name="BobPeriod"/> seconds; leaning into its speed <paramref name="TiltCruise"/> degrees and into its
    /// acceleration up to <paramref name="TiltMax"/>; a spring of <paramref name="SpringHz"/> and
    /// <paramref name="Damping"/> on the sim's place and speed; its cargo swinging up to about <paramref name="Swing"/> degrees.
    /// </summary>
    public sealed record Motion(
        double Lift, double Clear, double BobAmp, double BobPeriod, double TiltMax, double TiltCruise, double SpringHz, double Damping, double Swing);

    /// <summary>The user's pick, "lively": it springs and leans more than the mockup's and swings its cargo wide.</summary>
    public static readonly Motion Lively = new(Lift: 4, Clear: 2, BobAmp: 1.5, BobPeriod: 1.1, TiltMax: 20, TiltCruise: 9, SpringHz: 3.2, Damping: 0.5, Swing: 28);

    /// <summary>Over how many ticks a flying drone's drawn path is averaged (<c>Trail</c>): how wide it rounds a corner, and how far it runs behind the sim.</summary>
    public const int TrailTicks = 3;

    /// <summary>A flying drone's pictures (<c>drone_fly_0..3</c>, its rotors' frames), its size and the body's middle the client moves.</summary>
    public const int FlyWidth = 18, FlyHeight = 12, AnchorX = 9, AnchorY = 6, RotorFrames = 4;

    /// <summary>The cargo's clamp, pixels under the anchor, turning with the body; the stack's middle hangs this far from it.</summary>
    public const double Clamp = 5, Tether = 6;

    /// <summary>Its status lamp's middle, in the flying picture (<c>drone_lamp_work</c>, <c>drone_lamp_low</c>).</summary>
    public const double LampX = 8, LampY = 3.5;

    /// <summary>A docked drone's picture (<c>drone_dock</c>), set by its top-left on a pad (<c>BayInk.Pads</c>), and its body's middle in it.</summary>
    public const int DockWidth = 14, DockHeight = 12, DockCentreX = 7, DockCentreY = 6;

    /// <summary>
    /// A launch: the drone holds over its pad, a pixel up, for <see cref="LaunchHoldS"/> as its rotors spin up. A landing:
    /// over its pad, <see cref="LandAbove"/> pixels up, for <see cref="LandOverS"/>, then down onto it over
    /// <see cref="LandDownS"/> as its rotors slow; it touches down at the end.
    /// </summary>
    public const double LaunchHoldS = 0.22, LandAbove = 4, LandOverS = 0.55, LandDownS = 0.45;

    // Lights, in the colours of what gives them: a working drone's sky lamp, a low one's amber blinking, a pad's green
    // pulsing while it charges (as a charging station's), the beacon's sky.
    public const string Sky = "7dd3fc";
    public const float WorkLight = 0.22f, LowLight = 0.4f, PadLight = 0.22f, BeaconLight = 0.12f;
    public const double LowBlinkHz = 1.5, PadPulseHz = 0.8;
}
