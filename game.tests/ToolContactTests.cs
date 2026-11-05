using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Where a running tool is seen to work (tool-contact): on the face of what its fan strikes, else at its whole reach.</summary>
public class ToolContactTests
{
    private const int Unit = Hero.Unit;
    private static readonly string Grinder = Spec.Weapons.Grinder.Item, Cutter = Spec.Weapons.PlasmaCutter.Item;

    /// <summary>An open strip in soil a character a cell: open from column 1 to 30, rows 1 to 8.</summary>
    private static Layout Strip => Layout.Parse("""
        ################################
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        ################################
        """).WithMaterials("""
        ssssssssssssssssssssssssssssssss
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        ssssssssssssssssssssssssssssssss
        """) with { Kind = Spec.Surfaces.TestStrip.Id };

    /// <summary>A hero on the floor in column <paramref name="x"/>, its feet on row 9.</summary>
    private static Hero Standing(int x) => new(x * Unit + Unit / 2, 9 * Unit, 0, 0, false, 1);

    private static Contact ContactAt(Hero hero, int toX, string tool)
    {
        var m = new TestWorld(Strip).M;
        var hand = Aiming.HandOf(hero);
        var fan = Aiming.FanOf(m, hero, new Point(toX * Unit, hand.Y), tool);
        return Aiming.ContactOf(m, fan, 0);
    }

    [Fact]
    public void AGrinderAtAWallWorksOnItsFace()
    {
        // From column 26 the wall in column 31 is in the grinder's six cells.
        var (at, touches, cell) = ContactAt(Standing(26), 40, Grinder);
        Assert.Equal(Aiming.HandOf(Standing(26)).Y / Unit * 32 + 31, cell);
        Assert.True(touches);
        Assert.Equal(31 * Unit, at.X, 6);
        Assert.Equal(Aiming.HandOf(Standing(26)).Y, at.Y, 6);
    }

    [Fact]
    public void AToolAtNothingReachesItsWholeRangeAndTouchesNothing()
    {
        var hero = Standing(15);
        var hand = Aiming.HandOf(hero);
        var (at, touches, cell) = ContactAt(hero, 40, Cutter);
        Assert.Null(cell);
        Assert.False(touches);
        Assert.Equal(hand.X + Spec.Weapons.PlasmaCutter.Range, at.X, 6);
        Assert.Equal(hand.Y, at.Y, 6);
    }

    [Fact]
    public void AStreamThrowsAsMuchASecondAsTheHitsDidOneByOne()
    {
        foreach (var weapon in new[] { Spec.Weapons.Drill, Spec.Weapons.Grinder, Spec.Weapons.PlasmaCutter })
        {
            double hits = (double)Sim.World.TicksPerSecond / weapon.Pace * Features.Effects.Impacts.ShareOf(weapon.Item);
            Assert.Equal(hits, WorkLooks.ShareOf(weapon.Item) * WorkLooks.ThrowsPerSecond, 9);
        }
    }

    [Fact]
    public void TheCutterReachesFartherThanTheGrinderAndTheGrinderLessThanTheDrill()
    {
        Assert.True(Spec.Weapons.PlasmaCutter.Range > Spec.Weapons.Drill.Range);
        Assert.InRange(Spec.Weapons.Grinder.Range, Spec.Weapons.Drill.Range - 1000, Spec.Weapons.Drill.Range - 1);
    }
}
