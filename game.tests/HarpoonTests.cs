using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content.Harpoon;

namespace Stillwake.Game.Tests;

/// <summary>
/// The hand harpoon on the client (0044): the rod's flight, hook, reel and way back, the presses each state
/// takes or ignores, the cooldown that is the hero's, the point a piece is reeled to and the arm's aim.
/// </summary>
public class HarpoonTests
{
    /// <summary>The rod covers this many thousandths of a cell a ms in flight, and this many on its way back.</summary>
    private const double Flies = Spec.RodSpeed / 50.0, Returns = Spec.ReturnSpeed / 50.0;

    /// <summary>
    /// A world of still pieces, each a box 1000 across round its middle, that the rod meets in the order they
    /// were put and sticks in where it enters; those the sim would refuse and those it reels, by id; and a wall
    /// standing up at <see cref="Wall"/>, met if nearer, which lets go once it no longer <see cref="Holds"/>.
    /// </summary>
    private sealed class Pieces
    {
        public readonly Dictionary<int, (double X, double Y)> At = new();
        public readonly HashSet<int> Refused = [], Reeled = [];
        public double? Wall;
        public bool Holds = true;

        public HarpoonWorld World => new(
            Touches, id => At.TryGetValue(id, out var at) ? at : null, id => !Refused.Contains(id), Reeled.Contains, _ => Holds);

        private RodHit? Touches((double X, double Y) from, (double X, double Y) to)
        {
            foreach (var (id, (x, y)) in At)
            {
                if (Refused.Contains(id) || Math.Abs(from.Y - y) > 500) continue;
                double left = x - 500;
                if (from.X <= left && to.X >= left && !(Wall < left)) return new RodHit(id, (left, from.Y));
            }
            return Wall is double wall && from.X <= wall && to.X >= wall ? new RodHit(null, (wall, from.Y)) : null;
        }
    }

    private static readonly (double X, double Y) Muzzle = (0, 0);

    /// <summary>Held at <see cref="Muzzle"/>, the barrel pointing right, the hero's middle where it says.</summary>
    private static GunHold Out((double X, double Y)? muzzle = null, (double X, double Y) middle = default, bool letsGo = false) =>
        new(true, muzzle ?? Muzzle, (1, 0), middle, letsGo);

    private static readonly GunHold PutAway = new(false, Muzzle, (1, 0));

    /// <summary>Steps <paramref name="shot"/> <paramref name="ms"/> in frames of 10 ms, with no press.</summary>
    private static void Run(HarpoonShot shot, double ms, Pieces world, GunHold? hold = null)
    {
        for (double t = 0; t < ms; t += 10) shot.Step(10, false, hold ?? Out(), world.World);
    }

    /// <summary>A harpoon hooked in piece 1, a box from 7500 to 8500 cells along the barrel.</summary>
    private static (HarpoonShot Shot, Pieces World) Hooked()
    {
        var world = new Pieces();
        world.At[1] = (8_000, 0);
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        Run(shot, 250, world);
        Assert.Equal(Rod.Hooked, shot.Rod);
        return (shot, world);
    }

