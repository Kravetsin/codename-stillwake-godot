using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Tests;

/// <summary>Meteors falling by themselves (2026-10-28-hull-meteors).</summary>
public partial class HazardsTests
{
    private static readonly Content.Meteors.Shower Shower = Content.Meteors.StartingSpace;

    /// <summary>
    /// A meteor's strike seen from outside: where, how hard, its tick; the walls standing round it just before, ascending, and the
    /// cells of those gone after.
    /// </summary>
    private sealed record Strike(int Cell, int Power, int Tick, IReadOnlyList<(int Cell, Content.Material Wall)> Before, IReadOnlyList<int> Gone)
    {
        public int Strength => Before.FirstOrDefault(b => b.Cell == Cell).Wall?.Strength ?? 0;
        public bool Broke => Gone.Contains(Cell);
    }

    /// <summary>Runs <paramref name="w"/> till <paramref name="count"/> meteors have struck; with the ticks each was first seen coming.</summary>
    private static (List<Strike> Strikes, List<int> Seen) Fall(Scenario w, int count)
    {
        var strikes = new List<Strike>();
        var seen = new List<int>();
        var grid = w.M.Grid;
        for (int tick = 1; strikes.Count < count; tick++)
        {
            Assert.True(tick < count * (Shower.EveryMax + Shower.Warning + 2), "meteors stopped falling");
            var coming = w.M.Hazards.Incoming;
            var before = new List<(int Cell, Content.Material Wall)>();
            if (coming is { Left: 1 } m)
            {
                int most = HazardsModule.Farthest(m.Power, Shower), cx = m.Cell % grid.Width, cy = m.Cell / grid.Width;
                for (int y = Math.Max(0, cy - most); y <= Math.Min(grid.Height - 1, cy + most); y++)
                    for (int x = Math.Max(0, cx - most); x <= Math.Min(grid.Width - 1, cx + most); x++)
                        if (grid.WallAt(y * grid.Width + x) is { } wall) before.Add((y * grid.Width + x, wall));
            }
            w.Run();
            if (coming == null && w.M.Hazards.Incoming != null) seen.Add(tick);
            if (coming is { Left: 1 } hit)
                strikes.Add(new Strike(hit.Cell, hit.Power, tick, before, [.. before.Where(b => grid.WallAt(b.Cell) == null).Select(b => b.Cell)]));
        }
        return (strikes, seen);
    }

    /// <summary>Twenty-four rows of a scrap hull four thick, its right face iron in every other row, built at once; a tick run.</summary>
    private static Scenario Hull()
    {
        const int width = 8, rows = 24;
        var w = new Scenario(Layout.Parse(string.Join('\n', Enumerable.Repeat("..####..", rows))) with { Meteors = true });
        w.Do(new SetInstant(true));
        for (int y = 0; y < rows; y += 2) w.Do(new PlacePlan(y * width + 6, Placement.Wall("iron")));
        w.Do(new SetInstant(false)).Run();
        Assert.Empty(w.Refusals);
        return w;
    }

    [Fact]
    public void ComeEveryFourToSixMinutesSeenThreeSecondsBeforeTheyStrike()
    {
        var (strikes, seen) = Fall(Hull(), 6);
        // Hull ran the first tick: Fall counts from the second.
        Assert.InRange(seen[0] + 1, Shower.EveryMin, Shower.EveryMax);
        for (int i = 0; i < strikes.Count; i++)
        {
            Assert.Equal(seen[i] + Shower.Warning, strikes[i].Tick);
            if (i + 1 < seen.Count) Assert.InRange(seen[i + 1] - strikes[i].Tick, Shower.EveryMin, Shower.EveryMax);
        }
    }

    [Fact]
    public void BreakEveryWallWithinTheirBlastWhenTheyBreakTheOneTheyStrike()
    {
        var w = Hull();
        var (strikes, _) = Fall(w, 10);
        int width = w.Layout.Width;
        foreach (var s in strikes)
        {
            Assert.InRange(s.Power, 2, 7);
            // A wall nearer the struck one than Hole + (power - its strength) × Blast cells goes; a struck wall that holds stops it whole.
            int cx = s.Cell % width, cy = s.Cell / width;
            var blast = s.Before.Where(b =>
            {
                int dx = b.Cell % width - cx, dy = b.Cell / width - cy;
                int reach = s.Power < b.Wall.Strength ? 0 : Shower.Hole + (s.Power - b.Wall.Strength) * Shower.Blast;
                return reach > 0 && dx * dx + dy * dy < reach * reach;
            }).Select(b => b.Cell);
            Assert.Equal(s.Power >= s.Strength ? blast : [], s.Gone);
        }
        Assert.Contains(strikes, s => s.Gone.Count > 1);
        Assert.Contains(strikes, s => !s.Broke);
        // The holes are left unplanned (holes-not-planned).
        Assert.Empty(w.M.Building.Plans);
    }

