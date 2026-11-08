using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Grid;
using Xunit;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Sim.Tests;

/// <summary>Liquid in the cells (flowing-liquids): maps read a character a cell, their water drawn beside them.</summary>
public class LiquidsTests
{
    /// <summary>A world on <paramref name="map"/>, a character a cell, with the water of <paramref name="water"/> (<c>Legend.LiquidOf</c>).</summary>
    private static Scenario Wet(string map, string water, Box? zone = null) =>
        new(Layout.Parse(map).WithLiquids(water) with { Zone = zone });

    private static int Total(Scenario w)
    {
        int total = 0;
        foreach (byte b in w.M.Liquids.Layer) total += L.AmountOf(b);
        return total;
    }

    private static int Amount(Scenario w, int x, int y) => w.M.Liquids.AmountAt(y * w.Layout.Width + x);

    /// <summary>The amounts along row <paramref name="y"/> from <paramref name="from"/> to <paramref name="to"/>.</summary>
    private static int[] Row(Scenario w, int y, int from, int to) => [.. Enumerable.Range(from, to - from + 1).Select(x => Amount(w, x, y))];

    [Fact]
    public void ACellOfWaterSpreadThinOverAWideFloorDriesUp()
    {
        var w = Wet("""
            #########
            #.......#
            #.......#
            #.......#
            #########
            """, """
            .........
            ....w....
            .........
            .........
            .........
            """).Run(60);
        Assert.Equal(0, Total(w));
        Assert.Equal(0, w.M.Liquids.AwakeChunks);
    }

    [Fact]
    public void AShallowPuddleLevelsAlongItsRowInsteadOfLyingAsARamp()
    {
        var w = Wet("""
            #########
            #.......#
            #.......#
            #########
            """, """
            .........
            .........
            .7654321.
            .........
            """).Run(60);
        var floor = Row(w, 2, 1, 7);
        Assert.Equal(28, floor.Sum());
        Assert.InRange(floor.Max() - floor.Min(), 0, 1);
        Assert.Equal(0, w.M.Liquids.AwakeChunks);
    }

    [Fact]
    public void APoolsThinTopRowNeverDries()
    {
        var w = Wet("""
            #######
            #.....#
            #.....#
            #######
            """, """
            .......
            .11111.
            .wwwww.
            .......
            """).Run(60);
        Assert.Equal(5 * L.Full + 5, Total(w));
    }

    [Fact]
    public void AFilmOnALedgeWithNoDropDriesButADeeperPuddleStays()
    {
        // A film spread on the left dries; three units a cell walled in on the right stay.
        var w = Wet("""
            #########
            #....#..#
            #....#..#
            #########
            """, """
            .........
            .........
            .2....33.
            .........
            """).Run(60);
        Assert.Equal(6, Total(w));
        Assert.Equal([3, 3], Row(w, 2, 6, 7));
    }

    [Fact]
    public void FallsACellATickThroughAnEmptyShaft()
    {
        var w = Wet("""
            ###
            #.#
            #.#
            #.#
            #.#
            #.#
            #.#
            #.#
            #.#
            #.#
            ###
            """, """
            ...
            .w.
            """).Run();
        Assert.Equal(L.Full, Amount(w, 1, 2));
        Assert.Equal(L.Full, Total(w));
    }

    [Fact]
    public void APoolPouringDownAShaftFallsAsAWholeColumnWithNoGaps()
    {
        var w = Wet("""
            #######
            #.....#
            #.....#
            #.....#
            ###.###
            ###.###
            ###.###
            ###.###
            ###.###
            ###.###
            ###.###
            ###.###
            ###.###
            ###.###
            ###.###
            #######
            """, """
            .......
            .wwwww.
            .wwwww.
            .wwwww.
            """);
        for (int tick = 0; tick < 12; tick++)
        {
            w.Run();
            int head = Enumerable.Range(4, 11).LastOrDefault(y => Amount(w, 3, y) > 0);
            Assert.Equal(Math.Min(14, 4 + tick), head);
            Assert.All(Enumerable.Range(4, head - 4 + 1), y => Assert.True(Amount(w, 3, y) > 0, $"tick {tick}: a gap at row {y} over the head at {head}"));
        }
    }

