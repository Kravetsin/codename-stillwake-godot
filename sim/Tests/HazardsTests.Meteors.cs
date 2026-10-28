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

    /// <summary>A meteor's strike seen from outside: where, how hard, the strength of the wall it met, whether it broke, its tick.</summary>
    private sealed record Strike(int Cell, int Power, int Strength, bool Broke, int Tick);

    /// <summary>Runs <paramref name="w"/> till <paramref name="count"/> meteors have struck; with the ticks each was first seen coming.</summary>
    private static (List<Strike> Strikes, List<int> Seen) Fall(Scenario w, int count)
    {
        var strikes = new List<Strike>();
        var seen = new List<int>();
        for (int tick = 1; strikes.Count < count; tick++)
        {
            Assert.True(tick < count * (Shower.EveryMax + Shower.Warning + 2), "meteors stopped falling");
            var coming = w.M.Hazards.Incoming;
            int strength = coming is { } m ? w.M.Grid.WallAt(m.Cell)?.Strength ?? 0 : 0;
            w.Run();
            if (coming == null && w.M.Hazards.Incoming != null) seen.Add(tick);
            if (coming is { Left: 1 } hit) strikes.Add(new Strike(hit.Cell, hit.Power, strength, w.M.Grid.WallAt(hit.Cell) == null, tick));
        }
        return (strikes, seen);
    }

    /// <summary>Eight rows of scrap at x 2, iron built before it at once at x 3 in every other row, open to the right; a tick run.</summary>
    private static Scenario Hull()
    {
        var w = new Scenario(Layout.Parse(string.Join('\n', Enumerable.Repeat("..#..", 8))) with { Meteors = true });
        w.Do(new SetInstant(true));
        for (int y = 0; y < 8; y += 2) w.Do(new PlacePlan(y * 5 + 3, Placement.Wall("iron")));
        w.Do(new SetInstant(false)).Run();
        Assert.Empty(w.Refusals);
        return w;
    }

    [Fact]
    public void ComeEveryFourToSixMinutesSeenTwoSecondsBeforeTheyStrike()
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
    public void BreakAWallOnlyWhenTheirPowerReachesItsStrengthLeavingItPlanned()
    {
        var w = Hull();
        var (strikes, _) = Fall(w, 10);
        Assert.All(strikes, s => Assert.Equal(s.Power >= s.Strength, s.Broke));
        Assert.All(strikes, s => Assert.InRange(s.Power, 1, 6));
        Assert.Contains(strikes, s => s.Broke);
        Assert.Contains(strikes, s => !s.Broke);
        // Each wall broken is planned again as it was; nothing else is planned.
        var broken = strikes.Where(s => s.Broke).ToList();
        Assert.Equal(broken.Count, w.M.Building.Plans.Count);
        Assert.All(broken, s => Assert.Equal(
            Placement.Wall(s.Strength == Content.Materials.Get("iron").Strength ? "iron" : "scrap"),
            Assert.Single(w.M.Building.PlansAt(s.Cell)).Place));
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
