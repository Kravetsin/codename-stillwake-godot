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
    internal static Scenario Landed(Scenario w) => w.Do(new OpenSurface(Content.PlanetTypes.Small.Id)).Run();

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
                int most = shower.Reach, cx = m.Cell % grid.Width, cy = m.Cell / grid.Width;
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

    /// <summary>
    /// Twenty-four rows of an iron hull four thick, 500 a cell to break: a meteor before anyone lands breaks a cell or two of it at most,
    /// never through, so they keep striking it.
    /// </summary>
    private static Scenario Hull() =>
        new(Layout.Parse(string.Join('\n', Enumerable.Repeat("..####..", 24))).WithMaterials(string.Join('\n', Enumerable.Repeat("nnnnnnnn", 24)))
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
    public void FallNoneWhileCalmedFromTheDevPanelThenStartOverOnceStirred()
    {
        var w = Hull().Run(Weak.First);
        Assert.False(w.M.Hazards.Calm);
        Assert.NotNull(w.M.Hazards.Incoming);
        // Calmed with a meteor seen coming: it never strikes, none other is drawn, and a save keeps it calm.
        w.Do(new SetCalm(true)).Run().Reload();
        Assert.True(w.M.Hazards.Calm);
        for (int t = 0; t < Weak.First + Weak.EveryMax + Weak.Warning; t++)
        {
            Assert.Null(w.M.Hazards.Incoming);
            w.Run();
        }
        Assert.Empty(w.Refusals);
        // Stirred: the first is seen coming a whole First on, the command's tick counted, as in a new world.
        w.Do(new SetCalm(false)).Run(Weak.First - 1);
        Assert.False(w.M.Hazards.Calm);
        Assert.Null(w.M.Hazards.Incoming);
        w.Run();
        Assert.NotNull(w.M.Hazards.Incoming);
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

    /// <summary>What a scrap wall costs a meteor, in full (damage-scale): 400.</summary>
    private static readonly int Scrap = Content.Materials.Get("scrap").Strength;

    /// <summary>The front cracks, cell and damage.</summary>
    private static (int Cell, int Damage)[] Cracks(Scenario w) => [.. w.M.Building.Cracks.Where(c => !c.Back).Select(c => (c.Cell, c.Damage))];

    /// <summary>
    /// Before anyone lands (2026-10-29-weak-meteors-till-landing), on a row of scrap: power 2's burst, short of the 400 the wall struck
    /// costs, cracks it by all of itself; power 3 breaks it and cracks the next by what is left past the step.
    /// </summary>
    [Fact]
    public void CrackAScrapWallBeforeAnyoneLandsAndBreakItFromPowerThree()
    {
        var weak = Row("..##############");
        Assert.Empty(StrikeAt(weak, 15, 2, landed: false).Walls);
        Assert.Equal([(15, (HazardsModule.Charge(2, Weak)))], Cracks(weak));
        var strong = Row("..##############");
        Assert.Equal([15], StrikeAt(strong, 15, 3, landed: false).Walls);
        Assert.Equal([(14, (HazardsModule.Charge(3, Weak) - Scrap - Content.Damage.Step))], Cracks(strong));
    }

    /// <summary>Iron, 500 a cell to break (damage-scale): power 2 cracks it by its 340, power 3 breaks it.</summary>
    [Fact]
    public void CrackIronBeforeAnyoneLandsAndBreakItFromPowerThree()
    {
        var w = Row("......", 2, 3, 4, 5);
        Assert.Empty(StrikeAt(w, 5, 2, landed: false).Walls);
        Assert.Equal([(5, HazardsModule.Charge(2, Weak))], Cracks(w));
        Assert.Equal([5], StrikeAt(Row("......", 2, 3, 4, 5), 5, 3, landed: false).Walls);
    }

    /// <summary>
    /// A meteor of <paramref name="power"/> at the nine-wide <see cref="Room"/>'s right face on drawn row 2, its hull two cells thick
    /// (0104), and the tick run.
    /// </summary>
    private static Scenario HitRoom(Scenario w, int power) => w.Do(new MeteorHit(w.At(8, 2) + 1, power)).Run();

    /// <summary>Whether <see cref="Room"/> is open to nothing.</summary>
    private static bool Sealed(Scenario w) => w.M.Rooms.RoomById(w.M.Rooms.RoomAt(w.At(4, 2))!.Value)!.Openings.Count == 0;

    /// <summary>
    /// On a scrap hull two thick: power 4 holes its outer cell, short of the 802 to reach the inner one; 5 and up, one in five once
    /// landed, breach it alone, and what they break is lost.
    /// </summary>
    [Fact]
    public void BreachAScrapHullTwoThickAloneFromPowerFiveLosingWhatTheyBreak()
    {
        var holed = HitRoom(new Scenario(Room), 4);
        Assert.Null(holed.M.Grid.WallAt(holed.At(8, 2) + 1));
        Assert.True(Sealed(holed));
        var breached = HitRoom(new Scenario(Room), 5);
        Assert.False(Sealed(breached));
        Assert.Empty(breached.M.Items.Floor);
        Assert.Equal(20, Shower.Powers.Where(p => p.Power >= 5).Sum(p => p.Weight) * 100 / Shower.Powers.Sum(p => p.Weight));
    }

    /// <summary>
    /// A second on a cracked patch within the heal time breaches (unified-damage): power 2 cracks the outer cell by 340, leaving 60 to
    /// break it; ten seconds on, power 3 pays that and the inner's 402. Once the crack has healed, the same pair only holes the outer.
    /// </summary>
    [Fact]
    public void BreachAScrapHullWithASecondOnTheCrackTheFirstLeftWithinTheHealTime()
    {
        var w = HitRoom(new Scenario(Room), 2);
        Assert.True(Sealed(w));
        Assert.Equal([(w.At(8, 2) + 1, (HazardsModule.Charge(2, Shower)))], Cracks(w));
        Assert.False(Sealed(HitRoom(w.Run(10 * World.TicksPerSecond), 3)));
        var healed = HitRoom(new Scenario(Room), 2).Run(Content.Builds.CrackHeals);
        Assert.Empty(Cracks(healed));
        Assert.True(Sealed(HitRoom(healed, 3)));
    }

    /// <summary>
    /// A machine a meteor pays for breaks as to a charge, but lost (unified-damage): what it held spills at its cell, its cost goes.
    /// A crusher beside a scrap wall in bedrock, the wall struck by the strongest: 400 for the wall, 402 more for the crusher.
    /// </summary>
    [Fact]
    public void LoseTheCostOfAMachineTheyBreakSpillingWhatItHeld()
    {
        var spec = Content.Machines.Get("crusher");
        int wide = spec.Width + 4;
        var rows = new List<string> { new('#', wide) };
        for (int y = 0; y < spec.Height; y++) rows.Add("#." + new string(spec.Char!.Value, spec.Width) + "#.");
        rows.Add(new string('#', wide));
        var rock = rows.Select((r, y) => y == 0 || y == rows.Count - 1 ? new string('b', wide) : "b" + new string('.', wide - 1)).ToList();
        var w = new Scenario(Layout.Parse(rows).WithMaterials(rock)).GiveItems(0, ("asteroidOre", 4)).Run();
        var crusher = w.M.Grid.MachinesOf("crusher").Single();
        int slot = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "asteroidOre");
        w.Do(new Items.Move(new Items.Place(slot), new Items.Target(0, crusher.Id), 4)).Run();
        Assert.Empty(w.Refusals);
        w.Do(new MeteorHit(spec.Height * w.M.Grid.Width + 2 + spec.Width, 7)).Run();
        Assert.Empty(w.M.Grid.MachinesOf("crusher"));
        Assert.Equal(4, w.M.Items.Floor.Where(s => s.Item == "asteroidOre").Sum(s => s.Count));
        Assert.All(spec.Cost, c => Assert.DoesNotContain(w.M.Items.Floor, s => s.Item == c.Item));
    }

    /// <summary>
    /// A door no longer stops a meteor (unified-damage): it takes a burst as a wall does. Three cells in from the wall struck, what a
    /// power 5 has left there, 444 of the 500 a door costs, cracks it at its head.
    /// </summary>
    [Fact]
    public void CrackADoorInReachByWhatIsLeftThere()
    {
        var w = new Scenario(Layout.Parse("....DD..#\n....DD..#")).Do(new MeteorHit(17, 5)).Run();
        Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(5));
        var door = Assert.Single(Cracks(w), c => w.M.Grid.StructureAt(c.Cell) == Structure.DoorClosed);
        Assert.Equal(HazardsModule.Charge(5, Shower) - Scrap - 3 * Content.Damage.Step, door.Damage);
    }

    /// <summary>
    /// A meteor breaks the back wall behind each cell it enters where what is left there pays for it (unified-damage); a charge never
    /// does, whoever's. Power 2's 340 into soil, 40 a cell and 42 a step on: walls and back walls four out.
    /// </summary>
    [Fact]
    public void BreakTheBackWallsBehindWhatTheyEnterAsNoChargeDoes()
    {
        static Scenario Soil() => new(Layout.Parse("..######").WithMaterials("..ssssss") with { Meteors = true });
        var (walls, backs) = StrikeAt(Soil(), 7, 2);
        Assert.Equal([3, 4, 5, 6, 7], walls);
        Assert.Equal(walls, backs);
        var charged = Soil();
        Assert.True(charged.M.Building.Burst(FloorStack.Nobody, 7, HazardsModule.Charge(2, Shower), Shower.Reach, Spoils.Drop));
        Assert.All(walls, c => Assert.Null(charged.M.Grid.WallAt(c)));
        Assert.All(Enumerable.Range(0, 8), c => Assert.NotNull(charged.M.Grid.BackAt(c)));
    }

    /// <summary>
    /// A meteor of <paramref name="power"/> striking <paramref name="cell"/> on the next tick, set through the save, and that tick run:
    /// the walls and the back walls it broke, ascending.
    /// </summary>
    internal static (List<int> Walls, List<int> Backs) StrikeAt(Scenario w, int cell, int power, bool landed = true)
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

    private static string Seen(IEnumerable<Strike> strikes) =>
        string.Join("; ", strikes.Select(s => $"power {s.Power} at {s.Cell} (strength {s.Strength}): {s.Gone.Count} gone"));

    /// <summary>Strikes on the hull's scrap: its iron is too strong for many, and stands.</summary>
    private static IEnumerable<Strike> OnScrap(List<Strike> strikes) => strikes.Where(s => s.Strength == Content.Materials.Get("scrap").Strength);

    /// <summary>
    /// The new game's wreck, its hull scrap two thick, before anyone lands and after: a strike on scrap breaks the cell struck exactly
    /// when its burst pays the 400, and else cracks it, nothing gone. Before landing a hole is one a castaway patches: at most the
    /// strongest's eight, the face three up and down and the cell behind.
    /// </summary>
    [Fact]
    public void BreakTheStartingWrecksScrapStruckOnlyWhenTheirBurstPaysForItAndCrackItElse()
    {
        foreach (var w in new[] { new Scenario(Registry.Wreck), Landed(new Scenario(Registry.Wreck)) })
        {
            var shower = w.M.Hazards.Shower!;
            var (strikes, _) = Fall(w, 8);
            Assert.NotEmpty(OnScrap(strikes));
            Assert.All(OnScrap(strikes), s => Assert.Equal(HazardsModule.Charge(s.Power, shower) >= Scrap, s.Broke));
            Assert.All(OnScrap(strikes).Where(s => !s.Broke), s => Assert.Empty(s.Gone));
            if (shower == Weak) Assert.True(OnScrap(strikes).All(s => s.Gone.Count <= 8), Seen(strikes));
        }
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

    /// <summary>
    /// From the dev panel with no power, a meteor breaks the wall it hits alone, in full (damage-scale): a cracked scrap wall, alloy
    /// beside it untouched till it is hit in turn.
    /// </summary>
    [Fact]
    public void BreakTheWallHitFromTheDevPanelAloneCrackedOrAlloy()
    {
        var w = new Scenario(Layout.Parse("..##\n..##").WithMaterials("..wa\n..wa"));
        w.Do(new MeteorHit(2, 2)).Run();
        Assert.Equal([(2, HazardsModule.Charge(2, Shower))], Cracks(w));
        w.Do(new MeteorHit(2)).Run();
        Assert.Null(w.M.Grid.WallAt(2));
        Assert.NotNull(w.M.Grid.WallAt(3));
        Assert.Empty(Cracks(w));
        w.Do(new MeteorHit(3)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.WallAt(3));
        Assert.Equal(Weak.PerPower, Shower.PerPower);
    }

    [Fact]
    public void StrikeTheFirstThingFromTheRightInARowOnlyWhenItIsABreakableWallInTheZone()
    {
        // Column 11 is out of the zone; a door shields row 1, bedrock row 2. Rows 0 and 3 hold iron enough to outlast what breaks.
        var layout = Layout.Parse("########..##\n#######.D..#\n############\n########...#")
            .WithMaterials("nnnnnnnnnnnn\nnnnnnnnnnnnn\nnnnnnnnnnnbn\nnnnnnnnnnnnn") with
        { Zone = new Box(0, 0, 10, 3), Meteors = true };
        var (strikes, _) = Fall(new Scenario(layout), 12);
        Assert.Equal([0, 3], strikes.Select(s => s.Cell / 12).Distinct().Order());
        Assert.Contains(strikes, s => s.Cell == 10);
        Assert.Contains(strikes, s => s.Cell == 3 * 12 + 7);
        // Each the first wall from the right in its row within the zone, as the row stood.
        Assert.All(strikes, s => Assert.Equal(s.Cell, s.Before.Where(b => b.Cell / 12 == s.Cell / 12 && b.Cell % 12 <= 10).Max(b => b.Cell)));
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

    /// <summary>
    /// The README's invariant, for meteors falling by themselves (unified-damage): what they break, a wall, a piece, a machine or a
    /// back wall, turned empty; nothing added, no pipe touched, nothing planned.
    /// </summary>
    [Property(MaxTest = 25)]
    public Property FallingMeteorsOnlyEverBreakAndNeverAdd()
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
                if (grid.StructureAt(c) != Structure.Empty) return false;
            }
            // A back wall may go, as a wall may; none comes.
            for (int c = 0; c < size; c++)
                if (grid.BackLayer[c] != back[c] && grid.BackLayer[c] != Back.None) return false;
            return w.M.Building.Plans.Count == 0 && grid.Machines.All(machines.Contains)
                && w.M.Pipes.Spans.SequenceEqual(spans) && w.Refusals.Count == 0;
        });
    }
}
