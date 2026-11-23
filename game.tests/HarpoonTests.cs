using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content.Harpoon;

namespace Stillwake.Game.Tests;

/// <summary>
/// The hand harpoon on the client (0044, debris-physics): the rod's flight, hook, line and way back, the presses and holds each state
/// takes or ignores, the cooldown that is the hero's and the arm's aim.
/// </summary>
public class HarpoonTests
{
    /// <summary>The rod covers this many thousandths of a cell a ms in flight, and this many on its way back.</summary>
    private const double Flies = Spec.RodSpeed / 50.0, Returns = Spec.ReturnSpeed / 50.0;

    /// <summary>
    /// A world of still pieces, each a box 2000 across round its middle, upright or turned by <see cref="Angle"/> as drawn, that the rod
    /// meets in the order they were put and sticks in where it enters; those the sim would refuse; this hero's lines the sim holds, by
    /// piece, with their hook points, and its hooks in flight; and a wall standing up at <see cref="Wall"/>, met if nearer, which lets
    /// go once it no longer <see cref="Holds"/>.
    /// </summary>
    private sealed class Pieces
    {
        public readonly Dictionary<int, (double X, double Y)> At = new();
        public readonly Dictionary<int, double> Angle = new();
        public readonly Dictionary<int, (int X, int Y)> Lines = new();
        public readonly HashSet<int> Refused = [], Sent = [];
        public double? Wall;
        public bool Holds = true;

        public HarpoonWorld World => new(
            Touches, id => At.TryGetValue(id, out var at) ? new PiecePose(at.X, at.Y, Angle.GetValueOrDefault(id)) : null,
            id => Lines.TryGetValue(id, out var hook) ? hook : null, Sent.Contains, _ => Holds);

        private RodHit? Touches((double X, double Y) from, (double X, double Y) to)
        {
            foreach (var (id, (x, y)) in At)
            {
                if (Refused.Contains(id) || Math.Abs(from.Y - y) > 1000) continue;
                double left = x - 1000;
                if (from.X <= left && to.X >= left && !(Wall < left)) return new RodHit(id, (left, from.Y));
            }
            return Wall is double wall && from.X <= wall && to.X >= wall ? new RodHit(null, (wall, from.Y)) : null;
        }
    }

    private static readonly (double X, double Y) Muzzle = (0, 0);

    /// <summary>Held at <see cref="Muzzle"/>, the barrel pointing right, the hero's middle where it says.</summary>
    private static GunHold Out((double X, double Y)? muzzle = null, (double X, double Y) middle = default, bool letsGo = false, bool drops = false, bool holds = false) =>
        new(true, muzzle ?? Muzzle, (1, 0), middle, letsGo, drops, holds);

    private static readonly GunHold PutAway = new(false, Muzzle, (1, 0));

    /// <summary>Steps <paramref name="shot"/> <paramref name="ms"/> in frames of 10 ms, with no press.</summary>
    private static void Run(HarpoonShot shot, double ms, Pieces world, GunHold? hold = null)
    {
        for (double t = 0; t < ms; t += 10) shot.Step(10, false, hold ?? Out(), world.World);
    }

    /// <summary>A harpoon hooked in piece 1, a box from 15000 to 17000 thousandths along the barrel, its line in the sim, hooked at its left edge.</summary>
    private static (HarpoonShot Shot, Pieces World) Hooked()
    {
        var world = new Pieces();
        world.At[1] = (16_000, 0);
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        world.Lines[1] = (-1_000, 0);
        Run(shot, 250, world);
        Assert.Equal(Rod.Hooked, shot.Rod);
        return (shot, world);
    }

    [Fact]
    public void FiresOnAPressInSpaceFromTheMuzzleAlongTheBarrelAndStartsTheHerosCooldown()
    {
        var shot = new HarpoonShot();
        Assert.True(shot.Ready);
        var step = shot.Step(10, true, new GunHold(true, (600, -400), (0, -2)), new Pieces().World);
        Assert.True(step.Fired);
        Assert.Equal(Rod.Flying, shot.Rod);
        Assert.Equal(((600.0, -400.0), (0.0, -1.0)), (shot.Tip, shot.Heading));
        Assert.Equal(HarpoonShot.CooldownMs, shot.CooldownLeftMs);
        Assert.Equal(5_000, HarpoonShot.CooldownMs);
        Assert.False(shot.Ready);
    }

    [Fact]
    public void FliesAtItsSpeedOutToItsRangeThenSnapsBackFastAndSeats()
    {
        var (shot, world) = (new HarpoonShot(), new Pieces());
        shot.Step(10, true, Out(), world.World);
        shot.Step(100, false, Out(), world.World);
        Assert.Equal(100 * Flies, shot.Tip.X, 6);
        // 40 cells at 80 a second: 500 ms out; then back at 160 a second.
        Run(shot, Spec.Range / Flies - 100, world);
        Assert.Equal((Rod.Returning, (double)Spec.Range), (shot.Rod, shot.Tip.X));
        Run(shot, Spec.Range / Returns - 20, world);
        Assert.Equal(Rod.Returning, shot.Rod);
        Run(shot, 30, world);
        Assert.Equal(Rod.Seated, shot.Rod);
        Assert.True(Spec.Range / Returns < Spec.Range / Flies / 1.5);
    }

