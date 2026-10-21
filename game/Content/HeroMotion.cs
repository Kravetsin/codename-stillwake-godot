namespace Stillwake.Game;

/// <summary>
/// How the hero's figure answers its motion (0090): the lean of its acceleration, the skid of a hard stop, the landing by
/// how fast it fell and the poses through the air, as the research page proposed them. Speeds are the controller's,
/// thousandths of a cell a step; times in milliseconds; angles in radians, forward positive. Drawn only, never read by
/// the sim.
/// </summary>
public static class HeroMotion
{
    /// <summary>
    /// The body leans <see cref="Gain"/> radians per thousandth of a cell a step gained a step, smoothed so a step takes
    /// <see cref="Smooth"/> of the newest change, at most <see cref="Forward"/> into it and <see cref="Back"/> braking; on the
    /// ground only. The body's tilt settles in <see cref="SettleMs"/> off the floor's poses as on them.
    /// </summary>
    public static class Lean
    {
        public const double Gain = 0.006, Smooth = 0.3, Forward = 0.25, Back = 0.22, SettleMs = 60;
    }

    /// <summary>
    /// A skid: on the ground, losing at least <see cref="Brake"/> a step from over <see cref="Speed"/>, the skid pose is
    /// held <see cref="Ms"/> past the last such step (7 steps), blended in and out over <see cref="BlendMs"/>. While it
    /// slides on over <see cref="Speed"/>, the soles throw <see cref="StreaksPerStep"/> streaks of dust a step, every
    /// <see cref="MotesEvery"/>th with a mote.
    /// </summary>
    public static class Skid
    {
        public const int Speed = 50, Brake = 3, MotesEvery = 2;
        public const double Ms = 7 * 1000.0 / 60, BlendMs = 45, StreaksPerStep = 0.7;
    }

    /// <summary>
    /// A landing sinks the knees by the fall's speed over <see cref="PerDepth"/>, from <see cref="Least"/> to
    /// <see cref="Most"/> times the old sink, <see cref="Running"/> of that landing at over <see cref="RunningSpeed"/> across;
    /// it springs back in <see cref="Ms"/> plus up to <see cref="LongerMs"/> more, all of it from a fall of
    /// <see cref="LongFall"/>. From <see cref="Heavy"/> it is heavy: a wider, brighter ring and streaks along the floor.
    /// </summary>
    public static class Landing
    {
        public const int PerDepth = 420, RunningSpeed = 60, LongFall = 600, Heavy = 480;
        public const double Least = 0.35, Most = 1.8, Running = 0.6, Ms = 150, LongerMs = 110;

        /// <summary>
        /// The ring a landing sends along the floor, in pixels: from 3 out to <see cref="Ring"/> times the fall over
        /// <see cref="RingFall"/> (at least <see cref="RingLeast"/> of it), out to <see cref="HeavyRing"/> landing heavy;
        /// it fades in <see cref="RingMs"/>, <see cref="HeavyRingMs"/> heavy, at <see cref="RingAlpha"/> or <see cref="HeavyRingAlpha"/>.
        /// </summary>
        public const double Ring = 26, RingFall = 1200, RingLeast = 0.2, HeavyRing = 34, RingMs = 1000.0 / 6, HeavyRingMs = 260,
            RingAlpha = 0.32, HeavyRingAlpha = 0.45;

        /// <summary>Light streaks a heavy landing sends along the floor to either side.</summary>
        public const int HeavyStreaks = 2;
    }

    /// <summary>
    /// In the air the pose runs from rising to falling as the speed up or down goes from <see cref="RiseBelow"/> to
    /// <see cref="RiseBelow"/> + <see cref="Blend"/>; within <see cref="Apex"/> of still, the knees tuck up to
    /// <see cref="Tuck"/> of the way; for <see cref="PushMs"/> after a jump the legs push straight; falling for over
    /// <see cref="FlailAfterMs"/> faster than <see cref="FlailSpeed"/>, the arms flail, in full <see cref="FlailMs"/> later.
    /// Moving faster across than <see cref="DriftSpeed"/>, the body tips <see cref="Drift"/> into the way it goes.
    /// </summary>
    public static class Air
    {
        public const int RiseBelow = -150, Blend = 450, Apex = 60, FlailSpeed = 300, DriftSpeed = 100;
        public const double Tuck = 0.5, PushMs = 110, FlailAfterMs = 450, FlailMs = 300, Drift = 0.1;
    }
}
