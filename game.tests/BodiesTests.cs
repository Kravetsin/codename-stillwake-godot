using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using Enter = Stillwake.Sim.Crew.Enter;

namespace Stillwake.Game.Tests;

/// <summary>The bodies on the floor as the client reads them (0061): each death's stacks together, and this player's newest.</summary>
public class BodiesTests
{
    /// <summary>An airless sealed room, the spawn in it: a hero brought in dies there with what it carries.</summary>
    private const string Room = """
        ##########
        #S.......#
        #........#
        ##########
        """;

    private static readonly List<Pile> Piles = [];

    /// <summary>Peer <paramref name="peer"/>'s hero brought to <paramref name="cell"/>, run until it passes out.</summary>
    private static TestWorld DieAt(TestWorld w, int cell, int peer = 0)
    {
        w.Step(new Enter(cell) { From = peer });
        for (int i = 0; i < 2000 && (w.M.Crew.HeroOf(peer)?.Out ?? 0) == 0; i++) w.Step();
        Assert.True(w.M.Crew.HeroOf(peer)!.Out > 0);
        return w;
    }

    [Fact]
    public void PointsAtTheNewerOfTwoBodiesOfOnePlayerAndAtNoneOfAnothers()
    {
        var w = DieAt(new TestWorld(Room).Give(0, ("scrap", 10)), 1 * 10 + 6);
        int first = Assert.Single(w.M.Items.Floor).Body!.Died;
        // Woken at the spawn, it is given more and dies again there.
        for (int i = 0; i < Content.Crew.PassOut; i++) w.Step();
        DieAt(w.Give(0, ("iron", 5)), 1 * 10 + 1);
        Bodies.Of(w.M.Items.Floor, Piles);
        Assert.Equal([(0, first), (0, Piles[1].Died)], Piles.Select(p => (p.Peer, p.Died)));
        Assert.True(Piles[1].Died > first);
        var latest = Bodies.Latest(w.M.Items.Floor, 0);
        Assert.Equal(Piles[1], latest);
        Assert.Equal((1500.0, 2000.0), (latest!.Value.X, latest.Value.Y));
        Assert.Null(Bodies.Latest(w.M.Items.Floor, 1));
    }

    /// <summary>What a body's tip lists (2026-10-29-body-bag): each item once with all of it, the most first, another body's and no body's left out.</summary>
    [Fact]
    public void ListsWhatABodyHoldsEachItemOnceTheMostFirst()
    {
        static JsonObject Tagged(JsonObject stack, int peer, int died)
        {
            stack["body"] = new JsonObject { ["peer"] = peer, ["died"] = died };
            return stack;
        }
        var (m, _) = ItemsWorlds.With(Layout.Parse(Room), items => items["floor"] = new JsonArray(
            Tagged(ItemsWorlds.Lying(1, "scrap", 3, 2, 2, 10, -1, 9), 2, 9),
            Tagged(ItemsWorlds.Lying(2, "iron", 2, 2, 2, 10, -1, 9), 2, 9),
            Tagged(ItemsWorlds.Lying(3, "scrap", 4, 2, 2, 10, -1, 9), 2, 9),
            Tagged(ItemsWorlds.Lying(4, "biomass", 2, 2, 2, 10, -1, 9), 2, 9),
            Tagged(ItemsWorlds.Lying(5, "planks", 9, 7, 2, 10, -1, 4), 1, 4),
            ItemsWorlds.Lying(6, "planks", 9, 2, 2, 10, 0, 0)));
        Assert.Equal([("scrap", 7), ("biomass", 2), ("iron", 2)], Bodies.Contents(m.Items.Floor, new Pile(2, 9, 0, 0)));
    }

    [Fact]
    public void PutsABodyAtTheMiddleOfItsStacksAndLeavesStacksOfNoBodyOut()
    {
        static JsonObject Tagged(JsonObject stack, int peer, int died)
        {
            stack["body"] = new JsonObject { ["peer"] = peer, ["died"] = died };
            return stack;
        }
        var (m, _) = ItemsWorlds.With(Layout.Parse(Room), items => items["floor"] = new JsonArray(
            Tagged(ItemsWorlds.Lying(1, "scrap", 3, 2, 2, 10, -1, 9), 2, 9),
            ItemsWorlds.Lying(2, "iron", 3, 8, 2, 10, 0, 0),
            Tagged(ItemsWorlds.Lying(3, "iron", 1, 4, 2, 10, -1, 9), 2, 9),
            Tagged(ItemsWorlds.Lying(4, "planks", 1, 7, 2, 10, -1, 4), 1, 4)));
        Bodies.Of(m.Items.Floor, Piles);
        Assert.Equal([new Pile(1, 4, 7500, 3000), new Pile(2, 9, 3500, 3000)], Piles);
        Assert.Equal(new Pile(2, 9, 3500, 3000), Bodies.Latest(m.Items.Floor, 2));
        // Drawn between ticks: where the view places each stack.
        Bodies.Of(m.Items.Floor, Piles, s => (s.X + 100, s.Y));
        Assert.Equal(7600, Piles[0].X);
    }

    [Fact]
    public void ListsThePlayersBodyAfterHomeInItsSuitACellAboveWhereItLies()
    {
        var list = Markers.Of(3 * 20 + 4, 20, [(2, new Hero(12_500, 7_000, 0, 0, false, 1), Suits.All[1])], (new Pile(0, 50, 8_500, 6_000), Suits.All[2]));
        Assert.Equal([MarkerIcon.Home, MarkerIcon.Body, MarkerIcon.Friend], list.Select(m => m.Icon));
        Assert.Equal(new Marker(MarkerIcon.Body, 8.5, 5.0, Suits.All[2]), list[1]);
        Assert.Equal(2, Markers.Of(0, 20, [(2, new Hero(0, 0, 0, 0, false, 1), Suits.All[1])]).Count);
    }
}
