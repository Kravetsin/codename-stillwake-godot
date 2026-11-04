using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.Poses;

namespace Stillwake.Game.Tests;

/// <summary>
/// The limbs as the figure draws them, settling after the pose's each frame and kept out of the floor (<see cref="Poses.Unsunk"/>,
/// 2026-11-04-hero-gait). The figure's own settle, a share of the way each frame, as <c>Figure.Settle</c> over its 30 ms.
/// </summary>
public class SettledTests
{
    private static double Toward(double from, double to, double ms) => from + (to - from) * Math.Min(1, ms / 30);
    private static Bend Toward(Bend from, Bend to, double ms) => new(Toward(from.Upper, to.Upper, ms), Toward(from.Lower, to.Lower, ms));

    private static Limbs Settled(Limbs from, Limbs to, double ms) => Unsunk(new Limbs(Toward(from.FrontArm, to.FrontArm, ms),
        Toward(from.BackArm, to.BackArm, ms), Toward(from.FrontLeg, to.FrontLeg, ms), Toward(from.BackLeg, to.BackLeg, ms),
        Toward(from.Tilt, to.Tilt, ms), Toward(from.Lift, to.Lift, ms), false, Toward(from.FrontFoot, to.FrontFoot, ms),
        Toward(from.BackFoot, to.BackFoot, ms)));

    [Theory]
    [InlineData(60)]
    [InlineData(144)]
    public void ARunDrawnSettlingRidesAsSmoothlyAsItsPose(int fps)
    {
        // Set on the lower sole of legs lagging the pose, the drawn body jumped up to two pixels a frame though the pose rode smooth.
        var hero = new Hero(11000, 20000, 440, 0, false, 1);
        double ms = 1000.0 / fps, perFrame = 0.44 * 60 / fps / Stride.Run;
        Limbs Pose(int k) => LimbsOf(Features.Crew.Pose.Run, hero, new Moment(0, 0, Cycle: k * perFrame, Gait: 1, Moving: 1));
        var shown = Pose(0);
        for (int k = 1; k < fps * 2; k++)
        {
            var next = Settled(shown, Pose(k), ms);
            Assert.True(Math.Abs(next.Lift - shown.Lift) < 0.3, $"the drawn body jumped {next.Lift - shown.Lift} px at frame {k}");
            Assert.True(PoseTests.Soles(next).Max(p => p.Y) < 0.01, $"a drawn sole under the floor at frame {k}");
            shown = next;
        }
    }

    [Theory]
    [InlineData(HeroMotion.Landing.Least)]
    [InlineData(1)]
    [InlineData(HeroMotion.Landing.Most)]
    public void ALandingDrawnSettlingStandsOnFlatBoots(double depth)
    {
        double ms = 1000.0 / 60;
        var shown = LimbsOf(Features.Crew.Pose.Air, new Hero(0, 0, 0, 800, false, 1), new Moment(0, 0));
        for (int k = 0; k < 30; k++)
        {
            var to = LimbsOf(Features.Crew.Pose.Stand, new Hero(0, 0, 0, 0, true, 1),
                new Moment(0, k * ms, Landing: Math.Max(0, 1 - k * ms / 200), Momentum: new Momentum(Depth: depth)));
            shown = Settled(shown, to, ms);
            var soles = PoseTests.Soles(shown).ToList();
            if (k == 0) continue;
            Assert.True(Math.Abs(soles.Max(p => p.Y)) < 0.05, $"the lowest sole at {soles.Max(p => p.Y)} at frame {k}");
            Assert.True(soles.All(p => p.Y > -0.4), $"a boot tipped {soles.Min(p => p.Y)} off the floor at frame {k}");
        }
    }
}
