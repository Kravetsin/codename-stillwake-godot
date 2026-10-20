using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;

namespace Stillwake.Game.Tests;

/// <summary>
/// The legs on uneven ground (<see cref="Footing"/>): the pose untouched on the flat; on a slope the page's walk, each boot on
/// the ground and along it, never in it nor at its knee; a foot out of reach hanging.
/// </summary>
public class FootingTests
{
    private const int Unit = Hero.Unit;
    private const double Frame = 1000.0 / 60;


    // Flat floor from the left, a 45° ramp up three cells (/ is ◢), a plateau, and a wall.
    private static readonly Ship Ramp = HeroTests.ShipFrom("""
        ##############
        #............#
        #............#
        #............#
        #............#
        #............#
        #....../######
        #...../#######
        #S.../########
        ##############
        """);

    /// <summary>A hero on the ramp's middle cell, its feet on the diagonal under its middle.</summary>
    private static Hero OnRamp(int facing) => new(6 * Unit + 500, 7 * Unit + 499, 0, 0, false, facing);

    private static readonly Hero OnFlat = new(2 * Unit + 500, 9 * Unit, 0, 0, false, 1);

    private static Limbs Posed(Pose pose, Hero hero, double travelled) => LimbsOf(pose, hero, new Moment(travelled, 0));

    /// <summary>How far below the hero's feet the ramp's ground is, <paramref name="along"/> pixels ahead of them, from its picture.</summary>
    private static double RampBelow(Hero hero, double along)
    {
        double x = hero.X + hero.Facing * along / Rig.Cell * Unit;
        int column = (int)(x / Unit);
        double ground = column switch
        {
            5 or 6 or 7 => 14 * Unit - 1 - x,
            >= 8 => 6 * Unit,
            _ => 9 * Unit,
        };
        return (ground - hero.Y) * Rig.Cell / Unit;
    }

    /// <summary>Places <paramref name="limbs"/> for <paramref name="frames"/> frames: long enough for the page's walk to take over and the hips to settle.</summary>
    private static Stance Settled(Footing footing, Limbs limbs, Pose pose, double travelled, Ship? ship, Hero hero, int frames = 60)
    {
        Stance stance = default;
        for (int i = 0; i < frames; i++) stance = footing.Place(limbs, pose, travelled, ship, hero, Frame);
        return stance;
    }

    [Fact]
    public void FindsTheGroundUnderAPointUpOutOfASlopeDownOntoOneAndNoneAtAWallsFace()
    {
        // In the ramp's middle cell, a quarter in: up onto the diagonal, or down onto it from above.
        Assert.Equal(7 * Unit + 749, Hero.GroundAt(Ramp, 6 * Unit + 250, 7 * Unit + 900, 500, 500));
        Assert.Equal(7 * Unit + 749, Hero.GroundAt(Ramp, 6 * Unit + 250, 7 * Unit + 400, 500, 500));
        // On the flat, the floor's top; on the plateau, its top under the point.
        Assert.Equal(9 * Unit, Hero.GroundAt(Ramp, 2 * Unit + 300, 9 * Unit, 500, 500));
        Assert.Equal(6 * Unit, Hero.GroundAt(Ramp, 10 * Unit, 6 * Unit + 400, 500, 500));
        // Against the wall on the right: its top is past reach.
        Assert.Null(Hero.GroundAt(Ramp, 13 * Unit + 100, 9 * Unit, 750, 750));
        // Over the flat from the plateau's height: the floor is past reach.
        Assert.Null(Hero.GroundAt(Ramp, 3 * Unit, 6 * Unit, 750, 750));
    }

    [Fact]
    public void OnTheFlatEveryPoseKeepsItsOwnLegs()
    {
        var footing = new Footing();
        foreach (var (pose, hero) in new[] { (Pose.Stand, OnFlat), (Pose.Walk, OnFlat with { Vx = 120 }), (Pose.Run, OnFlat with { Vx = 220 }) })
            for (double travelled = 0; travelled < 5; travelled += 0.13)
            {
                var limbs = Posed(pose, hero, travelled);
                Assert.Equal(new Stance(limbs.FrontLeg, limbs.BackLeg, default, default, 0), footing.Place(limbs, pose, travelled, Ramp, hero, Frame));
            }
        Assert.Equal(0, footing.Steep);
    }

    [Fact]
    public void OnASlopeTheStrideRunsAsThePagesTwoRadiansAStrideLength()
    {
        PoseTests.Close(2 / FootLook.Stride, Footing.Quicken(Pose.Walk) * 2 * Math.PI / (Stride.Walk * Rig.Cell), 9);
        PoseTests.Close(2 / FootLook.Stride, Footing.Quicken(Pose.Run) * 2 * Math.PI / (Stride.Run * Rig.Cell), 9);
    }

    /// <summary>
    /// How far above the ramp the sole of a boot of <paramref name="stance"/> is, heel to toe: at its point lowest under the
    /// ground (most in it) and at its point highest over it.
    /// </summary>
    private static (double Deepest, double Highest) Sole(Limbs limbs, Stance stance, bool front, Hero hero)
    {
        var (leg, shoe) = front ? (stance.FrontLeg, stance.FrontShoe) : (stance.BackLeg, stance.BackShoe);
        double deepest = double.MaxValue, highest = double.MinValue;
        for (double along = -Footing.Heel; along <= Footing.Toe; along += 0.5)
        {
            var at = Footing.BootAt(limbs, front, leg, shoe, stance.Drop, along, Footing.Ankle);
            double above = RampBelow(hero, at.X) - at.Y;
            (deepest, highest) = (Math.Min(deepest, above), Math.Max(highest, above));
        }
        return (deepest, highest);
    }

