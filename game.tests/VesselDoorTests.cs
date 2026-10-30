using Stillwake.Game.Features.Dev;
using Stillwake.Sim;
using Stillwake.Sim.Places;
using Xunit;
using Vessels = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>Where the dev panel's teleport puts the hero in a vessel (generated-vessels).</summary>
public class VesselDoorTests
{
    /// <summary>The new game's world with a vessel of <paramref name="cls"/> jumped to.</summary>
    private static Registry Jumped(string cls, bool intact)
    {
        var m = new Registry();
        var world = World.Create(1, Registry.Wreck, m.All);
        world.Step([new Find(cls)]);
        world.Step([new Jump(1, intact)]);
        return m;
    }

    [Fact]
    public void NoVesselOutIsNowhere() => Assert.Null(VesselDoor.Inside(Fresh().Grid));

    private static Registry Fresh()
    {
        var m = new Registry();
        World.Create(1, Registry.Wreck, m.All);
        return m;
    }

    [Theory]
    [InlineData("shuttle")]
    [InlineData("freighter")]
    public void TheHeroStandsOnTheDeckJustInsideAnIntactVesselsDoorWithRoomForItsBody(string cls)
    {
        var grid = Jumped(cls, intact: true).Grid;
        int cell = VesselDoor.Inside(grid)!.Value, width = grid.Width;
        var box = grid.Reserved!.Value;
        Assert.True(box.Contains(cell % width, cell / width));
        // Two columns and a deck's six rows open over a floor that holds it.
        for (int up = 0; up < Vessels.Open; up++)
            for (int across = 0; across < 2; across++)
                Assert.False(grid.Blocks(cell - up * width + across), $"blocked {up} up, {across} across");
        Assert.True(grid.Solid(cell + width));
        Assert.True(Sim.Grid.Doorways.IsDoor(grid.StructureAt(cell - 3)));
    }
}
