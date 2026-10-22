using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Xunit;
using AroundHeroes = Stillwake.Content.AroundHeroes;
using ByHull = Stillwake.Content.ByHull;
using DebrisSpec = Stillwake.Content.Debris;
using HalfBox = Stillwake.Content.HalfBox;

namespace Stillwake.Sim.Tests;

/// <summary>Debris by zones (0052): the stream by the hull as it was, and the far field round every hero out past it.</summary>
public class DebrisZonesTests
{
    private static readonly AroundHeroes Around = (AroundHeroes)DebrisSpec.Far.Anchor;
    private static readonly ByHull Stream = (ByHull)DebrisSpec.Ship.Anchor;

    /// <summary>
    /// Space round a hull block of <paramref name="w"/>×<paramref name="h"/> cells at (<paramref name="x"/>,
    /// <paramref name="y"/>) in a grid of <paramref name="width"/>×<paramref name="height"/>; no zone, so no cold.
    /// </summary>
    internal static Layout Open(int width, int height, int x, int y, int w = 6, int h = 3) => Layout.Parse(
        [.. Enumerable.Range(0, height).Select(r => r >= y && r < y + h ? (new string(' ', x) + new string('#', w)).PadRight(width) : new string(' ', width))]);

    /// <summary>
    /// 1280×640, the hull at columns 200 to 211 and rows 300 to 305: its stream's box spans columns 72 to 339, rows 276 to 329.
    /// Every length here is twice the 16 px cell's (0104), as the zones' are.
    /// </summary>
    private static readonly Layout Wide = Open(1280, 640, 200, 300, 12, 6);

    private static IReadOnlyList<Piece> Pieces(Scenario w) => w.M.Debris.Pieces;
    private static IEnumerable<Piece> Far(Scenario w) => Pieces(w).Where(p => p.Zone == DebrisSpec.Far.Id);
    private static DebrisState Debris(Scenario w) => (DebrisState)w.M.Debris.Slice;
    private static (int X, int Y) CellOf(Piece p) => (p.X / 1000, p.Y / 1000);

    private static bool Within((int X, int Y) a, (int X, int Y) b, HalfBox box) => Math.Abs(a.X - b.X) <= box.X && Math.Abs(a.Y - b.Y) <= box.Y;
    private static bool OnRing((int X, int Y) cell, (int X, int Y) hero) => Within(cell, hero, Around.RingTo) && !InsideRingFrom(cell, hero);
    private static bool InsideRingFrom((int X, int Y) cell, (int X, int Y) hero) =>
        Math.Abs(cell.X - hero.X) < Around.RingFrom.X && Math.Abs(cell.Y - hero.Y) < Around.RingFrom.Y;
    private static int InKeep(Scenario w, (int X, int Y) hero) => Far(w).Count(p => Within(CellOf(p), hero, Around.Keep));

    /// <summary>The new pieces of each tick for <paramref name="ticks"/> ticks, as they spawned, with the debris clock they spawned on.</summary>
    private static List<(int Clock, Piece Piece)> Spawns(Scenario w, int ticks)
    {
        var seen = new List<(int, Piece)>();
        int last = Pieces(w).Count > 0 ? Pieces(w)[^1].Id : 0;
        for (int i = 0; i < ticks; i++)
        {
            w.Run();
            foreach (var p in Pieces(w).Where(p => p.Id > last))
                seen.Add((Debris(w).Clock, new Piece { Id = p.Id, Zone = p.Zone, Item = p.Item, X = p.X, Y = p.Y, Speed = p.Speed, Start = p.Start, End = p.End, SeenAt = p.SeenAt }));
            if (Pieces(w).Count > 0) last = Math.Max(last, Pieces(w)[^1].Id);
        }
        return seen;
    }

