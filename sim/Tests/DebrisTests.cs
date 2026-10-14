using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Inventory = Stillwake.Content.Inventory;
using ItemSpecs = Stillwake.Content.Items;
using Loot = Stillwake.Content.Loot;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/debris/debris.test.ts, ported, and an invariant test of its README's.</summary>
public class DebrisTests
{
    /// <summary>
    /// Twenty wide and eight tall: a room on the left from top to bottom, space right of it. Pieces
    /// spawn at the right edge, past which their lead would take them, in any row.
    /// </summary>
    private static readonly Layout Space = Layout.Parse(
    [
        " #####              ",
        " #...#              ",
        " #...#              ",
        " #.S.#              ",
        " #...#              ",
        " #...#              ",
        " #...#              ",
        " #####              ",
    ]);
    private const int Width = 20;
    private static int At(int x, int y) => y * Width + x;

    /// <summary>
    /// Space all round a small hull, wide and tall enough that neither its lead nor its margin reaches
    /// the grid's edges: the hull spans columns 70 to 75 and rows 25 to 30.
    /// </summary>
    private static readonly Layout Roomy = Layout.Parse(
        Enumerable.Range(0, 60).Select(y => y is >= 25 and <= 30 ? (new string(' ', 70) + "######").PadRight(160) : new string(' ', 160)).ToArray());

    /// <summary>A piece as it was when seen: what the query hands out is live.</summary>
    private sealed record Seen(int Id, string Item, int X, int Y, int Speed, int Start, int End)
    {
        public static Seen Of(Piece p) => new(p.Id, p.Item, p.X, p.Y, p.Speed, p.Start, p.End);
    }

    private static IReadOnlyList<Piece> Pieces(Scenario w) => w.M.Debris.Pieces;

    /// <summary>The first piece, once <c>Debris.Every</c> ticks have spawned it.</summary>
    private static Scenario Spawned(Layout? map = null)
    {
        var w = new Scenario(map ?? Space).Run(DebrisSpec.Every);
        Assert.Single(Pieces(w));
        return w;
    }

    /// <summary>Runs until the first piece's middle is in column <paramref name="x"/>.</summary>
    private static Scenario Until(Scenario w, int x)
    {
        while ((Pieces(w).FirstOrDefault()?.X ?? 0) >= (x + 1) * 1000) w.Run();
        return w;
    }

    /// <summary>What catching a piece of <paramref name="item"/> may give: itself, or a container's loot.</summary>
    private static string[] Gives(string item) => Loot.Of(item)?.Select(e => e.Item).ToArray() ?? [item];

    private static int Count(Scenario w, string item, int peer) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    private static Stillwake.Sim.Items.Stack[] Held(Scenario w, int peer) =>
        w.M.Items.Inventory(peer).Where(s => s != null).Select(s => s!).ToArray();

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>Every piece seen in <paramref name="ticks"/> ticks, by id in the order first seen, as it was then.</summary>
    private static List<Seen> Watch(Scenario w, int ticks)
    {
        var seen = new List<Seen>();
        int last = 0;
        for (int i = 0; i < ticks; i++)
        {
            w.Run();
            foreach (var p in Pieces(w))
                if (p.Id > last)
                {
                    seen.Add(Seen.Of(p));
                    last = p.Id;
                }
        }
        return seen;
    }

    [Fact]
    public void SpawnsItsFirstPieceOnItsBeatItsLeadRightOfTheHullGoneItsLeadLeftOfIt()
    {
        var w = new Scenario(Roomy).Run(DebrisSpec.Every - 1);
        Assert.Empty(Pieces(w));
        w.Run();
        var piece = Assert.Single(Pieces(w));
        int start = (75 + DebrisSpec.Lead) * 1000 + 500;
        Assert.Equal((1, start, start, (70 - DebrisSpec.Lead) * 1000), (piece.Id, piece.X, piece.Start, piece.End));
        Assert.Contains(piece.Item, DebrisSpec.Kinds.Select(k => k.Item));
    }

    [Fact]
    public void KeepsWhereAPieceSpawnsAndIsGoneOnTheGrid()
    {
        var piece = Pieces(Spawned())[0];
        Assert.Equal((19500, 0, 19500), (piece.Start, piece.End, piece.X));
    }

    [Fact]
    public void FliesInTheHullsRowsAndItsMarginAndInEveryOneOfThem()
    {
        var rows = Watch(new Scenario(Roomy), 40000).Select(p => p.Y).Distinct().Order();
        int top = 25 - DebrisSpec.Margin, bottom = 30 + DebrisSpec.Margin;
        Assert.Equal(Enumerable.Range(top, bottom - top + 1).Select(y => y * 1000 + 500), rows);
    }

