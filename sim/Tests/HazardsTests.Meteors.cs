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
    public void BreakEveryWallWithinTheirBlastWhenTheyBreakTheOneTheyStrikeLeavingEachPlanned()
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
        // Each wall broken is planned again as it was; nothing else is planned.
        var gone = strikes.SelectMany(s => s.Before.Where(b => s.Gone.Contains(b.Cell))).ToList();
        Assert.Equal(gone.Count, w.M.Building.Plans.Count);
        Assert.All(gone, b => Assert.Equal(Placement.Wall(b.Wall.Id), Assert.Single(w.M.Building.PlansAt(b.Cell)).Place));
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

    [Fact]
    public void KeepTheHolePlannedAsTheWallItWasKnownOrNotAndRefundNothing()
    {
        var w = new Scenario(Layout.Parse(Room).Doubled() with { Known = [] });
        var held = w.M.Items.Inventory(0).ToArray();
        int hole = RoomCell(8, 2);
        Assert.Equal("building.unknown", w.M.Building.Problem(RoomCell(4, 2), Placement.Wall("scrap")));
        w.Do(new MeteorHit(hole)).Run();
        var ghost = Assert.Single(w.M.Building.Plans);
        Assert.Equal((hole, Placement.Wall("scrap")), (ghost.Cell, ghost.Place));
        Assert.Equal(held, w.M.Items.Inventory(0));
        Assert.Empty(w.M.Items.Floor);
        w.Do(new SetCreative(true));
        for (int i = 0; i < Content.Materials.Get("scrap").BuildTicks; i++) w.Do(new Work(ghost.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(hole));
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

    /// <summary>The README's invariant, for meteors falling by themselves: a wall turned empty and planned as it was, nothing else touched.</summary>
    [Property(MaxTest = 25)]
    public Property FallingMeteorsOnlyEverTurnAWallIntoAnEmptyCellPlannedAsItWas()
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
            var walls = Enumerable.Range(0, size).Select(grid.WallAt).ToArray();
            var spans = w.M.Pipes.Spans.ToArray();
            var back = grid.BackLayer.ToArray();
            var machines = grid.Machines.ToArray();
            w.Run(3 * (Shower.EveryMax + Shower.Warning));
            int broken = 0;
            for (int c = 0; c < size; c++)
            {
                if (grid.StructureAt(c) == structure[c]) continue;
                if (structure[c] != Structure.Wall || grid.StructureAt(c) != Structure.Empty) return false;
                if (w.M.Building.PlansAt(c) is not [{ Progress: 0 } plan] || plan.Place != Placement.Wall(walls[c]!.Id)) return false;
                broken++;
            }
            return w.M.Building.Plans.Count == broken && grid.BackLayer.SequenceEqual(back) && grid.Machines.SequenceEqual(machines)
                && w.M.Pipes.Spans.SequenceEqual(spans) && w.Refusals.Count == 0;
        });
    }
}
