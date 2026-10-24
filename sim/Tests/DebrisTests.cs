using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using FloorStack = Stillwake.Sim.Items.FloorStack;
using Harpoon = Stillwake.Content.Harpoon;
using Inventory = Stillwake.Content.Inventory;
using ItemSpecs = Stillwake.Content.Items;
using Loot = Stillwake.Content.Loot;
using PickUp = Stillwake.Sim.Items.PickUp;
using Weapons = Stillwake.Content.Weapons;
using WreckageSpec = Stillwake.Content.Wreckage;

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
    /// the grid's edges; doubled (0104), the hull spans columns 140 to 151 and rows 50 to 61.
    /// </summary>
    private static readonly Layout Roomy = Layout.Parse(
        Enumerable.Range(0, 60).Select(y => y is >= 25 and <= 30 ? (new string(' ', 70) + "######").PadRight(160) : new string(' ', 160)).ToArray())
        .Doubled();

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

    /// <summary>What <paramref name="peer"/> holds besides the kit it came aboard with (0107).</summary>
    private static Stillwake.Sim.Items.Stack[] Held(Scenario w, int peer) =>
        w.M.Items.Inventory(peer).Where(s => s != null && !Content.Inventory.Kit.Any(k => k.Item == s.Item)).Select(s => s!).ToArray();

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>Every piece of the stream seen in <paramref name="ticks"/> ticks, by id in the order first seen, as it was then; the asteroid band's left out.</summary>
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
                    if (p.Zone == DebrisSpec.Ship.Id) seen.Add(Seen.Of(p));
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
        int start = (151 + DebrisSpec.Lead) * 1000 + 500;
        Assert.Equal((1, start, start, (140 - DebrisSpec.Lead) * 1000), (piece.Id, piece.X, piece.Start, piece.End));
        Assert.Contains(piece.Item, DebrisSpec.Kinds.Select(k => k.Item));
    }

    [Fact]
    public void BeginsAWorldWithItsMapsPiecesAlreadyAlongTheirWayThenSpawnsOnItsBeat()
    {
        var w = new Scenario(Roomy with { DebrisAtStart = 6 }).Run();
        int start = (151 + DebrisSpec.Lead) * 1000 + 500, end = (140 - DebrisSpec.Lead) * 1000;
        Assert.Equal([1, 2, 3, 4, 5, 6], Pieces(w).Select(p => p.Id));
        Assert.All(Pieces(w), p => Assert.Equal((start, end), (p.Start, p.End)));
        Assert.All(Pieces(w), p => Assert.InRange(p.X, end, start));
        Assert.All(Pieces(w), p => Assert.InRange(p.Y / 1000, 50 - DebrisSpec.Margin, 61 + DebrisSpec.Margin));
        // Spread along the way, not bunched at its start.
        Assert.Contains(Pieces(w), p => p.X < start - 60_000);
        w.Run(DebrisSpec.Every - 1);
        Assert.Equal(start, Pieces(w).Single(p => p.Id == 7).X);
    }

    [Fact]
    public void BeginsTheWrecksWorldWithItsPiecesOut()
    {
        var w = new Scenario(Registry.Wreck).Run();
        Assert.Equal(DebrisSpec.AtStart, Pieces(w).Count);
    }

    /// <summary>
    /// The wreck centred in the big world (0048): pieces still drift by in the band of the hull's rows,
    /// from its lead right of the hull to its lead left, none clamped by a grid edge far away.
    /// </summary>
    [Fact]
    public void SpawnsByTheWreckInTheBigWorldInItsHullsBandAsOnItsOwnGrid()
    {
        var w = new Scenario(Registry.Wreck).Run(3 * DebrisSpec.Every);
        var grid = w.M.Grid;
        var walls = Enumerable.Range(0, grid.Width * grid.Height).Where(c => grid.StructureAt(c) != Structure.Empty).ToList();
        int left = walls.Min(c => c % grid.Width), right = walls.Max(c => c % grid.Width);
        int top = walls.Min(c => c / grid.Width), bottom = walls.Max(c => c / grid.Width);
        int start = (right + DebrisSpec.Lead) * 1000 + 500, end = (left - DebrisSpec.Lead) * 1000;
        Assert.Contains(Pieces(w), p => p.Id > DebrisSpec.AtStart);
        Assert.All(Pieces(w), p => Assert.Equal((start, end), (p.Start, p.End)));
        Assert.All(Pieces(w), p => Assert.InRange(p.Y / 1000, top - DebrisSpec.Margin, bottom + DebrisSpec.Margin));
        // The same kinds in the same rows as the wreck drawn on its own grid, moved down with it.
        var drawn = new Scenario(Registry.WreckMap).Run(3 * DebrisSpec.Every);
        var (_, oy) = Layout.OffsetInWorld(Registry.WreckMap.Width, Registry.WreckMap.Height);
        var rows = Pieces(drawn).ToDictionary(p => p.Id, p => (p.Item, p.Y + oy * 1000));
        var shared = Pieces(w).Where(p => rows.ContainsKey(p.Id)).ToList();
        Assert.True(shared.Count > DebrisSpec.AtStart / 2);
        Assert.All(shared, p => Assert.Equal(rows[p.Id], (p.Item, p.Y)));
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
        // Twice the rows of the 16 px cell: twice the watch, and twice again for a piece that stays out twice as long.
        var rows = Watch(new Scenario(Roomy), 160000).Select(p => p.Y).Distinct().Order();
        int top = 50 - DebrisSpec.Margin, bottom = 61 + DebrisSpec.Margin;
        Assert.Equal(Enumerable.Range(top, bottom - top + 1).Select(y => y * 1000 + 500), rows);
    }

    [Fact]
    public void FollowsAShipGrownTallerFoundAgainOnEveryBeat()
    {
        var w = new Scenario(Roomy);
        w.Do(new Place(110 * 320 + 144, Placement.Wall("scrap"))).Run();
        var rows = Watch(w, 80000).Select(p => p.Y / 1000).ToList();
        Assert.Equal(119, rows.Max());
        Assert.Equal(50 - DebrisSpec.Margin, rows.Min());
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
        // A hull so wide that pieces stay out long enough to reach the cap, doubled for the doubled lead and drift.
        var w = new Scenario(Layout.Parse(
            Enumerable.Range(0, 8).Select(y => y == 4 ? new string(' ', 75) + new string('#', 151) : "").ToArray()), doubled: true);
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
            Assert.All(out_, p => Assert.True(p.X >= p.End && p.X <= p.Start && p.Y < 16000));
        }
        Assert.Equal(DebrisSpec.Max, peak);
        // The jitter at work: beats of more than one length.
        Assert.True(gaps.Count > 1);
    }

    [Fact]
    public void DrawsEveryKindTheHeavierOnesMoreOften()
    {
        // Short paths and no cap reached: about ten thousand spawns, each a draw, the rarest, the soil clod, some 150 times.
        var kinds = Watch(new Scenario(Layout.Parse(["   #   "])), 2_000_000).Select(p => p.Item).ToList();
        var share = DebrisSpec.Kinds.Select(k => (k.Weight, Count: kinds.Count(item => item == k.Item))).ToList();
        Assert.All(share, s => Assert.True(s.Count > 0));
        foreach (var heavier in share)
            foreach (var lighter in share.Where(s => s.Weight < heavier.Weight))
                Assert.True(heavier.Count > lighter.Count, $"weight {heavier.Weight} drawn {heavier.Count} times, {lighter.Weight} {lighter.Count}");
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
                    if (!seen.TryGetValue($"{container}.{item}", out var counts)) seen[$"{container}.{item}"] = counts = [];
                    counts.Add(n);
                }
        // The planks a cargo crate holds come in every count of their range.
        var planks = Loot.Of("cargoCrate")![0];
        Assert.Equal(planks.Max - planks.Min + 1, seen["cargoCrate.planks"].Count);
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

    /// <summary>
    /// A catch reports what it put into the inventory, by its piece, for its catcher's popups: a supply pod its
    /// loot as the inventory gained it; a refused grab and a wreck broken into floating loot nothing; and once
    /// the world is loaded again, nothing of before.
    /// </summary>
    [Fact]
    public void ReportsWhatACatchPutIntoTheInventoryAndNothingForARefusalOrAWreckBroken()
    {
        var w = Spawned();
        Pieces(w)[0].Item = "supplyPod";
        int id = Pieces(w)[0].Id;
        w.Do(new Grab(id) { From = 2 }).Run();
        Assert.Null(w.M.Debris.CaughtFrom(id));
        w.Do(new Enter(At(12, 1)) { From = 2 }).Do(new Grab(id) { From = 2 }).Run();
        var gave = w.M.Debris.CaughtFrom(id);
        Assert.NotNull(gave);
        Assert.Equal(Held(w, 2).Select(s => (s.Item, s.Count)), gave);
        Assert.Contains(("canister", 1), gave);
        Assert.Null(w.M.Debris.CaughtFrom(id + 1));
        // A wreck broken by a hit floats its loot, picked up as any floor stack: no catch.
        w.Run(DebrisSpec.Every + DebrisSpec.Jitter);
        var wreck = Pieces(w)[0];
        wreck.Item = "tankCluster";
        for (int i = 0; i < WreckageSpec.HpOf("tankCluster")!.Value; i++) w.Do(new Hit(wreck.Id, Weapons.Axe.Item) { From = 2 });
        w.Run();
        Assert.NotEmpty(w.M.Items.Floor);
        Assert.Null(w.M.Debris.CaughtFrom(wreck.Id));
        Assert.Null(w.Reload().M.Debris.CaughtFrom(id));
    }

    /// <summary>With no room, a catch reports only what fit: what floats is shown when it is picked up, never twice.</summary>
    [Fact]
    public void ReportsOnlyWhatFitAndNothingWhenAllOfACatchFloats()
    {
        var w = Spawned();
        Pieces(w)[0].Item = "cargoCrate";
        int id = Pieces(w)[0].Id;
        int planks = ItemSpecs.Get("planks").Stack;
        w.GiveItems(0, ("canister", (Inventory.Slots - 1) * ItemSpecs.Get("canister").Stack), ("planks", planks - 1));
        w.Do(new Enter(At(12, 1))).Do(new Grab(id)).Run();
        Assert.Equal([("planks", 1)], w.M.Debris.CaughtFrom(id));
        Assert.Equal(planks, Count(w, "planks", 0));
        Assert.NotEmpty(w.M.Items.Floor);
        w.Run(DebrisSpec.Every + DebrisSpec.Jitter);
        int next = Pieces(w)[0].Id;
        w.Do(new Grab(next)).Run();
        Assert.Equal([], w.M.Debris.CaughtFrom(next)!);
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

    // Wreckage and the axe (0034).

    private const string Wreck = "hullSection";
    private static readonly string Axe = Weapons.Axe.Item;
    private static readonly int Hp = WreckageSpec.HpOf(Wreck)!.Value;
    /// <summary>Hits a hull section takes to break: its hit points over the axe's damage, rounded up.</summary>
    private static readonly int HitsToBreak = (Hp + Weapons.Axe.Damage - 1) / Weapons.Axe.Damage;

    /// <summary>The first piece, made a hull section, and heroes <paramref name="peers"/> out in space by it.</summary>
    private static Scenario Wrecked(params int[] peers)
    {
        var w = Spawned();
        Pieces(w)[0].Item = Wreck;
        foreach (int peer in peers) w.Do(new Enter(At(12, 1)) { From = peer });
        return w.Run();
    }

    private static Scenario Hits(Scenario w, int times, int peer = 0, int piece = 1)
    {
        for (int i = 0; i < times; i++) w.Do(new Hit(piece, Axe) { From = peer });
        return w;
    }

    [Fact]
    public void RefusesAHitOnAPieceGoneOrNeverThere()
    {
        var w = Wrecked(0);
        w.Do(new Hit(7, Axe)).Run();
        w.Run(Width * 1000);
        w.Do(new Hit(1, Axe)).Run();
        Assert.Equal(["debris.gone", "debris.gone"], Reasons(w));
    }

    [Fact]
    public void RefusesAHitUnlessBothTheHeroAndTheWrecksMiddleAreInSpace()
    {
        var w = Spawned();
        Pieces(w)[0].Item = Wreck;
        w.Do(new Hit(1, Axe) { From = 1 }).Run();
        w.Do(new Enter(At(3, 4)) { From = 1 }).Do(new Hit(1, Axe) { From = 1 }).Run();
        Assert.Equal(["debris.notOutside", "debris.notOutside"], Reasons(w));
        // Its middle over the room: judged by that cell alone, however far it reaches out.
        Until(w, 4).Do(new Enter(At(12, 1)) { From = 1 });
        Assert.Equal("debris.notOutside", w.M.Debris.HitProblem(1, 1, Axe));
        w.Do(new Hit(1, Axe) { From = 1 }).Run();
        Assert.Equal("debris.notOutside", Reasons(w)[^1]);
        Assert.Equal(0, Pieces(w)[0].Damage);
    }

    [Fact]
    public void RefusesAHitOnAPieceCaughtByHand()
    {
        var w = Spawned();
        Pieces(w)[0].Item = "scrapChunk";
        w.Do(new Enter(At(12, 1))).Run();
        Assert.Equal("debris.notWreckage", w.M.Debris.HitProblem(0, 1, Axe));
        w.Do(new Hit(1, Axe)).Run();
        Assert.Equal(["debris.notWreckage"], Reasons(w));
        Assert.Equal(0, Pieces(w)[0].Damage);
    }

    [Fact]
    public void RefusesToCatchAWreckByHandTooBig()
    {
        var w = Wrecked(0);
        Assert.Equal("debris.tooBig", w.M.Debris.GrabProblem(0, 1));
        w.Do(new Grab(1)).Run();
        Assert.Equal(["debris.tooBig"], Reasons(w));
        Assert.Single(Pieces(w));
        Assert.Empty(Held(w, 0));
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void TakesTheAxesDamageOffAWreckHitByHitKeptAcrossASaveAndLoad()
    {
        var w = Wrecked(0);
        Hits(w, 1).Run();
        Assert.Equal(Weapons.Axe.Damage, Pieces(w)[0].Damage);
        Hits(w, 2).Run();
        Assert.Equal(3 * Weapons.Axe.Damage, Pieces(w)[0].Damage);
        w.Reload();
        Assert.Equal(3 * Weapons.Axe.Damage, Pieces(w)[0].Damage);
        Hits(w, 1).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(4 * Weapons.Axe.Damage, Pieces(w)[0].Damage);
    }

    [Fact]
    public void BreaksAWreckOnTheHitThatTakesItsLastHitPointsIntoItsLootFloatingAtItsMiddle()
    {
        var w = Wrecked(0);
        Hits(w, HitsToBreak - 1).Run();
        var wreck = Pieces(w)[0];
        Assert.True(wreck.Damage < Hp);
        int x = wreck.X, y = wreck.Y;
        Assert.Empty(w.M.Items.Floor);
        Hits(w, 1).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(Pieces(w));
        // Dropped where its middle was, the tick it broke on; drifting from there with all in space.
        var floor = w.M.Items.Floor;
        Assert.NotEmpty(floor);
        var loot = Loot.Of(Wreck)!;
        foreach (var entry in loot)
        {
            int n = floor.Where(s => s.Item == entry.Item).Sum(s => s.Count);
            Assert.InRange(n, entry.Min, entry.Max);
        }
        Assert.All(floor, s =>
        {
            Assert.Contains(s.Item, loot.Select(e => e.Item));
            Assert.Equal(FloorStack.Nobody, s.By);
            Assert.Equal(y / 1000, (s.Y - 1) / 1000);
            Assert.InRange(s.X / 1000, x / 1000 - 1, x / 1000);
        });
        Assert.Empty(Held(w, 0));
        // Dropped by nobody: whoever broke it sweeps it up at once.
        Assert.All(floor, s => Assert.Null(w.M.Items.PickUpProblem(0, s.Id)));
        w.Do(new PickUp(floor[0].Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.NotEmpty(Held(w, 0));
    }

    [Fact]
    public void BreaksAWreckIntoTheSameLootInTwoWorldsSteppedAlike()
    {
        Scenario Broken()
        {
            var w = Wrecked(0);
            return Hits(w, HitsToBreak).Run();
        }
        var (a, b) = (Broken(), Broken());
        Assert.Equal(a.World.Hash(), b.World.Hash());
        Assert.Equal(a.M.Items.Floor.Select(s => (s.Item, s.Count, s.X, s.Y)), b.M.Items.Floor.Select(s => (s.Item, s.Count, s.X, s.Y)));
    }

    /// <summary>Every kind of wreckage there is, for the theories below.</summary>
    public static TheoryData<string> WreckKinds() => [.. WreckageSpec.All.Select(w => w.Item)];

    /// <summary>The first piece, made a wreck of <paramref name="kind"/>, broken by peer 0 out in space by it, in a world of <paramref name="seed"/>.</summary>
    private static Scenario BrokenWreck(string kind, uint seed = 1)
    {
        var w = new Scenario(Space, seed).Run(DebrisSpec.Every);
        Pieces(w)[0].Item = kind;
        w.Do(new Enter(At(12, 1))).Run();
        int hits = (WreckageSpec.HpOf(kind)!.Value + Weapons.Axe.Damage - 1) / Weapons.Axe.Damage;
        Hits(w, hits - 1).Run();
        Assert.Single(Pieces(w));
        return Hits(w, 1).Run();
    }

    [Theory]
    [MemberData(nameof(WreckKinds))]
    public void BreaksEveryKindOfWreckOnItsLastHitPointIntoItsOwnLootAlikeInTwoWorlds(string kind)
    {
        var (a, b) = (BrokenWreck(kind), BrokenWreck(kind));
        Assert.Empty(a.Refusals);
        Assert.Empty(Pieces(a));
        var loot = Loot.Of(kind)!;
        var floor = a.M.Items.Floor;
        Assert.All(floor, s => Assert.Contains(s.Item, loot.Select(e => e.Item)));
        foreach (var entry in loot)
        {
            int n = floor.Where(s => s.Item == entry.Item).Sum(s => s.Count);
            Assert.InRange(n, entry.OneIn == null ? entry.Min : 0, entry.Max);
        }
        Assert.Equal(a.World.Hash(), b.World.Hash());
        Assert.Equal(floor.Select(s => (s.Item, s.Count, s.X, s.Y)), b.M.Items.Floor.Select(s => (s.Item, s.Count, s.X, s.Y)));
    }

    [Fact]
    public void BreaksEveryCableRackIntoExactlyOneCircuitWhateverTheWorldDraws()
    {
        for (uint seed = 1; seed <= 40; seed++)
        {
            var floor = BrokenWreck("cableRack", seed).M.Items.Floor;
            Assert.Equal(1, floor.Where(s => s.Item == "circuit").Sum(s => s.Count));
        }
    }

    [Fact]
    public void LetsTwoHeroesChopOneWreckTheirHitsAddingUp()
    {
        var w = Wrecked(1, 2);
        int half = (HitsToBreak - 1) / 2;
        Hits(w, half, peer: 1);
        Hits(w, half, peer: 2).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(2 * half * Weapons.Axe.Damage, Pieces(w)[0].Damage);
        Hits(w, HitsToBreak - 2 * half, peer: 2).Run();
        Assert.Empty(Pieces(w));
        Assert.NotEmpty(w.M.Items.Floor);
    }

    [Fact]
    public void RefusesAHitAfterTheBreakOnTheSameTickOrTheNext()
    {
        var w = Wrecked(1, 2);
        Hits(w, HitsToBreak - 1, peer: 1).Run();
        // Two heroes land the last blow on one tick: the host's order decides, and the second is too late.
        w.Do(new Hit(1, Axe) { From = 2 }).Do(new Hit(1, Axe) { From = 1 }).Run();
        w.Do(new Hit(1, Axe) { From = 2 }).Run();
        Assert.Equal(["debris.gone", "debris.gone"], Reasons(w));
    }

    [Fact]
    public void BeginsEveryPieceUnhurt()
    {
        Assert.All(Pieces(new Scenario(Registry.Wreck).Run()), p => Assert.Equal(0, p.Damage));
    }

    // The harpoon reels a piece to a point (0044).

    private static Reeling? ReelOf(Scenario w, int id = 1) => Pieces(w).Single(p => p.Id == id).Reel;

    /// <summary>The ticks a reel from (<paramref name="x"/>, <paramref name="y"/>) to its point takes: its longer axis at the reel's speed, rounded up.</summary>
    private static int ReelTicks(int x, int y, int toX, int toY) =>
        Math.Max(1, (Math.Max(Math.Abs(toX - x), Math.Abs(toY - y)) + Harpoon.ReelSpeed - 1) / Harpoon.ReelSpeed);

    /// <summary>The first piece and hero 0 out in space by it.</summary>
    private static Scenario Outside()
    {
        var w = Spawned();
        return w.Do(new Enter(At(12, 1))).Run();
    }

    [Fact]
    public void ReelsAPieceToItsPointNoFasterThanTheReelLandsItExactlyThereThenDriftsItOn()
    {
        var w = Outside();
        var piece = Seen.Of(Pieces(w)[0]);
        const int toX = 14_250, toY = 3_700;
        int ticks = ReelTicks(piece.X, piece.Y, toX, toY);
        w.Do(new Reel(1, toX, toY) { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((0, toX, toY, ticks - 1), (ReelOf(w)!.By, ReelOf(w)!.ToX, ReelOf(w)!.ToY, ReelOf(w)!.Left));
        int x = piece.X, y = piece.Y;
        for (int t = 1; t <= ticks; t++)
        {
            var now = Pieces(w)[0];
            Assert.InRange(Math.Abs(now.X - x), 0, Harpoon.ReelSpeed);
            Assert.InRange(Math.Abs(now.Y - y), 0, Harpoon.ReelSpeed);
            Assert.Equal(t == ticks, now.Reel == null);
            (x, y) = (now.X, now.Y);
            if (t < ticks) w.Run();
        }
        Assert.Equal((toX, toY), (x, y));
        // The line free, it drifts on from its point at its own speed.
        w.Run();
        Assert.Equal((toX - piece.Speed, toY), (Pieces(w)[0].X, Pieces(w)[0].Y));
        Assert.Null(ReelOf(w));
    }

    [Fact]
    public void RefusesAReelOfAPieceThatNeverWasOrIsGone()
    {
        var w = Outside();
        Assert.Equal("debris.gone", w.M.Debris.ReelProblem(0, 7));
        w.Do(new Reel(7, 14_000, 3_000)).Run();
        w.Run(Width * 1000);
        w.Do(new Reel(1, 14_000, 3_000)).Run();
        Assert.Equal(["debris.gone", "debris.gone"], Reasons(w));
    }

    [Fact]
    public void RefusesAReelUnlessBothTheHeroAndThePieceAreInSpaceAsAGrab()
    {
        var w = Spawned();
        var piece = Seen.Of(Pieces(w)[0]);
        w.Do(new Reel(1, 14_000, 3_000) { From = 1 }).Run();
        w.Do(new Enter(At(3, 4)) { From = 1 }).Do(new Reel(1, 14_000, 3_000) { From = 1 }).Run();
        Assert.Equal(["debris.notOutside", "debris.notOutside"], Reasons(w));
        Assert.Null(ReelOf(w));
        Assert.Equal(piece.X - 2 * piece.Speed, Pieces(w)[0].X);
        // Its middle over the room: judged by that cell, as a grab is.
        Until(w, 4).Do(new Enter(At(12, 1)) { From = 1 });
        Assert.Equal("debris.notOutside", w.M.Debris.ReelProblem(1, 1));
        w.Do(new Reel(1, 14_000, 3_000) { From = 1 }).Run();
        Assert.Equal("debris.notOutside", Reasons(w)[^1]);
        Assert.Null(ReelOf(w));
    }

    [Fact]
    public void CatchesAPieceMidReelLineAndAll()
    {
        var w = Outside();
        var piece = Seen.Of(Pieces(w)[0]);
        w.Do(new Reel(1, 13_500, 1_500)).Run(3);
        Assert.NotNull(ReelOf(w));
        Assert.Null(w.M.Debris.GrabProblem(0, 1));
        w.Do(new Grab(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(Pieces(w));
        Assert.All(Held(w, 0), s => Assert.Contains(s.Item, Gives(piece.Item)));
        Assert.NotEmpty(Held(w, 0));
    }

    [Fact]
    public void ChopsAWreckMidReelWhichComesOnTillItBreaksLineAndAll()
    {
        var w = Wrecked(0);
        const int toX = 13_500, toY = 1_500;
        w.Do(new Reel(1, toX, toY)).Run();
        int left = ReelOf(w)!.Left;
        Hits(w, HitsToBreak - 1).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((HitsToBreak - 1) * Weapons.Axe.Damage, Pieces(w)[0].Damage);
        Assert.Equal(left - 1, ReelOf(w)!.Left);
        w.Run(left - 1);
        Assert.Equal((toX, toY), (Pieces(w)[0].X, Pieces(w)[0].Y));
        Assert.Null(ReelOf(w));
        w.Do(new Reel(1, 16_000, 2_000)).Run();
        Hits(w, 1).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(Pieces(w));
        Assert.NotEmpty(w.M.Items.Floor);
    }

    [Fact]
    public void KeepsAPointBeyondTheGridOnItsEdgeSoThePieceStaysOnTheGrid()
    {
        var w = Outside();
        w.Do(new Reel(1, 999_999, -5_000)).Run();
        Assert.Equal((Width * 1000 - 1, 0), (ReelOf(w)!.ToX, ReelOf(w)!.ToY));
        w.Run(ReelOf(w)!.Left);
        Assert.Equal((Width * 1000 - 1, 0), (Pieces(w)[0].X, Pieces(w)[0].Y));
        w.Do(new Reel(1, -50_000, 99_999)).Run();
        Assert.Equal((0, 8 * 1000 - 1), (ReelOf(w)!.ToX, ReelOf(w)!.ToY));
        w.Run(ReelOf(w)!.Left);
        Assert.Equal((0, 7_999), (Pieces(w)[0].X, Pieces(w)[0].Y));
        // At its end already: the next drift takes it.
        w.Run();
        Assert.Empty(Pieces(w));
    }

    [Fact]
    public void LetsTheLastReelWinByAnyoneOnALaterTickOrTheSame()
    {
        var w = Spawned().Do(new Enter(At(12, 1)) { From = 1 }).Do(new Enter(At(13, 1)) { From = 2 }).Run();
        w.Do(new Reel(1, 12_000, 1_000) { From = 1 }).Run(2);
        var piece = Pieces(w)[0];
        int ticks = ReelTicks(piece.X, piece.Y, 15_000, 6_000);
        w.Do(new Reel(1, 15_000, 6_000) { From = 2 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((2, 15_000, 6_000, ticks - 1), (ReelOf(w)!.By, ReelOf(w)!.ToX, ReelOf(w)!.ToY, ReelOf(w)!.Left));
        w.Run(ticks - 1);
        Assert.Equal((15_000, 6_000), (Pieces(w)[0].X, Pieces(w)[0].Y));
        // Two on one tick: the host's order decides, and the later one is where it goes.
        w.Do(new Reel(1, 13_000, 2_000) { From = 2 }).Do(new Reel(1, 16_000, 4_000) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((1, 16_000, 4_000), (ReelOf(w)!.By, ReelOf(w)!.ToX, ReelOf(w)!.ToY));
    }

    [Fact]
    public void ReelsOnAlikeAfterASaveAndLoadMidReel()
    {
        Scenario Reeled()
        {
            var w = Outside();
            return w.Do(new Reel(1, 13_250, 6_400)).Run(3);
        }
        var (kept, loaded) = (Reeled(), Reeled().Reload());
        var reel = ReelOf(loaded)!;
        Assert.Equal((ReelOf(kept)!.Left, 13_250, 6_400), (reel.Left, reel.ToX, reel.ToY));
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
        for (int t = 0, ticks = reel.Left + 5; t < ticks; t++)
        {
            kept.Run();
            loaded.Run();
            Assert.Equal(Seen.Of(Pieces(kept)[0]), Seen.Of(Pieces(loaded)[0]));
        }
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
        Assert.Null(ReelOf(loaded));
    }

    [Fact]
    public void BeginsEveryPieceUnreeled()
    {
        Assert.All(Pieces(new Scenario(Registry.Wreck).Run()), p => Assert.Null(p.Reel));
    }

    // The README's invariants, whatever happens: a hull grown and cut down, heroes in and out of space
    // grabbing pieces out, gone and never spawned, reeling them to points on and off the grid, and long waits.

    private sealed record Step(string Type, int Cell = 0, int Peer = 0, int Ticks = 0, int ToX = 0, int ToY = 0);

    [Property(MaxTest = 60)]
    public Property KeepsIdsInSpawnOrderNoMoreThanTheCapAndEveryPieceOnTheGridItsPathAndItsHullsBand()
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
                       (2, Gen.Constant("wait")), (2, Gen.Constant("grab")), (2, Gen.Constant("enter")), (2, Gen.Constant("chop")), (2, Gen.Constant("reel")),
                       (1, Gen.Constant("wall")), (1, Gen.Constant("breach")))
                   from cell in Gen.Choose(-2, 3000)
                   from peer in Gen.Choose(0, 2)
                   from ticks in Gen.Frequency((3, Gen.Choose(1, 40)), (2, Gen.Choose(150, 400)), (1, Gen.Choose(1000, 6000)))
                       // Points mostly on a small grid, now and then anywhere, far off every edge.
                   from toX in Gen.Frequency((3, Gen.Choose(-2_000, 26_000)), (1, Gen.Choose(-300_000, 300_000)))
                   from toY in Gen.Frequency((3, Gen.Choose(-2_000, 14_000)), (1, Gen.Choose(-300_000, 300_000)))
                   select new Step(kind, cell, peer, ticks, toX, toY);
        var cases = from m in map from n in Gen.Choose(0, 25) from steps in step.ArrayOf(n) select (m, steps);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var (m, steps) = c;
            var rows = Enumerable.Range(0, m.h)
                .Select(y => new string(Enumerable.Range(0, m.w).Select(x => m.Walls[y * m.w + x] ? '#' : ' ').ToArray()))
                .ToArray();
            var w = new Scenario(Layout.Parse(rows));
            int cells = m.w * m.h, last = 0;
            var reeled = new HashSet<int>();
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
                    case "chop":
                        // A wreck when one is out, else any piece; a few swings landing on one tick.
                        var wreck = pieces.FirstOrDefault(p => WreckageSpec.HpOf(p.Item) != null) ?? (pieces.Count > 0 ? pieces[Math.Abs(s.Cell) % pieces.Count] : null);
                        for (int n = 0; n < s.Ticks % 4 + 1; n++) w.Do(new Hit(wreck?.Id ?? s.Cell % (last + 3), n % 2 == 0 ? Axe : Weapons.Drill.Item) { From = s.Peer });
                        break;
                    case "reel":
                        int reel = pieces.Count > 0 && s.Cell % 3 != 0 ? pieces[Math.Abs(s.Cell) % pieces.Count].Id : s.Cell % (last + 3);
                        reeled.Add(reel);
                        w.Do(new Reel(reel, s.ToX, s.ToY) { From = s.Peer });
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
                    var broken = Broken(w, ref last, reeled);
                    if (broken != null) return false.Label(broken);
                }
            }
            return true.ToProperty();
        });
    }

    /// <summary>
    /// What breaks the README's invariants after a tick, or null; <paramref name="last"/> is the newest id seen so
    /// far, <paramref name="reeled"/> the pieces a reel was sent for, which may have been pulled right of their start.
    /// </summary>
    private static string? Broken(Scenario w, ref int last, IReadOnlySet<int> reeled)
    {
        var pieces = Pieces(w);
        // The stream's own; a hero entered out past it brings the far field, whose invariants are DebrisZonesTests'.
        int stream = pieces.Count(p => p.Zone == DebrisSpec.Ship.Id);
        if (stream > DebrisSpec.Max) return $"{stream} pieces of the stream out";
        int width = w.M.Grid.Width, height = w.M.Grid.Height;
        var heroes = w.M.Crew.Heroes.Where(h => h.Cell >= 0 && h.Cell < width * height).Select(h => (X: h.Cell % width, Y: h.Cell / width)).ToList();
        bool Seen(int x, int y) => heroes.Any(h => Math.Abs(x - h.X) <= DebrisSpec.View.X && Math.Abs(y - h.Y) <= DebrisSpec.View.Y);
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            if (i > 0 && p.Id <= pieces[i - 1].Id) return "ids out of spawn order";
            if (p.X < 0 || p.X >= width * 1000 || p.Y < 0 || p.Y >= height * 1000) return $"piece {p.Id} off the grid";
            // Past its end only while a hero sees it.
            if ((p.X < p.End && !Seen(p.X / 1000, p.Y / 1000)) || (p.X > p.Start && !reeled.Contains(p.Id))) return $"piece {p.Id} off its path";
            if (p.Reel is { } reel && (reel.Left <= 0 || reel.ToX < 0 || reel.ToX >= width * 1000 || reel.ToY < 0 || reel.ToY >= height * 1000))
                return $"piece {p.Id} reeled to ({reel.ToX}, {reel.ToY}) in {reel.Left} ticks";
            int hp = WreckageSpec.HpOf(p.Item) ?? 0;
            if (hp == 0 ? p.Damage != 0 : p.Damage < 0 || p.Damage >= hp) return $"piece {p.Id}, a {p.Item}, at damage {p.Damage}";
            if (p.Id <= last) continue;
            // New this tick: in the band of the hull it spawned by, which the tick's commands have shaped.
            if (p.Id != last + 1) return $"piece {p.Id} after {last}: an id skipped or reused";
            last = p.Id;
            if (p.Zone != DebrisSpec.Ship.Id) continue;
            var hull = HullOf(w.M.Grid);
            if (hull is not var (left, right, top, bottom)) return $"piece {p.Id} spawned with no hull";
            int row = p.Y / 1000;
            if (row < Math.Max(0, top - DebrisSpec.Margin) || row > Math.Min(height - 1, bottom + DebrisSpec.Margin))
                return $"piece {p.Id} in row {row}, out of the hull's band";
            // On its lead, or farther right where no hero sees it start but at the grid's last column.
            int lead = Math.Min(width - 1, right + DebrisSpec.Lead), start = p.Start / 1000;
            bool onLead = Seen(lead, row) && lead < width - 1 ? start > lead && (!Seen(start, row) || start == width - 1) : start == lead;
            if (!onLead || p.Start % 1000 != 500 || p.End != Math.Max(0, left - DebrisSpec.Lead) * 1000)
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