    [Fact]
    public void IgnoresPressesInFlightOnTheWayBackAndInTheCooldownAndGetsReadyWhenItEnds()
    {
        var (shot, world) = (new HarpoonShot(), new Pieces());
        shot.Step(10, true, Out(), world.World);
        Assert.False(shot.Step(10, true, Out(), world.World).Fired);
        Assert.Equal(Rod.Flying, shot.Rod);
        Run(shot, Spec.Range / Flies, world);
        Assert.Equal(Rod.Returning, shot.Rod);
        Assert.Equal(default, shot.Step(10, true, Out(), world.World));
        Run(shot, 300, world);
        Assert.Equal(Rod.Seated, shot.Rod);
        Assert.Equal(default, shot.Step(10, true, Out(), world.World));
        Assert.False(shot.Ready);
        // Ready once, as the cooldown counted from the shot ends.
        int readies = 0;
        for (double t = 0; t < HarpoonShot.CooldownMs; t += 10)
            if (shot.Step(10, false, Out(), world.World).Ready) readies++;
        Assert.Equal(1, readies);
        Assert.True(shot.Ready);
        Assert.True(shot.Step(10, true, Out(), world.World).Fired);
    }

    [Fact]
    public void PutAwayOrNotYetDrawnItNeitherFiresNorCostsACooldown()
    {
        var shot = new HarpoonShot();
        Assert.Equal(default, shot.Step(10, true, PutAway, new Pieces().World));
        Assert.Equal(default, shot.Step(10, true, Out() with { Muzzle = null }, new Pieces().World));
        Assert.Equal((Rod.Seated, 0.0, true), (shot.Rod, shot.CooldownLeftMs, shot.Ready));
    }

    [Fact]
    public void HooksThePieceItMeetsSendingWhereInItsOwnFrameAndRidesItTurnedWithIt()
    {
        var world = new Pieces();
        world.At[1] = (16_000, 0);
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        ShotStep hit = default;
        for (int i = 0; i < 25 && hit.Hooked == null; i++) hit = shot.Step(10, false, Out(), world.World);
        Assert.Equal((1, new Hook(1, -1_000, 0)), (hit.Hooked, hit.Send));
        Assert.Equal((Rod.Hooked, 1), (shot.Rod, shot.Piece));
        Assert.Equal((15_000.0, 0.0), shot.Tip);
        // Its hook on its way: the rod rides the piece, and the next frames send nothing.
        world.Sent.Add(1);
        world.At[1] = (14_000, 600);
        Assert.Equal(default, shot.Step(10, false, Out(), world.World));
        Assert.Equal((Rod.Hooked, (13_000.0, 600.0)), (shot.Rod, shot.Tip));
        // Turned a quarter, clockwise as drawn: the rod with it, pointing down as it came in pointing right; the pointer turns nothing.
        world.Angle[1] = 16_384;
        shot.Step(10, false, Out() with { Barrel = (0, -1) }, world.World);
        Assert.Equal(14_000, shot.Tip.X, 6);
        Assert.Equal(-400, shot.Tip.Y, 6);
        Assert.Equal(0, shot.Heading.X, 6);
        Assert.Equal(1, shot.Heading.Y, 6);
    }

    [Fact]
    public void RidesTheHookPointTheSimHoldsOnceItHoldsTheLine()
    {
        var (shot, world) = Hooked();
        // The sim clamped the hook into the piece's box: the rod goes where it says.
        world.Lines[1] = (-500, 300);
        shot.Step(10, false, Out(), world.World);
        Assert.Equal((15_500.0, 300.0), shot.Tip);
    }

    [Fact]
    public void WindsWhileTheButtonIsHeldAfterTheHookTillItIsLetUp()
    {
        var (shot, world) = Hooked();
        // The hold that fired never winds: the button is let up first.
        var hook = new HarpoonShot();
        var w = new Pieces();
        w.At[1] = (16_000, 0);
        hook.Step(10, true, Out(holds: true), w.World);
        for (int i = 0; i < 25 && hook.Rod != Rod.Hooked; i++) hook.Step(10, false, Out(holds: true), w.World);
        w.Sent.Add(1);
        Assert.Equal(default, hook.Step(10, false, Out(holds: true), w.World));
        Assert.Equal(Rod.Hooked, hook.Rod);

        Assert.Equal(new Wind(true), shot.Step(10, false, Out(holds: true), world.World).Send);
        Assert.Equal(Rod.Reeling, shot.Rod);
        Assert.Equal(default, shot.Step(10, false, Out(holds: true), world.World));
        Assert.Equal(new Wind(false), shot.Step(10, false, Out(), world.World).Send);
        Assert.Equal(Rod.Hooked, shot.Rod);
        Assert.Equal(default, shot.Step(10, false, Out(), world.World));
    }