    [Fact]
    public void ASpillOffAPoolsEdgeIntoAShaftFallsAsAWholeColumn()
    {
        // A pool whose surface runs on over a wall to a shaft beside it, a cave under the shaft: it spills only at the surface.
        var w = Wet("""
            ##############
            #............#
            #............#
            ##...........#
            ##.#.........#
            ##.#.........#
            ##.#.........#
            ##.#.........#
            ##.#.........#
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            ##.###########
            #............#
            #............#
            #............#
            #............#
            #............#
            ##############
            """, """
            ..............
            ..............
            ..............
            ...wwwwwwwwww.
            ....wwwwwwwww.
            ....wwwwwwwww.
            ....wwwwwwwww.
            ....wwwwwwwww.
            ....wwwwwwwww.
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            ..............
            """);
        for (int tick = 0; tick < 30; tick++)
        {
            w.Run();
            int head = Enumerable.Range(3, 21).LastOrDefault(y => Amount(w, 2, y) > 0);
            Assert.All(Enumerable.Range(4, Math.Max(0, head - 4)), y => Assert.True(Amount(w, 2, y) > 0, $"tick {tick}: a gap at row {y} over the head at {head}"));
            if (tick >= 20) Assert.Equal(23, head);
        }
    }

    [Fact]
    public void APoolDugIntoPoursOutAndItsLevelSinks()
    {
        var w = Wet("""
            ##########
            #....#...#
            #....#...#
            #....#...#
            ##########
            """, """
            ..........
            .wwww.....
            .wwww.....
            .wwww.....
            ..........
            """);
        int before = Total(w);
        w.Do(new ClearAt(3 * 10 + 5, "structure")).Run(300);
        Assert.Equal(before, Total(w));
        Assert.True(Row(w, 3, 6, 8).Sum() > 0, "the water came through the hole");
        Assert.True(Row(w, 1, 1, 4).Sum() < 4 * L.Full, "the pool's top row sank");
    }

    [Fact]
    public void TwoBasinsJoinedAlongTheirSideEndWithin1UnitOfEachOther()
    {
        var w = Wet("""
            ##########
            #....#...#
            #....#...#
            #....#...#
            ##########
            """, """
            ..........
            .wwww.....
            .wwww.....
            .wwww.....
            ..........
            """);
        for (int y = 1; y <= 3; y++) w.Do(new ClearAt(y * 10 + 5, "structure"));
        w.Run(600);
        var surface = Row(w, 2, 1, 8);
        Assert.Equal(96, Total(w));
        for (int x = 1; x < surface.Length; x++) Assert.InRange(Math.Abs(surface[x] - surface[x - 1]), 0, 1);
        Assert.Equal(0, w.M.Liquids.AwakeChunks);
    }

    [Fact]
    public void AUTubeLevelsItsTwoSides()
    {
        var w = Wet("""
            #####
            #.#.#
            #.#.#
            #.#.#
            #...#
            #####
            """, """
            .....
            .w...
            .w...
            .w...
            .w...
            """).Run(200);
        Assert.Equal(32, Total(w));
        Assert.Equal([8, 8, 8], Row(w, 4, 1, 3));
        // The rest, 8 units over the bottom, shared between the sides within a unit, and the tube at rest.
        Assert.InRange(Amount(w, 1, 3), 3, 5);
        Assert.Equal(8, Amount(w, 1, 3) + Amount(w, 3, 3));
        Assert.Equal(0, Amount(w, 1, 2) + Amount(w, 3, 2));
        Assert.Equal(0, w.M.Liquids.AwakeChunks);
    }