    [Fact]
    public void SpawnsAFarPieceOnTheRingRoundAHeroOutPastTheStreamOutOfItsViewOnItsBeat()
    {
        var w = new Scenario(Wide).Do(new Enter(800 + 300 * 1280)).Run(DebrisSpec.Far.Every - 1);
        Assert.Empty(Far(w));
        w.Run();
        var piece = Assert.Single(Far(w));
        Assert.True(OnRing(CellOf(piece), (800, 300)), $"{CellOf(piece)} off the ring");
        Assert.Equal((piece.X, 0, DebrisSpec.Far.Every), (piece.Start, piece.End, piece.SeenAt));
        Assert.Equal(500, piece.X % 1000);
        Assert.InRange(piece.Speed, DebrisSpec.Far.DriftMin, DebrisSpec.Far.DriftMax);
        // Every one after it as well, on all four sides of the ring, never where the hero sees.
        var spawns = Spawns(w, 600).Where(s => s.Piece.Zone == DebrisSpec.Far.Id).ToList();
        Assert.True(spawns.Count > 20, $"{spawns.Count} far pieces");
        Assert.All(spawns, s => Assert.True(OnRing(CellOf(s.Piece), (800, 300)) && !Within(CellOf(s.Piece), (800, 300), DebrisSpec.View)));
        Assert.All(new Func<(int X, int Y), bool>[] { c => c.X >= 800 + Around.RingFrom.X, c => c.X <= 800 - Around.RingFrom.X, c => c.Y <= 300 - Around.RingFrom.Y, c => c.Y >= 300 + Around.RingFrom.Y },
            side => Assert.Contains(spawns, s => side(CellOf(s.Piece))));
    }

    [Fact]
    public void SpawnsNoFarPieceOffTheGridNorInTheStreamsBoxYetTakesTheDrawsForIt()
    {
        // The hull a wall down the left edge: its stream's box covers every row out to column 128. The hero
        // at column 200 is past it, its ring's left side in the box and the rest off the grid.
        var narrow = Layout.Parse([.. Enumerable.Range(0, 40).Select(_ => "#".PadRight(300))]);
        var alone = new Scenario(narrow);
        var watched = new Scenario(narrow).Do(new Enter(20 * 300 + 200));
        var (a, b) = (Spawns(alone, 600), Spawns(watched, 600));
        Assert.Empty(Far(watched));
        Assert.All(b, s => Assert.Equal(DebrisSpec.Ship.Id, s.Piece.Zone));
        // The draws were taken, so the stream's pieces after the first beat are others.
        Assert.NotEqual(a.Select(s => (s.Piece.Item, s.Piece.Y, s.Piece.Speed)), b.Select(s => (s.Piece.Item, s.Piece.Y, s.Piece.Speed)));
    }

    [Fact]
    public void TakesNoDrawForTheFarFieldWhileEveryHeroIsInsideTheStreamsBox()
    {
        // 320×120 round a hull at columns 140 to 151 and rows 50 to 61; the box spans columns 12 to 279, rows 26 to 85.
        // Both heroes in it, neither seeing where the stream starts (279) nor where it ends (12).
        var roomy = Open(320, 120, 140, 50, 12, 12);
        var alone = new Scenario(roomy);
        var near = new Scenario(roomy).Do(new Enter(40 * 320 + 140)).Do(new Enter(80 * 320 + 150) { From = 1 });
        var (a, b) = (Spawns(alone, 800), Spawns(near, 800));
        Assert.Empty(Far(near));
        Assert.True(a.Count > 3);
        Assert.Equal(a.Select(s => (s.Clock, s.Piece.Id, s.Piece.Item, s.Piece.X, s.Piece.Y, s.Piece.Speed)),
            b.Select(s => (s.Clock, s.Piece.Id, s.Piece.Item, s.Piece.X, s.Piece.Y, s.Piece.Speed)));
    }