    /// <summary>The bug from play: on the new game's wreck, its hull scrap, meteors broke one cell or none.</summary>
    [Fact]
    public void TearAnAreaOutOfTheStartingWrecksScrapHullOnEveryStrike()
    {
        var (strikes, _) = Fall(new Scenario(Registry.Wreck), 10);
        var seen = string.Join("; ", strikes.Select(s => $"power {s.Power} at {s.Cell} (strength {s.Strength}): {s.Gone.Count} gone"));
        Assert.True(strikes.All(s => s.Gone.Count >= 6), seen);
    }

    [Fact]
    public void StrikeTheFirstThingFromTheRightInARowOnlyWhenItIsABreakableWallInTheZone()
    {
        // Alloy never breaks to these, so the targets stay. Column 5 is out of the zone; a door shields row 1, bedrock row 2.
        var layout = Layout.Parse("#...##\n#..D.#\n#...##\n#....#").WithMaterials("aaaaaa\naaaaaa\naaaaba\naaaaaa")
            with
        { Zone = new Box(0, 0, 4, 3), Meteors = true };
        var (strikes, _) = Fall(new Scenario(layout), 12);
        Assert.Equal([4, 18], strikes.Select(s => s.Cell).Distinct().Order());
        Assert.DoesNotContain(strikes, s => s.Broke);
    }

    /// <summary>An iron wall broken is left a hole, unplanned, and refunds nothing: the player may patch it with scrap for now.</summary>
    [Fact]
    public void LeaveTheHoleUnplannedForThePlayerToPatchWithWhatTheyHaveAndRefundNothing()
    {
        int hole = RoomCell(8, 2);
        var w = new Scenario(Room).Do(new SetInstant(true)).Do(new MeteorHit(hole)).Run();
        w.Do(new PlacePlan(hole, Placement.Wall("iron"))).Do(new SetInstant(false)).Run();
        Assert.Equal("iron", w.M.Grid.WallAt(hole)?.Id);
        var held = w.M.Items.Inventory(0).ToArray();
        w.Do(new MeteorHit(hole)).Run();
        Assert.Null(w.M.Grid.WallAt(hole));
        Assert.Empty(w.M.Building.Plans);
        Assert.Equal(held, w.M.Items.Inventory(0));
        Assert.Empty(w.M.Items.Floor);
        w.Do(new PlacePlan(hole, Placement.Wall("scrap"))).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Placement.Wall("scrap"), Assert.Single(w.M.Building.Plans).Place);
    }

    [Fact]
    public void FallOnlyOnTheShipOfAWorldWhoseMapAsksForThem()
    {
        static bool Comes(Layout layout)
        {
            var w = new Scenario(layout);
            for (int i = 0; i <= Shower.EveryMax; i++)
                if (w.Run().M.Hazards.Incoming != null) return true;
            return false;
        }
        var hull = Layout.Parse("..#..\n..#..");
        Assert.True(Comes(hull with { Meteors = true }));
        Assert.False(Comes(hull));
        Assert.False(Comes(hull.OnPlanet() with { Meteors = true }));
    }

    [Fact]
    public void KeepAMeteorSeenComingThroughASave()
    {
        var w = Hull();
        while (w.M.Hazards.Incoming == null) w.Run();
        var coming = w.M.Hazards.Incoming!;
        w.Reload();
        Assert.Equal(coming, w.M.Hazards.Incoming);
        var (strikes, _) = Fall(w, 1);
        Assert.Equal((coming.Cell, coming.Power), (strikes[0].Cell, strikes[0].Power));
    }

    /// <summary>The README's invariant, for meteors falling by themselves: a wall turned empty, nothing else touched, nothing planned.</summary>
    [Property(MaxTest = 25)]
    public Property FallingMeteorsOnlyEverTurnAWallIntoAnEmptyCell()
    {
        var gen =
            from width in Gen.Choose(1, 10)
            from height in Gen.Choose(1, 8)
            from cells in Gen.Elements(Chars).ArrayOf(width * height)
            from under in Gen.Elements(' ', '-').ArrayOf(width * height)
            from seed in Gen.Choose(1, 1000)
            select (width, height, cells, under, seed: (uint)seed);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            string[] Rows(char[] chars) =>
                Enumerable.Range(0, t.height).Select(y => new string(chars, y * t.width, t.width)).ToArray();
            var w = new Scenario(Layout.Parse(Rows(t.cells), Rows(t.under)) with { Meteors = true }, t.seed);
            var grid = w.M.Grid;
            int size = t.width * t.height;
            var structure = grid.StructureLayer.ToArray();
            var spans = w.M.Pipes.Spans.ToArray();
            var back = grid.BackLayer.ToArray();
            var machines = grid.Machines.ToArray();
            w.Run(3 * (Shower.EveryMax + Shower.Warning));
            for (int c = 0; c < size; c++)
            {
                if (grid.StructureAt(c) == structure[c]) continue;
                if (structure[c] != Structure.Wall || grid.StructureAt(c) != Structure.Empty) return false;
            }
            return w.M.Building.Plans.Count == 0 && grid.BackLayer.SequenceEqual(back) && grid.Machines.SequenceEqual(machines)
                && w.M.Pipes.Spans.SequenceEqual(spans) && w.Refusals.Count == 0;
        });
    }
}