    [Fact]
    public void ARightClickLetsTheLineGoAndTheRodSnapsBack()
    {
        var (shot, world) = Hooked();
        shot.Step(10, false, Out(holds: true), world.World);
        Assert.Equal(new LetGo(), shot.Step(10, false, Out(drops: true), world.World).Send);
        Assert.Equal((Rod.Returning, (int?)null), (shot.Rod, shot.Piece));
        Run(shot, 1_000, world);
        Assert.Equal(Rod.Seated, shot.Rod);
    }

    [Fact]
    public void FreesTheRodWhenThePieceIsGoneOrTheSimHoldsNoLineOnItButNeverForTheWayAlone()
    {
        var (gone, a) = Hooked();
        a.At.Remove(1);
        gone.Step(10, false, Out(), a.World);
        Assert.Equal(Rod.Returning, gone.Rod);

        // The hook refused, or the line dropped by the sim: a sentry's cut, another's hook, the hero gone in.
        var (dropped, b) = Hooked();
        b.Lines.Remove(1);
        dropped.Step(10, false, Out(), b.World);
        Assert.Equal(Rod.Returning, dropped.Rod);

        // Far past the range, the line still holds it: the rope pays out, the sim keeps its length.
        var (far, c) = Hooked();
        c.At[1] = (Spec.Range + Spec.PayOut + 5_000, 0);
        far.Step(10, false, Out(), c.World);
        Assert.Equal(Rod.Hooked, far.Rod);
    }

    [Fact]
    public void PutAwayItKeepsAHeldLineOutUnwoundAndAnythingElseIsBackInTheGunAtOnce()
    {
        var (away, b) = Hooked();
        away.Step(10, false, Out(holds: true), b.World);
        Assert.Equal(new Wind(false), away.Step(10, false, PutAway, b.World).Send);
        Assert.Equal((Rod.Hooked, (int?)1), (away.Rod, away.Piece));
        Assert.Equal(default, away.Step(10, false, PutAway, b.World));
        // Taken out again, it holds on, the button let up first to wind.
        Assert.Equal(default, away.Step(10, false, Out(holds: true), b.World));
        Assert.Equal(default, away.Step(10, false, Out(), b.World));
        Assert.Equal(new Wind(true), away.Step(10, false, Out(holds: true), b.World).Send);
        // The line dropped while away: back in the gun.
        b.Lines.Remove(1);
        away.Step(10, false, PutAway, b.World);
        Assert.Equal((Rod.Seated, (int?)null), (away.Rod, away.Piece));

        var flying = new HarpoonShot();
        flying.Step(10, true, Out(), new Pieces().World);
        flying.Step(10, false, PutAway, new Pieces().World);
        Assert.Equal(Rod.Seated, flying.Rod);
    }

    [Fact]
    public void TheCooldownIsTheHerosSoTakingOutAnotherHarpoonDoesNotDodgeIt()
    {
        var (shot, world) = (new HarpoonShot(), new Pieces());
        shot.Step(10, true, Out(), world.World);
        // Put away, and another harpoon taken out of the next slot: one machine for the hero.
        shot.Step(10, false, PutAway, world.World);
        Assert.Equal(Rod.Seated, shot.Rod);
        Assert.False(shot.Step(10, true, Out(), world.World).Fired);
        // Its end is not heard while no harpoon is in hand, nor after.
        bool heard = false;
        for (double t = 0; t < HarpoonShot.CooldownMs; t += 10) heard |= shot.Step(10, false, PutAway, world.World).Ready;
        heard |= shot.Step(10, false, Out(), world.World).Ready;
        Assert.False(heard);
        Assert.True(shot.Step(10, true, Out(), world.World).Fired);
    }


    [Theory]
    [InlineData(80, 0)]
    [InlineData(60, -45)]
    [InlineData(20, 30)]
    [InlineData(-10, 90)]
    public void TurnsTheStraightArmSoTheBarrelsLineNotTheArmsRunsThroughThePoint(double dx, double dy)
    {
        var look = HeldLook.Harpoon;
        double arm = HarpoonShot.ArmFor(look, dx, dy);
        var (from, to) = AxeSwing.EdgeAt(look, arm);
        double ux = to.X - from.X, uy = to.Y - from.Y;
        // The point lies on the barrel's line, ahead of the muzzle.
        Assert.Equal(0, (ux * (dy - to.Y) - uy * (dx - to.X)) / Math.Sqrt(ux * ux + uy * uy), 6);
        Assert.True(ux * (dx - to.X) + uy * (dy - to.Y) > 0);
        // Held as a pistol: the barrel along the forearm, above the fist.
        Assert.Equal(arm, Math.Atan2(uy, ux), 6);
    }

