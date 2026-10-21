namespace Stillwake.Content;

/// <summary>
/// How the hero moves. Lengths are in thousandths of a cell, speeds in thousandths of a cell per
/// controller step, and the controller steps 60 times a second. Terraria's proportions (ADR 0005): the
/// hero is narrower than a cell and under three tall, so a doorway three cells tall and a shaft one cell
/// wide let them through, and a jump clears three cells but not four. <see cref="Reach"/> is how far from
/// the middle of the body the middle of a cell may be for the hero to act on it. <see cref="Swim"/> is the
/// speed along each axis with the body in space, where there is no gravity and no inertia: slow, about 1.8
/// cells a second, so the jetpack (<see cref="Jetpack"/>) is what crosses space. Held aboard, Shift runs at
/// <see cref="Run"/> rather than <see cref="Walk"/>; in space it only thrusts, with a jetpack worn.
/// <see cref="Tether"/> is how far the middle of the body may get from where the tether is fixed, under
/// the wreck's 16 cells of space round the hull, so a hero never reaches the grid's edge (0007).
/// The jump's grace, height and fall and the walk's start and stop are tuned for feel (0089).
/// </summary>
public static class Hero
{
    public const int Width = 950;
    public const int Height = 2625;
    public const int Walk = 120;
    public const int Run = 220;
    public const int Climb = 180;

    /// <summary>The push up a jump starts with: held to its top it clears three cells but not four (3.95), six on a planet (6.01).</summary>
    public const int Jump = 415;

    public const int Gravity = 21;
    public const int MaxFall = 1200;
    public const int Reach = 6000;
    public const int Tether = 12000;
    public const int Swim = 30;

    /// <summary>Steps after the feet leave the ground in which a jump still pushes off it (coyote time): 100 ms.</summary>
    public const int Coyote = 6;

    /// <summary>Steps after Space is pressed in the air in which landing still jumps (the jump buffer): 100 ms.</summary>
    public const int JumpBuffer = 6;

    /// <summary>Thousandths of a jump's rising speed kept when Space is let go while it rises, rounded toward zero: a tap jumps low.</summary>
    public const int JumpCut = 500;

    /// <summary>Below this speed up or down, Space held, a jump's top is eased by <see cref="ApexGravity"/>.</summary>
    public const int Apex = 60;

    /// <summary>Thousandths of gravity at a held jump's top (<see cref="Apex"/>), rounded down: it hangs there a moment.</summary>
    public const int ApexGravity = 600;

    /// <summary>Thousandths of gravity a falling hero gains aboard, rounded down: down faster than up.</summary>
    public const int FallGravity = 1150;

    /// <summary>
    /// Thousandths of a planet's gravity a falling hero gains, rounded down: its lower gravity lets a jump go six cells
    /// high in about a second, and this keeps the way down from floating.
    /// </summary>
    public const int PlanetFallGravity = 1500;

    /// <summary>Speed a walk gains a step on the ground: full <see cref="Walk"/> in 4 steps (67 ms).</summary>
    public const int WalkAccel = 30;

    /// <summary>Speed a run gains a step on the ground: full <see cref="Run"/> in 7 steps (117 ms).</summary>
    public const int RunAccel = 32;

    /// <summary>Speed lost a step on the ground with no key held, or turning: a walk stops in 4 steps (67 ms), a run in 8 (133 ms).</summary>
    public const int Stop = 30;

    /// <summary>Thousandths of <see cref="WalkAccel"/>, <see cref="RunAccel"/> and <see cref="Stop"/> the keys have in the air, rounded down.</summary>
    public const int AirControl = 650;
}
