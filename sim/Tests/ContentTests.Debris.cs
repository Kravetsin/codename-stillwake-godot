using System.Reflection;
using Stillwake.Content;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>Salvage, debris zones, wreckage, asteroids and derelicts.</summary>
public partial class ContentTests
{
    [Fact]
    public void SalvageDriftsPastAtWholeRatesUnderACellATickAsSalvageOrContainersOfPositiveWeight()
    {
        foreach (int value in (int[])[DebrisSpec.Every, DebrisSpec.Max, DebrisSpec.DriftMin, DebrisSpec.Margin, DebrisSpec.Lead, DebrisSpec.AimRadius])
            Assert.True(value > 0);
        Assert.True(DebrisSpec.Jitter >= 0);
        Assert.True(DebrisSpec.Jitter < DebrisSpec.Every);
        Assert.True(DebrisSpec.DriftMin <= DebrisSpec.DriftMax && DebrisSpec.DriftMax < 1000);
        foreach (var (item, weight) in DebrisSpec.Kinds)
        {
            bool opens = Loot.Of(item) != null;
            Assert.True(opens || Content.Items.Get(item).Yields != null, item);
            Assert.True(weight > 0, item);
        }
    }

    [Fact]
    public void DebrisZonesSpawnOnWholeBeatsUnderTheirCapsTiersInOrderOfDistanceTheFarFieldsBoxesNested()
    {
        var zones = DebrisSpec.Zones;
        Assert.Equal(zones.Count, zones.Select(z => z.Id).Distinct().Count());
        // The stream first: a world with no hero out draws as it did before zones.
        Assert.Same(DebrisSpec.Ship, zones[0]);
        Assert.Contains(DebrisSpec.Far, zones);
        foreach (var zone in zones)
        {
            Assert.True(zone.Every > 0 && zone.Max > 0 && zone.Jitter >= 0 && zone.Jitter < zone.Every, zone.Id);
            Assert.True(zone.DriftMin > 0 && zone.DriftMin <= zone.DriftMax && zone.DriftMax < 1000, zone.Id);
            Assert.NotEmpty(zone.Tiers);
            Assert.Equal(0, zone.Tiers[0].From);
            for (int i = 1; i < zone.Tiers.Count; i++) Assert.True(zone.Tiers[i].From > zone.Tiers[i - 1].From, zone.Id);
            foreach (var tier in zone.Tiers)
            {
                Assert.Equal(tier.Kinds.Count, tier.Kinds.Select(k => k.Item).Distinct().Count());
                foreach (var (item, weight) in tier.Kinds)
                {
                    Assert.True(weight > 0, $"{zone.Id}: {item}");
                    Assert.True(Loot.Of(item) != null || Content.Items.Get(item).Yields != null, $"{zone.Id}: {item}");
                }
            }
        }
        var around = (AroundHeroes)DebrisSpec.Far.Anchor;
        bool Inside(HalfBox a, HalfBox b) => a.X < b.X && a.Y < b.Y;
        // The ring outside what the farthest zoom shows, inside the keep box, inside the hard box.
        Assert.True(Inside(DebrisSpec.View, around.RingFrom) && around.RingFrom.X <= around.RingTo.X && around.RingFrom.Y <= around.RingTo.Y);
        Assert.True(Inside(around.RingTo, around.Keep) && Inside(around.Keep, around.Hard));
        Assert.True(around.PerHero > 0 && around.Linger > 0);
        // Room for four heroes' shares apart, the co-op the game is made for, before the cap holds any back.
        Assert.True(DebrisSpec.Far.Max >= 4 * around.PerHero);
    }

    [Fact]
    public void SalvageOpensContainersIntoWholeCountsOfRealItemsAOneInRollAboveOne()
    {
        foreach (var (container, loot) in Loot.All)
        {
            Assert.True(Content.Items.TryGet(container, out _), container);
            Assert.NotEmpty(loot);
            foreach (var entry in loot)
            {
                Assert.True(Content.Items.TryGet(entry.Item, out _), entry.Item);
                Assert.Null(Loot.Of(entry.Item));
                Assert.True(entry.Min >= 0, entry.Item);
                Assert.True(entry.Max >= Math.Max(1, entry.Min), entry.Item);
                if (entry.OneIn != null) Assert.True(entry.OneIn > 1, entry.Item);
            }
        }
    }