    [Fact]
    public void ALakeJoinedAtItsBottomToAShaftFillsItToItsOwnLevel()
    {
        // A lake on the left joined along its bottom to a shaft as tall as it.
        var w = Wet("""
            ##########
            #....###.#
            #....###.#
            #....###.#
            #....###.#
            #........#
            ##########
            """, """
            ..........
            .wwww.....
            .wwww.....
            .wwww.....
            .wwww.....
            ..........
            ..........
            """).Run(600);
        Assert.Equal(16 * L.Full, Total(w));
        // Over the full bottom row the rest stands in the lake and the shaft alike, within a unit.
        int Level(int x)
        {
            int units = 0;
            for (int y = 1; y <= 4; y++) units += Amount(w, x, y);
            return units;
        }
        Assert.InRange(Level(8) - Level(1), -1, 1);
        Assert.True(Level(8) > L.Full, "the shaft filled past its bottom row");
        Assert.Equal(0, w.M.Liquids.AwakeChunks);
    }

    [Fact]
    public void ALoneUnitOnAStepRunsOffItAndNoFilmIsLeft()
    {
        var w = Wet("""
            #######
            #.....#
            #.....#
            #.....#
            ###...#
            ####..#
            #######
            """, """
            .......
            .......
            .......
            ..1....
            .......
            ....33.
            .......
            """).Run(60);
        Assert.Equal(0, Amount(w, 2, 3) + Amount(w, 3, 4));
        Assert.Equal(7, Amount(w, 4, 5) + Amount(w, 5, 5));
        Assert.Equal(0, w.M.Liquids.AwakeChunks);
    }

    [Fact]
    public void PassesLaddersFloorsAndGratesButNotWalls()
    {
        var w = Wet("""
            #####
            #...#
            #H=%#
            #...#
            #####
            """, """
            .....
            .www.
            """).Run(40);
        Assert.Equal(24, Row(w, 3, 1, 3).Sum());
        Assert.Equal(0, Row(w, 1, 1, 3).Sum());
    }

    [Fact]
    public void AClosedDoorHoldsTheWaterAndOpenedLetsItOn()
    {
        // Drawn at 16 px and doubled, as doors are pieces two cells wide.
        var layout = Layout.Parse("""
            ######
            #..D.#
            ######
            """).WithLiquids("""
            ......
            .ww...
            """);
        var w = new Scenario(layout, doubled: true).Run(100);
        int door = w.At(3, 1), past = w.At(4, 1) + w.Layout.Width;
        Assert.Equal(0, w.M.Liquids.AmountAt(past));
        w.Do(new SetDoor(door, true)).Run(200);
        Assert.True(w.M.Liquids.AmountAt(past) > 0, "the water went on through the open door");
        Assert.Equal(4 * L.Full * 2, Total(w));
    }

    [Fact]
    public void AWallBuiltIntoWaterEmptiesOnlyItsOwnCell()
    {
        var w = Wet("""
            #####
            #...#
            #####
            """, """
            .....
            .www.
            """);
        w.Do(new Place(1 * 5 + 2, Placement.Wall())).Run();
        Assert.Equal(0, Amount(w, 2, 1));
        Assert.Equal(2 * L.Full, Total(w));
    }

    [Fact]
    public void WaterPastTheZoneIsGone()
    {
        var w = Wet("""
            ########
            #......#
            ########
            """, """
            ........
            .ww.....
            """, zone: new Box(0, 0, 3, 2)).Run(200);
        Assert.True(Total(w) < 2 * L.Full, "what spread past column 3 went");
        Assert.Equal(0, Row(w, 1, 4, 6).Sum());
    }

    [Fact]
    public void ADrawnMapsWaterInAWallIsDryOnceTheWorldIsMade()
    {
        var w = Wet("""
            ###
            #.#
            ###
            """, """
            .w.
            .w.
            """);
        Assert.Equal(0, Amount(w, 1, 0));
        Assert.Equal(L.Full, Amount(w, 1, 1));
    }

