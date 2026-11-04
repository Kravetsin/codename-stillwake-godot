using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;
using Spec = Stillwake.Content.Hero;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Tests;

/// <summary>Poses and the head: apps/game/src/features/crew/pose.test.ts, one for one.</summary>
public class PoseTests
{
    private static readonly Hero Standing = new(11000, 20000, 0, 0, false, 1);
    private static readonly Hero Walking = Standing with { Vx = 240 };

    private static Moment At(double travelled, double timeMs = 0) => new(travelled, timeMs);

    /// <summary>Vitest's toBeCloseTo: within half a unit of the last of <paramref name="digits"/> decimals.</summary>
    internal static void Close(double expected, double actual, int digits = 2) =>
        Assert.True(Math.Abs(expected - actual) < Math.Pow(10, -digits) / 2, $"{actual} is not close to {expected}");

    [Fact]
    public void PoseOfReadsWhatTheHeroDoesFromHowItMoves()
    {
        Assert.Equal(Pose.Stand, PoseOf(Standing, false, false));
        Assert.Equal(Pose.Walk, PoseOf(Standing with { Vx = 240 }, false, false));
        Assert.Equal(Pose.Run, PoseOf(Standing with { Vx = -440 }, false, false));
        Assert.Equal(Pose.Air, PoseOf(Standing with { Vx = 480, Vy = -840 }, false, false));
        Assert.Equal(Pose.Air, PoseOf(Standing with { Vy = 42 }, false, false));
        Assert.Equal(Pose.Climb, PoseOf(Standing with { Climbing = true }, false, false));
        Assert.Equal(Pose.Float, PoseOf(Standing with { Vy = 240 }, true, false));
    }

    [Fact]
    public void JumpedOnlyLeavingTheDeckOrALadderRising()
    {
        var ship = new Ship(20, 24, cell => cell / 20 == 20 && cell % 20 < 10 ? Structure.Floor : Structure.Empty, _ => false);
        var standing = new Hero(5000, 20000, 0, 0, false, 1);
        var jumped = standing.Step(new HeroInput(0, 0, true), ship);
        Assert.True(Jumped(Pose.Stand, PoseOf(jumped, false, false), jumped));
        Assert.True(Jumped(Pose.Run, Pose.Air, jumped));
        Assert.True(Jumped(Pose.Climb, Pose.Air, jumped));
        // Seen a send late, a few steps into the rise, it is still a jump: a tap's, Space let go and the rise halved, too (0089).
        var risen = jumped.Step(new HeroInput(0, 0, false), ship).Step(new HeroInput(0, 0, false), ship).Step(new HeroInput(0, 0, false), ship);
        Assert.True(Jumped(Pose.Walk, Pose.Air, risen));

        // Walking off the floor's edge, the hero falls: no jump.
        var atEdge = new Hero(10800, 20000, 0, 0, false, 1);
        var off = atEdge.Step(new HeroInput(1, 0, false), ship);
        for (int i = 0; i < 10 && PoseOf(off, false, false) != Pose.Air; i++) off = off.Step(new HeroInput(1, 0, false), ship);
        Assert.Equal(Pose.Air, PoseOf(off, false, false));
        Assert.False(Jumped(Pose.Walk, Pose.Air, off));

        // Already in the air, or still on the deck, or the first frame drawn: none.
        Assert.False(Jumped(Pose.Air, Pose.Air, jumped));
        Assert.False(Jumped(Pose.Stand, Pose.Stand, standing));
        Assert.False(Jumped(null, Pose.Air, jumped));
        Assert.False(Jumped(Pose.Float, Pose.Air, jumped));
    }

