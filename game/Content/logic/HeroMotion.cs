namespace Stillwake.Game;

/// <summary>
/// How the hero's figure answers its motion (0090): the lean of its acceleration, the landing by
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
        public const double Gain = 0.003, Smooth = 0.3, Forward = 0.25, Back = 0.22, SettleMs = 60;
    }

    /// <summary>
    /// The run: the body leans <see cref="Lean"/> radians in and rises <see cref="Bounce"/> pixels in each stride's flight,
    /// both feet off the floor; the free arms pump <see cref="Pump"/> radians either way; the head takes back
    /// <see cref="HeadSteady"/> of the body's lean on the floor, so it looks ahead rather than at the floor. Running backward
    /// it leans back <see cref="BackLean"/>.
    /// </summary>
    public static class Run
    {
        public const double Lean = 0.3, Bounce = 1.5, Pump = 1.0, HeadSteady = 0.5, BackLean = 0.15;
    }

    /// <summary>
    /// Running with an item in hand, it is carried trailing: the arm <see cref="Upper"/> radians back of hanging, the elbow
    /// bent as far as turns the item <see cref="Angle"/> (down behind the hero), within <see cref="LowerLeast"/> and
    /// <see cref="LowerMost"/>; bobbing <see cref="Bob"/> radians each step; the item turned along the fist, the axe too.
    /// It comes and goes over <see cref="BlendMs"/>. Carrying it and for <see cref="SpringAfterMs"/> after, the arm swings on a
    /// spring of <see cref="SpringHz"/> damped <see cref="SpringDamping"/>, so it lags a start and swings on past a stop; a
    /// landing moves it as the pose does.
    /// </summary>
    public static class Carry
    {
        public const double Upper = -0.6, Angle = 2.25, LowerLeast = 0.05, LowerMost = 2.2, Bob = 0.06, BlendMs = 160;
        public const double SpringHz = 2.2, SpringDamping = 0.45, SpringAfterMs = 450;
    }

    /// <summary>How long the figure takes to close a step of a cell, up or down, the controller made whole (<c>StepEase</c>, 0106), in milliseconds.</summary>
    public const double StepMs = 100;

    /// <summary>
    /// A landing sinks the knees by the fall's speed over <see cref="PerDepth"/>, from <see cref="Least"/> to
    /// <see cref="Most"/> times the old sink, <see cref="Running"/> of that landing at over <see cref="RunningSpeed"/> across;
    /// it springs back in <see cref="Ms"/> plus up to <see cref="LongerMs"/> more, all of it from a fall of
    /// <see cref="LongFall"/>. From <see cref="Heavy"/> it is heavy: a wider, brighter ring and streaks along the floor.
    /// </summary>
    public static class Landing
    {
        public const int PerDepth = 840, RunningSpeed = 120, LongFall = 1200, Heavy = 960;
        public const double Least = 0.35, Most = 1.8, Running = 0.6, Ms = 150, LongerMs = 110;

        /// <summary>
        /// The ring a landing sends along the floor, in pixels: from 3 out to <see cref="Ring"/> times the fall over
        /// <see cref="RingFall"/> (at least <see cref="RingLeast"/> of it), out to <see cref="HeavyRing"/> landing heavy;
        /// it fades in <see cref="RingMs"/>, <see cref="HeavyRingMs"/> heavy, at <see cref="RingAlpha"/> or <see cref="HeavyRingAlpha"/>.
        /// </summary>
        public const double Ring = 26, RingFall = 2400, RingLeast = 0.2, HeavyRing = 34, RingMs = 1000.0 / 6, HeavyRingMs = 260,
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
        public const int RiseBelow = -300, Blend = 900, Apex = 120, FlailSpeed = 600, DriftSpeed = 200;
        public const double Tuck = 0.5, PushMs = 110, FlailAfterMs = 450, FlailMs = 300, Drift = 0.1;
    }

    /// <summary>
    /// On a ladder (0092), in pixels of the world. The limbs' cycle is <see cref="Stride"/> cells climbed, read from the hero's
    /// height so every peer draws it alike: a hand reaches over the other every half of it, about 3 times a second climbing up.
    /// A hand holds from <see cref="HandTop"/> over the shoulder down <see cref="HandRange"/> for <see cref="Grip"/> of the
    /// cycle, <see cref="HandOut"/> before the body, then is brought back up <see cref="HandSwing"/> out; a foot the same from
    /// <see cref="FootTop"/> under the hip, with the opposite hand. Not tied to the rungs: the hands slide up the rails.
    /// </summary>
    public static class Ladder
    {
        public const double Stride = 8, Grip = 0.6, HandTop = -8.6, HandRange = 8.8, HandOut = 8.4, HandSwing = 3,
            FootTop = 8.4, FootRange = 8.8, FootOut = 3.8, FootSwing = 3.8;

        /// <summary>
        /// The pull-up at the top: from the ledge at the hands' highest hold to <see cref="PullBlend"/> under it
        /// the hands go onto it and stay there as the body rises past them, pushing down once under the shoulder; within
        /// <see cref="KneeFrom"/> the front foot steps up onto it over <see cref="KneeBlend"/>; within <see cref="PushFrom"/> the
        /// back leg straightens under the body; the body leans <see cref="Lean"/> into the ledge till <see cref="LeanTill"/> of it.
        /// </summary>
        public const double PullBlend = 4, KneeFrom = 18, KneeBlend = 3, PushFrom = 8, Lean = 0.15, LeanTill = 10;

        /// <summary>Sliding (Shift and down, seen by a fall faster than the climb down) the gloves throw this many sparks a step off the rails.</summary>
        public const double SparksPerStep = 0.8;
    }

    /// <summary>
    /// Rammed (0103): the figure flinches over <see cref="Ms"/>, thrown into it in its first <see cref="In"/> share and easing
    /// out after; at its height the body tilts <see cref="Tilt"/> away from the blow and the arms come up (<c>Poses.Flinching</c>).
    /// </summary>
    public static class Flinch
    {
        public const double Ms = 460, In = 0.12, Tilt = 0.42;
    }
}
