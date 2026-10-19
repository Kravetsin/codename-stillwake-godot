using Stillwake.Game.Features.Crew;
using Xunit;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>A door or hatch shut on the hero pushes it out to the nearest side, over about the 200 ms the door slides.</summary>
public class ShutOutTests
{
    private const int Unit = Hero.Unit;
    private const int Half = Spec.Width / 2;
    /// <summary>The steps a push may take: 200 ms of a 60 Hz controller.</summary>
    private const int Slide = 12;

    /// <summary>Two rooms joined by a door three cells tall in column 4, open or shut.</summary>
    private static Ship Doorway(char door) => HeroTests.ShipFrom($"""
        #########
        #.......#
        #...#...#
        #...{door}...#
        #...{door}...#
        #...{door}...#
        #########
        """);

    /// <summary>A hatch three wide in a deck, a ladder under its middle (0067).</summary>
    private static Ship Hatch(char hatch) => HeroTests.ShipFrom($"""
        #######
        #.....#
        #.....#
        #.....#
        ##{hatch}{hatch}{hatch}##
        #..H..#
        #..H..#
        #..H..#
        #######
        """);

    private static Hero Run(Ship ship, Hero hero, int steps)
    {
        for (int i = 0; i < steps; i++) hero = hero.Step(HeroTests.Still, ship);
        return hero;
    }

    [Theory]
    [InlineData(4600, 1, 5 * Unit + Half)]
    [InlineData(4600, -1, 5 * Unit + Half)]
    [InlineData(4400, 1, 4 * Unit - Half)]
    [InlineData(4500, 1, 5 * Unit + Half)]
    [InlineData(4500, -1, 4 * Unit - Half)]
    public void PushesAHeroOutOfADoorShutOnItToTheNearerSideTheWayItFacesOnATie(int x, int facing, int outside)
    {
        var standing = new Hero(x, 6 * Unit, 0, 0, false, facing);
        Assert.Equal(standing.X, Run(Doorway('d'), standing, 1).X);
        var pushed = Run(Doorway('D'), standing, Slide);
        Assert.Equal((outside, 6 * Unit), (pushed.X, pushed.Y));
        var settled = HeroTests.Settle(Doorway('D'), pushed, HeroTests.Still);
        Assert.Equal((pushed.X, pushed.Y), (settled.X, settled.Y));
    }

    [Fact]
    public void LiftsAHeroOntoAHatchShutUnderItsFeetAndPushesOneMostlyBelowItDown()
    {
        var shut = Hatch('D');
        var high = Run(shut, new Hero(3500, 4 * Unit + 300, 0, 0, true, 1), Slide);
        Assert.Equal((3500, 4 * Unit), (high.X, high.Y));
        Assert.Equal(4 * Unit, HeroTests.Settle(shut, high, HeroTests.Still).Y);
        var low = HeroTests.Settle(shut, Run(shut, new Hero(3500, 7 * Unit + 500, 0, 0, true, 1), Slide), HeroTests.Still);
        Assert.True(low.Y - Spec.Height >= 5 * Unit, $"head at {low.Y - Spec.Height}, inside the hatch");
    }

    [Fact]
    public void LeavesAHeroWhereItIsWhenNoSideOfTheDoorFits()
    {
        var walled = HeroTests.ShipFrom("""
            #####
            ##D##
            ##D##
            ##D##
            #####
            """);
        var stuck = new Hero(2500, 4 * Unit, 0, 0, false, 1);
        Assert.Equal((stuck.X, stuck.Y), (Run(walled, stuck, Slide).X, Run(walled, stuck, Slide).Y));
    }
}