    /// <summary>The most of the far field in each hero's keep box at once over <paramref name="ticks"/> ticks, and the most in all; never more than its share in any.</summary>
    private static (int[] Each, int All) Peaks(Scenario w, int ticks, params (int X, int Y)[] heroes)
    {
        var each = new int[heroes.Length];
        int all = 0;
        for (int t = 0; t < ticks; t++)
        {
            w.Run();
            for (int h = 0; h < heroes.Length; h++)
            {
                int n = InKeep(w, heroes[h]);
                Assert.True(n <= Around.PerHero, $"{n} far pieces round hero {h}");
                each[h] = Math.Max(each[h], n);
            }
            all = Math.Max(all, Far(w).Count());
        }
        return (each, all);
    }

    private static Scenario With(Layout map, params (int X, int Y)[] heroes)
    {
        var w = new Scenario(map);
        for (int i = 0; i < heroes.Length; i++) w.Do(new Enter(heroes[i].Y * map.Width + heroes[i].X) { From = i });
        return w;
    }

    [Fact]
    public void CountsOnlyTheStreamsOwnPiecesTowardItsCap()
    {
        // The stream one short of its cap from the start, a hero far out filling its share meanwhile: the
        // stream's next beat fills it, however many far pieces are out.
        var w = new Scenario(Wide with { DebrisAtStart = DebrisSpec.Ship.Max - 1 }).Do(new Enter(120 * 1280 + 900));
        bool full = false;
        for (int t = 0; t < 850 && !full; t++)
        {
            w.Run();
            full = Pieces(w).Count(p => p.Zone == DebrisSpec.Ship.Id) == DebrisSpec.Ship.Max;
        }
        Assert.True(full, "the stream never filled");
        Assert.NotEmpty(Far(w));
    }

    [Fact]
    public void FillsAHerosShareOfItsKeepBoxAndNoMore()
    {
        var (each, _) = Peaks(With(Wide, (800, 300)), 850, (800, 300));
        Assert.Equal(Around.PerHero, each[0]);
    }

    [Fact]
    public void LetsTwoHeroesTogetherFillOneShareAndTwoApartFillTwo()
    {
        // Together they share one keep box: never more than one share in it (Peaks), and that share filled.
        var (shared, _) = Peaks(With(Wide, (800, 300), (800, 300)), 850, (800, 300));
        Assert.Equal(Around.PerHero, shared[0]);
        var (each, apart) = Peaks(With(Wide, (500, 160), (1040, 480)), 850, (500, 160), (1040, 480));
        Assert.Equal([Around.PerHero, Around.PerHero], each);
        Assert.True(apart >= 2 * Around.PerHero, $"{apart} far pieces for two heroes apart");
    }

    [Fact]
    public void StopsAtTheFarFieldsCapTheLowerIdsSpawningFirst()
    {
        // Six heroes 600 cells apart: six shares are more than the cap.
        var heroes = Enumerable.Range(0, 6).Select(i => (600 + 600 * i, 400)).ToArray();
        var w = With(Open(3800, 800, 100, 400, 12, 6), heroes);
        bool capped = false;
        for (int t = 0; t < 850 && !capped; t++)
        {
            w.Run();
            int far = Far(w).Count();
            Assert.True(far <= DebrisSpec.Far.Max, $"{far} far pieces");
            if (far < DebrisSpec.Far.Max) continue;
            capped = true;
            Assert.True(InKeep(w, heroes[^1]) < InKeep(w, heroes[0]), "the last hero spawned as many as the first");
        }
        Assert.True(capped);
    }

    /// <summary>Wide with a hero at (800, 300) and its first far piece spawned; the piece's id.</summary>
    private static (Scenario W, int Piece) FirstFar()
    {
        var w = new Scenario(Wide).Do(new Enter(800 + 300 * 1280)).Run(DebrisSpec.Far.Every);
        return (w, Far(w).Single().Id);
    }

    private static Piece? PieceOf(Scenario w, int id) => Pieces(w).FirstOrDefault(p => p.Id == id);

