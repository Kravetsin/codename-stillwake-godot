using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Items;
using Xunit;
using Tune = Stillwake.Content.Floor;

namespace Stillwake.Sim.Tests;

/// <summary>Loot sprayed out of a creature (2026-10-29-kill-feel): dealt into pieces, thrown away from the striker.</summary>
public class ItemsSprayTests
{
    /// <summary>A room ten cells wide with a floor across it and three rows of air above, and space to its right.</summary>
    private static readonly string[] Map =
        new[] { "############", "#..........#", "#..........#", "#..........#", "#==========#", "############" }
            .Select(r => r.PadRight(Width)).ToArray();

    private const int Width = 26;
    private static int At(int x, int y) => y * Width + x;
    private static Scenario Room() => new(Layout.Parse(Map));
    private static IReadOnlyList<FloorStack> Floor(Scenario w) => w.M.Items.Floor;
    private static int Of(Scenario w, string item) => Floor(w).Where(s => s.Item == item).Sum(s => s.Count);

    [Fact]
    public void LootIsDealtAUnitAPieceItemByItemUpToTheMostTheRestOnEachItemsLast()
    {
        Assert.Equal([("biomass", 1), ("biomass", 1), ("biomass", 1), ("hide", 1), ("hide", 1), ("fat", 1)],
            SprayRules.Pieces([("biomass", 3), ("hide", 2), ("fat", 1)], 6));
        Assert.Equal([("biomass", 1), ("biomass", 3), ("hide", 1), ("hide", 2)], SprayRules.Pieces([("biomass", 4), ("hide", 3)], 4));
        // Every item a piece even past the most; nothing for none.
        Assert.Equal([("a", 2), ("b", 1), ("c", 1)], SprayRules.Pieces([("a", 2), ("b", 1), ("c", 1), ("d", 0)], 2));
    }

    [Property(MaxTest = 200)]
    public Property ThePiecesHoldTheLootExactlyAndAreNoMoreThanTheMostUnlessItemsAreMore()
    {
        var loot = Gen.Choose(0, 9).ListOf().Select(counts => counts.Select((n, i) => (Item: $"item{i % 4}", Count: n)).ToList());
        return Prop.ForAll(loot.ToArbitrary(), Gen.Choose(1, 8).ToArbitrary(), (entries, most) =>
        {
            var pieces = SprayRules.Pieces(entries, most);
            Assert.All(pieces, p => Assert.True(p.Count > 0));
            foreach (var item in entries.Select(e => e.Item).Distinct())
                Assert.Equal(entries.Where(e => e.Item == item).Sum(e => e.Count), pieces.Where(p => p.Item == item).Sum(p => p.Count));
            int items = entries.Where(e => e.Count > 0).Count();
            Assert.True(pieces.Count <= Math.Max(most, items));
        });
    }

    [Fact]
    public void EachPieceFliesAwayFromTheStrikerWithinItsSpeedAndLiftAndLandsOnTheFloor()
    {
        foreach (int away in new[] { 1, -1 })
        {
            var w = Room();
            w.M.Items.Spray([("scrap", 3), ("iron", 2)], At(5, 1), away);
            Assert.Equal(Tune.SprayMost - 1, Floor(w).Count);
            Assert.All(Floor(w), s =>
            {
                Assert.Equal(away, Math.Sign(s.Vx));
                Assert.InRange(Math.Abs(s.Vx), Tune.SprayLeast, Tune.SprayFastest);
                Assert.InRange(-s.Vy, Tune.SprayLiftLeast, Tune.SprayLiftMost);
                Assert.Equal(FloorStack.Nobody, s.By);
            });
            w.Run(80);
            Assert.Equal((3, 2), (Of(w, "scrap"), Of(w, "iron")));
            Assert.All(Floor(w), s => Assert.Equal((3, 0, 0), (s.Cell / Width, s.Vx, s.Vy)));
            Assert.All(Floor(w), s => Assert.True(Math.Sign(s.Cell % Width - 5) != -away, $"{s.Cell % Width}"));
        }
    }

    [Fact]
    public void WithNoStrikerPiecesFlyEitherWayByADraw()
    {
        var sides = new HashSet<int>();
        var w = Room();
        for (int i = 0; i < 10; i++) w.M.Items.Spray([("scrap", 6)], At(5, 1), 0);
        foreach (var s in Floor(w)) sides.Add(Math.Sign(s.Vx));
        Assert.Equal([-1, 1], sides.Order());
    }

    [Fact]
    public void PiecesComingToRestInOneCellMergeIntoOneStack()
    {
        var w = Room();
        // Against the left wall: every piece slides to the room's first column.
        w.M.Items.Spray([("scrap", 6)], At(1, 3), -1);
        w.Run(80);
        var s = Assert.Single(Floor(w));
        Assert.Equal(("scrap", 6, At(1, 3)), (s.Item, s.Count, s.Cell));
    }

    [Fact]
    public void AfloatAPieceFliesAShareOfItsSpeedUpOrDown()
    {
        var w = Room();
        for (int i = 0; i < 8; i++) w.M.Items.Spray([("scrap", 6)], At(20, 2), 1);
        Assert.All(Floor(w), s =>
        {
            Assert.InRange(s.Vx, Tune.SprayLeast * Tune.SpraySpaceShare / 1000, Tune.SprayFastest * Tune.SpraySpaceShare / 1000);
            Assert.InRange(Math.Abs(s.Vy), Tune.SprayLiftLeast * Tune.SpraySpaceShare / 1000, Tune.SprayLiftMost * Tune.SpraySpaceShare / 1000);
        });
        Assert.Contains(Floor(w), s => s.Vy > 0);
        Assert.Contains(Floor(w), s => s.Vy < 0);
    }
}