    [Fact]
    public void ASurfaceWithNoLiquidHoldsNoLayerAndWakesNothing()
    {
        var w = new Scenario("""
            #####
            #...#
            #####
            """).Run(5);
        Assert.True(w.M.Liquids.Layer.IsEmpty);
        Assert.Equal(0, w.M.Liquids.AwakeChunks);
    }

    [Fact]
    public void AGridChangeTooBigForTheLogStillEmptiesTheCellsWallsFill()
    {
        // More walls in a tick than the grid's log holds: the liquid looks at every cell again.
        const int size = 260;
        var rows = Enumerable.Range(0, size).Select(y => y == 0 || y == size - 1 ? new string('#', size) : "#" + new string('.', size - 2) + "#");
        var water = new[] { new string('.', size), "." + new string('w', size - 2) + "." };
        var w = new Scenario(Layout.Parse([.. rows]).WithLiquids(string.Join("\n", water)));
        for (int y = 1; y < size - 1; y++)
            for (int x = 1; x < size - 1; x++)
                if (y != 1 || x % 2 == 0) w.Do(new Place(y * size + x, Placement.Wall()));
        Assert.Null(w.Run().M.Grid.ChangedSince(0));
        Assert.Equal(0, Amount(w, 2, 1));
        Assert.Equal(L.Full, Amount(w, 1, 1));
    }

    [Fact]
    public void ASaveLoadedMidFlowRunsOnAsTheWorldKeptRunning()
    {
        var map = """
            ##########
            #....#...#
            #....#...#
            #....#...#
            ##########
            """;
        var water = """
            ..........
            .wwww.....
            .w3w1.....
            .wwww.....
            """;
        var kept = Wet(map, water).Do(new ClearAt(2 * 10 + 5, "structure")).Run(7);
        var loaded = Wet(map, water).Do(new ClearAt(2 * 10 + 5, "structure")).Run(7).Reload();
        for (int t = 0; t < 40; t++)
        {
            kept.Run();
            loaded.Run();
            Assert.Equal(kept.World.Hash(), loaded.World.Hash());
        }
    }

    /// <summary>A closed box of walls with walls and water scattered inside: every rule of the flow meets every other.</summary>
    private static Gen<(string Map, string Water)> Boxes =>
        from width in Gen.Choose(6, 40)
        from height in Gen.Choose(4, 24)
        from cells in Gen.Choose(0, 99).ArrayOf(width * height)
        select Drawn(width, height, cells);

