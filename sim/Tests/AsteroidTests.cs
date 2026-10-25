using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using HullBand = Stillwake.Content.HullBand;
using Inventory = Stillwake.Content.Inventory;
using Loot = Stillwake.Content.Loot;
using Structure = Stillwake.Content.Structure;
using Weapons = Stillwake.Content.Weapons;
using WreckageSpec = Stillwake.Content.Wreckage;

namespace Stillwake.Sim.Tests;

/// <summary>The asteroid band and the drill (0054): rock round the ship, and every weapon working alike on every wreck.</summary>
public class AsteroidTests
{
    private static readonly HullBand Band = (HullBand)DebrisSpec.Asteroids.Anchor;
    private static readonly string Axe = Weapons.Axe.Item, Drill = Weapons.Drill.Item;
    /// <summary>What the drill draws a tick it runs, in power-ticks (0055).</summary>
    private static readonly int Draw = Stillwake.Content.Items.Get(Weapons.Drill.Item).Draw!.Value;
    private const int S = Stillwake.Content.Charge.Scale;
    private static readonly int Full = Stillwake.Content.Charge.Battery;
    private static readonly int Belt = Inventory.Slots + 2;

    private static IReadOnlyList<Piece> Pieces(Scenario w) => w.M.Debris.Pieces;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Stack Battery(int charge) => new("battery", 1, charge);

    // The band.

    /// <summary>
    /// 400×240, the hull at columns 180 to 191 and rows 100 to 105: its bands are rows 12 to 56 and 149 to 193. Its lengths are
    /// twice the 16 px cell's (0104), as the band's are.
    /// </summary>
    private static readonly Layout Tall = DebrisZonesTests.Open(400, 240, 180, 100, 12, 6);

    [Fact]
    public void SpawnsAsteroidsRarelyOnlyInTheBandsAboveAndBelowTheHullNeverMoreThanTwoOut()
    {
        var w = new Scenario(Tall);
        var seen = new List<Piece>();
        int last = 0, most = 0;
        // Forty minutes.
        for (int t = 0; t < 48_000; t++)
        {
            w.Run();
            foreach (var p in Pieces(w).Where(p => p.Id > last))
            {
                if (p.Zone == DebrisSpec.Asteroids.Id) seen.Add(p);
                last = p.Id;
            }
            most = Math.Max(most, Pieces(w).Count(p => p.Zone == DebrisSpec.Asteroids.Id));
        }
        // About one every four minutes: ten in forty, give or take what the beats and the cap draw.
        Assert.InRange(seen.Count, 5, 13);
        Assert.InRange(most, 1, DebrisSpec.Asteroids.Max);
        Assert.All(seen, p =>
        {
            Assert.StartsWith("asteroid", p.Item);
            int row = p.Y / 1000;
            Assert.True(row is >= 100 - 88 and <= 100 - 44 or >= 105 + 44 and <= 105 + 88, $"an asteroid in row {row}");
            Assert.Equal(((191 + Band.Lead) * 1000 + 500, (180 - Band.Lead) * 1000), (p.Start, p.End));
        });
        Assert.Contains(seen, p => p.Y / 1000 < 100);
        Assert.Contains(seen, p => p.Y / 1000 > 105);
        // None in the gap nor in the stream's rows: the band is past the tether.
        Assert.DoesNotContain(seen, p => p.Y / 1000 is > 56 and < 149);
    }

    [Fact]
    public void KeepsAtMostTwoAsteroidsOutAlongAHullSoLongTheyOutliveTheBeats()
    {
        // A hull two thousand cells long: an asteroid takes some 30 000 ticks to drift past it, so the band fills.
        var w = new Scenario(DebrisZonesTests.Open(2400, 200, 200, 100, w: 2000, h: 2));
        int most = 0, full = 0;
        for (int t = 0; t < 40_000; t++)
        {
            w.Run();
            int n = Pieces(w).Count(p => p.Zone == DebrisSpec.Asteroids.Id);
            most = Math.Max(most, n);
            if (n == DebrisSpec.Asteroids.Max) full++;
        }
        Assert.Equal(DebrisSpec.Asteroids.Max, most);
        Assert.True(full > 10_000, $"the band full for {full} ticks");
    }

