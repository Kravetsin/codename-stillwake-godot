using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Light;
using Stillwake.Sim;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Tests;

/// <summary>
/// A grate, added as one row of the structure table and nothing else (0112): rooms, light and the hero take it by its traits,
/// a deck stood on that neither seals nor stops light, with no line of theirs naming it.
/// </summary>
public class GrateTests
{
    private const int Block = HeroTests.Block;
    private static readonly HeroInput Down = HeroTests.Still with { Y = 1 };

    /// <summary>Two rooms one over the other, a cell a character, the deck between them <paramref name="deck"/> across its middle.</summary>
    private static string Decked(char deck) => $"""
        #######
        #.....#
        #.....#
        ##{deck}{deck}{deck}##
        #.....#
        #.....#
        #######
        """;

    private static int[] RoomsOf(string map)
    {
        var m = new Registry();
        var layout = Layout.Parse(map);
        World.Create(1, layout, m.All);
        return Enumerable.Range(0, layout.Width * layout.Height).Select(c => m.Rooms.RoomAt(c) ?? -1).ToArray();
    }

    [Fact]
    public void RoomsPassThroughAGrateAsThroughTheOpen()
    {
        Assert.Equal(RoomsOf(Decked('.')), RoomsOf(Decked('%')));
        Assert.NotEqual(RoomsOf(Decked('h')), RoomsOf(Decked('%')));
    }

    [Fact]
    public void LightPassesAGrateAShutHatchStopsIt()
    {
        int[] structure = [Structure.Grate, Structure.HatchClosed, Structure.Floor];
        var cells = new byte[3];
        Sources.Occupy(structure, c => structure[c], 3, new Stillwake.Sim.Box(0, 0, 2, 0), cells);
        Assert.Equal(new[] { Sources.Open, Sources.Wall, Sources.Open }, cells);
        Assert.False(Sources.Blocks(c => structure[c], 3, 1, 16, 8, 8));
    }

    /// <summary>The hero's floors map with its decks of grates: it stands on one, drops through with down, and down held takes it through both.</summary>
    [Fact]
    public void TheHeroStandsOnAGrateAndDropsThroughItAsThroughAFloor()
    {
        var ship = HeroTests.ShipFrom("""
            #######
            #.....#
            #.....#
            #.....#
            #.....#
            #.....#
            #.....#
            #%%%%%#
            #.....#
            #.....#
            #.....#
            #%%%%%#
            #.....#
            #.....#
            #.....#
            #######
            """);
        var hero = new Hero(3 * Block + Block / 2, 7 * Block, 0, 0, false, 1);
        Assert.Equal(7 * Block, HeroTests.Settle(ship, hero, HeroTests.Still).Y);
        var dropped = HeroTests.Until(ship, hero, Down, h => h.Y > 7 * Block);
        Assert.Equal(11 * Block, HeroTests.Settle(ship, dropped, HeroTests.Still).Y);
        Assert.Equal(15 * Block, HeroTests.Settle(ship, hero, Down).Y);
    }
}