    [Fact]
    public void LandsOnceAJumpComesDownNotAtItsTop()
    {
        var ship = new Ship(20, 24, cell => cell / 20 == 20 ? Structure.Floor : Structure.Empty, _ => false);
        var hero = new Hero(5000, 20000, 0, 0, false, 1);
        Pose? was = PoseOf(hero, false, false);
        int fall = 0;
        var landed = new List<int>();
        for (int step = 0; step < 90; step++)
        {
            hero = hero.Step(new HeroInput(0, 0, step == 0), ship);
            var now = PoseOf(hero, false, false);
            // As the figure reads it: the fall speed kept from the last step in the air.
            if (now == Pose.Air) fall = Math.Max(0, hero.Vy);
            if (Landed(was, now, fall)) landed.Add(hero.Y);
            was = now;
        }
        Assert.Equal([20000], landed);
        Assert.False(Landed(Pose.Air, Pose.Stand, 0));
        Assert.True(Landed(Pose.Air, Pose.Run, 42));
        Assert.False(Landed(Pose.Walk, Pose.Stand, 800));
    }

    [Fact]
    public void PoseOfLiesOnTheFloorPassedOutAndTumblesInSpace()
    {
        Assert.Equal(Pose.Lie, PoseOf(Standing with { Vy = 42 }, false, true));
        Assert.Equal(Pose.Tumble, PoseOf(Standing, true, true));
    }

    [Fact]
    public void SwingsLegsAgainstEachOtherAndArmsAgainstLegsWhileWalking()
    {
        var l = LimbsOf(Pose.Walk, Walking, At(Stride.Walk / 4));
        Assert.True(l.FrontLeg.Upper > 0.3);
        Close(-l.FrontLeg.Upper, l.BackLeg.Upper);
        Assert.Equal(-Math.Sign(l.FrontLeg.Upper), Math.Sign(l.FrontArm.Upper));
    }

    [Fact]
    public void LiftsTheKneeOfTheLegSwingingForwardAndKeepsTheOtherNearStraight()
    {
        // A stride begun, the front leg swings forward past the back one.
        var l = LimbsOf(Pose.Walk, Walking, At(0));
        Assert.True(l.FrontLeg.Lower < -0.8);
        Assert.True(l.BackLeg.Lower > -0.2);
    }

    [Fact]
    public void TakesOneStridePerStrideLengthWalkedWhateverTheTime()
    {
        Bend Leg(double cells, double ms) => LimbsOf(Pose.Walk, Walking, At(cells, ms)).FrontLeg;
        Close(Leg(1.1, 5000).Upper, Leg(1.1 + Stride.Walk, 0).Upper);
        Close(0, Leg(Stride.Walk / 2, 0).Upper);
    }

    /// <summary>The heel's and the toe's corner of each boot's sole, in pixels from the feet as drawn: turned with the body about its middle, raised by its lift; +y down.</summary>
    internal static IEnumerable<Vec> Soles(Limbs l)
    {
        foreach (var (leg, foot, hipX) in new[] { (l.FrontLeg, l.FrontFoot, Rig.HipApart), (l.BackLeg, l.BackFoot, -Rig.HipApart) })
        {
            var ankle = Rig.EndOf(leg, Rig.ToAnkle);
            double turn = leg.Upper + leg.Lower + foot;
            foreach (double along in new[] { -Rig.Heel, Rig.Toe })
            {
                double x = hipX + ankle.X + along * Math.Cos(turn) + Rig.Sole * Math.Sin(turn);
                double y = -Rig.Hip + ankle.Y - along * Math.Sin(turn) + Rig.Sole * Math.Cos(turn) + Rig.H / 2;
                double c = Math.Cos(l.Tilt), s = Math.Sin(l.Tilt);
                yield return new Vec(x * c - y * s, x * s + y * c - Rig.H / 2 - l.Lift);
            }
        }
    }

