using System.Text.Json.Nodes;
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
    /// <summary>The ship's shower once a hero has landed, and the weaker one till then (2026-10-29-weak-meteors-till-landing).</summary>
    private static readonly Content.Meteors.Shower Shower = Content.Meteors.StartingSpace, Weak = Content.Meteors.BeforeLanding;

    /// <summary>Opens a planet on <paramref name="w"/>, as a hero landing does: the landed shower falls from its next tick.</summary>
    private static Scenario Landed(Scenario w) => w.Do(new OpenSurface(Content.PlanetTypes.Small.Id)).Run();

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
            var shower = w.M.Hazards.Shower!;
            Assert.True(tick < Weak.First + count * (shower.EveryMax + shower.Warning + 2), "meteors stopped falling");
            var coming = w.M.Hazards.Incoming;
            var before = new List<(int Cell, Content.Material Wall)>();
            if (coming is { Left: 1 } m)
            {
                int most = HazardsModule.Farthest(m.Power, shower), cx = m.Cell % grid.Width, cy = m.Cell / grid.Width;
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

    /// <summary>Twenty-four rows of an alloy hull four thick, which no meteor here breaks: they keep striking it.</summary>
    private static Scenario Hull() =>
        new(Layout.Parse(string.Join('\n', Enumerable.Repeat("..####..", 24))).WithMaterials(string.Join('\n', Enumerable.Repeat("aaaaaaaa", 24)))
            with
        { Meteors = true });

    [Fact]
    public void ComeAQuarterOfAnHourInThenEveryFourToSixMinutesSeenThreeSecondsBeforeTheyStrike()
    {
        var (strikes, seen) = Fall(Hull(), 6);
        Assert.Equal(15 * 60 * World.TicksPerSecond, Weak.First);
        Assert.Equal(Weak.First, seen[0]);
        for (int i = 0; i < strikes.Count; i++)
        {
            Assert.Equal(seen[i] + Weak.Warning, strikes[i].Tick);
            if (i + 1 < seen.Count) Assert.InRange(seen[i + 1] - strikes[i].Tick, Weak.EveryMin, Weak.EveryMax);
        }
    }

    [Fact]
    public void FallWeakTillAPlanetIsOpenedThenAsTheLandedShower()
    {
        var w = Hull();
        Assert.Equal(Weak, w.M.Hazards.Shower);
        Assert.Equal(Shower, Landed(w).M.Hazards.Shower);
        Assert.Equal(Shower, w.Reload().M.Hazards.Shower);
        Assert.Null(new Scenario(Layout.Parse("..#..").OnPlanet() with { Meteors = true }).M.Hazards.Shower);
    }

    /// <summary>
    /// Before anyone lands (2026-10-29-weak-meteors-till-landing): a blast of 7 at power 2 breaks the scrap struck alone, 16 at 5 two
    /// more along a row; iron stands to all but 5, which breaks the iron struck and nothing past it.
    /// </summary>
    [Fact]
    public void BreakLittleBeforeAnyoneLandsIronOnlyAtTheirStrongestAndOnlyTheCellStruck()
    {
        Assert.Equal([15], StrikeAt(Row("..##############"), 15, 2, landed: false).Walls);
        Assert.Equal([13, 14, 15], StrikeAt(Row("..##############"), 15, 5, landed: false).Walls);
        Assert.Empty(StrikeAt(Row("......", 2, 3, 4, 5), 5, 4, landed: false).Walls);
        Assert.Equal([5], StrikeAt(Row("......", 2, 3, 4, 5), 5, 5, landed: false).Walls);
    }

    /// <summary>
    /// A meteor of <paramref name="power"/> striking <paramref name="cell"/> on the next tick, set through the save, and that tick run:
    /// the walls and the back walls it broke, ascending.
    /// </summary>
    private static (List<int> Walls, List<int> Backs) StrikeAt(Scenario w, int cell, int power, bool landed = true)
    {
        if (landed) Landed(w);
        var grid = w.M.Grid;
        var cells = Enumerable.Range(0, grid.Width * grid.Height).ToList();
        var walls = cells.Where(c => grid.WallAt(c) != null).ToList();
        var backs = cells.Where(c => grid.BackAt(c) != null).ToList();
        var root = JsonNode.Parse(w.World.Save())!;
        root["slices"]!["hazards"]!["incoming"] = new JsonObject { ["cell"] = cell, ["power"] = power, ["left"] = 1 };
        w.Reload(root.ToJsonString()).Run();
        var after = w.M.Grid;
        return ([.. walls.Where(c => after.WallAt(c) == null)], [.. backs.Where(c => after.BackAt(c) == null)]);
    }

    /// <summary>One row of <paramref name="row"/> as drawn, a cell a character, meteors falling; iron built at once over <paramref name="iron"/>.</summary>
    private static Scenario Row(string row, params int[] iron)
    {
        var w = new Scenario(Layout.Parse(row) with { Meteors = true }).Do(new SetInstant(true));
        foreach (int cell in iron) w.Do(new PlacePlan(cell, Placement.Wall("iron")));
        w.Do(new SetInstant(false)).Run();
        Assert.Empty(w.Refusals);
        return w;
    }

    [Fact]
    public void TearAScrapHullAsFarAsTheirBlastPaysForBreakingTheBackWallsItLeavesSomethingFor()
    {
        // The struck wall costs 4 of the blast, each wall more a step and its strength, 6: 22 at power 2 pays for four.
        var weak = StrikeAt(Row("..##############"), 15, 2);
        Assert.Equal([12, 13, 14, 15], weak.Walls);
        Assert.Equal([13, 14, 15], weak.Backs);
        // 52 at power 7 pays for nine; a back wall breaks where 4 of it is left.
        var strong = StrikeAt(Row("..##############"), 15, 7);
        Assert.Equal([7, 8, 9, 10, 11, 12, 13, 14, 15], strong.Walls);
        Assert.Equal([8, 9, 10, 11, 12, 13, 14, 15], strong.Backs);
    }

    [Fact]
    public void HaveTheirBlastSoakedUpByIronSparingWhatLiesBehindIt()
    {
        // Scrap face, then six iron: each iron costs 12, so the 52 of power 7 dies three into it, where all scrap went nine deep.
        var w = Row("..######......##", 8, 9, 10, 11, 12, 13);
        Assert.Equal([11, 12, 13, 14, 15], StrikeAt(w, 15, 7).Walls);
        Assert.Equal("iron", w.M.Grid.WallAt(10)?.Id);
    }

    [Fact]
    public void BreakNothingWhenTheWallTheyStrikeIsStrongerThanTheirPower()
    {
        var held = StrikeAt(Row("..###.", 5), 5, 4);
        Assert.Empty(held.Walls);
        Assert.Empty(held.Backs);
        Assert.Contains(5, StrikeAt(Row("..###.", 5), 5, 5).Walls);
    }

    /// <summary>A wall stands up to meteors by its armour, not by how hard it is to dig (wall-roles): stone digs at 12, stands at 4.</summary>
    [Fact]
    public void ReadAWallsArmourNotItsHardnessToDig()
    {
        Scenario Stone() => new(Layout.Parse("..###").WithMaterials("..sst") with { Meteors = true });
        Assert.Empty(StrikeAt(Stone(), 4, 3).Walls);
        Assert.Contains(4, StrikeAt(Stone(), 4, 4).Walls);
        // A seal gives way to the weakest, soaking little: power 2 on glass goes as far as on soil.
        var w = new Scenario(Layout.Parse("..#####") with { Meteors = true }).Do(new SetInstant(true));
        for (int x = 2; x < 7; x++) w.Do(new ClearAt(x, "structure")).Do(new PlacePlan(x, Placement.Wall("glass")));
        w.Do(new SetInstant(false)).Run();
        Assert.Equal([2, 3, 4, 5, 6], StrikeAt(w, 6, 2).Walls);
    }

    [Fact]
    public void StopAtADoorItsBlastBreakingTheBackWallsBeforeIt()
    {
        var w = Row("....D...#");
        var (walls, backs) = StrikeAt(w, 8, 7);
        Assert.Equal([8], walls);
        Assert.Equal([5, 6, 7, 8], backs);
        Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(4));
        Assert.Empty(w.M.Building.Plans);
    }

    private static string Seen(IEnumerable<Strike> strikes) =>
        string.Join("; ", strikes.Select(s => $"power {s.Power} at {s.Cell} (strength {s.Strength}): {s.Gone.Count} gone"));

    /// <summary>The bug from play: on the new game's wreck, its hull scrap, meteors broke one cell or none; once landed, an area.</summary>
    [Fact]
    public void TearAnAreaOutOfTheStartingWrecksScrapHullOnEveryStrikeOnceAHeroHasLanded()
    {
        var (strikes, _) = Fall(Landed(new Scenario(Registry.Wreck)), 6);
        Assert.True(OnScrap(strikes).All(s => s.Gone.Count >= 5), Seen(strikes));
    }

    /// <summary>Strikes on the hull's scrap: its iron is too strong for many, and stands.</summary>
    private static IEnumerable<Strike> OnScrap(List<Strike> strikes) => strikes.Where(s => s.Strength == Content.Materials.Get("scrap").Strength);

    /// <summary>
    /// The softlock from play: a strike tore more than a castaway could patch before the next. Till anyone lands each takes a cell or
    /// a few, a corner's most at the strongest: patched in the minutes before the next.
    /// </summary>
    [Fact]
    public void TakeACellOrAFewOfTheStartingWrecksScrapHullBeforeAnyoneLands()
    {
        var (strikes, _) = Fall(new Scenario(Registry.Wreck), 10);
        // Fins on its back stand alone, so a blast takes more of them; nothing a room needs.
        int roof = (Layout.OffsetInWorld(Registry.WreckMap.Width, Registry.WreckMap.Height).Y + 34) * Registry.Wreck.Width;
        Assert.True(OnScrap(strikes).All(s => s.Gone.Count >= 1), Seen(strikes));
        Assert.True(strikes.Where(s => s.Cell >= roof).All(s => s.Gone.Count <= 7), Seen(strikes));
        Assert.True(strikes.Average(s => s.Gone.Count) <= 4, Seen(strikes));
    }

    /// <summary>A hole is patched of a raw scrap chunk (2026-10-29-weak-meteors-till-landing): no furnace, so no air, is needed.</summary>
    [Fact]
    public void LeaveAHoleAHeroPatchesWithARawScrapChunkHoldingNoScrap()
    {
        int hole = RoomCell(8, 2);
        var w = new Scenario(Room).Do(new MeteorHit(hole)).Run();
        w.GiveItems(0, ("scrapChunk", 1));
        Assert.Equal(0, w.M.Items.Inventory(0).ToArray().Where(s => s?.Item == "scrap").Sum(s => s!.Count));
        w.Do(new PlacePlan(hole, Placement.Wall("scrap"))).Run();
        int plan = Assert.Single(w.M.Building.Plans).Id;
        for (int i = 0; i < 200 && w.M.Grid.WallAt(hole) == null; i++) w.Do(new Work(plan)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal("scrap", w.M.Grid.WallAt(hole)?.Id);
        Assert.DoesNotContain(w.M.Items.Inventory(0).ToArray(), s => s?.Item == "scrapChunk");
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
            for (int i = 0; i <= Weak.First + Weak.EveryMax; i++)
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

    /// <summary>The README's invariant, for meteors falling by themselves: a wall or a back wall turned empty, nothing else touched, nothing planned.</summary>
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
            // A back wall may go, as a wall may; none comes.
            for (int c = 0; c < size; c++)
                if (grid.BackLayer[c] != back[c] && grid.BackLayer[c] != Back.None) return false;
            return w.M.Building.Plans.Count == 0 && grid.Machines.SequenceEqual(machines)
                && w.M.Pipes.Spans.SequenceEqual(spans) && w.Refusals.Count == 0;
        });
    }
}