    private static (string, string) Drawn(int width, int height, int[] roll)
    {
        var map = new System.Text.StringBuilder();
        var water = new System.Text.StringBuilder();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int r = roll[y * width + x];
                bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                map.Append(edge || r < 20 ? '#' : r < 24 ? '=' : '.');
                water.Append(!edge && r >= 50 ? (char)('1' + (r - 50) % 8) : '.');
            }
            map.Append('\n');
            water.Append('\n');
        }
        return (map.ToString(), water.ToString());
    }

    [Property(MaxTest = 60)]
    public Property KeepsEveryUnitInAClosedBoxAndComesToRest()
    {
        return Prop.ForAll(Boxes.ToArbitrary(), drawn =>
        {
            var w = Wet(drawn.Map, drawn.Water);
            int total = Total(w);
            for (int t = 0; t < 3000 && w.M.Liquids.AwakeChunks > 0; t++)
            {
                w.Run();
                if (Total(w) + w.M.Liquids.Dried != total) return false.Label($"total {Total(w)} and {w.M.Liquids.Dried} dried, not {total}, at tick {t}");
                if (!w.M.Liquids.AwakeWhereItCanMove()) return false.Label($"a chunk that can move asleep at tick {t}");
                for (int cell = 0; cell < w.Layout.Width * w.Layout.Height; cell++)
                {
                    int a = w.M.Liquids.AmountAt(cell);
                    if (a < 0 || a > L.Full) return false.Label($"{a} in cell {cell}");
                    if (a > 0 && w.M.Grid.Blocks(cell)) return false.Label($"liquid in the wall at {cell}");
                }
            }
            if (w.M.Liquids.AwakeChunks > 0) return false.Label("still moving after 3,000 ticks");
            var layer = w.M.Liquids.Layer.ToArray();
            w.Run(20);
            return w.M.Liquids.Layer.SequenceEqual(layer).Label("still once at rest");
        });
    }

    [Property(MaxTest = 30)]
    public Property ASaveLoadedAtAnyTickRunsOnAsTheWorldKeptRunning()
    {
        return Prop.ForAll(Boxes.ToArbitrary(), Gen.Choose(0, 60).ToArbitrary(), (drawn, at) =>
        {
            var kept = Wet(drawn.Map, drawn.Water).Run(at);
            var loaded = Wet(drawn.Map, drawn.Water).Run(at).Reload();
            for (int t = 0; t < 30; t++)
            {
                kept.Run();
                loaded.Run();
                if (kept.World.Hash() != loaded.World.Hash()) return false.Label($"apart {t} ticks after the load");
            }
            return true.ToProperty();
        });
    }

    [Fact]
    public void AColumnLosingAUnitWakesTheLoneUnitAcrossAChunkCornerFromIt()
    {
        // A lone unit at (15, 15), the corner cell of its chunk, lies on a full cell; across the corner, a full cell over one
        // short of full lets a unit fall, so the unit can run off past the corner. Its chunk must wake: a peer whose chunks
        // were all awake (as one that just loaded the world rechecks them) would let it run.
        string Drawn(Func<int, int, char> at) =>
            string.Join('\n', Enumerable.Range(0, 20).Select(y => string.Concat(Enumerable.Range(0, 20).Select(x => at(x, y)))));
        var map = Drawn((x, y) => (x, y) is (15 or 16, 15) or (15 or 16, 16) or (16, 17) ? '.' : '#');
        var water = Drawn((x, y) => (x, y) switch { (15, 15) => '1', (15 or 16, 16) => 'w', (16, 17) => '7', _ => '.' });
        var skipping = Wet(map, water);
        var awake = Wet(map, water);
        awake.M.Liquids.KeepAwake();
        for (int t = 0; t < 10; t++)
        {
            skipping.Run();
            awake.Run();
            Assert.Equal(awake.World.Hash(), skipping.World.Hash());
        }
    }

    [Property(MaxTest = 40)]
    public Property SkippingSleepingChunksNeverChangesTheFlow()
    {
        return Prop.ForAll(Boxes.ToArbitrary(), drawn =>
        {
            var skipping = Wet(drawn.Map, drawn.Water);
            var awake = Wet(drawn.Map, drawn.Water);
            awake.M.Liquids.KeepAwake();
            for (int t = 0; t < 200; t++)
            {
                skipping.Run();
                awake.Run();
                if (skipping.World.Hash() != awake.World.Hash()) return false.Label($"apart at tick {t}");
            }
            return true.ToProperty();
        });
    }

    [Fact]
    public void TheNearestFallIsFoundWithinReachTheLeftOnATie()
    {
        string Drawn(Func<int, int, char> at) =>
            string.Join('\n', Enumerable.Range(0, 8).Select(y => string.Concat(Enumerable.Range(0, 13).Select(x => at(x, y)))));
        var w = Wet(Drawn((x, y) => x is 2 or 10 && y is > 0 and < 7 ? '.' : '#'), Drawn((x, y) => x is 2 or 10 && y == 1 ? 'w' : '.'));
        int Cell(int x, int y) => y * 13 + x;
        Assert.Null(w.M.Liquids.FallNear(Cell(6, 2), 12));
        w.Run();
        Assert.Equal(Cell(2, 2), w.M.Liquids.FallNear(Cell(6, 2), 12));
        Assert.Equal(Cell(10, 2), w.M.Liquids.FallNear(Cell(9, 1), 12));
        Assert.Null(w.M.Liquids.FallNear(Cell(6, 2), 3));
        w.Run(10);
        Assert.Null(w.M.Liquids.FallNear(Cell(6, 2), 12));
    }
}