    [Fact]
    public void StandsOnItsSolesNeverSunkInNorFloatingLeaningOrNot()
    {
        var moments = new List<(Pose Pose, Hero Hero, Moment Moment)>();
        for (int i = 0; i < 40; i++)
        {
            moments.Add((Pose.Stand, Standing, At(0, i * 170)));
            moments.Add((Pose.Walk, Walking, At(i * Stride.Walk / 40)));
            moments.Add((Pose.Walk, Walking, At(i * Stride.Walk / 40) with { Momentum = new Momentum(Lean: HeroMotion.Lean.Forward) }));
            moments.Add((Pose.Stand, Standing, At(0) with { Landing = i / 40.0, Momentum = new Momentum(Depth: 1.8) }));
            moments.Add((Pose.Stand, Standing, At(0) with { Momentum = new Momentum(Lean: -HeroMotion.Lean.Back * i / 40) }));
        }
        foreach (var (pose, hero, moment) in moments)
        {
            double lowest = Soles(LimbsOf(pose, hero, moment)).Max(p => p.Y);
            Assert.True(Math.Abs(lowest) < 0.05, $"{pose} at {moment.Travelled}, {moment.TimeMs} ms, landing {moment.Landing}: lowest sole at {lowest}");
        }
        // A run leaves the floor between steps, but never goes through it, leaning well in.
        for (int i = 0; i < 40; i++)
        {
            var run = LimbsOf(Pose.Run, Walking with { Vx = 440 }, At(i * Stride.Run / 40) with { Momentum = new Momentum(Lean: HeroMotion.Lean.Forward) });
            Assert.True(Soles(run).Max(p => p.Y) < 0.05, $"a run's sole {Soles(run).Max(p => p.Y)} under the floor at {i}");
        }
        // Standing, both boots flat on it.
        Assert.All(Soles(LimbsOf(Pose.Stand, Standing, At(0))), p => Assert.True(Math.Abs(p.Y) < 0.1, $"{p}"));
    }

    [Fact]
    public void AWalkSinksWithBothFeetApartAndRisesPassingOverTheStraightLeg()
    {
        // Lowest with both feet apart, highest passing over the straight leg.
        double apart = LimbsOf(Pose.Walk, Walking, At(Stride.Walk / 4)).Lift;
        double passing = LimbsOf(Pose.Walk, Walking, At(0)).Lift;
        Assert.True(apart < passing - 0.5);
    }

    [Fact]
    public void StandsOnNearStraightLegsOnlyTheArmsBreathing()
    {
        foreach (var ms in new[] { 0, 700, 1300, 2000 })
        {
            var l = LimbsOf(Pose.Stand, Standing, At(3, ms));
            Assert.True(Math.Abs(l.FrontLeg.Lower) < 0.1);
            Close(Rig.Hip, Rig.EndOf(l.FrontLeg, Rig.Leg).Y, 0);
            Assert.True(Math.Abs(l.FrontArm.Upper) < 0.1);
        }
    }

    [Fact]
    public void SinksIntoALandingWithTheFeetKeptOnTheFloorThenSpringsBack()
    {
        var deep = LimbsOf(Pose.Stand, Standing, At(0) with { Landing = 2.0 / 3 });
        Assert.True(deep.Lift < -Land.Sink * 0.9, $"{deep}");
        // The hips come down by the sink, the boots on the floor.
        Close(-Land.Sink, deep.Lift, 2);
        Assert.True(Math.Abs(Soles(deep).Max(p => p.Y)) < 0.05);
        Assert.True(deep.FrontLeg.Lower < -0.3);
        var done = LimbsOf(Pose.Stand, Standing, At(0) with { Landing = 0 });
        Assert.True(done.Lift > -1);
    }

    [Theory]
    [InlineData(HeroMotion.Landing.Least)]
    [InlineData(1)]
    [InlineData(HeroMotion.Landing.Most)]
    public void LandsOnFlatBootsNeverOnItsToes(double depth)
    {
        // Folded deep, the boots rolled onto their toes, and it looked as if the hero landed on tiptoe: the feet go further
        // ahead instead, so the shins stand within how far an ankle turns.
        for (int i = 0; i <= 20; i++)
        {
            var l = LimbsOf(Pose.Stand, Standing, At(0) with { Landing = i / 20.0, Momentum = new Momentum(Depth: depth) });
            var soles = Soles(l).ToList();
            for (int foot = 0; foot < 2; foot++)
                Assert.True(Math.Abs(soles[2 * foot].Y) < 0.3 && Math.Abs(soles[2 * foot + 1].Y) < 0.3,
                    $"depth {depth}, landing {i / 20.0}: the {(foot == 0 ? "front" : "back")} boot's heel at {soles[2 * foot].Y}, toe at {soles[2 * foot + 1].Y}");
        }
    }