    /// <summary>A cell between its keep and hard boxes' heights above or below piece <paramref name="id"/>: out of its keep box, within its hard box however far it drifts the next 600 ticks.</summary>
    private static int Aside(Scenario w, int id)
    {
        var (x, y) = CellOf(PieceOf(w, id)!);
        int off = (Around.Keep.Y + Around.Hard.Y) / 2;
        return (y >= off ? y - off : y + off) * 1280 + x;
    }

    [Fact]
    public void RemovesAFarPieceLeftOutOfEveryKeepBoxExactlyItsLingerAfterItWasLastSeen()
    {
        var (w, id) = FirstFar();
        w.Do(new Enter(Aside(w, id))).Run();
        Assert.Equal(DebrisSpec.Far.Every, PieceOf(w, id)!.SeenAt);
        w.Run(DebrisSpec.Far.Every + Around.Linger - 1 - Debris(w).Clock);
        Assert.NotNull(PieceOf(w, id));
        w.Run();
        Assert.Null(PieceOf(w, id));
    }

    [Fact]
    public void KeepsAFarPieceAHeroCameBackToItsLingerCountedAgainFromThen()
    {
        var (w, id) = FirstFar();
        w.Do(new Enter(Aside(w, id))).Run(300);
        w.Do(new Enter(300 * 1280 + 800)).Run();
        int back = Debris(w).Clock;
        Assert.Equal(back, PieceOf(w, id)!.SeenAt);
        w.Do(new Enter(Aside(w, id))).Run(Around.Linger - 1);
        Assert.NotNull(PieceOf(w, id));
        w.Run();
        Assert.Null(PieceOf(w, id));
    }

    [Fact]
    public void RemovesAFarPieceOutOfEveryHerosHardBoxOnThatTick()
    {
        var (w, id) = FirstFar();
        var (x, y) = CellOf(PieceOf(w, id)!);
        w.Do(new Enter(y * 1280 + x - Around.Hard.X - 1)).Run();
        Assert.Null(PieceOf(w, id));
        // The last hero gone from the world takes the far field with it: no box keeps it.
        var (left, _) = FirstFar();
        left.Do(new Leave()).Run();
        Assert.Empty(Far(left));
    }

    [Fact]
    public void NeverRemovesAStreamPieceByDistanceOnlyAtItsEnd()
    {
        // A hero far off, the stream out: its pieces drift on, each gone only past its end.
        var w = new Scenario(Wide with { DebrisAtStart = 6 }).Do(new Enter(20 * 1280 + 1260)).Run();
        var last = Pieces(w).Where(p => p.Zone == DebrisSpec.Ship.Id).ToDictionary(p => p.Id, p => (p.X, p.Speed, p.End));
        Assert.Equal(6, last.Count);
        for (int t = 0; t < Around.Linger + 100; t++)
        {
            w.Run();
            foreach (var (id, (x, speed, end)) in last.ToList())
                if (PieceOf(w, id) is { } now) last[id] = (now.X, now.Speed, now.End);
                else
                {
                    Assert.True(x - speed < end, $"stream piece {id} gone at {x}, before its end {end}");
                    last.Remove(id);
                }
        }
        Assert.All(Pieces(w).Where(p => p.Zone == DebrisSpec.Ship.Id), p => Assert.Equal(0, p.SeenAt));
    }

    [Fact]
    public void StartsAStreamPieceRightOfEveryHerosViewThatHoldsItsLeadAndDriftsItPastTheShip()
    {
        // The lead is column 339. One hero in the stream's box sees it, out to 420; a second, out past the box,
        // sees 421, out to 640: the stream starts at 641, where neither sees a piece appear.
        var w = With(Wide, (300, 302), (520, 280));
        var spawns = Spawns(w, 800).Where(s => s.Piece.Zone == DebrisSpec.Ship.Id).ToList();
        Assert.True(spawns.Count > 2, $"{spawns.Count} stream pieces");
        Assert.All(spawns, s => Assert.Equal((641_500, 72_000), (s.Piece.Start, s.Piece.End)));
        // Alone, no one sees the lead: the stream starts on it.
        var alone = Spawns(new Scenario(Wide), 400).Where(s => s.Piece.Zone == DebrisSpec.Ship.Id);
        Assert.All(alone, s => Assert.Equal(339_500, s.Piece.Start));
        // A hero far ahead, not seeing the lead, moves nothing.
        var ahead = Spawns(With(Wide, (600, 302)), 400).Where(s => s.Piece.Zone == DebrisSpec.Ship.Id);
        Assert.All(ahead, s => Assert.Equal(339_500, s.Piece.Start));
    }