    [Fact]
    public void FollowsAShipGrownTallerFoundAgainOnEveryBeat()
    {
        var w = new Scenario(Roomy);
        w.Do(new Place(55 * 160 + 72, Placement.Wall("scrap"))).Run();
        var rows = Watch(w, 20000).Select(p => p.Y / 1000).ToList();
        Assert.Equal(59, rows.Max());
        Assert.Equal(25 - DebrisSpec.Margin, rows.Min());
    }

    [Fact]
    public void DriftsEachPieceByASpeedOfItsOwnWithinTheRangeAndDropsItAtItsEnd()
    {
        var w = Spawned(Roomy);
        var piece = Seen.Of(Pieces(w)[0]);
        w.Run();
        Assert.Equal(piece.X - piece.Speed, Pieces(w)[0].X);
        int ticks = (piece.X - piece.End) / piece.Speed + 1;
        w.Run(ticks - 2);
        Assert.Contains(Pieces(w), p => p.Id == piece.Id);
        w.Run();
        Assert.DoesNotContain(Pieces(w), p => p.Id == piece.Id);

        var speeds = Watch(new Scenario(Roomy), 20000).Select(p => p.Speed).Distinct().ToList();
        Assert.All(speeds, speed => Assert.InRange(speed, DebrisSpec.DriftMin, DebrisSpec.DriftMax));
        Assert.True(speeds.Count > 3);
    }

    [Fact]
    public void SpawnsNothingWithoutAHull()
    {
        var w = new Scenario(Layout.Parse(Enumerable.Repeat(new string(' ', 20), 8).ToArray())).Run(1000);
        Assert.Empty(Pieces(w));
    }

    [Fact]
    public void SpawnsAgainWithinItsBeatAndJitterNeverMoreThanItsCapWithIdsNeverReused()
    {
        // A hull so wide that pieces stay out long enough to reach the cap.
        var w = new Scenario(Layout.Parse(
            Enumerable.Range(0, 8).Select(y => y == 4 ? new string(' ', 75) + new string('#', 151) : "").ToArray()));
        int last = 0, spawns = 0, gap = DebrisSpec.Every, peak = 0;
        var gaps = new HashSet<int>();
        for (int tick = 1; tick <= 8000; tick++)
        {
            w.Run();
            var out_ = Pieces(w);
            if (out_.Count > 0 && out_[^1].Id > last)
            {
                Assert.Equal(last + 1, out_[^1].Id);
                if (spawns > 0) Assert.True(gap >= DebrisSpec.Every);
                if (spawns > 0 && out_.Count < DebrisSpec.Max) Assert.True(gap <= DebrisSpec.Every + DebrisSpec.Jitter);
                last = out_[^1].Id;
                spawns += 1;
                // Until the cap is first reached no beat is skipped, so gaps are beats.
                if (spawns > 1 && peak < DebrisSpec.Max) gaps.Add(gap);
                gap = 0;
            }
            gap += 1;
            peak = Math.Max(peak, out_.Count);
            Assert.True(out_.Count <= DebrisSpec.Max);
            Assert.Equal(out_.Select(p => p.Id).Order(), out_.Select(p => p.Id));
            Assert.All(out_, p => Assert.True(p.X >= p.End && p.X <= p.Start && p.Y < 8000));
        }
        Assert.Equal(DebrisSpec.Max, peak);
        // The jitter at work: beats of more than one length.
        Assert.True(gaps.Count > 1);
    }

    [Fact]
    public void DrawsEveryKindTheHeavierOnesMoreOften()
    {
        // Short paths and no cap reached: about a hundred spawns, each a draw.
        var kinds = Watch(new Scenario(Layout.Parse(["   #   "])), 20000).Select(p => p.Item).ToList();
        var share = DebrisSpec.Kinds.Select(k => kinds.Count(item => item == k.Item)).ToList();
        Assert.All(share, n => Assert.True(n > 0));
        Assert.Equal(share.OrderDescending(), share);
    }