    [Fact]
    public void WreckageDriftsByBreaksIntoLootTakesMoreThanOneHitOfAnyWeaponAndNoneOfItIsCaughtByHand()
    {
        Assert.NotEmpty(Wreckage.All);
        Assert.Equal(Wreckage.All.Count, Wreckage.All.Select(w => w.Item).Distinct().Count());
        var drifting = DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).Select(k => k.Item).ToHashSet();
        foreach (var (item, hp) in Wreckage.All)
        {
            Assert.Contains(item, drifting);
            Assert.True(Content.Items.TryGet(item, out var spec), item);
            // Never carried: broken where it drifts, a stack of one only to be named.
            Assert.Equal(1, spec!.Stack);
            Assert.NotEmpty(Loot.Of(item) ?? []);
            Assert.All(Weapons.All, weapon => Assert.True(hp > weapon.Damage, item));
        }
    }

    [Fact]
    public void CircuitsComeSurelyFromCableRacksOneEachAndRarelyFromDerelictsBesidesTheAssembler()
    {
        var sources = Loot.All.Where(l => l.Entries.Any(e => e.Item == "circuit")).Select(l => l.Container);
        Assert.Equal(["cableRack", "engineBlock", "podCluster"], sources);
        Assert.Equal(new LootEntry("circuit", 1, 1), Loot.Of("cableRack")!.Single(e => e.Item == "circuit"));
        // A derelict's are a chance, one at most, and a twentieth of the rack's or fewer for the hit points (0056,
        // amended): in thousandths of a circuit a hit point, the rack's 1 in 9.
        long rack = 1000 / Wreckage.HpOf("cableRack")!.Value;
        foreach (var source in sources.Skip(1))
        {
            var entry = Loot.Of(source)!.Single(e => e.Item == "circuit");
            Assert.NotNull(entry.OneIn);
            Assert.Equal(1, entry.Max);
            long each = 1000L * (entry.Min + entry.Max) / 2 / entry.OneIn!.Value / Wreckage.HpOf(source)!.Value;
            Assert.True(each * 20 <= rack, $"{source}: {each} thousandths of a circuit a hit point, the rack's {rack}");
        }
        // Racks are at most 2 in 135 a spawn far out (0056, amended): a flying hero meets
        // far more spawns, so found circuits stay a bonus and the assembler's are the supply.
        static (int Racks, int Of) Racks(IReadOnlyList<(string Item, int Weight)> kinds) =>
            (kinds.Single(k => k.Item == "cableRack").Weight, kinds.Sum(k => k.Weight));
        var stream = (Racks: 1, Of: 135);
        foreach (var tier in DebrisSpec.Far.Tiers)
        {
            var far = Racks(tier.Kinds);
            Assert.True(far.Racks * stream.Of <= 2 * stream.Racks * far.Of, $"far tier {tier.From}: {far.Racks} racks of {far.Of}, the stream's {stream.Racks} of {stream.Of}");
        }
        // Made besides of silicon and wire, at the circuit assembler alone, which is built of salvaged ones (0057).
        Assert.Equal(["silicon", "wire"], Crafts.Get("circuit").Cost.Select(c => c.Item));
        Assert.Equal(["assembler"], Stations.All.Where(s => s.Rows.Any(r => r.Contains("circuit"))).Select(s => s.Machine));
        Assert.Contains("circuit", Machines.Get("assembler").Cost.Select(c => c.Item));
        Assert.Null(Content.Items.Get("circuit").Yields);
    }

    [Fact]
    public void TheAsteroidBandLiesPastTheTetherIsRareAndCappedAndItsRockBreaksIntoOreToCarry()
    {
        var band = Assert.IsType<HullBand>(DebrisSpec.Asteroids.Anchor);
        Assert.True(band.From * 1000 > Hero.Tether, $"the band's {band.From} rows within the tether's reach");
        Assert.True(band.From <= band.To && band.Lead > 0);
        // About one every two and a half minutes, at most 3 out.
        Assert.Equal(150 * 20, DebrisSpec.Asteroids.Every + DebrisSpec.Asteroids.Jitter / 2);
        Assert.Equal(3, DebrisSpec.Asteroids.Max);
        Assert.All(DebrisSpec.Asteroids.Tiers.SelectMany(t => t.Kinds), k => Assert.All(Loot.Of(k.Item)!, e => Assert.Equal("asteroidOre", e.Item)));
        Assert.Equal(120, Wreckage.HpOf("asteroid"));
        Assert.Equal([new LootEntry("asteroidOre", 3, 5)], Loot.Of("asteroid"));
        Assert.Equal(250, Content.Items.Get("asteroidOre").Stack);
    }

    [Fact]
    public void TheBiggerTheAsteroidTheHarderAndTheMoreOreTheRichOneMostOreForItsHitPoints()
    {
        // In thousandths of an ore a hit point, the middle of each range.
        long Hp(string kind) => Wreckage.HpOf(kind)!.Value;
        long Ore(string kind) => Loot.Of(kind)!.Single() is { Item: "asteroidOre" } e ? 1000L * (e.Min + e.Max) / 2 : throw new InvalidOperationException(kind);
        string[] sizes = ["asteroidSmall", "asteroid", "asteroidLarge"];
        for (int i = 1; i < sizes.Length; i++)
        {
            Assert.True(Hp(sizes[i]) > Hp(sizes[i - 1]), sizes[i]);
            Assert.True(Ore(sizes[i]) > Ore(sizes[i - 1]), sizes[i]);
            Assert.True(Ore(sizes[i]) / Hp(sizes[i]) >= Ore(sizes[i - 1]) / Hp(sizes[i - 1]), $"{sizes[i]} pays less a hit point");
        }
        Assert.All(sizes, size => Assert.True(Ore("asteroidRich") / Hp("asteroidRich") >= 3 * Ore(size) / Hp(size) / 2, size));
        // The rich only on the band's far edge, its last tier, and far out.
        Assert.DoesNotContain(DebrisSpec.Asteroids.Tiers[0].Kinds, k => k.Item == "asteroidRich");
        Assert.Contains(DebrisSpec.Asteroids.Tiers[^1].Kinds, k => k.Item == "asteroidRich");
        var band = (HullBand)DebrisSpec.Asteroids.Anchor;
        Assert.InRange(DebrisSpec.Asteroids.Tiers[^1].From, Math.Max(band.Lead, 2 * band.From) + 1, 2 * band.To);
    }

    [Fact]
    public void DerelictsAreAsHardAsRockGiveTheWrecksOwnLootAndDriftOnlyFarOutRarely()
    {
        string[] derelicts = ["engineBlock", "cargoHold", "podCluster"];
        string[] wrecks = ["hullSection", "cargoContainer", "machineBlock", "cableRack", "tankCluster", "shieldSlab"];
        static long Expected(IReadOnlyList<LootEntry> loot) => loot.Sum(e => 1000L * (e.Min + e.Max) / 2 / (e.OneIn ?? 1));
        // Every item a container or a wreck gives, but rock: the derelicts give nothing new.
        var given = Loot.All.Where(l => !derelicts.Contains(l.Container)).SelectMany(l => l.Entries).Select(e => e.Item).ToHashSet();
        foreach (var derelict in derelicts)
        {
            Assert.InRange(Wreckage.HpOf(derelict)!.Value, 100, 200);
            var loot = Loot.Of(derelict)!;
            Assert.All(loot, e => Assert.Contains(e.Item, given));
            Assert.DoesNotContain(loot, e => e.Item == "rock");
            // Some three ordinary wrecks' worth or more, counted in items on average, in thousandths.
            Assert.True(Expected(loot) >= 3 * wrecks.Sum(w => Expected(Loot.Of(w)!)) / wrecks.Length, derelict);
        }
        // Never by the ship, nor in the band, nor in the far field's first tier.
        foreach (var zone in DebrisSpec.Zones)
            foreach (var tier in zone.Tiers)
            {
                int total = tier.Kinds.Sum(k => k.Weight), rare = tier.Kinds.Where(k => derelicts.Contains(k.Item)).Sum(k => k.Weight);
                bool outer = zone == DebrisSpec.Far && tier.From >= 128;
                Assert.True(outer || rare == 0, $"{zone.Id} {tier.From}");
                // Rare: one spawn in twenty at the most.
                Assert.True(rare * 20 <= total, $"{zone.Id} {tier.From}: {rare} of {total}");
            }
        Assert.Contains(DebrisSpec.Far.Tiers, t => t.Kinds.Any(k => derelicts.Contains(k.Item)));
    }
}
