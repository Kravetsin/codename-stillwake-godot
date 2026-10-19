using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>A wall planned cut to a shape (0069, part 2) as friends see it: the shape crosses the wire with the plan.</summary>
public class ShapedPlanTests
{
    [Fact]
    public void AFriendSeesAShapedPlanAndTheShapedWallItBuilds()
    {
        var game = Definition.Game();
        var layout = Layout.Parse(["#######", "#.....#", "#######"]);
        var host = new Host(0);
        var clients = Enumerable.Range(0, 2).Select(_ =>
        {
            var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
            return new Client(game.Create(1, layout), toHost, host.Connect(toClient));
        }).ToList();
        void Tick()
        {
            host.Advance();
            foreach (var c in clients) c.Update();
        }
        var shaped = Placement.Wall() with { Shape = Shape.TopRight };
        clients[1].Send(new SetCreative(true));
        clients[1].Send(new PlacePlan(9, shaped));
        Tick();
        Assert.All(clients, c => Assert.Equal(shaped, game.Module<BuildingModule>(c.World).Plans.Single().Place));
        while (game.Module<BuildingModule>(clients[0].World).Plans.Count > 0)
        {
            clients[1].Send(new Work(1));
            Tick();
        }
        Assert.All(clients, c => Assert.Equal(Shape.TopRight, game.Module<GridModule>(c.World).ShapeAt(9)));
        Assert.Single(clients.Select(c => c.World.Hash()).Distinct());
    }
}