    [Fact]
    public void TakesAStreamPiecePastItsEndOnlyOnceNoHeroSeesIt()
    {
        // The hull at columns 600 to 611: the stream ends at 472. A hero at 400, out past the box, sees from 280 to 520.
        var map = Open(1280, 640, 600, 300, 12, 6) with { DebrisAtStart = 1 };
        var w = With(map, (400, 302)).Run();
        var piece = Pieces(w).Single(p => p.Zone == DebrisSpec.Ship.Id);
        Assert.Equal(472_000, piece.End);
        w.Do(new Reel(piece.Id, 281_000, 303_000)).Run();
        while (PieceOf(w, piece.Id)!.Reel != null) w.Run();
        Assert.Empty(w.Refusals);
        // Past its end and seen: it drifts on to the view's edge, and is gone the tick it leaves it.
        int ticks = 0;
        while (PieceOf(w, piece.Id) is { } now)
        {
            Assert.True(now.X / 1000 >= 280, $"at {now.X}, out of sight");
            w.Run();
            ticks++;
        }
        Assert.InRange(ticks, 1, 1000 / DebrisSpec.DriftMin + 1);
        // Alone, it is gone as it passes its end.
        var alone = new Scenario(map).Run();
        var lone = Pieces(alone).Single();
        int left = (lone.X - lone.End) / lone.Speed + 1;
        alone.Run(left - 1);
        Assert.NotNull(PieceOf(alone, lone.Id));
        alone.Run();
        Assert.Null(PieceOf(alone, lone.Id));
    }

    [Fact]
    public void DriftsAFarPieceToTheGridsLeftEdgeAndNoFurther()
    {
        // A hero by the left edge, above the stream's box: whatever it spawns stays in its keep box till the
        // edge takes it. Its first piece is reeled to two cells from the edge, to be there soon.
        var w = new Scenario(Wide).Do(new Enter(120 * 1280 + 60)).Run(DebrisSpec.Far.Every);
        // A beat whose draw falls off the grid spawns nothing, and the ring's left side is off it.
        for (int t = 0; t < 100 && !Far(w).Any(); t++) w.Run();
        var first = Far(w).First();
        Assert.Equal(0, first.End);
        w.Do(new Reel(first.Id, 2_000, first.Y)).Run();
        Assert.Empty(w.Refusals);
        var last = new Dictionary<int, (int X, int Speed)>();
        (int X, int Speed) lastOfFirst = (first.X, first.Speed);
        for (int t = 0; t < 800 && PieceOf(w, first.Id) != null; t++)
        {
            w.Run();
            var now = Far(w).ToDictionary(p => p.Id, p => (p.X, p.Speed));
            Assert.All(now.Values, p => Assert.InRange(p.X, 0, 1280 * 1000 - 1));
            foreach (var (id, was) in last.Where(kv => !now.ContainsKey(kv.Key)))
                Assert.True(was.X - was.Speed < 0, $"piece {id} gone at {was.X}");
            if (now.TryGetValue(first.Id, out var seen)) lastOfFirst = seen;
            last = now;
        }
        Assert.Null(PieceOf(w, first.Id));
        Assert.True(lastOfFirst.X < lastOfFirst.Speed, $"gone at {lastOfFirst.X}");
    }

