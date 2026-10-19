using Stillwake.Game.Dev;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The hills dev map (0069): aired, lit and its stacks lying on slopes the moment it opens.</summary>
public class HillsDemoTests
{
    [Fact]
    public void OpensAiredLitAndWithAStackOnASlopeForEachDropped()
    {
        var m = new Registry();
        var world = HillsDemo.Seed(World.Create(1, HillsDemo.Map, new Registry().All), () => m.All);
        world.Step([]);
        int room = m.Rooms.RoomAt(m.Crew.Spawn)!.Value;
        Assert.Equal(RoomStatus.Aired, m.Atmosphere.StatusOf(room));
        Assert.All(m.Grid.MachinesOf("lamp"), lamp => Assert.Equal(1000, m.Power.Satisfaction(lamp.Id)));
        Assert.Equal(6, m.Items.Floor.Count);
        Assert.All(m.Items.Floor, s => Assert.True(Shape.IsFloor(m.Grid.ShapeAt(s.Cell)), $"{s.Item} at {s.Cell}"));
    }
}