    [Fact]
    public void FiresOnAPressInSpaceFromTheMuzzleAlongTheBarrelAndStartsTheHerosCooldown()
    {
        var shot = new HarpoonShot();
        Assert.True(shot.Ready);
        var step = shot.Step(10, true, new GunHold(true, (300, -200), (0, -2)), new Pieces().World);
        Assert.True(step.Fired);
        Assert.Equal(Rod.Flying, shot.Rod);
        Assert.Equal(((300.0, -200.0), (0.0, -1.0)), (shot.Tip, shot.Heading));
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
        // 20 cells at 40 a second: 500 ms out; then back at 80 a second.
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
    public void HooksThePieceItMeetsAndRidesItWithoutReelingTillAFreshPress()
    {
        var world = new Pieces();
        world.At[1] = (8_000, 0);
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        int? hooked = null;
        for (int i = 0; i < 25 && hooked == null; i++) hooked = shot.Step(10, false, Out(), world.World).Hooked;
        Assert.Equal(1, hooked);
        Assert.Equal((Rod.Hooked, 1), (shot.Rod, shot.Piece));
        Assert.Equal((7_500.0, 0.0), shot.Tip);
        // The piece drifts: the rod rides it, slack; frames with no fresh press never reel, held or not.
        world.At[1] = (7_000, 300);
        Run(shot, 2_000, world);
        Assert.Equal((Rod.Hooked, (6_500.0, 300.0)), (shot.Rod, shot.Tip));
        var reel = shot.Step(10, true, Out(), world.World).Reel;
        Assert.Equal(HarpoonShot.ReelOf(1, Muzzle, (7_000, 300)), reel);
        Assert.Equal(Rod.Reeling, shot.Rod);
    }

    [Fact]
    public void ReelsTillTheSimSaysItIsOverThenSnapsBackAndAPressMeanwhileReelsAgainFromWhereTheHeroIs()
    {
        var (shot, world) = Hooked();
        shot.Step(10, true, Out(), world.World);
        world.Reeled.Add(1);
        Run(shot, 1_000, world);
        Assert.Equal(Rod.Reeling, shot.Rod);
        var moved = (2_000.0, 1_000.0);
        Assert.Equal(HarpoonShot.ReelOf(1, moved, world.At[1]), shot.Step(10, true, Out(moved), world.World).Reel);
        world.Reeled.Remove(1);
        shot.Step(10, false, Out(), world.World);
        Assert.Equal((Rod.Returning, (int?)null), (shot.Rod, shot.Piece));
    }

    [Fact]
    public void FreesTheLineWhenThePieceIsGoneRefusedOrDriftsPastTheRangeUnreeled()
    {
        var (gone, a) = Hooked();
        a.At.Remove(1);
        gone.Step(10, false, Out(), a.World);
        Assert.Equal(Rod.Returning, gone.Rod);

        var (refused, b) = Hooked();
        b.Refused.Add(1);
        refused.Step(10, false, Out(), b.World);
        Assert.Equal(Rod.Returning, refused.Rod);

        var (drifted, c) = Hooked();
        c.At[1] = (Spec.Range + 400, 0);
        drifted.Step(10, false, Out(), c.World);
        Assert.Equal(Rod.Hooked, drifted.Rod);
        c.At[1] = (Spec.Range + 600, 0);
        drifted.Step(10, false, Out(), c.World);
        Assert.Equal(Rod.Returning, drifted.Rod);

        // Reeling, it goes on past the range: the piece is coming in.
        var (reeling, d) = Hooked();
        reeling.Step(10, true, Out(), d.World);
        d.Reeled.Add(1);
        d.At[1] = (Spec.Range + 2_000, 0);
        reeling.Step(10, false, Out(), d.World);
        Assert.Equal(Rod.Reeling, reeling.Rod);
        d.At.Remove(1);
        reeling.Step(10, false, Out(), d.World);
        Assert.Equal(Rod.Returning, reeling.Rod);
    }

    [Fact]
    public void PutAwayOrPassedOutHasTheRodBackInTheGunAtOnce()
    {
        var (away, b) = Hooked();
        away.Step(10, true, Out(), b.World);
        b.Reeled.Add(1);
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

    [Fact]
    public void ReelsAPieceToThePointItsStopBeforeTheMuzzleOnTheWayToIt()
    {
        Assert.Equal(new Reel(4, 900, 1_200), HarpoonShot.ReelOf(4, (0, 0), (3_000, 4_000)));
        Assert.Equal(new Reel(4, 10_000 - Spec.Stop, 2_000), HarpoonShot.ReelOf(4, (10_000, 2_000), (1_000, 2_000)));
        // Nearer than the stop, it is pushed out to it; on the muzzle, it stays.
        Assert.Equal(new Reel(4, Spec.Stop, 0), HarpoonShot.ReelOf(4, (0, 0), (200, 0)));
        Assert.Equal(new Reel(4, 500, 500), HarpoonShot.ReelOf(4, (500, 500), (500, 500)));
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
    public void TheLineHangsSlackHookedAndIsWoundInTautToItsSpanReeling()
    {
        var line = new HarpoonLine((0, 0), (10_000, 0));
        for (int i = 0; i < 120; i++) line.Update((0, 0), (10_000, 0), taut: false, 1000.0 / 60);
        Assert.Equal(11_500, line.Length, 0);
        line.Update((0, 0), (10_000, 0), taut: true, 1000.0 / 60);
        Assert.Equal(11_100, line.Length, 0);
        for (int i = 0; i < 10; i++) line.Update((0, 0), (10_000, 0), taut: true, 1000.0 / 60);
        Assert.Equal(10_000, line.Length, 0);
        // Reeled in at five cells a second, it stays as long as the way it spans.
        for (int i = 1; i <= 60; i++) line.Update((0, 0), (10_000 - i * 5_000 / 60.0, 0), taut: true, 1000.0 / 60);
        Assert.Equal(5_000, line.Length, 0);
    }

    /// <summary>A harpoon reeling piece 1 in, the sim showing the reel, sent from <see cref="Muzzle"/>.</summary>
    private static (HarpoonShot Shot, Pieces World) Reeling()
    {
        var (shot, world) = Hooked();
        Assert.NotNull(shot.Step(10, true, Out(), world.World).Reel);
        world.Reeled.Add(1);
        return (shot, world);
    }

    /// <summary>The reels <paramref name="shot"/> sends over <paramref name="ms"/> in frames of 10 ms, the muzzle at <paramref name="muzzle"/>.</summary>
    private static List<Reel> Sent(HarpoonShot shot, Pieces world, double ms, (double X, double Y) muzzle)
    {
        var sent = new List<Reel>();
        for (double t = 0; t < ms; t += 10)
            if (shot.Step(10, false, Out(muzzle), world.World).Reel is { } reel) sent.Add(reel);
        return sent;
    }

    [Fact]
    public void ReelsAgainToFollowAHeroThatSwimsMoreThanACellOnceTheWaitIsOver()
    {
        var (shot, world) = Reeling();
        Assert.Equal(500, HarpoonShot.FollowMs);
        var swum = (0.0, Spec.FollowBeyond + 500.0);
        // Swum off at once: the first goes once the wait since the press is over, then none while it stays.
        var sent = Sent(shot, world, 1_000, swum);
        Assert.Equal([HarpoonShot.ReelOf(1, swum, world.At[1])], sent);
        // On a cell and more again: once more, the wait since the last long over.
        var farther = (0.0, 3 * Spec.FollowBeyond + 500.0);
        Assert.Equal([HarpoonShot.ReelOf(1, farther, world.At[1])], Sent(shot, world, 20, farther));
    }

    [Fact]
    public void NeverReelsAgainForLessThanACellNorSoonerThanTheWait()
    {
        var (shot, world) = Reeling();
        Assert.Empty(Sent(shot, world, 2_000, (0, Spec.FollowBeyond - 200)));
        // Far enough, but within the wait after a press: held back till it is over.
        Assert.NotNull(shot.Step(10, true, Out(), world.World).Reel);
        var swum = (0.0, Spec.FollowBeyond + 500.0);
        Assert.Empty(Sent(shot, world, HarpoonShot.FollowMs - 20, swum));
        Assert.Single(Sent(shot, world, 20, swum));
    }

    [Fact]
    public void NeverReelsAgainOnceTheReelIsOverOrTheLineFree()
    {
        var (shot, world) = Reeling();
        world.Reeled.Remove(1);
        Assert.Empty(Sent(shot, world, 2_000, (0, 3 * Spec.FollowBeyond)));
        Assert.NotEqual(Rod.Reeling, shot.Rod);
        var (freed, other) = Reeling();
        other.At.Remove(1);
        Assert.Empty(Sent(freed, other, 2_000, (0, 3 * Spec.FollowBeyond)));
    }

    // The grapple: the rod stuck in the ship's structure pulls the hero to it.

    /// <summary>A harpoon whose rod is stuck in a wall 6000 along the barrel, the hero's middle a little behind the muzzle.</summary>
    private static (HarpoonShot Shot, Pieces World) Gripping()
    {
        var world = new Pieces { Wall = 6_000 };
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        bool gripped = false;
        for (int i = 0; i < 30 && !gripped; i++) gripped = shot.Step(10, false, Out(), world.World).Gripped;
        Assert.True(gripped);
        Assert.Equal((Rod.Hooked, (int?)null, (6_000.0, 0.0)), (shot.Rod, shot.Piece, shot.Tip));
        return (shot, world);
    }

    private static readonly (double X, double Y) Behind = (-300, 1_000);

    [Fact]
    public void HooksTheNearerOfAPieceAndAWall()
    {
        var world = new Pieces { Wall = 6_000 };
        world.At[1] = (8_000, 0);
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        Run(shot, 250, world);
        Assert.Equal((Rod.Hooked, (int?)null), (shot.Rod, shot.Piece));
        world.Wall = 9_000;
        var other = new HarpoonShot();
        other.Step(10, true, Out(), world.World);
        Run(other, 250, world);
        Assert.Equal((Rod.Hooked, (int?)1), (other.Rod, other.Piece));
    }

    [Fact]
    public void PullsTheHeroOnAFreshPressToPullStopShortOfTheHookThenHangsTillAJumpOrAMove()
    {
        var (shot, world) = Gripping();
        // Hooked, nothing pulls till a press; a held button is no press.
        Run(shot, 1_000, world, Out(middle: Behind));
        Assert.Equal((Rod.Hooked, (Point?)null), (shot.Rod, shot.PullTo));
        shot.Step(10, true, Out(middle: Behind), world.World);
        Assert.Equal(Rod.Pulling, shot.Rod);
        var to = HarpoonShot.PullOf(Behind, (6_000, 0));
        Assert.Equal(to, shot.PullTo);
        Assert.Equal(Spec.PullStop, Math.Sqrt(Math.Pow(6_000 - to.X, 2) + Math.Pow(to.Y, 2)), 0);
        // On its way, presses and keys do nothing; there, it hangs.
        shot.Step(10, true, Out(middle: (2_000, 600), letsGo: true), world.World);
        Assert.Equal(Rod.Pulling, shot.Rod);
        shot.Step(10, false, Out(middle: (to.X, to.Y)), world.World);
        Assert.Equal(Rod.Hanging, shot.Rod);
        Run(shot, 3_000, world, Out(middle: (to.X, to.Y)));
        Assert.Equal((Rod.Hanging, (Point?)to), (shot.Rod, shot.PullTo));
        // A jump or a movement key lets go, the line snapping back and the hero its own again.
        shot.Step(10, false, Out(middle: (to.X, to.Y), letsGo: true), world.World);
        Assert.Equal((Rod.Returning, (Point?)null), (shot.Rod, shot.PullTo));
    }

    [Fact]
    public void HangsWhereAWallOrTheTetherStopsThePullShortAndLetsGoOnAClick()
    {
        var (shot, world) = Gripping();
        shot.Step(10, true, Out(middle: Behind), world.World);
        shot.Step(10, false, Out(middle: (1_000, 800)), world.World);
        Assert.Equal(Rod.Pulling, shot.Rod);
        shot.Step(10, false, Out(middle: (1_000, 800)), world.World);
        Assert.Equal(Rod.Hanging, shot.Rod);
        shot.Step(10, true, Out(middle: (1_000, 800)), world.World);
        Assert.Equal(Rod.Returning, shot.Rod);
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
        far.Step(10, false, Out(muzzle: (6_000 - Spec.Range - 10, 0)), b.World);
        Assert.Equal(Rod.Returning, far.Rod);
    }

    [Fact]
    public void TheGrappleWaitsOnTheSameCooldownAsTheReel()
    {
        var (shot, world) = Gripping();
        shot.Step(10, true, Out(middle: Behind), world.World);
        shot.Step(10, false, Out(middle: Behind, letsGo: true), world.World);
        shot.Step(10, false, Out(middle: Behind), world.World);
        shot.Step(10, true, Out(middle: Behind), world.World);
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
        var hero = Hero.At(ship, 8 * 12 + 2);
        var to = new Point(8_500, 3_200);
        var pull = HeroTests.Still with { Pull = to };
        var first = hero.Step(pull, ship);
        double moved = Math.Sqrt(Math.Pow(first.Middle.X - hero.Middle.X, 2) + Math.Pow(first.Middle.Y - hero.Middle.Y, 2));
        Assert.InRange(moved, Hero.PullStep - 1, Hero.PullStep + 1);
        var there = HeroTests.Until(ship, hero, pull, h => h.Middle == to);
        // Held there, keys and gravity aside, till the pull ends; then it falls.
        var held = there.Step(pull with { X = 1, Jump = true }, ship);
        Assert.Equal((to, 0, 0), (held.Middle, held.Vx, held.Vy));
        Assert.True(held.Step(HeroTests.Still, ship).Y > held.Y);
        // Pulled at a wall, it stops flush against it.
        var wall = HeroTests.Settle(ship, hero, HeroTests.Still with { Pull = new Point(11_500, 5_000) });
        Assert.Equal(11 * Hero.Unit - Stillwake.Content.Hero.Width / 2, wall.X);
        Assert.Equal(10, Hero.PullStep * 60 / Hero.Unit);
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
    public void PullsToHalfACellShortOfTheHookOrNotAtAllFromNearer()
    {
        Assert.Equal(new Point(3_000 - Spec.PullStop, 0), HarpoonShot.PullOf((0, 0), (3_000, 0)));
        Assert.Equal(new Point(300, -400 + Spec.PullStop), HarpoonShot.PullOf((300, 2_000), (300, -400)));
        Assert.Equal(new Point(100, 100), HarpoonShot.PullOf((100, 100), (300, 100)));
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
        shot.Step(10, true, new GunHold(true, (300, -200), (0, -2)), world.World);
        var flying = shot.Seen;
        Assert.Equal(new RodSeen(Rod.Flying, new Point(300, -200 - Spec.Range)), flying);
        Run(shot, 100, world, new GunHold(true, (300, -200), (0, -2)));
        Assert.Equal(flying, shot.Seen);
    }

    [Fact]
    public void ShowsOthersARodInAPieceWhereItSticksFromItsMiddleHookedOrReeling()
    {
        var (shot, world) = Hooked();
        var hooked = new RodSeen(Rod.Hooked, new Point(-500, 0), 1);
        Assert.Equal(hooked, shot.Seen);
        // The piece drifts: the rod rides it, and what others are told holds.
        world.At[1] = (7_000, 300);
        shot.Step(10, false, Out(), world.World);
        Assert.Equal(hooked, shot.Seen);
        shot.Step(10, true, Out(), world.World);
        Assert.Equal(hooked with { Rod = Rod.Reeling }, shot.Seen);
    }

    [Fact]
    public void ShowsOthersARodInTheStructureWhereItSticksAndNoPointOnTheWayBack()
    {
        var world = new Pieces { Wall = 6_000 };
        var shot = new HarpoonShot();
        shot.Step(10, true, Out(), world.World);
        Run(shot, 200, world);
        Assert.Equal(new RodSeen(Rod.Hooked, new Point(6_000, 0)), shot.Seen);
        shot.Step(10, true, Out(middle: (0, 0)), world.World);
        Assert.Equal(new RodSeen(Rod.Pulling, new Point(6_000, 0)), shot.Seen);
        world.Holds = false;
        shot.Step(10, false, Out(), world.World);
        Assert.Equal(new RodSeen(Rod.Returning), shot.Seen);
    }
}