    [Fact]
    public void TheLineEndsAtTheGunOnlyWhileTheRodsEyeIsStillInIt()
    {
        // A rod stuck pointing up and right, its eye two cells behind its point.
        (double X, double Y) tip = (10_000, 0), pointing = (0.6, -0.8), eye = (8_800, 1_600);
        // Leaving the muzzle, the gun on the rod between its eye and its point: the line runs back through the gun.
        Assert.Equal((9_400.0, 800.0), HarpoonLine.EndOf((9_400, 800), tip, pointing, 2_000));
        // The hero flown up past it, far behind its point as it flew: the line still runs to its eye.
        var end = HarpoonLine.EndOf((0, -20_000), tip, pointing, 2_000);
        Assert.Equal(eye.X, end.X, 6);
        Assert.Equal(eye.Y, end.Y, 6);
    }

    [Fact]
    public void TheLineHangsSlackHookedAndIsWoundInTautToItsSpanReeling()
    {
        var line = new HarpoonLine((0, 0), (20_000, 0));
        for (int i = 0; i < 120; i++) line.Update((0, 0), (20_000, 0), taut: false, 1000.0 / 60);
        Assert.Equal(23_000, line.Length, 0);
        line.Update((0, 0), (20_000, 0), taut: true, 1000.0 / 60);
        Assert.Equal(22_200, line.Length, 0);
        for (int i = 0; i < 10; i++) line.Update((0, 0), (20_000, 0), taut: true, 1000.0 / 60);
        Assert.Equal(20_000, line.Length, 0);
        // Reeled in at ten cells a second, it stays as long as the way it spans.
        for (int i = 1; i <= 60; i++) line.Update((0, 0), (20_000 - i * 10_000 / 60.0, 0), taut: true, 1000.0 / 60);
        Assert.Equal(10_000, line.Length, 0);
    }

    [Fact]
    public void APiecesLineSagsByItsSlackAndIsStraightOnceStretched()
    {
        var line = new HarpoonLine((0, 0), (12_000, 0));
        for (int i = 0; i < 120; i++) line.Update((0, 0), (12_000, 0), taut: false, 1000.0 / 60, slack: 8_000);
        Assert.Equal(20_000, line.Length, 0);
        // Stretched past its length, the way it spans: straight.
        for (int i = 0; i < 5; i++) line.Update((0, 0), (24_000, 0), taut: false, 1000.0 / 60, slack: -4_000);
        Assert.Equal(24_000, line.Length, 0);
    }

    [Fact]
    public void ALinesSlackIsItsLengthLessTheWayFromItsHolderToItsHookTurnedWithItsPiece()
    {
        // Hooked half a cell right of the middle of a piece turned a quarter (down): the hook 500 below it, 6 cells from the holder.
        var piece = new PiecePose(10_000, 4_500, 0x4000);
        var holder = (10_000.0, -1_000.0);
        // As long as the sim holds it, as a guest draws it: it sags.
        Assert.Equal(3_000, HarpoonLine.SlackOf(9_000, piece, (500, 0), holder), 6);
        // Shorter than the way, as the host player's drum may have it wound: stretched, straight.
        Assert.Equal(-500, HarpoonLine.SlackOf(5_500, piece, (500, 0), holder), 6);
    }

    [Fact]
    public void TheStreakHomeRunsOutWithTheRodSeatedAndNeverWaitsForTheNextShot()
    {
        const double Frame = 1000.0 / 60;
        var seated = new RodFlourish();
        seated.Step(Rod.Reeling, true, Frame);
        Assert.True(seated.Step(Rod.Returning, true, Frame).ZipBegan);
        // The rod is home a frame later, well before the streak.
        var shown = new List<RodFlourish.Shown>();
        for (int i = 0; i < 12; i++) shown.Add(seated.Step(Rod.Seated, true, Frame));
        Assert.Contains(shown, s => s.Zip != null);
        Assert.Single(shown, s => s.ZipHome);
        Assert.Equal(default, seated.Step(Rod.Flying, true, Frame));

        // The gun put away mid-streak, or drawn again for the next shot: nothing is left of it.
        var putAway = new RodFlourish();
        putAway.Step(Rod.Reeling, true, Frame);
        putAway.Step(Rod.Returning, true, Frame);
        Assert.Equal(default, putAway.Step(Rod.Seated, false, Frame));
        Assert.Equal(default, putAway.Step(Rod.Flying, true, Frame));
    }

    [Fact]
    public void TheBeadRunsDownTheLineAsItGoesTautAndIsGoneWithTheLine()
    {
        const double Frame = 1000.0 / 60;
        var bead = new RodFlourish();
        bead.Step(Rod.Hooked, true, Frame);
        Assert.Equal(0, bead.Step(Rod.Reeling, true, Frame).Bead);
        Assert.NotNull(bead.Step(Rod.Reeling, true, Frame).Bead);
        Assert.Null(bead.Step(Rod.Seated, true, Frame).Bead);
        Assert.Null(bead.Step(Rod.Flying, true, Frame).Bead);
    }