    [Fact]
    public void SpawnsNoAsteroidWhereTheBandIsOffTheGridYetTakesItsDraws()
    {
        // A hull filling a short grid: both bands lie past its edges.
        var w = new Scenario(DebrisZonesTests.Open(400, 40, 180, 16, 12, 6));
        w.Run(20_000);
        Assert.DoesNotContain(Pieces(w), p => p.Zone == DebrisSpec.Asteroids.Id);
        var beat = ((DebrisState)w.M.Debris.Slice).Beats.Single(b => b.Zone == DebrisSpec.Asteroids.Id);
        Assert.True(beat.Next > 20_000 - DebrisSpec.Asteroids.Every - DebrisSpec.Asteroids.Jitter);
    }

    // Weapons alike on everything.

    /// <summary>
    /// Twenty wide and eight tall, as DebrisTests' space: the first piece made a wreck of <paramref name="kind"/>,
    /// hero 0 out in space by it, carrying the drill, with <paramref name="batteries"/> on its belt, left to right.
    /// </summary>
    private static Scenario Facing(string kind, params int[] batteries)
    {
        var w = new Scenario(Layout.Parse(
        [
            " #####              ",
            " #...#              ",
            " #...#              ",
            " #.S.#              ",
            " #...#              ",
            " #...#              ",
            " #...#              ",
            " #####              ",
        ])).Run(DebrisSpec.Every);
        Pieces(w)[0].Item = kind;
        w.GiveItems(0, (Drill, 1));
        for (int i = 0; i < batteries.Length; i++) w.Put(Belt + i, Battery(batteries[i]));
        return w.Do(new Enter(1 * 20 + 12)).Run();
    }

    /// <summary><paramref name="times"/> hits with <paramref name="tool"/> by <paramref name="peer"/>, an electric one run first, as the client sends them.</summary>
    private static Scenario Hits(Scenario w, int times, string tool, int peer = 0)
    {
        if (Stillwake.Content.Items.Get(tool).Draw != null) w.Do(new Run(tool, true) { From = peer });
        for (int i = 0; i < times; i++) w.Do(HewTesting.Hit(w, 1, tool) with { From = peer });
        return w;
    }