    [Fact]
    public void PutsAPieceGrabbedFromSpaceIntoTheInventorySalvageAsItselfAContainerOpened()
    {
        var w = Spawned();
        var piece = Seen.Of(Pieces(w)[0]);
        w.Do(new Enter(At(12, 1)) { From = 2 }).Do(new Grab(piece.Id) { From = 2 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(Pieces(w));
        var got = Held(w, 2);
        Assert.NotEmpty(got);
        Assert.All(got, s => Assert.Contains(s.Item, Gives(piece.Item)));
        if (Gives(piece.Item)[0] == piece.Item) Assert.Equal(1, Count(w, piece.Item, 2));
    }

    [Fact]
    public void OpensAContainerIntoItsLootEachEntryWithinItsBoundsAndSalvageIntoItself()
    {
        var rng = new Rng(new RngState { S = 7 });
        var seen = new Dictionary<string, HashSet<int>>();
        for (int i = 0; i < 400; i++)
            foreach (var (container, loot) in Loot.All)
                foreach (var (item, n) in DebrisModule.LootOf(container, rng))
                {
                    var entry = loot.FirstOrDefault(e => e.Item == item) ?? throw new Xunit.Sdk.XunitException($"{container} gave {item}");
                    Assert.InRange(n, entry.Min, entry.Max);
                    if (!seen.TryGetValue(item, out var counts)) seen[item] = counts = [];
                    counts.Add(n);
                }
        // The planks a cargo crate holds come in every count of their range.
        var planks = Loot.Of("cargoCrate")![0];
        Assert.Equal(planks.Max - planks.Min + 1, seen["planks"].Count);
        Assert.Equal([("scrapChunk", 1)], DebrisModule.LootOf("scrapChunk", rng));
    }

    [Fact]
    public void GivesAPieceGrabbedTwiceOnOneTickToTheFirstAndRefusesTheSecond()
    {
        var w = Spawned();
        int id = Pieces(w)[0].Id;
        w.Do(new Enter(At(12, 1)) { From = 1 }).Do(new Enter(At(13, 1)) { From = 2 });
        w.Do(new Grab(id) { From = 2 }).Do(new Grab(id) { From = 1 }).Run();
        Assert.Equal(["debris.gone"], Reasons(w));
        Assert.Empty(Held(w, 1));
        Assert.NotEmpty(Held(w, 2));
    }

    [Fact]
    public void RefusesAPieceThatNeverWasOrIsGone()
    {
        var w = Spawned().Do(new Enter(At(12, 1))).Run();
        w.Do(new Grab(7)).Run();
        w.Run(Width * 1000);
        w.Do(new Grab(1)).Run();
        Assert.Equal(["debris.gone", "debris.gone"], Reasons(w));
    }

    [Fact]
    public void RefusesAGrabUnlessBothTheHeroAndThePieceAreInSpace()
    {
        var w = Spawned();
        int id = Pieces(w)[0].Id;
        w.Do(new Grab(id) { From = 1 }).Run();
        w.Do(new Enter(At(3, 4)) { From = 1 }).Do(new Grab(id) { From = 1 }).Run();
        Assert.Equal(["debris.notOutside", "debris.notOutside"], Reasons(w));
        // Over the room's cells and walls the piece is not in space either.
        Until(w, 4).Do(new Enter(At(12, 1)) { From = 1 });
        Assert.Equal("debris.notOutside", w.M.Debris.GrabProblem(1, id));
        w.Do(new Grab(id) { From = 1 }).Run();
        Assert.Equal("debris.notOutside", Reasons(w)[^1]);
        Assert.Contains(Pieces(w), p => p.Id == id);
    }

    [Fact]
    public void TakesAPieceWithNoRoomForItWhichFloatsWhereTheHeroIs()
    {
        var w = Spawned();
        var piece = Seen.Of(Pieces(w)[0]);
        w.GiveItems(0, ("canister", Inventory.Slots * ItemSpecs.Get("canister").Stack));
        w.Do(new Enter(At(12, 1))).Do(new Grab(piece.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(Pieces(w));
        // In space nothing falls: it stays in the hero's cell for anyone to pick up.
        var floor = w.M.Items.Floor;
        Assert.NotEmpty(floor);
        Assert.All(floor, stack =>
        {
            Assert.Equal((At(12, 1), 0), (stack.Cell, stack.By));
            Assert.Contains(stack.Item, Gives(piece.Item));
        });
    }

    [Fact]
    public void FindsTheCellUnderAPieceAndThePieceInACell()
    {
        Assert.Equal(At(2, 1), DebrisModule.CellOf(2999, 1500, Width));
        Assert.Equal(0, DebrisModule.CellOf(0, 0, Width));
        var w = Spawned();
        var piece = Pieces(w)[0];
        int cell = DebrisModule.CellOf(piece.X, piece.Y, Width);
        Assert.Same(piece, w.M.Debris.PieceAt(cell));
        Assert.Null(w.M.Debris.PieceAt(cell - 1));
    }

    // The README's invariants, whatever happens: a hull grown and cut down, heroes in and out of space
    // grabbing pieces out, gone and never spawned, and long waits.

    private sealed record Step(string Type, int Cell = 0, int Peer = 0, int Ticks = 0);

    [Property(MaxTest = 60)]
    public Property KeepsIdsInSpawnOrderNoMoreThanTheCapAndEveryPieceOnItsPathInItsHullsBand()
    {
        // Mostly small grids, now and then one wide enough for the cap; hulls from none to the whole grid.
        var size = Gen.Frequency(
            (3, from w in Gen.Choose(1, 24) from h in Gen.Choose(1, 12) select (w, h)),
            (1, from w in Gen.Choose(150, 230) from h in Gen.Choose(1, 4) select (w, h)));
        var map = from s in size
                  from density in Gen.Elements(0, 2, 10, 50, 100)
                  from rolls in Gen.Choose(0, 99).ArrayOf(s.w * s.h)
                  select (s.w, s.h, Walls: rolls.Select(r => r < density).ToArray());
        var step = from kind in Gen.Frequency(
                       (2, Gen.Constant("wait")), (2, Gen.Constant("grab")), (2, Gen.Constant("enter")),
                       (1, Gen.Constant("wall")), (1, Gen.Constant("breach")))
                   from cell in Gen.Choose(-2, 3000)
                   from peer in Gen.Choose(0, 2)
                   from ticks in Gen.Frequency((3, Gen.Choose(1, 40)), (2, Gen.Choose(150, 400)), (1, Gen.Choose(1000, 6000)))
                   select new Step(kind, cell, peer, ticks);
        var cases = from m in map from n in Gen.Choose(0, 25) from steps in step.ArrayOf(n) select (m, steps);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var (m, steps) = c;
            var rows = Enumerable.Range(0, m.h)
                .Select(y => new string(Enumerable.Range(0, m.w).Select(x => m.Walls[y * m.w + x] ? '#' : ' ').ToArray()))
                .ToArray();
            var w = new Scenario(Layout.Parse(rows));
            int cells = m.w * m.h, last = 0;
            foreach (var s in steps)
            {
                var pieces = Pieces(w);
                switch (s.Type)
                {
                    case "enter":
                        w.Do(new Enter(s.Cell % (cells + 4) - 2) { From = s.Peer });
                        break;
                    case "grab":
                        w.Do(new Grab(pieces.Count > 0 && s.Cell % 3 != 0 ? pieces[Math.Abs(s.Cell) % pieces.Count].Id : s.Cell % (last + 3)) { From = s.Peer });
                        break;
                    case "wall":
                        int at = Math.Abs(s.Cell) % cells;
                        if (w.M.Grid.StructureAt(at) == Structure.Empty) w.Do(new Place(at, Placement.Wall()));
                        break;
                    case "breach":
                        w.Do(new ClearAt(Math.Abs(s.Cell) % cells, "structure"));
                        break;
                }
                for (int t = 0; t < (s.Type == "wait" ? s.Ticks : 1); t++)
                {
                    w.Run();
                    var broken = Broken(w, ref last);
                    if (broken != null) return false.Label(broken);
                }
            }
            return true.ToProperty();
        });
    }

    /// <summary>What breaks the README's invariants after a tick, or null; <paramref name="last"/> is the newest id seen so far.</summary>
    private static string? Broken(Scenario w, ref int last)
    {
        var pieces = Pieces(w);
        if (pieces.Count > DebrisSpec.Max) return $"{pieces.Count} pieces out";
        int width = w.M.Grid.Width, height = w.M.Grid.Height;
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            if (i > 0 && p.Id <= pieces[i - 1].Id) return "ids out of spawn order";
            if (p.X < 0 || p.X >= width * 1000 || p.Y < 0 || p.Y >= height * 1000) return $"piece {p.Id} off the grid";
            if (p.X < p.End || p.X > p.Start) return $"piece {p.Id} off its path";
            if (p.Id <= last) continue;
            // New this tick: in the band of the hull it spawned by, which the tick's commands have shaped.
            if (p.Id != last + 1) return $"piece {p.Id} after {last}: an id skipped or reused";
            last = p.Id;
            var hull = HullOf(w.M.Grid);
            if (hull is not var (left, right, top, bottom)) return $"piece {p.Id} spawned with no hull";
            int row = p.Y / 1000;
            if (row < Math.Max(0, top - DebrisSpec.Margin) || row > Math.Min(height - 1, bottom + DebrisSpec.Margin))
                return $"piece {p.Id} in row {row}, out of the hull's band";
            if (p.Start != Math.Min(width - 1, right + DebrisSpec.Lead) * 1000 + 500 || p.End != Math.Max(0, left - DebrisSpec.Lead) * 1000)
                return $"piece {p.Id} not on its lead";
        }
        return null;
    }

    private static (int Left, int Right, int Top, int Bottom)? HullOf(IGridQueries grid)
    {
        var cells = Enumerable.Range(0, grid.Width * grid.Height).Where(c => grid.StructureAt(c) != Structure.Empty).ToList();
        if (cells.Count == 0) return null;
        return (cells.Min(c => c % grid.Width), cells.Max(c => c % grid.Width), cells[0] / grid.Width, cells[^1] / grid.Width);
    }
}
