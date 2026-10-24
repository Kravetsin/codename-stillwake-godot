namespace Stillwake.Content;

/// <summary>
/// How the hero moves. Lengths are in thousandths of a cell (8 px, 0104), speeds in thousandths of a cell per
/// controller step, and the controller steps 60 times a second. Terraria's proportions (ADR 0005): the
/// hero is narrower than two cells and under six tall, so a doorway six cells tall and a shaft two cells
/// wide let them through, and a jump clears seven cells but not eight. <see cref="Reach"/> is how far from
/// the middle of the body the middle of a cell may be for the hero to act on it. <see cref="Swim"/> is the
/// speed along each axis with the body in space, where there is no gravity and no inertia: slow, about 3.6
/// cells a second, so the jetpack (<see cref="Jetpack"/>) is what crosses space. Held aboard, Shift runs at
/// <see cref="Run"/> rather than <see cref="Walk"/>; in space it only thrusts, with a jetpack worn.
/// <see cref="Tether"/> is how far the middle of the body may get from where the tether is fixed, under
/// the wreck's 32 cells of space round the hull, so a hero never reaches the grid's edge (0007).
/// The jump's grace, height and fall and the walk's start and stop are tuned for feel (0089).
/// </summary>
public static class Hero
{
    public const int Width = 1900;
    public const int Height = 5250;

    /// <summary>How far over its head a hero takes hold of a ladder or an open hatch, in thousandths of a cell: an arm's reach (0112).</summary>
    public const int HoldReach = 1000;
    public const int Walk = 240;
    public const int Run = 440;

    /// <summary>Up a ladder: 13.2 cells a second, slow enough to read the hands taking rungs (0092).</summary>
    public const int ClimbUp = 220;

    /// <summary>Down a ladder: 18 cells a second.</summary>
    public const int ClimbDown = 300;

    /// <summary>Down a ladder with Shift held, the hands on the rails: 36 cells a second.</summary>
    public const int Slide = 600;

    /// <summary>Thousandths of the way to a ladder's middle the hero moves a step on it, rounded toward the hero: about 5 steps from its edge.</summary>
    public const int LadderCentring = 450;

    /// <summary>Within this of a ladder's middle the hero is on it: the easing's last thousandths are not drawn out.</summary>
    public const int LadderSnap = 50;

    /// <summary>Steps after a jump or a walk off a ladder before up grabs one again (200 ms): a jump off it with up held goes on.</summary>
    public const int Regrab = 12;

    /// <summary>Thousandths of <see cref="Run"/> a jump off a ladder to one side starts across with, rounded down.</summary>
    public const int LadderJumpAcross = 900;

    /// <summary>
    /// The ledge assist (0092): a jump whose feet fall short of a ledge it is pressed against by no more than its drawn foot is
    /// raised over it, at most this much a step, as a climb is, then put onto it <see cref="MantleAcross"/> across.
    /// </summary>
    public const int MantleRise = 220;

    /// <summary>How far onto a ledge the assist puts the hero, across, the step its feet reach the top.</summary>
    public const int MantleAcross = 120;

    /// <summary>
    /// How high a walk steps up onto ground in its way, the head permitting, and how far down it follows ground without a fall:
    /// a cell (0106), so a stair of cells is walked and a rise of two stops the hero, who jumps or mantles.
    /// </summary>
    public const int StepUp = 1000;

    /// <summary>Thousandths of <see cref="Jump"/> a jump off a ladder to one side rises with, rounded toward zero: a leap off, not up.</summary>
    public const int LadderJumpRise = 850;

    /// <summary>The push up a jump starts with: held to its top it clears seven cells but not eight (7.9), eleven on a planet (11.6: its gravity rounds to 29, not the 28 of twice the 16 px cell's 14, 0104).</summary>
    public const int Jump = 830;

    public const int Gravity = 42;
    public const int MaxFall = 2400;
    public const int Reach = 12000;
    public const int Tether = 24000;
    public const int Swim = 60;

    /// <summary>Steps after the feet leave the ground in which a jump still pushes off it (coyote time): 100 ms.</summary>
    public const int Coyote = 6;

    /// <summary>Steps after Space is pressed in the air in which landing still jumps (the jump buffer): 100 ms.</summary>
    public const int JumpBuffer = 6;

    /// <summary>Thousandths of a jump's rising speed kept when Space is let go while it rises, rounded toward zero: a tap jumps low.</summary>
    public const int JumpCut = 500;

    /// <summary>Below this speed up or down, Space held, a jump's top is eased by <see cref="ApexGravity"/>.</summary>
    public const int Apex = 120;

    /// <summary>Thousandths of gravity at a held jump's top (<see cref="Apex"/>), rounded down: it hangs there a moment.</summary>
    public const int ApexGravity = 600;

    /// <summary>Thousandths of gravity a falling hero gains aboard, rounded down: down faster than up.</summary>
    public const int FallGravity = 1150;

    /// <summary>
    /// Thousandths of a planet's gravity a falling hero gains, rounded down: its lower gravity lets a jump go eleven and a half cells
    /// high in about a second, and this keeps the way down from floating.
    /// </summary>
    public const int PlanetFallGravity = 1500;

    /// <summary>Speed a walk gains a step on the ground: full <see cref="Walk"/> in 4 steps (67 ms).</summary>
    public const int WalkAccel = 60;

    /// <summary>Speed a run gains a step on the ground: full <see cref="Run"/> in 7 steps (117 ms).</summary>
    public const int RunAccel = 64;

    /// <summary>Speed lost a step on the ground with no key held, or turning: a walk stops in 4 steps (67 ms), a run in 8 (133 ms).</summary>
    public const int Stop = 60;

    /// <summary>Thousandths of <see cref="WalkAccel"/>, <see cref="RunAccel"/> and <see cref="Stop"/> the keys have in the air, rounded down.</summary>
    public const int AirControl = 650;

    /// <summary>
    /// A ram's knock (0103), in the client: thrown away from the creature at <see cref="KnockAcross"/> and up at
    /// <see cref="KnockUp"/> a step, each more by its <c>Per</c> for every unit the ram takes — a tapir's five some ten cells on
    /// the first planet, a loper's three some six — with no control for <see cref="Reel"/> steps (0.4 s). Its client sends
    /// no other hit for <see cref="RamGuard"/> ticks after its own.
    /// </summary>
    public const int KnockAcross = 200, KnockAcrossPer = 40, KnockUp = 200, KnockUpPer = 32, Reel = 24, RamGuard = 20;
}
