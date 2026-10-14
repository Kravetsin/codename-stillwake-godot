using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Items;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;
using static Stillwake.Game.Tests.ItemsWorlds;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// A crate under the pointer (the crate tests of apps/game/src/features/crew/aim.test.ts, whose crates
/// the items feature opens here), and the slots as this peer shows them with its own moves in flight.
/// </summary>
public class CrateTests
{
    private const int Unit = Hero.Unit;

    /// <summary>A room with a crate drawn below it, and peer 0 carrying scrap and iron.</summary>
    private static readonly Layout Store = Layout.Parse(["##########", "#........#", "#........#", "#........#", "##########", "KK"]);

    private static Registry WithStore(out World world)
    {
        (var m, world) = With(Store, items => items["inventories"] = new JsonArray(
            new JsonObject
            {
                ["id"] = 0,
                ["slots"] = new JsonArray(
                    [Stack("scrap", 360), Stack("iron", 60), .. Enumerable.Repeat<JsonNode?>(null, Spec.Inventory.Slots - 2)]),
            }));
        return m;
    }

    /// <summary>A hero whose body is centred on the middle of cell (x, y).</summary>
    private static Hero HeroOn(int x, int y) => new(x * Unit + Unit / 2, y * Unit + Unit / 2 + Hero.MiddleUp, 0, 0, false, 1);

    private static Func<int, bool> ReachOf(Hero hero, int width) => cell => Aiming.Reaches(hero, cell, width);

    [Fact]
    public void OpensOnAPressWithinReachAndOnNoPressOrOutOfReach()
    {
        var m = WithStore(out _);
        var crate = m.Grid.Machines.First(c => c.Kind == "crate");
        int width = m.Grid.Width, row = crate.Cell / width;
        // The crate sits on the row below the room's floor, from column 0: its second cell is aimed at.
        int aimed = row * width + 1;
        Assert.Equal(crate.Id, Reach.CrateAt(m.Grid, aimed));
        Assert.True(ReachOf(HeroOn(2, row - 1), width)(aimed));
        Assert.False(ReachOf(HeroOn(8, row - 7), width)(aimed));
        Assert.Null(Reach.CrateAt(m.Grid, (row - 2) * width + 1));
    }

    [Fact]
    public void StaysInReachWhileAnyCellOfItIsAndNotOnceItIsTakenApart()
    {
        var m = WithStore(out _);
        var crate = m.Grid.Machines.First(c => c.Kind == "crate");
        int width = m.Grid.Width, row = crate.Cell / width, reach = Spec.Hero.Reach / Unit;
        Assert.True(Reach.CrateInReach(m.Grid, crate.Id, ReachOf(HeroOn(1 + reach, row), width)));
        Assert.False(Reach.CrateInReach(m.Grid, crate.Id, ReachOf(HeroOn(2 + reach, row), width)));
        Assert.False(Reach.CrateInReach(m.Grid, crate.Id + 1, ReachOf(HeroOn(1, row), width)));
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
