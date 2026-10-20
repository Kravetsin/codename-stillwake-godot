using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;

namespace Stillwake.Game.Tests;

/// <summary>Feet planted on uneven ground (<see cref="Footing"/>): the pose untouched on the flat, both feet on a slope, a foot out of reach hanging.</summary>
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
        #......./#####
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

    /// <summary>Places <paramref name="pose"/> for <paramref name="frames"/> frames: long enough for the hips to settle.</summary>
    private static Stance Settled(Footing footing, Limbs pose, Ship? ship, Hero hero, int frames = 60)
    {
        Stance stance = default;
        for (int i = 0; i < frames; i++) stance = footing.Place(pose, ship, hero, Frame);
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
                var stance = footing.Place(limbs, Ramp, hero, Frame);
                Assert.Equal(new Stance(limbs.FrontLeg, limbs.BackLeg, 0, 0, 0), stance);
            }
        Assert.Equal(1, footing.Squeeze);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void StandingOnASlopeEachFootStandsOnTheGroundUnderItsBootAlongIt(int facing)
    {
        var hero = OnRamp(facing);
        var limbs = Posed(Pose.Stand, hero, 0);
        var stance = Settled(new Footing(), limbs, Ramp, hero);
        Assert.True(stance.Drop > 0, "the hips drop for the downhill foot");
        foreach (bool front in new[] { true, false })
        {
            var leg = front ? stance.FrontLeg : stance.BackLeg;
            var sole = Footing.TurnedSoleOf(limbs, front, leg, front ? stance.FrontBoot : stance.BackBoot, stance.Drop);
            PoseTests.Close(RampBelow(hero, sole.X), sole.Y, 1);
            // Its sole flush with the ramp: up ahead facing right, its toe turned up; facing left, down.
            PoseTests.Close(-facing * Math.PI / 4, Footing.Lies(limbs, leg, front ? stance.FrontBoot : stance.BackBoot), 2);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void WalkingASlopeNoToeOrHeelSinksAndTheLowerFootStandsOnTheGround(int facing)
    {
        var hero = OnRamp(facing) with { Vx = 120 * facing };
        for (double travelled = 0; travelled < Stride.Walk; travelled += 0.1)
        {
            var limbs = Posed(Pose.Walk, hero, travelled);
            var footing = new Footing();
            var stance = Settled(footing, limbs, Ramp, hero);
            double nearest = double.MaxValue;
            foreach (bool front in new[] { true, false })
            {
                var (leg, boot) = front ? (stance.FrontLeg, stance.FrontBoot) : (stance.BackLeg, stance.BackBoot);
                var sole = Footing.TurnedSoleOf(limbs, front, leg, boot, stance.Drop);
                double lies = Footing.Lies(limbs, leg, boot);
                // The sole under the ankle, its toe and its heel: none in the ground.
                foreach (double along in new[] { 0, Footing.Toe, -Footing.Heel })
                {
                    var at = new Vec(sole.X + along * Math.Cos(lies), sole.Y + along * Math.Sin(lies));
                    double above = RampBelow(hero, at.X) - at.Y;
                    Assert.True(above > -0.3, $"a foot sinks {-above} px {along} along its sole at {travelled}");
                }
                nearest = Math.Min(nearest, RampBelow(hero, sole.X) - sole.Y);
            }
            Assert.True(nearest < 0.3, $"no foot stands on the ground at {travelled}: the lower is {nearest} px up");
            Assert.True(footing.Squeeze < 1, "the steps shorten on the ramp");
        }
    }

    [Fact]
    public void AFootOutOfReachHangsFromAStraightLegAndTheHipsDropNoFurtherThanTheirMost()
    {
        var limbs = Posed(Pose.Stand, OnFlat, 0);
        var deep = new Underfoot(30, 0);
        double drop = Footing.DropFor(limbs, deep, new Underfoot(0, 0), 1);
        Assert.InRange(drop, 1, FootLook.MostDrop);
        var stance = Footing.Solve(limbs, deep, new Underfoot(0, 0), 1, drop);
        Assert.True(Math.Abs(stance.FrontLeg.Lower) < 0.05, $"the leg reaches straight: its knee bends {stance.FrontLeg.Lower}");
        var sole = Footing.SoleOf(limbs, true, stance.FrontLeg, drop);
        Assert.True(sole.Y < 30 - 1, "the foot hangs above the ground it cannot reach");
        // Straight down toward it, not off to the side.
        PoseTests.Close(Footing.SoleOf(limbs, true, limbs.FrontLeg, 0).X, sole.X, 0);
    }

    [Fact]
    public void OffTheGroundTheLegsAreThePosesAndTheHipsEaseBackUp()
    {
        var hero = OnRamp(1);
        var limbs = Posed(Pose.Stand, hero, 0);
        var footing = new Footing();
        double dropped = Settled(footing, limbs, Ramp, hero).Drop;
        var air = LimbsOf(Pose.Air, hero with { Vy = -420 }, new Moment(0, 0));
        var first = footing.Place(air, null, hero, Frame);
        Assert.Equal((air.FrontLeg, air.BackLeg, 0.0, 0.0), (first.FrontLeg, first.BackLeg, first.FrontBoot, first.BackBoot));
        Assert.InRange(first.Drop, 0.01, dropped * (1 - FootLook.Ease) + 1e-9);
        Assert.Equal(0, Settled(footing, air, null, hero).Drop);
        Assert.Equal(1, footing.Squeeze);
    }
}