    [Fact]
    public void PicksATierByTheDistanceFromTheHullsBoxRowsCountingDouble()
    {
        var hull = (Left: 100, Right: 105, Top: 150, Bottom: 152);
        int TierFrom(int x, int y) => DebrisModule.TierOf(DebrisSpec.Far, x, y, hull).From;
        Assert.Equal(0, TierFrom(102, 151));
        Assert.Equal((0, 256), (TierFrom(105 + 255, 151), TierFrom(105 + 256, 151)));
        Assert.Equal((256, 512), (TierFrom(100 - 511, 151), TierFrom(100 - 512, 151)));
        Assert.Equal((0, 256), (TierFrom(102, 152 + 127), TierFrom(102, 152 + 128)));
        Assert.Equal((256, 512), (TierFrom(102, 150 - 255), TierFrom(102, 150 - 256)));
        // The deeper of the two decides.
        Assert.Equal(256, TierFrom(105 + 40, 152 + 140));
        Assert.Equal(0, DebrisModule.TierOf(DebrisSpec.Ship, 5000, 5000, hull).From);
    }

    [Fact]
    public void DrawsTheFarthestTiersKindsForAHeroFarOut()
    {
        // Every point of its ring 1400 cells and more from the hull: machine blocks drift by more often than scrap there.
        var kinds = new List<string>();
        for (uint seed = 1; seed <= 5; seed++)
        {
            var w = new Scenario(Open(2800, 600, 100, 300, 12, 6), seed).Do(new Enter(300 * 2800 + 1800));
            kinds.AddRange(Spawns(w, 700).Where(s => s.Piece.Zone == DebrisSpec.Far.Id).Select(s => s.Piece.Item));
        }
        Assert.True(kinds.Count > 120, $"{kinds.Count} spawned");
        int scrap = kinds.Count(k => k == "scrapChunk"), blocks = kinds.Count(k => k == "machineBlock");
        Assert.True(blocks > scrap, $"{blocks} machine blocks, {scrap} scrap chunks");
    }

    [Fact]
    public void ReloadsTheFarFieldAndPlaysOnAlikeTheHullFoundAgain()
    {
        Scenario Played() => With(Wide, (800, 300), (500, 120)).Run(400);
        var (kept, loaded) = (Played(), Played().Reload());
        Assert.NotEmpty(Far(loaded));
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
        // The ship grown after the load: both find its new box, the far field kept out of it alike.
        foreach (var w in new[] { kept, loaded }) w.Do(new Place(280 * 1280 + 600, Placement.Wall())).Run(400);
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
        Assert.Equal(Pieces(kept).Select(p => (p.Id, p.X, p.Y, p.SeenAt)), Pieces(loaded).Select(p => (p.Id, p.X, p.Y, p.SeenAt)));
    }

    [Fact]
    public void GivesAZoneWithNoBeatOneOnTheFirstTickDueItsEveryOn()
    {
        var w = new Scenario(Wide).Run(5);
        Debris(w).Beats.RemoveAll(b => b.Zone == DebrisSpec.Far.Id);
        w.Run();
        Assert.Equal(6 + DebrisSpec.Far.Every, Debris(w).Beats.Single(b => b.Zone == DebrisSpec.Far.Id).Next);
        Assert.Equal(DebrisSpec.Zones.Select(z => z.Id).Order(), Debris(w).Beats.Select(b => b.Zone).Order());
    }

    // The invariants of the zones, whatever happens: heroes entering anywhere, on the grid and off it, a
    // hull grown and cut down, long waits and short.

    private sealed record Step(string Type, int Cell = 0, int Peer = 0, int Ticks = 0);