    [Fact]
    public void TheLineNeverRunsBackThroughTheGunAsTheRodLeavesIt()
    {
        const double RodLength = 2_000, Frame = 1000.0 / 60;
        (double X, double Y) gun = (0, 0), along = (1, 0);
        var line = new HarpoonLine(gun, HarpoonLine.EndOf(gun, gun, along, RodLength));
        var points = new List<(double X, double Y)>();
        for (int i = 0; i < 20; i++)
        {
            (double X, double Y) tip = (i * Spec.RodSpeed * Frame / 50, 0);
            line.Update(gun, HarpoonLine.EndOf(gun, tip, along, RodLength), taut: false, Frame);
            points.Clear();
            line.DrawnInto(points);
            // A hair of drift aside, all of it ahead of the muzzle.
            Assert.All(points, p => Assert.True(p.X > -100, $"frame {i}: a point at {p.X:F0}"));
        }
        Assert.Equal((5_000, 0), HarpoonLine.EndOf(gun, (7_000, 0), along, RodLength));
    }


    // The grapple: the rod stuck in the ship's structure pulls the hero to it.

    /// <summary>A harpoon whose rod is stuck in a wall 12000 along the barrel, the hero's middle a little behind the muzzle.</summary>
    private static (HarpoonShot Shot, Pieces World) Gripping()
    {
        var world = new Pieces { Wall = 12_000 };
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        bool gripped = false;
        for (int i = 0; i < 30 && !gripped; i++) gripped = shot.Step(10, false, Out(), world.World).Gripped;
        Assert.True(gripped);
        Assert.Equal((Rod.Hooked, (int?)null, (12_000.0, 0.0)), (shot.Rod, shot.Piece, shot.Tip));
        return (shot, world);
    }

    private static readonly (double X, double Y) Behind = (-600, 2_000);

    [Fact]
    public void HooksTheNearerOfAPieceAndAWall()
    {
        var world = new Pieces { Wall = 12_000 };
        world.At[1] = (16_000, 0);
        world.Lines[1] = (-1_000, 0);
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        Run(shot, 250, world);
        Assert.Equal((Rod.Hooked, (int?)null), (shot.Rod, shot.Piece));
        world.Wall = 18_000;
        var other = new HarpoonShot();
        other.Step(10, true, Out(), world.World);
        Run(other, 250, world);
        Assert.Equal((Rod.Hooked, (int?)1), (other.Rod, other.Piece));
    }

    [Fact]
    public void PullsTheHeroOnAFreshPressToPullStopShortOfTheHookThenHangsTillAJump()
    {
        var (shot, world) = Gripping();
        // Hooked, nothing pulls till a press; a held button is no press.
        Run(shot, 1_000, world, Out(middle: Behind));
        Assert.Equal((Rod.Hooked, (Point?)null), (shot.Rod, shot.PullTo));
        shot.Step(10, true, Out(middle: Behind), world.World);
        Assert.Equal(Rod.Pulling, shot.Rod);
        var to = HarpoonShot.PullOf(Behind, (12_000, 0));
        Assert.Equal(to, shot.PullTo);
        Assert.Equal(Spec.PullStop, Math.Sqrt(Math.Pow(12_000 - to.X, 2) + Math.Pow(to.Y, 2)), 0);
        // On its way, presses and keys do nothing; there, it hangs.
        shot.Step(10, true, Out(middle: (4_000, 1_200), letsGo: true), world.World);
        Assert.Equal(Rod.Pulling, shot.Rod);
        shot.Step(10, false, Out(middle: (to.X, to.Y)), world.World);
        Assert.Equal(Rod.Hanging, shot.Rod);
        Run(shot, 3_000, world, Out(middle: (to.X, to.Y)));
        Assert.Equal((Rod.Hanging, (Point?)to), (shot.Rod, shot.PullTo));
        // A jump lets go, the line snapping back and the hero its own again.
        shot.Step(10, false, Out(middle: (to.X, to.Y), letsGo: true), world.World);
        Assert.Equal((Rod.Returning, (Point?)null), (shot.Rod, shot.PullTo));
    }

    [Fact]
    public void HangsWhereAWallOrTheTetherStopsThePullShort()
    {
        var (shot, world) = Gripping();
        shot.Step(10, true, Out(middle: Behind), world.World);
        shot.Step(10, false, Out(middle: (2_000, 1_600)), world.World);
        Assert.Equal(Rod.Pulling, shot.Rod);
        shot.Step(10, false, Out(middle: (2_000, 1_600)), world.World);
        Assert.Equal(Rod.Hanging, shot.Rod);
    }

    /// <summary>
    /// A harpoon hanging from the wall at 12000, the hero's middle where the pull left it; reloaded unless
    /// <paramref name="ready"/> is false, when a little of the cooldown has run.
    /// </summary>
    private static (HarpoonShot Shot, Pieces World, (double X, double Y) Middle) Hung(bool ready = true)
    {
        var (shot, world) = Gripping();
        shot.Step(10, true, Out(middle: Behind), world.World);
        var to = shot.PullTo!.Value;
        (double X, double Y) middle = (to.X, to.Y);
        shot.Step(10, false, Out(middle: middle), world.World);
        if (ready) Run(shot, HarpoonShot.CooldownMs, world, Out(middle: middle));
        Assert.Equal(Rod.Hanging, shot.Rod);
        return (shot, world, middle);
    }