    [Theory]
    [InlineData(400)]
    [InlineData(900)]
    public void FallsWithTheBootsTurnedFlatForTheFloor(int vy)
    {
        // Falling, the boots hung along the shins, toes down, and met the floor toe first.
        var l = LimbsOf(Pose.Air, Standing with { Vy = vy }, At(0));
        foreach (var (leg, foot) in new[] { (l.FrontLeg, l.FrontFoot), (l.BackLeg, l.BackFoot) })
            Assert.True(Math.Abs(l.Tilt - leg.Upper - leg.Lower - foot) < 0.2, $"a boot {l.Tilt - leg.Upper - leg.Lower - foot} toe down from flat at {vy}");
    }

    [Fact]
    public void TakesRungsOverItsHeadOnALadderOneHandUpWhileTheOtherHolds()
    {
        var l = LimbsOf(Pose.Climb, Standing with { Climbing = true, Y = 20600 }, At(0));
        var front = Rig.EndOf(l.FrontArm, Rig.Arm);
        var back = Rig.EndOf(l.BackArm, Rig.Arm);
        Assert.True(Math.Min(front.Y, back.Y) < -Rig.Arm.Upper);
        Assert.False(Math.Abs(front.Y - back.Y) < 0.5);
    }

    [Fact]
    public void CollapsesPassingOutKneesFirstThenTopplesFasterAndFasterOntoItsBack()
    {
        double Tilt(double ms) => LimbsOf(Pose.Lie, Standing, At(0) with { OutMs = ms }).Tilt;
        Close(0, Tilt(0));
        Assert.True(LimbsOf(Pose.Lie, Standing, At(0) with { OutMs = Faint.BuckleMs }).Lift < -1);
        double quarter = Tilt(Faint.BuckleMs + Faint.FallMs / 4), half = Tilt(Faint.BuckleMs + Faint.FallMs / 2);
        Assert.True(half < quarter && quarter < 0);
        // Faster and faster: the second quarter of the fall turns it further than the first.
        Assert.True(quarter - half > Tilt(Faint.BuckleMs) - quarter);
        Close(-Math.PI / 2, Tilt(Faint.Ms), 1);
    }

    [Fact]
    public void EndsEachFaintInAHeapOfItsOwn()
    {
        var one = LimbsOf(Pose.Lie, Standing, At(0) with { OutMs = Faint.Ms, Slump = 1 });
        var other = LimbsOf(Pose.Lie, Standing, At(0) with { OutMs = Faint.Ms, Slump = 2 });
        Assert.NotEqual(one.FrontArm, other.FrontArm);
        Assert.Equal(one, LimbsOf(Pose.Lie, Standing, At(0) with { OutMs = Faint.Ms, Slump = 1 }));
    }

    [Fact]
    public void LiesTurnedOntoItsBackAboardAndKeepsItsTurnPassedOutInSpace()
    {
        var lying = LimbsOf(Pose.Lie, Standing, At(0));
        Assert.Equal(-Math.PI / 2, lying.Tilt);
        Assert.True(lying.Lying);
        double Tumble(double ms) => LimbsOf(Pose.Tumble, Standing, At(0, ms) with { OutTilt = 1.2 }).Tilt;
        Assert.Equal(1.2, Tumble(0));
        Assert.Equal(1.2, Tumble(5000));
        Assert.False(LimbsOf(Pose.Tumble, Standing, At(0, 1000)).Lying);
    }

