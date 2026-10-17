using System.Text.Json.Nodes;
using Stillwake.Game.Features.Items;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;
using static Stillwake.Game.Tests.ItemsWorlds;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The slots of a crate and the inventory as this peer shows them, with its own moves in flight (0014).</summary>
public class CrateTests
{
    /// <summary>A room with a crate drawn below it, and peer 0 carrying scrap and iron.</summary>
    private static readonly Layout Store = Layout.Parse(["##########", "#........#", "#........#", "#........#", "##########", "KK"]);

    private static Registry WithStore(out World world)
    {
        (var m, world) = With(Store, items => items["inventories"] = new JsonArray(
            new JsonObject
            {
                ["id"] = 0,
                ["slots"] = new JsonArray(
                    [Stack("scrap", 360), Stack("iron", 60), .. Enumerable.Repeat<JsonNode?>(null, Spec.Inventory.Total - 2)]),
            }));
        return m;
    }

    [Fact]
    public void ShowsThisPeersOwnMovesAtOnceBeforeTheirTickRuns()
    {
        var m = WithStore(out var world);
        var crate = m.Grid.Machines.First(c => c.Kind == "crate").Id;
        var (_, client) = LocalSession.Create(world);
        client.Send(new Move(new Place(0), new Target(Crate: crate), 100));
        client.Send(new Drop(new Place(1), 10, 11, 1));
        var shown = Reading.Of(client.Id, client.InFlight, m.Items, m.Grid, m.Research, crate);
        Assert.Equal(new Stack("scrap", 260), shown.At(new Place(0)));
        Assert.Equal(new Stack("iron", 50), shown.At(new Place(1)));
        Assert.Equal(new Stack("scrap", 100), shown.At(new Place(0, crate)));
        Assert.Equal("crate", shown.Kind);
        // The world itself is untouched until the tick runs.
        Assert.Equal(new Stack("scrap", 360), m.Items.Inventory(0)[0]);
        Assert.All(m.Items.Crate(crate), Assert.Null);
    }
}