    [Property(MaxTest = 60)]
    public Property FollowsTheHullByTheGridsLogAsAScanOfItWouldFindIt()
    {
        // Walls built and taken anywhere, the hull's edges and its last cell included (0051).
        var edit = from wall in Gen.Elements(true, false)
                   from x in Gen.Choose(0, 23)
                   from y in Gen.Choose(0, 11)
                   select (wall, cell: y * 24 + x);
        var edits = from n in Gen.Choose(1, 60) from e in edit.ArrayOf(n) select e;
        return Prop.ForAll(edits.ToArbitrary(), all =>
        {
            var w = new Scenario(Layout.Parse(Enumerable.Range(0, 12).Select(y => y is >= 4 and <= 7 ? "         ######         " : new string(' ', 24)).ToArray()));
            var debris = w.M.Debris;
            Assert.Equal(debris.HullOf(), debris.Hull());
            foreach (var (wall, cell) in all)
            {
                w.Do(wall ? new Place(cell, Placement.Wall()) : new ClearAt(cell, "structure")).Run();
                Assert.Equal(debris.HullOf(), debris.Hull());
            }
        });
    }

    [Property(MaxTest = 40)]
    public Property KeepsEveryZoneUnderItsCapOutOfEveryHerosViewAndTheFarFieldOnTheRingOfAHeroUnderItsShare()
    {
        var map = from w in Gen.Choose(60, 520)
                  from h in Gen.Choose(20, 260)
                  from x in Gen.Choose(0, w - 1)
                  from y in Gen.Choose(0, h - 1)
                  from hw in Gen.Choose(1, 30)
                  from hh in Gen.Choose(1, 12)
                  select (w, h, x, y, hw: Math.Min(hw, w - x), hh: Math.Min(hh, h - y));
        var step = from kind in Gen.Frequency((4, Gen.Constant("enter")), (3, Gen.Constant("wait")), (1, Gen.Constant("wall")), (1, Gen.Constant("breach")))
                   from cell in Gen.Choose(0, 1_000_000)
                   from peer in Gen.Choose(0, 3)
                   from ticks in Gen.Frequency((3, Gen.Choose(1, 40)), (2, Gen.Choose(100, 400)))
                   select new Step(kind, cell, peer, ticks);
        var cases = from m in map from n in Gen.Choose(1, 20) from steps in step.ArrayOf(n) select (m, steps);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var (m, steps) = c;
            var w = new Scenario(Open(m.w, m.h, m.x, m.y, m.hw, m.hh));
            int cells = m.w * m.h, last = 0;
            foreach (var s in steps)
            {
                switch (s.Type)
                {
                    case "enter":
                        // Mostly on the grid, now and then a little off it.
                        w.Do(new Enter(s.Cell % (cells + 40) - 20) { From = s.Peer });
                        break;
                    case "wall":
                        if (w.M.Grid.StructureAt(s.Cell % cells) == Structure.Empty) w.Do(new Place(s.Cell % cells, Placement.Wall()));
                        break;
                    case "breach":
                        w.Do(new ClearAt(s.Cell % cells, "structure"));
                        break;
                }
                for (int t = 0; t < (s.Type == "wait" ? s.Ticks : 1); t++)
                {
                    // Where each piece will be after this tick's drift: nothing here reels.
                    var moved = Pieces(w).Select(p => (p.Id, X: p.X - p.Speed, p.Y)).ToList();
                    w.Run();
                    if (Broken(w, ref last, moved) is { } broken) return false.Label(broken);
                }
            }
            return true.ToProperty();
        });
    }

    /// <summary>
    /// What breaks the zones' invariants after a tick, or null; <paramref name="last"/> is the newest id seen,
    /// <paramref name="moved"/> the pieces before the tick, where its drift took them.
    /// </summary>
    private static string? Broken(Scenario w, ref int last, IReadOnlyList<(int Id, int X, int Y)> moved)
    {
        var pieces = Pieces(w);
        int width = w.M.Grid.Width, height = w.M.Grid.Height, clock = Debris(w).Clock;
        foreach (var zone in DebrisSpec.Zones)
        {
            int n = pieces.Count(p => p.Zone == zone.Id);
            if (n > zone.Max) return $"{n} pieces of {zone.Id}";
        }
        var heroes = w.M.Crew.Heroes.Where(h => h.Cell >= 0 && h.Cell < width * height).Select(h => (X: h.Cell % width, Y: h.Cell / width)).ToList();
        bool Seen((int X, int Y) c) => heroes.Any(h => Within(c, h, DebrisSpec.View));
        // No piece of any zone appears in a hero's farthest view, but one by the hull pushed to the grid's last column;
        // none is taken away in one, but past the grid's left edge.
        int newest = last;
        foreach (var p in pieces.Where(p => p.Id > newest))
            if (Seen(CellOf(p)) && !(p.Zone != DebrisSpec.Far.Id && p.Start / 1000 == width - 1))
                return $"{p.Zone} piece {p.Id} appeared at {CellOf(p)}, in a hero's view";
        var now = pieces.Select(p => p.Id).ToHashSet();
        foreach (var (id, x, y) in moved)
            if (!now.Contains(id) && x >= 0 && Seen((x / 1000, y / 1000)))
                return $"piece {id} gone at {(x / 1000, y / 1000)}, in a hero's view";
        // Found only for a tick that spawned round the heroes: walking the grid is what the test costs.
        (int Left, int Right, int Top, int Bottom)? hull = null;
        bool found = false;
        var grid = w.M.Grid;
        (int Left, int Right, int Top, int Bottom)? Hull()
        {
            if (!found) (hull, found) = (HullOf(grid), true);
            return hull;
        }
        bool InStream((int X, int Y) c) => Hull() is var (l, r, t, b) && c.X >= l - Stream.Lead && c.X <= r + Stream.Lead && c.Y >= t - Stream.Margin && c.Y <= b + Stream.Margin;
        int before = last;
        foreach (var p in pieces)
        {
            if (p.X < 0 || p.X >= width * 1000 || p.Y < 0 || p.Y >= height * 1000) return $"piece {p.Id} off the grid";
            if (p.SeenAt > clock) return $"piece {p.Id} seen at {p.SeenAt}, the clock {clock}";
            if (p.Zone == DebrisSpec.Ship.Id && p.SeenAt != 0) return $"stream piece {p.Id} seen";
            if (p.Id <= last || p.Zone != DebrisSpec.Far.Id) continue;
            // New this tick and round the heroes: where the tick's heroes and hull allow it.
            var at = CellOf(p);
            if (p.X != p.Start || p.End != 0 || p.SeenAt != clock) return $"far piece {p.Id} not on its way";
            if (Hull() == null) return $"far piece {p.Id} with no hull";
            if (InStream(at)) return $"far piece {p.Id} at {at}, in the stream's box";
            if (heroes.Any(h => Within(at, h, DebrisSpec.View))) return $"far piece {p.Id} at {at}, in a hero's view";
            // Its hero out past the stream, on whose ring it is, held fewer than its share before this tick's spawns.
            bool owned = heroes.Any(h => !InStream(h) && OnRing(at, h)
                && pieces.Count(q => q.Zone == DebrisSpec.Far.Id && q.Id <= before && Within(CellOf(q), h, Around.Keep)) < Around.PerHero);
            if (!owned) return $"far piece {p.Id} at {at}, on the ring of no hero under its share";
        }
        if (pieces.Count > 0) last = Math.Max(last, pieces[^1].Id);
        return null;
    }

    /// <summary>The hull's bounds walked over the whole grid, not trusting its <c>Built</c> box.</summary>
    private static (int Left, int Right, int Top, int Bottom)? HullOf(IGridQueries grid)
    {
        var structure = grid.StructureLayer;
        int width = grid.Width;
        (int Left, int Right, int Top, int Bottom)? hull = null;
        for (int c = 0; c < structure.Length; c++)
        {
            if (structure[c] == Structure.Empty) continue;
            int x = c % width, y = c / width;
            hull = hull is var (l, r, t, _) ? (Math.Min(l, x), Math.Max(r, x), t, y) : (x, x, y, y);
        }
        return hull;
    }
}