    [Fact]
    public void LeansIntoASwimAlongTheFloorInSpaceUprightRising()
    {
        Close(Adrift.Lean, LimbsOf(Pose.Float, Standing with { Vx = Spec.Swim }, At(1)).Tilt);
        Close(0, LimbsOf(Pose.Float, Standing with { Vy = -Spec.Swim }, At(1)).Tilt);
        Close(Adrift.Lean + Adrift.Tuck, LimbsOf(Pose.Float, Standing with { Vx = Spec.Swim, Vy = Spec.Swim }, At(1)).Tilt);
    }

    [Fact]
    public void LeansFurtherSwimmingFastTowardTheWayItSwimsWhicheverWayItFaces()
    {
        Close(Adrift.LeanFast, LimbsOf(Pose.Float, Standing with { Vx = 2 * Spec.Swim }, At(0)).Tilt);
        Close(Adrift.Lean, LimbsOf(Pose.Float, Standing with { Vx = -Spec.Swim, Facing = -1 }, At(0)).Tilt);
        Close(-Adrift.Lean, LimbsOf(Pose.Float, Standing with { Vx = -Spec.Swim }, At(0)).Tilt);
    }

    [Fact]
    public void StaysUprightAfloatWhicheverWayAndHoweverFastItSwims()
    {
        int[] speeds = [-320, -160, 0, 160, 320];
        foreach (int vx in speeds)
            foreach (int vy in speeds)
                foreach (int facing in new[] { -1, 1 })
                    for (double ms = 0; ms < 6000; ms += 250)
                    {
                        var l = LimbsOf(Pose.Float, Standing with { Vx = vx, Vy = vy, Facing = facing }, At(0, ms) with { Fast = 0.5 });
                        // Never more than about 25 degrees.
                        Assert.True(Math.Abs(l.Tilt) <= 0.45, $"{vx},{vy}: {l.Tilt}");
                    }
    }

    [Fact]
    public void TrailsItsLegsSwimmingAlongDrawsThemUpSinkingAndHangsThemRising()
    {
        var across = LimbsOf(Pose.Float, Standing with { Vx = 160 }, At(0, 300));
        // A foot in the world, the body turned forward by its tilt: behind the middle when negative.
        double Ahead(Limbs l, Bend leg)
        {
            var end = Rig.EndOf(leg, Rig.Leg);
            return end.X * Math.Cos(l.Tilt) - (end.Y + Rig.H / 2 - Rig.Hip) * Math.Sin(l.Tilt);
        }
        Assert.True(Ahead(across, across.FrontLeg) < 0 && Ahead(across, across.BackLeg) < 0);
        var sinking = LimbsOf(Pose.Float, Standing with { Vy = 160 }, At(0, 300));
        Assert.True(sinking.FrontLeg.Upper > 0.6 && sinking.FrontLeg.Lower < -1);
        var rising = LimbsOf(Pose.Float, Standing with { Vy = -160 }, At(0, 300));
        Assert.True(Math.Abs(rising.FrontLeg.Lower) < 0.3 && Math.Abs(rising.BackLeg.Lower) < 0.3);
    }

    [Fact]
    public void SwimsWithTheHandsHeldCalmNeverPaddling()
    {
        // The suit moves the hero (2026-11-04-hero-gait): swimming, fast or not, axe in hand or not, the hands only drift.
        foreach (var (vx, fast) in new[] { (160, 0.0), (600, 1.0) })
            foreach (bool wields in new[] { false, true })
            {
                var seen = Enumerable.Range(0, 24).Select(k => LimbsOf(Pose.Float, Standing with { Vx = vx }, At(0, k * Adrift.PaddleMs / 12) with { Wields = wields, Swim = 1, Fast = fast })).ToList();
                foreach (var (arm, name) in new (Func<Limbs, Bend>, string)[] { (l => l.FrontArm, "near"), (l => l.BackArm, "far") })
                {
                    Assert.True(seen.Max(l => arm(l).Upper) - seen.Min(l => arm(l).Upper) < 0.3, $"the {name} arm swings at {vx}, axe {wields}");
                    Assert.True(seen.Max(l => arm(l).Lower) - seen.Min(l => arm(l).Lower) < 0.3, $"the {name} elbow works at {vx}, axe {wields}");
                }
            }
    }

