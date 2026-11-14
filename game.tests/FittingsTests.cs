using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;
using static Stillwake.Game.Tests.ItemsWorlds;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The figure shows the modules of the tool in its hand, not of another of its kind carried (tool-in-hand).</summary>
public class FittingsTests
{
    private const string Drill = "drill";

    [Fact]
    public void AFigureShowsTheStackInItsHandPlainBesideAFittedOneAndFittedOnceItIsWielded()
    {
        var fitted = Stack(Drill, 1);
        fitted["module"] = "arcModule";
        var (m, world) = With(Layout.Parse(["######", "#....#", "######"]).Doubled(), items => items["inventories"] = new JsonArray(
            new JsonObject
            {
                ["id"] = 0,
                ["slots"] = new JsonArray([Stack(Drill, 1), fitted, .. Enumerable.Repeat<JsonNode?>(null, Spec.Inventory.Total - 2)]),
            }));
        Assert.Empty(Fittings.Of(m.Items, 0, Drill));
        world.Step([new Wield(1) { From = 0 }]);
        Assert.Equal(["arcModule"], Fittings.Of(m.Items, 0, Drill).Select(s => s.Item));
        world.Step([new Wield(2) { From = 0 }]);
        Assert.Empty(Fittings.Of(m.Items, 0, Drill));
        Assert.Empty(Fittings.Of(m.Items, 0, null));
    }
}
