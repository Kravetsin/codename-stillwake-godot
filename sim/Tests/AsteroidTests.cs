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
using Weapons = Stillwake.Content.Weapons;
using WreckageSpec = Stillwake.Content.Wreckage;

namespace Stillwake.Sim.Tests;

/// <summary>The asteroid band and the drill (0054): rock round the ship, and every weapon working alike on every wreck.</summary>
public class AsteroidTests
{
    private static readonly HullBand Band = (HullBand)DebrisSpec.Asteroids.Anchor;
    private static readonly string Axe = Weapons.Axe.Item, Drill = Weapons.Drill.Item;
    /// <summary>What a hit of the drill spends, in power-ticks: its draw for each tick of its pace.</summary>
    private static readonly int Spent = DebrisModule.Spent(Weapons.Drill)!.Value;
    private const int S = Stillwake.Content.Charge.Scale;
    private static readonly int Full = Stillwake.Content.Charge.Battery;
    private static readonly int Belt = Inventory.Slots + 2;

    private static IReadOnlyList<Piece> Pieces(Scenario w) => w.M.Debris.Pieces;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Stack Battery(int charge) => new("battery", 1, charge);

    // The band.

    /// <summary>200×120, the hull at columns 90 to 95 and rows 50 to 52: its bands are rows 6 to 28 and 74 to 96.</summary>
    private static readonly Layout Tall = DebrisZonesTests.Open(200, 120, 90, 50);

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
            Assert.Equal("asteroid", p.Item);
            int row = p.Y / 1000;
            Assert.True(row is >= 50 - 44 and <= 50 - 22 or >= 52 + 22 and <= 52 + 44, $"an asteroid in row {row}");
            Assert.Equal(((95 + Band.Lead) * 1000 + 500, (90 - Band.Lead) * 1000), (p.Start, p.End));
        });
        Assert.Contains(seen, p => p.Y / 1000 < 50);
        Assert.Contains(seen, p => p.Y / 1000 > 52);
        // None in the gap nor in the stream's rows: the band is past the tether.
        Assert.DoesNotContain(seen, p => p.Y / 1000 is > 28 and < 74);
    }

    [Fact]
    public void KeepsAtMostTwoAsteroidsOutAlongAHullSoLongTheyOutliveTheBeats()
    {
        // A hull a thousand cells long: an asteroid takes some 30 000 ticks to drift past it, so the band fills.
        var w = new Scenario(DebrisZonesTests.Open(1200, 100, 100, 50, w: 1000, h: 1));
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
        var w = new Scenario(DebrisZonesTests.Open(200, 20, 90, 8));
        w.Run(20_000);
        Assert.DoesNotContain(Pieces(w), p => p.Zone == DebrisSpec.Asteroids.Id);
        var beat = ((DebrisState)w.M.Debris.Slice).Beats.Single(b => b.Zone == DebrisSpec.Asteroids.Id);
        Assert.True(beat.Next > 20_000 - DebrisSpec.Asteroids.Every - DebrisSpec.Asteroids.Jitter);
    }

    // Weapons alike on everything.

    /// <summary>
    /// Twenty wide and eight tall, as DebrisTests' space: the first piece made a wreck of <paramref name="kind"/>,
    /// hero 0 out in space by it with <paramref name="batteries"/> on its belt, left to right.
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
        for (int i = 0; i < batteries.Length; i++) w.Put(Belt + i, Battery(batteries[i]));
        return w.Do(new Enter(1 * 20 + 12)).Run();
    }

    private static Scenario Hits(Scenario w, int times, string tool)
    {
        for (int i = 0; i < times; i++) w.Do(new Hit(1, tool));
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
    public void TakesTwiceTheHitsOfTheAxeWithTheDrillYetTwoAndAHalfTimesLessTime()
    {
        int hp = WreckageSpec.HpOf("asteroid")!.Value;
        Assert.Equal(60, hp / Weapons.Axe.Damage);
        Assert.Equal(120, hp / Weapons.Drill.Damage);
        // In ticks, at each one's pace: 30 s of the axe, 12 s of the drill.
        Assert.Equal(600, hp / Weapons.Axe.Damage * Weapons.Axe.Pace);
        Assert.Equal(240, hp / Weapons.Drill.Damage * Weapons.Drill.Pace);
    }

    [Fact]
    public void BreaksAnAsteroidOnItsHundredAndTwentiethPointIntoThreeToFiveRockAlikeInTwoWorlds()
    {
        Scenario Broken(uint seed)
        {
            var w = new Scenario(DebrisZonesTests.Open(20, 8, 1, 1, w: 1, h: 1), seed).Run(DebrisSpec.Every);
            Pieces(w)[0].Item = "asteroid";
            w.Do(new SetCreative(true)).Do(new Enter(1 * 20 + 12)).Run();
            Hits(w, 119, Drill).Run();
            Assert.Equal(119, Pieces(w)[0].Damage);
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

    // The drill on the belt's batteries.

    [Fact]
    public void DrainsADrillHitFromTheFirstBatteryOnTheBeltThatCanPayItWholeAndNothingForTheAxe()
    {
        // The first holds a hit and a little more; the second is full.
        var w = Facing("asteroid", (Spent + 2) * S, Full);
        Hits(w, 1, Axe).Run();
        Assert.Equal([Battery((Spent + 2) * S), Battery(Full)], w.M.Items.Inventory(0).Skip(Belt).Take(2));
        Hits(w, 1, Drill).Run();
        Assert.Equal([Battery(2 * S), Battery(Full)], w.M.Items.Inventory(0).Skip(Belt).Take(2));
        Hits(w, 1, Drill).Run();
        Assert.Equal([Battery(2 * S), Battery(Full - Spent * S)], w.M.Items.Inventory(0).Skip(Belt).Take(2));
        Assert.Empty(w.Refusals);
        Assert.Equal(Weapons.Axe.Damage + 2 * Weapons.Drill.Damage, Pieces(w)[0].Damage);
    }

    [Fact]
    public void EmptiesAFullBatteryInThreeHundredHitsAndRefusesTheNextFlat()
    {
        var w = Facing("asteroid", Full);
        Assert.Equal(300, Full / S / Spent);
        // A hundred hits a tick, the rock made whole again between, so it never breaks.
        for (int i = 0; i < 3; i++)
        {
            Hits(w, 100, Drill).Run();
            Pieces(w)[0].Damage = 0;
        }
        Assert.Empty(w.Refusals);
        Assert.Equal(Battery(0), w.M.Items.Inventory(0)[Belt]);
        Assert.Equal("debris.flat", w.M.Debris.HitProblem(0, 1, Drill));
        Hits(w, 1, Drill).Run();
        Assert.Equal(["debris.flat"], Reasons(w));
        Assert.Equal(0, Pieces(w)[0].Damage);
        // The axe needs no battery.
        Assert.Null(w.M.Debris.HitProblem(0, 1, Axe));
    }

    [Fact]
    public void RefusesTheDrillFlatWithNoBatteryWornOrNoneHoldingAHitTheBagsLeftAlone()
    {
        var w = Facing("hullSection", Spent * S - 1, Spent * S - 1).Put(0, Battery(Full));
        Hits(w, 1, Drill).Run();
        var bare = Facing("hullSection");
        Hits(bare, 1, Drill).Run();
        Assert.Equal(["debris.flat"], Reasons(w));
        Assert.Equal(["debris.flat"], Reasons(bare));
        Assert.Equal(0, Pieces(w)[0].Damage);
        Assert.Equal(Battery(Full), w.M.Items.Inventory(0)[0]);
        Assert.Equal(Battery(Spent * S - 1), w.M.Items.Inventory(0)[Belt]);
    }

    [Fact]
    public void RefusesTheDrillForTheReasonsTheAxeIsRefusedFirst()
    {
        var w = Facing("scrapChunk");
        Assert.Equal("debris.notWreckage", w.M.Debris.HitProblem(0, 1, Drill));
        Assert.Equal("debris.gone", w.M.Debris.HitProblem(0, 9, Drill));
        Assert.Equal("debris.notOutside", w.M.Debris.HitProblem(1, 1, Drill));
    }

    [Fact]
    public void DrillsWithoutDrainingInCreativeMode()
    {
        var w = Facing("asteroid", Spent * S);
        w.Do(new SetCreative(true)).Run();
        Hits(w, 5, Drill).Run();
        var bare = Facing("asteroid");
        bare.Do(new SetCreative(true)).Run();
        Hits(bare, 5, Drill).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(bare.Refusals);
        Assert.Equal(Battery(Spent * S), w.M.Items.Inventory(0)[Belt]);
        Assert.Equal((5, 5), (Pieces(w)[0].Damage, Pieces(bare)[0].Damage));
    }

    [Fact]
    public void LetsTwoHeroesDrillOneRockEachFromItsOwnBelt()
    {
        var w = Facing("asteroid", Full).Put(Belt, Battery(Full), peer: 1).Do(new Enter(1 * 20 + 13) { From = 1 }).Run();
        w.Do(new Hit(1, Drill)).Do(new Hit(1, Drill) { From = 1 }).Do(new Hit(1, Axe) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Full - Spent * S, w.M.Items.Inventory(0)[Belt]!.Charge);
        Assert.Equal(Full - Spent * S, w.M.Items.Inventory(1)[Belt]!.Charge);
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
            var w = new Scenario(DebrisZonesTests.Open(m.width, m.height, m.x, m.y, m.w, m.h), (uint)(m.x * 31 + m.y)).Put(Belt, Battery(Full));
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
                            for (int i = 0; i < 1 + s.Ticks % 40; i++) w.Do(new Hit(wreck.Id, i % 2 == 0 ? Drill : Axe) { From = s.Peer });
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
        foreach (var p in pieces)
        {
            if (p.Id <= last) continue;
            last = p.Id;
            if (p.Zone != DebrisSpec.Asteroids.Id) continue;
            if (p.Item != "asteroid") return $"a {p.Item} in the asteroid band";
            var cells = Enumerable.Range(0, width * height).Where(c => w.M.Grid.StructureAt(c) != Structure.Empty).ToList();
            if (cells.Count == 0) return $"asteroid {p.Id} spawned with no hull";
            int left = cells.Min(c => c % width), right = cells.Max(c => c % width), top = cells[0] / width, bottom = cells[^1] / width;
            int row = p.Y / 1000;
            bool above = row >= top - Band.To && row <= top - Band.From, below = row >= bottom + Band.From && row <= bottom + Band.To;
            if (!above && !below) return $"asteroid {p.Id} in row {row}, the hull's rows {top} to {bottom}";
            if (p.Start != Math.Min(width - 1, right + Band.Lead) * 1000 + 500 || p.End != Math.Max(0, left - Band.Lead) * 1000)
                return $"asteroid {p.Id} not on its lead";
        }
        return null;
    }
}