    [Fact]
    public void BlendsDriftingIntoSwimmingAndSwimmingIntoSwimmingFastWithoutAJump()
    {
        double Leg(double swim, double fast) => LimbsOf(Pose.Float, Standing, At(0, 200) with { Swim = swim, Fast = fast }).FrontLeg.Upper;
        double still = Leg(0, 0), swimming = Leg(1, 0), fast = Leg(1, 1);
        Assert.InRange(Leg(0.5, 0), Math.Min(still, swimming), Math.Max(still, swimming));
        Assert.NotEqual(still, Leg(0.5, 0));
        Assert.InRange(Leg(1, 0.5), Math.Min(swimming, fast), Math.Max(swimming, fast));
    }

    [Fact]
    public void DriftsStillAfloatUprightArmsLowNeverFlailing()
    {
        for (double ms = 0; ms < 20000; ms += 100)
        {
            var l = LimbsOf(Pose.Float, Standing, At(0, ms));
            Assert.True(Math.Abs(l.Tilt) < 0.05);
            Assert.True(l.FrontArm.Upper is > 0 and < 0.6 && l.BackArm.Upper is > 0 and < 0.6);
        }
    }

    [Fact]
    public void KeepsAFootUnderTheBodyRunningSoItNeverSinksFar()
    {
        // Measured on the boots' soles, the body turned as it leans: lowest pushing off, the stance leg far behind.
        for (double at = 0; at < Stride.Run; at += 0.1)
            Assert.True(LimbsOf(Pose.Run, Standing with { Vx = 440 }, At(at)).Lift > -Rig.Hip * 0.2, $"{at}: {LimbsOf(Pose.Run, Standing with { Vx = 440 }, At(at))}");
    }

    [Fact]
    public void RunsLeaningInElbowsBentKneesHigherThanItWalks()
    {
        var run = LimbsOf(Pose.Run, Standing with { Vx = 440 }, At(Stride.Run / 2));
        var walk = LimbsOf(Pose.Walk, Walking, At(Stride.Walk / 2));
        Assert.True(run.Tilt > walk.Tilt);
        Assert.True(run.FrontArm.Lower > 1.2);
        Assert.True(Math.Min(run.FrontLeg.Lower, run.BackLeg.Lower) < Math.Min(walk.FrontLeg.Lower, walk.BackLeg.Lower));
    }

    [Fact]
    public void HoldsThePlanInOneHandAfloatTheOtherSwimmingOn()
    {
        var swimming = Standing with { Vx = 160 };
        var free = LimbsOf(Pose.Float, swimming, At(0, 300));
        var holding = LimbsOf(Pose.Float, swimming, At(0, 300) with { Plan = 0 });
        Assert.Equal(free.BackArm, holding.BackArm);
        Assert.NotEqual(free.FrontArm, holding.FrontArm);
    }

    [Fact]
    public void KeepsItsStrokeOnTheDiagonalTimedNotByDistance()
    {
        var across = LimbsOf(Pose.Float, Standing with { Vx = 160 }, At(0, 500));
        var diagonal = LimbsOf(Pose.Float, Standing with { Vx = 160, Vy = 160 }, At(7, 500));
        Assert.Equal(across.FrontArm, diagonal.FrontArm);
    }

    [Fact]
    public void BendsItsElbowsForwardOnALadderAsArmsBend()
    {
        // Through the climb's cycle, read from where the hero is (0092).
        for (int y = 20000; y < 22000; y += 100)
        {
            var l = LimbsOf(Pose.Climb, Standing with { Climbing = true, Y = y }, At(0));
            Assert.True(l.FrontArm.Lower >= 0 && l.BackArm.Lower >= 0);
        }
    }