    /// <summary>Steps <paramref name="shot"/> held at <paramref name="middle"/> till <paramref name="done"/>, at most a second.</summary>
    private static ShotStep Until(HarpoonShot shot, Pieces world, (double X, double Y) middle, Func<HarpoonShot, bool> done)
    {
        var step = new ShotStep();
        for (int i = 0; i < 100 && !done(shot); i++) step = shot.Step(10, false, Out(middle: middle), world.World);
        Assert.True(done(shot));
        return step;
    }

    [Fact]
    public void AClickWhileHangingWaitsForTheReloadThenFiresAgainTheHeroHeldWhileItFlies()
    {
        var (shot, world, middle) = Hung(ready: false);
        var held = shot.PullTo;
        Assert.True(shot.CooldownLeftMs > 0);
        Assert.False(shot.Step(10, true, Out(middle: middle), world.World).Fired);
        Assert.Equal((Rod.Hanging, held), (shot.Rod, shot.PullTo));
        Run(shot, HarpoonShot.CooldownMs, world, Out(middle: middle));
        Assert.Equal((Rod.Hanging, held), (shot.Rod, shot.PullTo));
        Assert.True(shot.Step(10, true, Out(middle: middle), world.World).Fired);
        Assert.Equal((Rod.Flying, held, HarpoonShot.CooldownMs), (shot.Rod, shot.PullTo, shot.CooldownLeftMs));
        shot.Step(10, false, Out(middle: middle), world.World);
        Assert.Equal((Rod.Flying, held), (shot.Rod, shot.PullTo));
    }

    [Fact]
    public void AGripFromAHangPullsOnWithNoClickTowardTheNewHook()
    {
        var (shot, world, middle) = Hung();
        world.Wall = 30_000;
        shot.Step(10, true, Out(middle: middle), world.World);
        var step = Until(shot, world, middle, s => s.Rod != Rod.Flying);
        Assert.True(step.Gripped);
        Assert.Equal((Rod.Pulling, (Point?)HarpoonShot.PullOf(middle, (30_000, 0))), (shot.Rod, shot.PullTo));
    }

    [Fact]
    public void AMissFromAHangLetsTheHeroFallAndAPieceHookedLetsItFallTheRodLeftInThePiece()
    {
        var (missed, a, middle) = Hung();
        a.Wall = null;
        missed.Step(10, true, Out(middle: middle), a.World);
        Until(missed, a, middle, s => s.Rod != Rod.Flying);
        Assert.Equal((Rod.Returning, (Point?)null), (missed.Rod, missed.PullTo));

        var (hooked, b, at) = Hung();
        b.At[1] = (8_000, 0);
        hooked.Step(10, true, Out(middle: at), b.World);
        Assert.Equal((int?)1, Until(hooked, b, at, s => s.Rod != Rod.Flying).Hooked);
        Assert.Equal((Rod.Hooked, (int?)1, (Point?)null), (hooked.Rod, hooked.Piece, hooked.PullTo));
    }

    [Fact]
    public void AJumpWhileTheRodFliesFromAHangLetsGoAndItHooksAsAShotFromTheGround()
    {
        var (shot, world, middle) = Hung();
        shot.Step(10, true, Out(middle: middle), world.World);
        shot.Step(10, false, Out(middle: middle, letsGo: true), world.World);
        Assert.Equal((Rod.Flying, (Point?)null), (shot.Rod, shot.PullTo));
        Until(shot, world, middle, s => s.Rod != Rod.Flying);
        Assert.Equal((Rod.Hooked, (Point?)null), (shot.Rod, shot.PullTo));
    }

    [Fact]
    public void PutAwayWhileTheRodFliesFromAHangSeatsItAndLetsGo()
    {
        var (shot, world, middle) = Hung();
        shot.Step(10, true, Out(middle: middle), world.World);
        shot.Step(10, false, PutAway, world.World);
        Assert.Equal((Rod.Seated, (Point?)null), (shot.Rod, shot.PullTo));
    }

    [Fact]
    public void ARightClickLetsGoOfTheLineMidPullHangingOrWhileTheNextRodFlies()
    {
        // Mid-pull, where a jump does nothing, the line lets go and snaps back.
        var (pulled, a) = Gripping();
        pulled.Step(10, true, Out(middle: Behind), a.World);
        Assert.Equal(Rod.Pulling, pulled.Rod);
        pulled.Step(10, false, Out(middle: Behind, drops: true), a.World);
        Assert.Equal((Rod.Returning, (Point?)null), (pulled.Rod, pulled.PullTo));

        var (hung, b, middle) = Hung();
        hung.Step(10, false, Out(middle: middle, drops: true), b.World);
        Assert.Equal((Rod.Returning, (Point?)null), (hung.Rod, hung.PullTo));

        // While the next rod flies from a hang, the hero drops and the rod flies on as a shot from the ground.
        var (flying, c, at) = Hung();
        flying.Step(10, true, Out(middle: at), c.World);
        flying.Step(10, false, Out(middle: at, drops: true), c.World);
        Assert.Equal((Rod.Flying, (Point?)null), (flying.Rod, flying.PullTo));
    }

