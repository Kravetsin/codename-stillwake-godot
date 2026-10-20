using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>A world made of surfaces (0076), slice 1: the ship alone, commands routed to their sender's surface.</summary>
public class SurfacesTests
{
    private const string Map = """
        ..........
        .########.
        .#..D...#.
        .########.
        """;

    [Fact]
    public void AWorldIsTheShipAloneWithEveryPeerOnIt()
    {
        var w = new Scenario(Map);
        Assert.Equal([new Surface(0, Surface.Ship, 0)], w.World.Surfaces);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Equal(0, w.World.SurfaceOf(7));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void ACommandUnstampedOrStampedWithTheShipReachesTheShipsModules(int? surface)
    {
        var w = new Scenario(Map);
        w.Do(new SetDoor(w.At(4, 2), true) { From = 3, Surface = surface }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(w.At(4, 2)));
    }

    [Fact]
    public void ACommandStampedOnASurfaceItsSenderIsNotOnIsRefusedBeforeAnyModuleSeesIt()
    {
        var w = new Scenario(Map);
        var still = new Scenario(Map);
        // Not a door: the grid would refuse it grid.noDoor, had it seen it.
        w.Do(new SetDoor(w.At(2, 2), true) { From = 1, Surface = 1 }).Run();
        still.Run();
        Assert.Equal(World.Elsewhere, Assert.Single(w.Refusals).Reason);
        Assert.Equal(still.World.Save(), w.World.Save());
    }

    [Fact]
    public void AStampedCommandCarriesItsSurfaceOverTheWireAndAnUnstampedOneNothing()
    {
        var w = new Scenario(Map);
        var text = w.World.Encode(new SetDoor(12, true) { From = 2, Surface = 0 });
        Assert.Equal("""{"type":"grid.setDoor","cell":12,"open":true,"from":2,"surface":0}""", text);
        Assert.Equal(new SetDoor(12, true) { From = 2, Surface = 0 }, w.World.Decode(text));
        Assert.DoesNotContain("surface", w.World.Encode(new SetDoor(12, true)));
    }

    [Fact]
    public void TheShipSavesAsAWorldDidBeforeSurfaces()
    {
        // The fixture of this format, 19 written before surfaces, loads and saves byte for byte; a hash is of those bytes.
        string before = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"format-{World.SaveFormat}.json"));
        Assert.Equal(before, World.Load(before, new Registry().All).Save());
    }
}
