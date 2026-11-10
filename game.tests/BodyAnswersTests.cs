using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Crew;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The body answers the weapon (body-answers): a stroke leaned into, a shot's recoil thrown back and, on the ground, shoved.</summary>
public class BodyAnswersTests
{
    private const int Unit = Hero.Unit;

    /// <summary>A room on a floor at block row 4 that ends in a pit four blocks wide on the left.</summary>
    private static readonly Ship Room = HeroTests.ShipFrom("""
        ############
        #..........#
        #..........#
        #..........#
        #....#######
        #....#######
        ############
        """);

    /// <summary>A hero standing on the floor with its middle at <paramref name="x"/> thousandths, facing right.</summary>
    private static Hero StandingAt(int x) => new(x, 8 * Unit, 0, 0, false, 1);

    /// <summary>Where a hero settles after <paramref name="kick"/> across, nothing pressed.</summary>
    private static Hero Shoved(Hero hero, int kick)
    {
        var h = hero.Kicked(kick, 0, Room, (int)Math.Round(kick * BodyAnswers.Shove));
        for (int i = 0; i < 120; i++) h = h.Step(HeroTests.Still, Room);
        return h;
    }

    [Fact]
    public void ARailgunShotOnTheGroundShovesTheHeroAboutHalfACellBack()
    {
        var hero = StandingAt(16 * Unit);
        var after = Shoved(hero, -Stillwake.Content.Shots.Railgun.Recoil);
        Assert.Equal(hero.Y, after.Y);
        Assert.InRange(hero.X - after.X, 400, 600);
        // A nailer's barely: under a tenth of a cell.
        Assert.InRange(hero.X - Shoved(hero, -Stillwake.Content.Shots.Nailer.Recoil).X, 1, 100);
    }

    [Fact]
    public void WithItsBackToAWallTheHeroStaysWhereItIs()
    {
        var against = HeroTests.Settle(Room, StandingAt(16 * Unit), HeroTests.Left);
        var after = Shoved(against, -Stillwake.Content.Shots.Railgun.Recoil);
        Assert.Equal((against.X, against.Y), (after.X, after.Y));
    }

    [Fact]
    public void AtALedgeTheShoveCarriesTheHeroOffAndItFalls()
    {
        // Its far side 0.4 of a cell over the floor: shoved half a cell, nothing holds it up.
        var edge = StandingAt(10 * Unit - Spec.Width / 2 + 400);
        Assert.True(Shoved(edge, -Stillwake.Content.Shots.Railgun.Recoil).Y > edge.Y + Unit);
    }

    [Fact]
    public void InTheAirOnALadderOrUnshovedAShotMovesNoOne()
    {
        var aloft = StandingAt(16 * Unit) with { Y = 6 * Unit, Vy = -100 };
        Assert.Equal(aloft, aloft.Kicked(-220, 0, Room, -286));
        var climbing = StandingAt(16 * Unit) with { Climbing = true };
        Assert.Equal(climbing, climbing.Kicked(-220, 0, Room, -286));
        var standing = StandingAt(16 * Unit);
        Assert.Equal(standing, standing.Kicked(-220, 0, Room));
        // A shot straight down kicks only up: nothing across, nothing done.
        Assert.Equal(standing, standing.Kicked(0, -220, Room, 0));
    }

    /// <summary>Every answer with the span of its clock it plays over: a stroke's from its start to its end about its hit.</summary>
    public static IEnumerable<object[]> Answers()
    {
        var tools = new (Stroke, Stillwake.Content.Weapon)[]
        {
            (Strokes.Axe, Stillwake.Content.Weapons.Axe), (Strokes.Hammer, Stillwake.Content.Weapons.Sledgehammer),
            (Strokes.Pick, Stillwake.Content.Weapons.Pickaxe), (Strokes.Spear, Stillwake.Content.Weapons.Spear),
        };
        foreach (var (stroke, weapon) in tools)
        {
            var (answer, heft) = BodyAnswers.StrokeOf(weapon.Item)!.Value;
            yield return [answer, -stroke.HitShare, 1 - stroke.HitShare, heft];
        }
        yield return [BodyAnswers.Recoil, 0.0, BodyAnswers.RecoilMs, 1.0];
    }

    [Theory]
    [MemberData(nameof(Answers))]
    public void EveryAnswerStartsAndEndsAtRest(BodyAnswer answer, double from, double to, double scale)
    {
        Assert.Equal(BodyState.Rest, BodyPlay.At(answer, from, scale));
        Assert.Equal(BodyState.Rest, BodyPlay.At(answer, to, scale));
    }

    [Theory]
    [MemberData(nameof(Answers))]
    public void NoFootGoesFurtherThanTheFloorIsFree(BodyAnswer answer, double from, double to, double scale)
    {
        for (double at = from; at <= to; at += (to - from) / 200)
        {
            var stance = BodyPlay.Stance(BodyPlay.At(answer, at, scale), 1, 0.5, 0);
            double front = stance.HipX + Rig.HipApart + Rig.EndOf(stance.FrontLeg, Rig.ToAnkle).X;
            double back = stance.HipX - Rig.HipApart + Rig.EndOf(stance.BackLeg, Rig.ToAnkle).X;
            Assert.InRange(front - Rig.HipApart, -0.5 - 1e-6, 1 + 1e-6);
            Assert.InRange(back + Rig.HipApart, -0.5 - 1e-6, 1 + 1e-6);
        }
    }

    [Fact]
    public void AStrokeLeansIntoItsHitAndAShotThrowsTheBodyBackAndTheArmUp()
    {
        var hit = BodyPlay.At(BodyAnswers.Overhand, 0, 1);
        Assert.True(hit.Lean > 0 && hit.Dip > 0 && hit.Front > 0);
        var shot = BodyPlay.At(BodyAnswers.Recoil, 40, 1);
        Assert.True(shot.Lean < 0 && shot.Kick > 0 && shot.Push < 0);
        Assert.True(BodyPlay.At(BodyAnswers.Recoil, 100, 1).Back < 0);
        // A nailer's is a railgun's scaled down by its recoil.
        double nail = (double)Stillwake.Content.Shots.Nailer.Recoil / BodyAnswers.FullRecoil;
        Assert.Equal(shot.Lean * nail, BodyPlay.At(BodyAnswers.Recoil, 40, nail).Lean, 9);
    }
}