    [Fact]
    public void OnlyAJumpLetsGoOfTheLineNeverAMovementKey()
    {
        Assert.False(HarpoonShot.LetsGoBy(new HeroInput(1, 0, false)));
        Assert.False(HarpoonShot.LetsGoBy(new HeroInput(-1, 1, false)));
        Assert.True(HarpoonShot.LetsGoBy(new HeroInput(0, 0, true)));
    }

    // The grappling gun: the same machine, pulling at once on every grip, faster, reloaded in a second.

    [Fact]
    public void TheGrapplingGunPullsTheHeroOnTheGripWithNoClickAtItsOwnSpeed()
    {
        var world = new Pieces { Wall = 12_000 };
        var shot = HarpoonShot.Grapple();
        Assert.Equal((true, Hero.GrapplePullStep), (shot.PullsAtOnce, shot.PullBy));
        Assert.Equal((false, Hero.PullStep), (HarpoonShot.Harpoon().PullsAtOnce, HarpoonShot.Harpoon().PullBy));
        Assert.Equal(30, Math.Round(Hero.GrapplePullStep * 60.0 / Hero.Unit));
        shot.Step(10, true, Out(middle: Behind), world.World);
        var step = Until(shot, world, Behind, s => s.Rod != Rod.Flying);
        Assert.True(step.Gripped);
        Assert.Equal((Rod.Pulling, (Point?)HarpoonShot.PullOf(Behind, (12_000, 0))), (shot.Rod, shot.PullTo));
    }

    [Fact]
    public void TheGrapplingGunReloadsInASecondAMissCostingOnlyThat()
    {
        var world = new Pieces();
        var shot = HarpoonShot.Grapple();
        Assert.Equal(1_000, shot.FullCooldownMs);
        Assert.True(HarpoonShot.CooldownMs > shot.FullCooldownMs);
        Assert.True(shot.Step(10, true, Out(), world.World).Fired);
        Run(shot, 980, world);
        Assert.Equal(Rod.Seated, shot.Rod);
        Assert.False(shot.Step(10, true, Out(), world.World).Fired);
        Run(shot, 10, world);
        Assert.True(shot.Step(10, true, Out(), world.World).Fired);
    }

    [Fact]
    public void FreesTheGrappleWhenTheStructureLetsGoOrIsPastTheRangeUnpulled()
    {
        var (pulled, a) = Gripping();
        pulled.Step(10, true, Out(middle: Behind), a.World);
        a.Holds = false;
        pulled.Step(10, false, Out(middle: Behind), a.World);
        Assert.Equal((Rod.Returning, (Point?)null), (pulled.Rod, pulled.PullTo));

        var (far, b) = Gripping();
        far.Step(10, false, Out(muzzle: (12_000 - Spec.Range - 20, 0)), b.World);
        Assert.Equal(Rod.Returning, far.Rod);
    }

    [Fact]
    public void TheGrappleWaitsOnTheSameCooldownAsTheReel()
    {
        var (shot, world) = Gripping();
        shot.Step(10, true, Out(middle: Behind), world.World);
        shot.Step(10, false, Out(middle: Behind, letsGo: true), world.World);
        shot.Step(10, false, Out(middle: Behind), world.World);
        shot.Step(10, false, Out(middle: Behind, letsGo: true), world.World);
        Run(shot, 500, world);
        Assert.Equal(Rod.Seated, shot.Rod);
        Assert.False(shot.Step(10, true, Out(), world.World).Fired);
        Run(shot, HarpoonShot.CooldownMs, world);
        Assert.True(shot.Step(10, true, Out(), world.World).Fired);
    }

    [Fact]
    public void PullsTheHerosMiddleStraightToThePointAtItsSpeedAndHoldsItThereWithoutGravity()
    {
        var ship = HeroTests.ShipFrom(["############", .. Enumerable.Repeat("#..........#", 8), "############"]);
        var hero = Hero.At(ship, HeroTests.BlockCell(ship, 2, 8));
        var to = new Point(17_000, 6_400);
        var pull = HeroTests.Still with { Pull = to };
        var first = hero.Step(pull, ship);
        double moved = Math.Sqrt(Math.Pow(first.Middle.X - hero.Middle.X, 2) + Math.Pow(first.Middle.Y - hero.Middle.Y, 2));
        Assert.InRange(moved, Hero.PullStep - 1, Hero.PullStep + 1);
        var there = HeroTests.Until(ship, hero, pull, h => h.Middle == to);
        // Held there, keys and gravity aside, till the pull ends; then it falls, or jumps off the line on a Space pressed on it.
        var held = there.Step(pull with { X = 1 }, ship);
        Assert.Equal((to, 0, 0), (held.Middle, held.Vx, held.Vy));
        Assert.True(held.Step(HeroTests.Still, ship).Y > held.Y);
        var sprung = there.Step(pull with { Jump = true }, ship);
        Assert.Equal((to, 0), (sprung.Middle, sprung.Vy));
        Assert.Equal(-Stillwake.Content.Hero.Jump + ship.Gravity, sprung.Step(HeroTests.Still with { JumpHeld = true }, ship).Vy);
        // Pulled at a wall, it stops flush against it.
        var wall = HeroTests.Settle(ship, hero, HeroTests.Still with { Pull = new Point(23_000, 10_000) });
        Assert.Equal(11 * HeroTests.Block - Stillwake.Content.Hero.Width / 2, wall.X);
        Assert.Equal(20, Math.Round(Hero.PullStep * 60.0 / Hero.Unit));
    }