    /// <summary>How far a leg reaches, hip to ankle, as a share of its reach straight.</summary>
    private static double Reached(Bend leg)
    {
        var ankle = Rig.EndOf(leg, Rig.Leg with { Lower = Rig.Leg.Lower - Footing.Ankle });
        return Math.Sqrt(ankle.X * ankle.X + ankle.Y * ankle.Y) / (Rig.Leg.Upper + Rig.Leg.Lower - Footing.Ankle);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void StandingOnASlopeEachBootIsLevelItsLowEndOnTheGroundItsOtherEndInItNoDeeperThanTheLip(int facing)
    {
        var hero = OnRamp(facing);
        var limbs = Posed(Pose.Stand, hero, 0);
        var stance = Settled(new Footing(), limbs, Pose.Stand, 0, Ramp, hero);
        foreach (bool front in new[] { true, false })
        {
            Assert.Equal(new Shoe(1), front ? stance.FrontShoe : stance.BackShoe);
            var (deepest, highest) = Sole(limbs, stance, front, hero);
            PoseTests.Close(0, highest, 1);
            Assert.True(deepest > -FootLook.Lip, $"a boot is {-deepest} px in the ground, under the lip");
        }
        Assert.True(Reached(stance.FrontLeg) >= 0.45 && Reached(stance.BackLeg) >= 0.45, "a knee folds shut");
    }

    [Theory]
    [InlineData(Pose.Walk, 1)]
    [InlineData(Pose.Walk, -1)]
    [InlineData(Pose.Run, 1)]
    [InlineData(Pose.Run, -1)]
    public void OnASlopeNoBootThatIsDownFloatsNoneSinksPastTheLipNoSwingingOneSinksAndNoKneeFoldsShut(Pose pose, int facing)
    {
        var hero = OnRamp(facing) with { Vx = (pose == Pose.Run ? 176 : 96) * facing };
        double cycle = pose == Pose.Run ? Stride.Run : Stride.Walk;
        for (double travelled = 0; travelled < cycle; travelled += cycle / 40)
        {
            var limbs = Posed(pose, hero, travelled);
            var stance = Settled(new Footing(), limbs, pose, travelled, Ramp, hero);
            foreach (bool front in new[] { true, false })
            {
                var leg = front ? stance.FrontLeg : stance.BackLeg;
                // The hips never drop a knee past Fold; the stride itself, the hips at their height, bends one further, never shut.
                Assert.True(Reached(leg) >= 0.4, $"a knee folds to {Reached(leg)} of its reach at {travelled}");
                var (deepest, highest) = Sole(limbs, stance, front, hero);
                Assert.True(deepest > -FootLook.Lip, $"a boot is {-deepest} px in the ground, under the lip, at {travelled}");
                if (Footing.Fall(limbs, front, pose, travelled, 1, null).Down)
                    Assert.True(highest < 0.05 && highest > -0.3, $"a foot that is down stands {highest} px over the ground at its highest at {travelled}");
                else Assert.True(deepest > -FootLook.Lip, $"a swinging boot is {-deepest} px in the ground at {travelled}");
            }
        }
    }

    [Fact]
    public void AFootOutOfReachHangsFromAStraightLeg()
    {
        var limbs = Posed(Pose.Stand, OnFlat, 0);
        var deep = new Footfall(new Vec(2, 30), true, new Underfoot(30, 30, 30), 0);
        var level = Footing.Fall(limbs, false, Pose.Stand, 0, 0, new Underfoot(0, 0, 0));
        double drop = Footing.DropFor(limbs, deep, level, 0);
        // As low as the other leg, on the level, lets them go before its knee folds past Fold.
        Assert.InRange(drop, 1, FootLook.MostDrop);
        var stance = Footing.Solve(limbs, deep, level, drop);
        Assert.True(Math.Abs(stance.FrontLeg.Lower) < 0.05, $"the leg reaches straight: its knee bends {stance.FrontLeg.Lower}");
        Assert.True(Footing.SoleOf(limbs, true, stance.FrontLeg, drop).Y < 30 - 1, "the foot hangs above the ground it cannot reach");
    }

    [Fact]
    public void OffTheGroundTheLegsAreThePosesAndTheHipsEaseBackUp()
    {
        var hero = OnRamp(1);
        var limbs = Posed(Pose.Stand, hero, 0);
        var footing = new Footing();
        double dropped = Settled(footing, limbs, Pose.Stand, 0, Ramp, hero).Drop;
        var air = LimbsOf(Pose.Air, hero with { Vy = -420 }, new Moment(0, 0));
        var first = footing.Place(air, Pose.Air, 0, Ramp, hero, Frame);
        Assert.Equal((air.FrontLeg, air.BackLeg, default(Shoe), default(Shoe)), (first.FrontLeg, first.BackLeg, first.FrontShoe, first.BackShoe));
        Assert.InRange(first.Drop, 0.01, dropped * (1 - FootLook.Ease) + 1e-9);
        var settled = Settled(footing, air, Pose.Air, 0, Ramp, hero);
        Assert.Equal((0.0, 0.0), (settled.Drop, footing.Steep));
    }
}
