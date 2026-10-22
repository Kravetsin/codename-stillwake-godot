using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Items;
using Stillwake.Sim;
using Xunit;
using static Stillwake.Game.Tests.ItemsWorlds;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What the hero picks up as it goes: apps/game/src/features/crew/pickup.test.ts.</summary>
public class PickUpTests
{
    /// <summary>Drawn nine wide and doubled (0104): the floor's top is row 6.</summary>
    private const int Width = 18;
    private static readonly Layout Room = Layout.Parse(["#########", "#.......#", "#.......#", "#=======#", "#########"]).Doubled();

    /// <summary>A hero standing on the floor over the drawing's column <paramref name="x"/>, its cells 2x and 2x + 1.</summary>
    private static Hero Standing(int x) => new((2 * x + 1) * Hero.Unit, 6 * Hero.Unit, 0, 0, false, 1);

    private static IReadOnlyList<int> PickUps(Registry m, int peer, Hero hero, params int[] taking) =>
        Reach.PickUpsOf(m.Items, m.Grid.Width, peer, hero.X, hero.Y, taking.ToHashSet());

    [Fact]
    public void IsEveryStackInACellItsBodyOverlapsThatTheSimWouldLetItTake()
    {
        // Stacks laid on the floor straight into the slice, as a drop long ago would leave them.
        var (m, _) = With(Room, items => items["floor"] = new JsonArray(
            Lying(1, "scrap", 3, 4, 5, Width, by: 1, at: 0),
            Lying(2, "scrap", 3, 13, 5, Width, by: 1, at: 0),
            Lying(3, "iron", 1, 5, 3, Width, by: 1, at: 0)));
        // Two cells wide, as a cell of the drawing.
        Assert.True(Spec.Hero.Width <= 2 * Hero.Unit);
        Assert.Equal([1, 3], PickUps(m, 0, Standing(2)));
        Assert.Equal([2], PickUps(m, 0, Standing(6)));
        // One picked up already, its pick-up in flight, is not sent again.
        Assert.Equal([3], PickUps(m, 0, Standing(2), taking: 1));
        Assert.Empty(PickUps(m, 0, Standing(4)));
    }

    [Fact]
    public void LeavesAStackTheHeroJustDroppedItself()
    {
        var (m, world) = With(Room, items =>
            items["floor"] = new JsonArray(Lying(1, "scrap", 3, 4, 5, Width, by: 0, at: (int)items["ticks"]!)));
        Assert.Empty(PickUps(m, 0, Standing(2)));
        Assert.Equal([1], PickUps(m, 1, Standing(2)));
        for (int i = 0; i < Spec.Floor.OwnPickupDelay; i++) world.Step([]);
        Assert.Equal([1], PickUps(m, 0, Standing(2)));
    }
}