    [Fact]
    public void ThePullKeepsTheHeroOnItsTether()
    {
        var opened = HeroTests.Opened();
        var outside = HeroTests.Until(opened, Hero.At(opened, HeroTests.Spawn), HeroTests.Right, h => h.Anchor != null);
        var far = new Point(outside.Middle.X + 3 * Stillwake.Content.Hero.Tether, outside.Middle.Y);
        var pulled = HeroTests.Settle(opened, outside, HeroTests.Still with { Pull = far });
        var anchor = pulled.Anchor!.Value;
        double reach = Math.Sqrt(Math.Pow(pulled.Middle.X - anchor.X, 2) + Math.Pow(pulled.Middle.Y - anchor.Y, 2));
        Assert.InRange(reach, Stillwake.Content.Hero.Tether - Hero.PullStep, Stillwake.Content.Hero.Tether);
    }

    [Fact]
    public void PullsToACellShortOfTheHookOrNotAtAllFromNearer()
    {
        Assert.Equal(new Point(6_000 - Spec.PullStop, 0), HarpoonShot.PullOf((0, 0), (6_000, 0)));
        Assert.Equal(new Point(600, -800 + Spec.PullStop), HarpoonShot.PullOf((600, 4_000), (600, -800)));
        Assert.Equal(new Point(200, 200), HarpoonShot.PullOf((200, 200), (600, 200)));
    }

    [Fact]
    public void PuffsAJetOfColdGasAlongTheBarrelAndALittleVapourWiderSlowerAndLit()
    {
        var puff = HarpoonShot.Puff((0, -2));
        var (gas, vapour) = (puff[0], puff[1]);
        Assert.All(puff, b => Assert.Equal(-Math.PI / 2, b.Heading, 6));
        Assert.True(gas.Count > vapour.Count);
        Assert.True(vapour.Spread > gas.Spread && vapour.Speed.To < gas.Speed.From);
        // Gas glows faintly as light; the vapour is matter, lit by what is round it.
        Assert.Equal((Glimmer.Streak, false), (gas.Look.Kind, gas.Look.Matter));
        Assert.Equal((Glimmer.Wisp, true), (vapour.Look.Kind, vapour.Look.Matter));
    }

    // As others see it (0065): one point a state, so the rod adds no sends of its own.

    [Fact]
    public void ShowsOthersNothingInTheGunAndInFlightTheEndOfTheRangeItFliesToward()
    {
        var (shot, world) = (new HarpoonShot(), new Pieces());
        Assert.Null(shot.Seen);
        shot.Step(10, true, new GunHold(true, (600, -400), (0, -2)), world.World);
        var flying = shot.Seen;
        Assert.Equal(new RodSeen(Rod.Flying, new Point(600, -400 - Spec.Range)), flying);
        Run(shot, 100, world, new GunHold(true, (600, -400), (0, -2)));
        Assert.Equal(flying, shot.Seen);
    }

    [Fact]
    public void ShowsOthersARodInAPieceWhereItSticksFromItsMiddleInItsOwnFrameHookedOrWinding()
    {
        var (shot, world) = Hooked();
        var hooked = new RodSeen(Rod.Hooked, new Point(-1_000, 0), 1);
        Assert.Equal(hooked, shot.Seen);
        // The piece drifts and turns: the rod rides it, and what others are told holds.
        world.At[1] = (14_000, 600);
        world.Angle[1] = 9_000;
        shot.Step(10, false, Out(), world.World);
        Assert.Equal(hooked, shot.Seen);
        shot.Step(10, false, Out(holds: true), world.World);
        Assert.Equal(hooked with { Rod = Rod.Reeling }, shot.Seen);
    }

    [Fact]
    public void ShowsOthersARodInTheStructureWhereItSticksAndNoPointOnTheWayBack()
    {
        var world = new Pieces { Wall = 12_000 };
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        Run(shot, 200, world);
        Assert.Equal(new RodSeen(Rod.Hooked, new Point(12_000, 0)), shot.Seen);
        shot.Step(10, true, Out(middle: (0, 0)), world.World);
        Assert.Equal(new RodSeen(Rod.Pulling, new Point(12_000, 0)), shot.Seen);
        world.Holds = false;
        shot.Step(10, false, Out(), world.World);
        Assert.Equal(new RodSeen(Rod.Returning), shot.Seen);
    }
}