    public static TheoryData<string, string> EveryWeaponOnEveryKind()
    {
        var data = new TheoryData<string, string>();
        foreach (var weapon in Weapons.All)
            foreach (var kind in new[] { "hullSection", "asteroid" }) data.Add(weapon.Item, kind);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryWeaponOnEveryKind))]
    public void BreaksAWreckOrAnAsteroidWithAnyWeaponItsDamageAHitTillItsHitPointsAreGone(string tool, string kind)
    {
        var w = Facing(kind, Full, Full);
        var weapon = Weapons.Of(tool)!;
        int hp = WreckageSpec.HpOf(kind)!.Value, hits = (hp + weapon.Damage - 1) / weapon.Damage;
        Hits(w, 1, tool).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(weapon.Damage, Pieces(w)[0].Damage);
        Hits(w, hits - 2, tool).Run();
        Assert.Single(Pieces(w));
        Hits(w, 1, tool).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(Pieces(w));
        Assert.All(w.M.Items.Floor, s => Assert.Contains(s.Item, Loot.Of(kind)!.Select(e => e.Item)));
    }

    [Fact]
    public void TakesAsManyHitsOfTheDrillAsOfTheAxeYetFiveTimesLessTime()
    {
        int hp = WreckageSpec.HpOf("asteroid")!.Value;
        Assert.Equal(60, hp / Weapons.Axe.Damage);
        Assert.Equal(60, hp / Weapons.Drill.Damage);
        // In ticks, at each one's pace: 30 s of the axe, 6 s of the drill.
        Assert.Equal(600, hp / Weapons.Axe.Damage * Weapons.Axe.Pace);
        Assert.Equal(120, hp / Weapons.Drill.Damage * Weapons.Drill.Pace);
    }

    [Fact]
    public void BreaksAnAsteroidOnItsSixtiethHitOfTheDrillIntoThreeToFiveRockAlikeInTwoWorlds()
    {
        Scenario Broken(uint seed)
        {
            var w = new Scenario(DebrisZonesTests.Open(20, 8, 1, 1, w: 1, h: 1), seed).Run(DebrisSpec.Every);
            Pieces(w)[0].Item = "asteroid";
            w.GiveItems(0, (Drill, 1)).Do(new SetCreative(true)).Do(new Enter(1 * 20 + 12)).Run();
            Hits(w, 59, Drill).Run();
            Assert.Equal(118, Pieces(w)[0].Damage);
            Assert.Empty(w.M.Items.Floor);
            return Hits(w, 1, Drill).Run();
        }
        for (uint seed = 1; seed <= 20; seed++)
        {
            var (a, b) = (Broken(seed), Broken(seed));
            Assert.Empty(a.Refusals);
            Assert.Empty(Pieces(a));
            Assert.All(a.M.Items.Floor, s => Assert.Equal("rock", s.Item));
            Assert.InRange(a.M.Items.Floor.Sum(s => s.Count), 3, 5);
            Assert.Equal(a.World.Hash(), b.World.Hash());
        }
    }

    // The drill: run by the tick, its hits free (0055).

    [Fact]
    public void HitsWithTheDrillOnlyWhileItsHeroRunsIt()
    {
        var w = Facing("asteroid", Full);
        w.Do(HewTesting.Hit(w, 1, Drill)).Run();
        Assert.Equal(["items.notRunning"], Reasons(w));
        Assert.Equal(0, Pieces(w)[0].Damage);
        // The client sends its run before the frame's hits: the host applies them in that order, on one tick.
        w.Do(new Run(Drill, true)).Do(HewTesting.Hit(w, 1, Drill)).Run();
        Assert.Single(w.Refusals);
        Assert.Equal(Weapons.Drill.Damage, Pieces(w)[0].Damage);
        w.Do(new Run(Drill, false)).Do(HewTesting.Hit(w, 1, Drill)).Run();
        Assert.Equal(["items.notRunning", "items.notRunning"], Reasons(w));
        // What the client asks before it sends says nothing of the run, so the first hit goes with the run.
        Assert.Null(HewTesting.HitProblem(w, 0, 1, Drill));
    }

    [Fact]
    public void AHitOfTheDrillSpendsNothingItsRunPaysByTheTickAndTheAxeNeither()
    {
        var w = Facing("asteroid", Full);
        Hits(w, 1, Axe).Run(5);
        Assert.Equal(Battery(Full), w.M.Items.Inventory(0)[Belt]);
        // Fifty hits on one tick and none on the next three cost the same: four ticks of the draw.
        Hits(w, 50, Drill).Run(4);
        Assert.Equal(Battery(Full - 4 * Draw * S), w.M.Items.Inventory(0)[Belt]);
        Assert.Empty(w.Refusals);
        Assert.Equal(Weapons.Axe.Damage + 50 * Weapons.Drill.Damage, Pieces(w)[0].Damage);
    }

    [Fact]
    public void AFullBatteryRunsTheDrillSixHundredTicksThenItsHitsAreRefusedFlat()
    {
        var w = Facing("asteroid", Full);
        // Held still, so the rock stays by the hero for the battery's whole run.
        Pieces(w)[0].Speed = 0;
        Assert.Equal(600, Full / S / Draw);
        Hits(w, 1, Drill).Run(600);
        Assert.Equal(Battery(0), w.M.Items.Inventory(0)[Belt]);
        Assert.Equal("items.flat", HewTesting.HitProblem(w, 0, 1, Drill));
        w.Do(HewTesting.Hit(w, 1, Drill)).Run();
        Assert.Null(w.M.Items.Running(0));
        Assert.Equal(["items.flat"], Reasons(w));
        Assert.Equal(Weapons.Drill.Damage, Pieces(w)[0].Damage);
        // The axe needs no battery.
        Assert.Null(HewTesting.HitProblem(w, 0, 1, Axe));
    }

    [Fact]
    public void RefusesTheDrillFlatWithNoBatteryWornOrNoneHoldingATickTheBagsLeftAlone()
    {
        var w = Facing("hullSection", Draw * S - 1, Draw * S - 1).Put(5, Battery(Full));
        Hits(w, 1, Drill).Run();
        var bare = Facing("hullSection");
        Hits(bare, 1, Drill).Run();
        // The run is refused flat, and so is its hit: the belt's want says more than the run's.
        Assert.Equal(["items.flat", "items.flat"], Reasons(w));
        Assert.Equal(["items.flat", "items.flat"], Reasons(bare));
        Assert.Equal(0, Pieces(w)[0].Damage);
        Assert.Equal(Battery(Full), w.M.Items.Inventory(0)[5]);
        Assert.Equal(Battery(Draw * S - 1), w.M.Items.Inventory(0)[Belt]);
    }

    [Fact]
    public void RefusesAHitFlatWhileRunningOnceTheBatteryIsTakenOffTheBelt()
    {
        var w = Facing("asteroid", Full);
        Hits(w, 1, Drill).Run();
        w.Do(new Move(new Items.Place(Belt), new Target(5), 1)).Do(HewTesting.Hit(w, 1, Drill)).Run();
        Assert.Equal(["items.flat"], Reasons(w));
        Assert.Null(w.M.Items.Running(0));
    }

    [Fact]
    public void RefusesTheDrillForTheReasonsTheAxeIsRefusedFirst()
    {
        var w = Facing("scrapChunk");
        // A piece caught by hand is passed by, as is one gone: the fan strikes nothing.
        Assert.Equal("debris.nothingToHit", HewTesting.HitProblem(w, 0, 1, Drill));
        Assert.Equal("debris.nothingToHit", HewTesting.HitProblem(w, 0, 9, Drill));
        Assert.Equal("debris.notOutside", HewTesting.HitProblem(w, 1, 1, Drill));
    }

    [Fact]
    public void DrillsWithoutDrainingInCreativeMode()
    {
        var w = Facing("asteroid", Draw * S);
        w.Do(new SetCreative(true)).Run();
        Hits(w, 5, Drill).Run(10);
        var bare = Facing("asteroid");
        bare.Do(new SetCreative(true)).Run();
        Hits(bare, 5, Drill).Run(10);
        Assert.Empty(w.Refusals);
        Assert.Empty(bare.Refusals);
        Assert.Equal(Battery(Draw * S), w.M.Items.Inventory(0)[Belt]);
        Assert.Equal((10, 10), (Pieces(w)[0].Damage, Pieces(bare)[0].Damage));
    }

    [Fact]
    public void LetsTwoHeroesDrillOneRockEachFromItsOwnBelt()
    {
        var w = Facing("asteroid", Full).GiveItems(1, (Drill, 1)).Put(Belt, Battery(Full), peer: 1).Do(new Enter(1 * 20 + 13) { From = 1 }).Run();
        Hits(w, 1, Drill);
        Hits(w, 1, Drill, peer: 1).Do(HewTesting.Hit(w, 1, Axe) with { From = 1 }).Run(2);
        Assert.Empty(w.Refusals);
        Assert.Equal(Full - 2 * Draw * S, w.M.Items.Inventory(0)[Belt]!.Charge);
        Assert.Equal(Full - 2 * Draw * S, w.M.Items.Inventory(1)[Belt]!.Charge);
        Assert.Equal(2 * Weapons.Drill.Damage + Weapons.Axe.Damage, Pieces(w)[0].Damage);
    }

    // Invariants.

    private sealed record Step(string Type, int Cell, int Peer, int Ticks);

    [Property(MaxTest = 25)]
    public Property KeepsEveryZoneUnderItsCapAndSpawnsEveryAsteroidInTheBandOfTheHullItSpawnedBy()
    {
        // Tall enough for a band on at least one side, a small hull anywhere; heroes in and out, walls built and
        // torn down so the hull moves, wrecks struck with either weapon, long waits for the rare beat.
        var map = from width in Gen.Frequency((2, Gen.Choose(10, 70)), (1, Gen.Choose(300, 600)))
                  from height in Gen.Choose(30, 110)
                  from w in Gen.Frequency((2, Gen.Choose(1, 6)), (1, Gen.Choose(1, Math.Min(width, 400))))
                  from h in Gen.Choose(1, 4)
                  from x in Gen.Choose(0, width - w)
                  from y in Gen.Choose(0, height - h)
                  select (width, height, w, h, x, y);
        var step = from type in Gen.Elements("wait", "wait", "enter", "strike", "wall", "breach")
                   from cell in Gen.Choose(0, 20_000)
                   from peer in Gen.Choose(0, 1)
                   from ticks in Gen.Frequency((2, Gen.Choose(1, 50)), (3, Gen.Choose(1000, 9000)))
                   select new Step(type, cell, peer, ticks);
        var cases = from m in map from n in Gen.Choose(1, 12) from steps in step.ArrayOf(n) select (m, steps);
        return Prop.ForAll(cases.ToArbitrary(), c =>
        {
            var (m, steps) = c;
            var w = new Scenario(DebrisZonesTests.Open(m.width, m.height, m.x, m.y, m.w, m.h), (uint)(m.x * 31 + m.y)).Put(Belt, Battery(Full)).GiveItems(0, (Drill, 1));
            int cells = m.width * m.height, last = 0;
            foreach (var s in steps)
            {
                switch (s.Type)
                {
                    case "enter":
                        w.Do(new Enter(s.Cell % cells) { From = s.Peer });
                        break;
                    case "strike":
                        if (Pieces(w).FirstOrDefault(p => WreckageSpec.HpOf(p.Item) != null) is { } wreck)
                        {
                            w.Do(new Run(Drill, true) { From = s.Peer });
                            for (int i = 0; i < 1 + s.Ticks % 40; i++) w.Do(HewTesting.Hit(w, wreck.Id, i % 2 == 0 ? Drill : Axe) with { From = s.Peer });
                        }
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
                    w.Run();
                    if (Broken(w, ref last) is { } broken) return false.Label(broken);
                }
            }
            return true.ToProperty();
        });
    }

    /// <summary>What breaks the zones' invariants after a tick, or null: a zone over its cap, or an asteroid new this tick out of its band or off its lead.</summary>
    private static string? Broken(Scenario w, ref int last)
    {
        var pieces = Pieces(w);
        foreach (var zone in DebrisSpec.Zones)
            if (pieces.Count(p => p.Zone == zone.Id) is var n && n > zone.Max) return $"{n} pieces of {zone.Id} out";
        int width = w.M.Grid.Width, height = w.M.Grid.Height;
        var heroes = w.M.Crew.Heroes.Where(h => h.Cell >= 0 && h.Cell < width * height).Select(h => (X: h.Cell % width, Y: h.Cell / width)).ToList();
        bool Seen(int x, int y) => heroes.Any(h => Math.Abs(x - h.X) <= DebrisSpec.View.X && Math.Abs(y - h.Y) <= DebrisSpec.View.Y);
        foreach (var p in pieces)
        {
            if (p.Id <= last) continue;
            last = p.Id;
            if (p.Zone != DebrisSpec.Asteroids.Id) continue;
            if (Loot.Of(p.Item) is not [{ Item: "rock" }]) return $"a {p.Item} in the asteroid band";
            var cells = Enumerable.Range(0, width * height).Where(c => w.M.Grid.StructureAt(c) != Structure.Empty).ToList();
            if (cells.Count == 0) return $"asteroid {p.Id} spawned with no hull";
            int left = cells.Min(c => c % width), right = cells.Max(c => c % width), top = cells[0] / width, bottom = cells[^1] / width;
            int row = p.Y / 1000;
            bool above = row >= top - Band.To && row <= top - Band.From, below = row >= bottom + Band.From && row <= bottom + Band.To;
            if (!above && !below) return $"asteroid {p.Id} in row {row}, the hull's rows {top} to {bottom}";
            // On its lead, or farther right where no hero sees it start but at the grid's last column, as the stream's.
            int lead = Math.Min(width - 1, right + Band.Lead), start = p.Start / 1000;
            bool onLead = Seen(lead, row) && lead < width - 1 ? start > lead && (!Seen(start, row) || start == width - 1) : start == lead;
            if (!onLead || p.Start % 1000 != 500 || p.End != Math.Max(0, left - Band.Lead) * 1000)
                return $"asteroid {p.Id} not on its lead";
        }
        return null;
    }
}
