using System;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The catcher's motion as the user picked it (catcher-motion), its numbers as its page drew them: its flight «Как герой», the hero's
/// float drawn along a curve through the ticks' places, its plume longer as it speeds up or brakes; its throw «С пояса, толчком», the
/// charge hung at its belt, unhooked in the tell, its fuse flaring, drawn to its chest and pushed off straight ahead; its gun as the sim
/// turns it, at a limited rate, drawn between ticks, its thread running on along it.
/// </summary>
public static class CatcherMotion
{
    private static double Ease(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    // The flight.

    /// <summary>A move between ticks longer than this, thousandths of a cell, is a jump: drawn straight, never curved.</summary>
    public const int JumpMost = 2000;

    /// <summary>
    /// Where a flyer is drawn <paramref name="t"/> of the way through its tick, from <paramref name="from"/> to <paramref name="to"/>,
    /// having come from <paramref name="before"/>: on a curve whose way at each place is the mean of the moves either side, the move
    /// after guessed from the last two; straight where either move is a jump.
    /// </summary>
    public static double Along(int before, int from, int to, double t)
    {
        double came = from - before, goes = to - from;
        if (Math.Abs(came) > JumpMost || Math.Abs(goes) > JumpMost) return from + goes * t;
        double m0 = (came + goes) / 2, m1 = goes + (goes - came) / 2, t2 = t * t, t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * from + (t3 - 2 * t2 + t) * m0 + (3 * t2 - 2 * t3) * to + (t3 - t2) * m1;
    }

    /// <summary>
    /// How hard its jetpack thrusts, 0 to 1, by how much its speed changed this tick against <paramref name="accel"/>, the most it may
    /// change on an axis: the plume short coasting, long speeding up or braking.
    /// </summary>
    public static double Thrust(int dvx, int dvy, int accel) =>
        accel <= 0 ? 1 : Math.Clamp(Math.Sqrt((double)dvx * dvx + (double)dvy * dvy) / accel, 0, 1);

    /// <summary>
    /// How far it leans against how its speed along <paramref name="facing"/> changed this tick, <paramref name="dvx"/>, radians, forward
    /// positive: back speeding up, forward braking, <see cref="BrakeLean"/> at its whole <paramref name="accel"/>.
    /// </summary>
    public static double Leaning(int dvx, int facing, int accel) => accel <= 0 ? 0 : -BrakeLean * Math.Clamp((double)dvx / accel, -1, 1) * facing;

    /// <summary>How far it leans against a whole tick of thrust, radians.</summary>
    public const double BrakeLean = 0.25;

    /// <summary>How fast it must go across, thousandths of a cell a tick, to turn to face its way afloat: drifting it keeps its facing.</summary>
    public const int FaceBand = 440;

    // The throw.

    /// <summary>When, in ms into the tell, its hand unhooks the charge (its fuse flaring), lets it go, and is back as it floats.</summary>
    public const double GrabMs = 260, ReleaseMs = 600, DoneMs = 1200;

    /// <summary>How long the fuse's flare at the unhooking lasts, ms.</summary>
    public const double FlareMs = 160;

    /// <summary>The flare of the fuse lighting at its belt: its warm glow and white core.</summary>
    public static readonly (int R, int G, int B) Flare = (255, 200, 120), FlareCore = (255, 250, 230);

    /// <summary>
    /// Where the charge hangs at its belt, in the figure's frame (feet at 0, y down, facing right): its grip, its turn, and the strap
    /// over it (its corner and size in the charge's own frame).
    /// </summary>
    public static readonly (double X, double Y) BeltAt = (2.4, -Rig.Hip + 2.2);

    public const double BeltTurn = Math.PI * 0.93;
    public static readonly (double X, double Y, double W, double H) Strap = (0.6, -2.4, 1.1, 4.8);

    /// <summary>The throw's key poses of the back arm, ms into the tell: its bend, the charge's turn back from the forearm, the body's lean; null bend as it floats.</summary>
    private static readonly (double Ms, Bend? Arm, double Hold, double Lean)[] Keys =
    [
        (0, null, Math.PI / 2, 0),
        (240, new(0.25, 0.2), Math.PI / 2, 0.02),
        (420, new(0.05, 1.55), 0, -0.08),
        (570, new(-0.05, 1.65), 0, -0.1),
        (650, new(1.45, 0.05), 0, 0.12),
        (800, new(1.35, 0.15), 0, 0.1),
        (DoneMs, null, Math.PI / 2, 0),
    ];

    /// <summary>
    /// The back arm in its throw: its bend, how much of it over the arm's own (0 as it floats), the charge's turn back from the forearm,
    /// the body's lean more; whether the charge is in its hand (lit) or on its belt, and how bright the flare of its fuse lighting.
    /// </summary>
    public readonly record struct Throw(Bend Arm, double Share, double Hold, double Lean, bool InHand, bool OnBelt, double Flare);

    /// <summary>
    /// Its throw <paramref name="ms"/> into the tell (null throwing none): before the tell and once done the charge on its belt and
    /// the arm its own; let go, the hand empty till it is back.
    /// </summary>
    public static Throw ThrowOf(double? ms)
    {
        if (ms is not double u || u >= DoneMs || u < 0) return new(default, 0, Math.PI / 2, 0, false, true, 0);
        int j = 0;
        while (Keys[j + 1].Ms <= u) j++;
        var (a, b) = (Keys[j], Keys[j + 1]);
        double k = Ease((u - a.Ms) / (b.Ms - a.Ms));
        // From or to its own arm, the bend is the other key's, its share eased in or out.
        Bend from = a.Arm ?? b.Arm!.Value, to = b.Arm ?? a.Arm!.Value;
        double share = (a.Arm == null ? 0 : 1) + ((b.Arm == null ? 0 : 1) - (a.Arm == null ? 0 : 1)) * k;
        var arm = new Bend(from.Upper + (to.Upper - from.Upper) * k, from.Lower + (to.Lower - from.Lower) * k);
        double lean = a.Lean + (b.Lean - a.Lean) * k;
        // Let go, its body gives a little the other way.
        if (u > ReleaseMs) lean -= 0.06 * Math.Sin(Math.PI * Math.Clamp((u - ReleaseMs) / 500, 0, 1));
        double flare = u >= GrabMs && u < GrabMs + FlareMs ? 1 - (u - GrabMs) / FlareMs : 0;
        return new(arm, share, a.Hold + (b.Hold - a.Hold) * k, lean, u >= GrabMs && u < ReleaseMs, u < GrabMs, flare);
    }

    // The aim.

    /// <summary>
    /// Its gun's angle, radians, <paramref name="t"/> of the way from fine heading <paramref name="from"/> to <paramref name="to"/>
    /// (<see cref="Rays.FineHeadingTo"/>), the short way round: the sim turns its gun (catcher-motion), the client draws it between ticks.
    /// From none (-1), at <paramref name="to"/>.
    /// </summary>
    public static double GunAngle(int from, int to, double t)
    {
        int f = from < 0 ? to : from;
        return (f + Rays.FineApart(f, to) * Math.Clamp(t, 0, 1)) * 2 * Math.PI / Rays.FineTurn;
    }

    /// <summary>How fast its drawn gun may turn, rad/s, catching up where it changes what it aims at: well past the sim's turn, which it follows.</summary>
    public const double ChaseRate = 8;

    /// <summary>Its drawn gun <paramref name="elapsedMs"/> on from <paramref name="drawn"/> toward <paramref name="want"/> at <see cref="ChaseRate"/>, never past it.</summary>
    public static double Chase(double drawn, double want, double elapsedMs)
    {
        double apart = Poses.Apart(drawn, want), most = ChaseRate * elapsedMs / 1000;
        return Math.Abs(apart) <= most ? drawn + apart : drawn + Math.Sign(apart) * most;
    }

    /// <summary>How far past upright its gun must point, its cosine, before it turns round to face it: aiming straight up or down it does not flip back and forth.</summary>
    public const double FaceSlack = 0.2;

    /// <summary>Which way it faces aiming at <paramref name="angle"/>, having faced <paramref name="facing"/>: turned round only past <see cref="FaceSlack"/>.</summary>
    public static int FacingOf(double angle, int facing) => Math.Cos(angle) * facing < -FaceSlack ? -facing : facing;

    /// <summary>How far past its quarry the thread fades out, pixels; how near the line must pass its quarry's chest to mark it.</summary>
    public const double FadeOver = 360, Marks = 20;
}