    [Fact]
    public void PutsTheFrontHandWhereTheSheetIsHeldTurnedWithTheHeadTheFarHandFurtherOn()
    {
        foreach (var look in new[] { 0, Head.Down, -Head.Up })
        {
            var l = LimbsOf(Pose.Walk, Walking, new Moment(1, 0, Plan: look));
            var hand = Rig.EndOf(l.FrontArm, Rig.Arm);
            Close(PlanHand(look).X, hand.X, 1);
            Close(PlanHand(look).Y, hand.Y, 1);
            Assert.True(Rig.EndOf(l.BackArm, Rig.Arm).X > hand.X);
        }
        Assert.True(PlanHand(-Head.Up).Y < PlanHand(0).Y);
        Assert.True(PlanHand(Head.Down).Y > PlanHand(0).Y);
    }

    [Fact]
    public void HoldsToolsAndThePlanClimbingTooButNothingLyingOrTumbling()
    {
        // On a ladder the hands dig, swing and plan as anywhere (0077).
        Assert.All(new[] { Pose.Stand, Pose.Walk, Pose.Air, Pose.Float, Pose.Climb }, p => Assert.True(HoldsIn(p)));
        Assert.All(new[] { Pose.Lie, Pose.Tumble }, p => Assert.False(HoldsIn(p)));
    }

    [Fact]
    public void HeadTurnLooksStraightAtAPointAheadDownPositive()
    {
        Assert.Equal(0, HeadTurn(5, 0));
        Close(Math.Atan2(1, 4), HeadTurn(4, 1));
        Close(-Math.Atan2(1, 4), HeadTurn(4, -1));
    }

    [Fact]
    public void HeadTurnNeverLooksFurtherUpOrDownThanTheNeckAllows()
    {
        Assert.Equal(-Head.Up, HeadTurn(0.1, -10));
        Assert.Equal(Head.Down, HeadTurn(0.1, 10));
        Assert.Equal(-Head.Up, HeadTurn(0, -3));
    }

    [Fact]
    public void HeadTurnOnlyLooksUpOrDownAtAPointBehindAsAtOneAsFarAhead()
    {
        Close(HeadTurn(4, 1), HeadTurn(-4, 1));
        Assert.Equal(0, HeadTurn(-4, 0));
    }

    [Fact]
    public void SittingRestsTheHipsOnTheLedgeTheThighsLevelAndTheShinsHangingHoldingNothing()
    {
        Assert.False(HoldsIn(Pose.Sit));
        for (double ms = 0; ms < Seated.SwingMs; ms += 500)
        {
            var sat = LimbsOf(Pose.Sit, Standing, At(0, ms));
            Assert.Equal(-Rig.Hip, sat.Lift);
            foreach (var leg in new[] { sat.FrontLeg, sat.BackLeg })
            {
                // The limbs turn with the body (a limb at Upper is turned back by it, the body forward by Tilt): the thigh
                // points level ahead and the shin straight down, give or take the swing.
                Assert.InRange(leg.Upper - sat.Tilt, Math.PI / 2 - 0.05, Math.PI / 2 + 0.05);
                Assert.InRange(leg.Upper + leg.Lower - sat.Tilt, -0.2, 0.2);
            }
        }
    }

    [Fact]
    public void StrappedInSitsUprightTheThighsLevelAndTheShinsDownHoldingNothing()
    {
        Assert.False(HoldsIn(Pose.Strapped));
        for (double ms = 0; ms < Seated.BreathMs; ms += 500)
        {
            var sat = LimbsOf(Pose.Strapped, Standing, At(0, ms));
            Assert.Equal(-Rig.Hip, sat.Lift);
            Assert.InRange(sat.Tilt, -0.05, 0.05);
            foreach (var leg in new[] { sat.FrontLeg, sat.BackLeg })
            {
                Assert.InRange(leg.Upper - sat.Tilt, Math.PI / 2 - 0.05, Math.PI / 2 + 0.05);
                Assert.InRange(leg.Upper + leg.Lower - sat.Tilt, -0.05, 0.05);
            }
        }
    }
}
