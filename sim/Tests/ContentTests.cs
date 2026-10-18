using System.Reflection;
using Stillwake.Content;
using Stillwake.Game;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>packages/content/src/content.test.ts, ported.</summary>
public class ContentTests
{
    private const int Cell = 1000;

    /// <summary>Every constant of a table, by name: all of them must be whole numbers.</summary>
    private static IReadOnlyList<(string Name, int Value)> Constants(Type table)
    {
        var fields = table.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral).ToList();
        Assert.All(fields, f => Assert.True(f.FieldType == typeof(int), $"{table.Name}.{f.Name} is no whole number"));
        return fields.Select(f => (f.Name, (int)f.GetRawConstantValue()!)).ToList();
    }

    private static void AllPositive(Type table) =>
        Assert.All(Constants(table), c => Assert.True(c.Value > 0, $"{table.Name}.{c.Name} is {c.Value}"));

    /// <summary>Slots <paramref name="amounts"/> fill in an empty inventory, every item in stacks as full as they go.</summary>
    private static int SlotsFor(IEnumerable<(string Item, int Count)> amounts) =>
        amounts.Sum(a => (a.Count + Content.Items.Get(a.Item).Stack - 1) / Content.Items.Get(a.Item).Stack);

    private static IEnumerable<(IReadOnlyList<(string Item, int Count)> Cost, int BuildTicks)> AllBuilds() =>
        Builds.All.Select(b => (b.Cost, b.BuildTicks)).Concat(Machines.All.Where(m => !m.Fixed).Select(m => (m.Cost, m.BuildTicks)))
            .Concat(Materials.All.Select(m => (m.Cost, m.BuildTicks)));

    // Materials

    [Fact]
    public void MaterialsHaveUniquePositiveCodesSinceSavesStoreTheCode()
    {
        var codes = Materials.All.Select(m => m.Code).ToList();
        Assert.All(codes, c => Assert.True(c > 0));
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void MaterialsHavePositiveWholeNumberStats()
    {
        foreach (var m in Materials.All)
            foreach (int value in (int[])[m.Strength, m.Leak, m.BuildTicks])
                Assert.True(value > 0, m.Id);
    }

    // Machines

    [Fact]
    public void MachinesHaveAFootprintOfAtLeastOneCellAndPortsInsideIt()
    {
        foreach (var m in Machines.All)
        {
            Assert.True(m.Width >= 1 && m.Height >= 1, m.Kind);
            foreach (var p in m.Ports)
                Assert.True(p.X >= 0 && p.X < m.Width && p.Y >= 0 && p.Y < m.Height, m.Kind);
        }
    }

    // Hero

    [Fact]
    public void HeroHasPositiveWholeNumberStats() => AllPositive(typeof(Hero));

    [Fact]
    public void HeroFitsThroughADoorwayThreeCellsTallAndAShaftOneWideAndCentresOnALadder()
    {
        Assert.True(Hero.Height > 2 * Cell);
        Assert.True(Hero.Height < 3 * Cell);
        Assert.True(Hero.Width < Cell);
        Assert.Equal(0, Hero.Width % 2);
    }

    [Fact]
    public void HeroWalksClimbsAndSwimsLessThanACellPerStep()
    {
        foreach (int speed in (int[])[Hero.Walk, Hero.Run, Hero.Climb, Hero.Swim, Jetpack.Top]) Assert.True(speed < Cell);
    }

    [Fact]
    public void HeroJumpsOntoALedgeThreeCellsHighNotFour()
    {
        int rise = 0;
        for (int v = Hero.Jump - Hero.Gravity; v > 0; v -= Hero.Gravity) rise += v;
        Assert.True(rise > 3 * Cell, $"rise {rise}");
        Assert.True(rise < 4 * Cell, $"rise {rise}");
    }

    // Items

    [Fact]
    public void ItemsStackWholeNumbersAndEveryMaterialIsOne()
    {
        foreach (var item in Content.Items.All) Assert.True(item.Stack > 0, item.Id);
        foreach (var material in Materials.All) Assert.Contains(material.Id, Content.Items.All.Select(i => i.Id));
    }

    [Fact]
    public void ItemsGoIntoInventoriesAndMachinesOfWholeSlotsAMachineTakingThemIntoItsFirstFewOrNone()
    {
        Assert.True(Inventory.Slots > 0);
        foreach (var m in Machines.All)
        {
            if (m.Slots != null) Assert.True(m.Slots > 0, m.Kind);
            if (m.Inputs == null) continue;
            Assert.NotNull(m.Slots);
            Assert.True(m.Inputs >= 0 && m.Inputs < m.Slots, m.Kind);
        }
    }

    [Fact]
    public void MachinesBuiltIntoTheShipAreTheLifepodAloneAMapDrawsItAndItCostsNothing()
    {
        Assert.Equal(["lifepod"], Machines.All.Where(m => m.Fixed).Select(m => m.Kind));
        var pod = Machines.Get("lifepod");
        Assert.Empty(pod.Cost);
        Assert.Equal((0, null, null), (pod.Inputs, pod.Makes, pod.Draws));
        Assert.Contains(Legend.Map, e => Legend.MachineOf(e.Char) == "lifepod");
        Assert.Single(Layout.Parse(Wreck.Rows).Machines(), m => m.Kind == "lifepod");
        var (x, y) = Content.Crew.PodHatch;
        Assert.True(x >= 0 && x < pod.Width && y >= 0 && y < pod.Height);
    }

    /// <summary>
    /// Every fed station's outputs take what any one item of its process makes, with the most of itself it may give
    /// back: a grow capsule's two, biomass and a seed's stack (0060).
    /// </summary>
    [Fact]
    public void ItemsSmeltAtEveryFedStationOfTheirProcessWhoseOutputsTakeAnyOneItemsYieldsAndReturns()
    {
        foreach (var station in Stations.All.Where(s => s.Fed))
        {
            var spec = Machines.Get(station.Machine);
            int outputs = spec.Slots!.Value - spec.Inputs!.Value;
            foreach (var item in Content.Items.All.Where(i => i.Yields != null && i.Process == station.Process))
            {
                IReadOnlyList<(string Item, int Count)> made = item.Returns is { } back ? [.. item.Yields!, (item.Id, back.Count - 1)] : item.Yields!;
                Assert.True(SlotsFor(made) <= outputs, $"{station.Machine}: {item.Id}");
            }
        }
    }

    /// <summary>
    /// Two processes, each with a station: smelt (the furnace, the smelter) and grow (the grow capsule); an item's is
    /// one of them. Returns are weights, none negative, some above zero, only on what is worked (0060).
    /// </summary>
    [Fact]
    public void EveryItemsProcessHasAFedStationAndItsReturnsAreWeightsOfWhatIsWorked()
    {
        var processes = Stations.All.Where(s => s.Fed).Select(s => s.Process).Distinct().Order(StringComparer.Ordinal);
        Assert.Equal(["grow", "smelt"], processes);
        Assert.All(Stations.All.Where(s => !s.Fed), s => Assert.Equal("smelt", s.Process));
        foreach (var item in Content.Items.All)
        {
            Assert.Contains(item.Process, processes);
            if (item.Returns is not { } weights) continue;
            Assert.NotNull(item.Yields);
            Assert.All(weights, w => Assert.True(w >= 0, item.Id));
            Assert.True(weights.Sum() > 0, item.Id);
        }
        Assert.Equal(["seed"], Content.Items.All.Where(i => i.Process == "grow").Select(i => i.Id));
        Assert.Equal(["growCapsule"], Stations.All.Where(s => s.Process == "grow").Select(s => s.Machine));
    }

    // The floor

    [Fact]
    public void TheFloorKeepsADropperFromItsOwnStackForAWholeNumberOfTicksButNotLong()
    {
        Assert.True(Floor.OwnPickupDelay > 0);
        Assert.True(Floor.OwnPickupDelay <= 10 * 20);
    }

    [Fact]
    public void TheFloorThrowsInWholeThousandthsATickOutPastTheDriftAndSlowsWhatItThrows()
    {
        AllPositive(typeof(Floor));
        // Thrown out into space, a stack must leave the ship behind, not drift back onto it.
        Assert.True(Floor.ThrowSpeed > Floor.SpaceDrift);
        Assert.True(Floor.Friction < Floor.ThrowSpeed);
        Assert.True(Floor.Gravity < Floor.MaxFall);
    }

    [Fact]
    public void TheFloorPushesAStackOutOfAHullCornerAndTheMiddleOfAnyMachine()
    {
        // A hull corner is two steps from the room; a machine's middle half its narrower side, rounded up.
        Assert.True(Floor.PushReach >= 2);
        foreach (var m in Machines.All) Assert.True(Floor.PushReach >= (Math.Min(m.Width, m.Height) + 1) / 2, m.Kind);
    }

    // Fuel

    [Fact]
    public void FuelBurnsForWholeTicksLongEnoughToSmeltSomethingAndIsNoSalvage()
    {
        var fuels = Content.Items.All.Where(i => i.BurnTicks != null).ToList();
        Assert.NotEmpty(fuels);
        foreach (var spec in fuels)
        {
            Assert.True(spec.BurnTicks > 0, spec.Id);
            Assert.Null(spec.Yields);
        }
        int fastest = Content.Items.All.Min(s => s.SmeltTicks ?? int.MaxValue);
        Assert.True(fuels.Max(s => s.BurnTicks!.Value) > fastest);
    }

    [Fact]
    public void FuelBreathesAFireInWholeUnitsOnWholeBeatsItsFuelASlotThingsArePutInto()
    {
        Constants(typeof(Content.Furnace));
        Assert.True(Content.Furnace.Breath > 0);
        Assert.True(Content.Furnace.BreathEvery > 0);
        // The furnace's fuel after its input; the generator's its only slot. Either is put into.
        Assert.Equal(1, Machines.Get("furnace").FuelSlot);
        Assert.Equal(0, Machines.Get("burnerGenerator").FuelSlot);
        Assert.All(Machines.All.Where(m => m.FuelSlot != null), m => Assert.True(m.FuelSlot < (m.Inputs ?? m.Slots)));
    }

    // Salvage

    /// <summary>
    /// Salvage smelts into materials and canisters, never more salvage; rock into silicon, which smelts on into glass
    /// (0057): whole amounts in whole ticks, each step of a chain as hot as the one before it or hotter, never back
    /// into what it came from.
    /// </summary>
    [Fact]
    public void SmeltingMakesWholeAmountsInWholeTicksOfItemsThatAreNotSalvageAndChainsOnlyUpInHeat()
    {
        string[] refined = ["silicon", "glass", "biomass", "biofuel"];
        foreach (var spec in Content.Items.All)
        {
            Assert.True(spec.Yields != null == (spec.SmeltTicks != null), spec.Id);
            if (spec.Yields == null) continue;
            Assert.True(spec.SmeltTicks > 0, spec.Id);
            Assert.NotEmpty(spec.Yields);
            foreach (var (item, amount) in spec.Yields)
            {
                // What the Yields type held in TypeScript, materials and canisters, then silicon, glass, biomass and biofuel.
                Assert.True(Materials.TryGet(item, out _) || item == "canister" || refined.Contains(item), item);
                Assert.True(Content.Items.TryGet(item, out var yielded), item);
                Assert.Null(Loot.Of(item));
                Assert.True(yielded!.Yields == null || yielded.Heat >= spec.Heat, item);
                Assert.True(amount > 0, item);
            }
            // Taken apart in its own slot, one piece yields what that slot and an empty inventory hold.
            Assert.True(SlotsFor(spec.Yields) <= Inventory.Slots, spec.Id);
        }
        // What is smelted from something and smelts on yields what smelts no more: a chain of two, no cycle.
        var chained = Content.Items.All.Where(i => i.Yields != null && Content.Items.All.Any(f => f.Yields?.Any(y => y.Item == i.Id) == true)).ToList();
        Assert.Equal(["silicon", "biomass"], chained.Select(i => i.Id));
        Assert.All(chained, spec => Assert.All(spec.Yields!, y => Assert.Null(Content.Items.Get(y.Item).Yields)));
    }

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
        // Racks are at most twice as likely a spawn far out as in the stream (0056, amended): a flying hero meets
        // far more spawns, so found circuits stay a bonus and the assembler's are the supply.
        static (int Racks, int Of) Racks(IReadOnlyList<(string Item, int Weight)> kinds) =>
            (kinds.Single(k => k.Item == "cableRack").Weight, kinds.Sum(k => k.Weight));
        var stream = Racks(DebrisSpec.Kinds);
        foreach (var tier in DebrisSpec.Far.Tiers)
        {
            var far = Racks(tier.Kinds);
            Assert.True(far.Racks * stream.Of <= 2 * stream.Racks * far.Of, $"far tier {tier.From}: {far.Racks} racks of {far.Of}, the stream's {stream.Racks} of {stream.Of}");
        }
        // Made besides of silicon and wire, at the circuit assembler alone, which is built of one salvaged (0057).
        Assert.Equal([("silicon", 1), ("wire", 2)], Crafts.Get("circuit").Cost);
        Assert.Equal(["assembler"], Stations.All.Where(s => s.Rows.Any(r => r.Contains("circuit"))).Select(s => s.Machine));
        Assert.Contains(("circuit", 1), Machines.Get("assembler").Cost);
        Assert.Null(Content.Items.Get("circuit").Yields);
    }

    [Fact]
    public void TheAxeIsAToolCraftedAtThePodKnownFromTheStartThatHitsAndSwingsInWholeUnits()
    {
        Assert.True(Weapons.Axe.Damage > 0);
        Assert.True(Weapons.Axe.Pace > 0);
        Assert.True(Content.Items.TryGet(Weapons.Axe.Item, out var axe));
        Assert.Equal(1, axe!.Stack);
        Assert.True(Crafts.TryGet(Weapons.Axe.Item, out _));
        Assert.Contains(Weapons.Axe.Item, Content.Research.StartKnown);
    }

    [Fact]
    public void EveryWeaponIsAToolOfOneCraftedHitsInWholeUnitsAndTheDrillOutdamagesTheAxeOnTheBelt()
    {
        Assert.Equal(Weapons.All.Count, Weapons.All.Select(w => w.Item).Distinct().Count());
        foreach (var weapon in Weapons.All)
        {
            Assert.True(weapon.Damage > 0 && weapon.Pace > 0, weapon.Item);
            Assert.Equal(1, Content.Items.Get(weapon.Item).Stack);
            Assert.True(Crafts.TryGet(weapon.Item, out _), weapon.Item);
            Assert.Same(weapon, Weapons.Of(weapon.Item));
        }
        Assert.Null(Weapons.Of(Harpoon.Item));
        // Damage a second is all that sets them apart (0054): the drill 10, the axe 4.
        Assert.Equal(10, Weapons.Drill.Damage * 20 / Weapons.Drill.Pace);
        Assert.Equal(4, Weapons.Axe.Damage * 20 / Weapons.Axe.Pace);
        // Electric: it draws every tick it runs (0055), so a full battery runs it 600 ticks, 30 s.
        int draw = Content.Items.Get(Weapons.Drill.Item).Draw!.Value;
        Assert.Equal(3, draw);
        Assert.Equal(600, Charge.Battery / Charge.Scale / draw);
        Assert.Null(Content.Items.Get(Weapons.Axe.Item).Draw);
        Assert.Equal([("frame", 1), ("rotor", 1), ("coil", 1), ("gear", 2), ("tube", 1), ("circuit", 2)], Crafts.Get(Weapons.Drill.Item).Cost);
        Assert.Equal(160, Crafts.Get(Weapons.Drill.Item).Ticks);
        Assert.DoesNotContain(Weapons.Drill.Item, Content.Research.StartKnown);
    }

    [Fact]
    public void TheAsteroidBandLiesPastTheTetherIsRareAndCappedAndItsRockBreaksIntoRockToCarry()
    {
        var band = Assert.IsType<HullBand>(DebrisSpec.Asteroids.Anchor);
        Assert.True(band.From * 1000 > Hero.Tether, $"the band's {band.From} rows within the tether's reach");
        Assert.True(band.From <= band.To && band.Lead > 0);
        // About one every 4 minutes, at most 2 out.
        Assert.Equal(4 * 60 * 20, DebrisSpec.Asteroids.Every + DebrisSpec.Asteroids.Jitter / 2);
        Assert.Equal(2, DebrisSpec.Asteroids.Max);
        Assert.All(DebrisSpec.Asteroids.Tiers.SelectMany(t => t.Kinds), k => Assert.All(Loot.Of(k.Item)!, e => Assert.Equal("rock", e.Item)));
        Assert.Equal(120, Wreckage.HpOf("asteroid"));
        Assert.Equal([new LootEntry("rock", 3, 5)], Loot.Of("asteroid"));
        Assert.Equal(10, Content.Items.Get("rock").Stack);
    }

    [Fact]
    public void TheBiggerTheAsteroidTheHarderAndTheMoreRockTheRichOneMostRockForItsHitPoints()
    {
        // In thousandths of a rock a hit point, the middle of each range.
        long Hp(string kind) => Wreckage.HpOf(kind)!.Value;
        long Rock(string kind) => Loot.Of(kind)!.Single() is { Item: "rock" } e ? 1000L * (e.Min + e.Max) / 2 : throw new InvalidOperationException(kind);
        string[] sizes = ["asteroidSmall", "asteroid", "asteroidLarge"];
        for (int i = 1; i < sizes.Length; i++)
        {
            Assert.True(Hp(sizes[i]) > Hp(sizes[i - 1]), sizes[i]);
            Assert.True(Rock(sizes[i]) > Rock(sizes[i - 1]), sizes[i]);
            Assert.True(Rock(sizes[i]) / Hp(sizes[i]) >= Rock(sizes[i - 1]) / Hp(sizes[i - 1]), $"{sizes[i]} pays less a hit point");
        }
        Assert.All(sizes, size => Assert.True(Rock("asteroidRich") / Hp("asteroidRich") >= 3 * Rock(size) / Hp(size) / 2, size));
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

    [Fact]
    public void TheJetpackOutfliesTheSwimIsWornOnTheBackAndACanisterRefillsLessThanAWholeSuit()
    {
        AllPositive(typeof(Jetpack));
        Assert.True(Jetpack.Top > Hero.Swim * 4);
        Assert.True(Jetpack.Accel < Jetpack.Top && Jetpack.Drag < Jetpack.Accel);
        Assert.Equal("back", Content.Items.Get(Jetpack.Item).Equip);
        Assert.Equal(1, Content.Items.Get(Jetpack.Item).Stack);
        Assert.Contains(Inventory.Equipment, kind => kind == "back");
        // Everything worn names a kind of slot there is.
        Assert.All(Content.Items.All.Where(i => i.Equip != null), i => Assert.Contains(i.Equip, Inventory.Equipment));
        Assert.InRange(Content.Crew.CanisterRefill, 1, Content.Crew.Suit - 1);
        Assert.Contains(Crafts.Get(Jetpack.Item).Cost, c => c.Item == Content.Crew.Canister);
    }

    [Fact]
    public void EverythingWornElectricOrAWeaponIsGearKeptInDeathAndBodiesLastAWhileButNotForever()
    {
        // The harpoon is named by hand: it is no weapon, worn nor electric, and still a tool kept (0061).
        Assert.True(Content.Items.Get("harpoon").Gear);
        Assert.All(
            Content.Items.All.Where(i => i.Equip != null || i.Draw != null || Weapons.Of(i.Id) != null),
            i => Assert.True(i.Gear, $"{i.Id} is not gear"));
        // Resources are not: dying costs what was gathered.
        Assert.All(new[] { "scrap", "iron", "canister", "planks", "rock", "circuit" }, id => Assert.False(Content.Items.Get(id).Gear));
        Assert.InRange(Content.Floor.BodyLasts, 60 * World.TicksPerSecond, 60 * 60 * World.TicksPerSecond);
    }

    [Fact]
    public void EveryItemSortsInTheGroupItsOtherFieldsSayAndTheHotbarIsTheBagsFirstRow()
    {
        // Worn is a wearable; gear not worn is a tool: a new tool sorts with the tools without being told.
        Assert.All(Content.Items.All, i => Assert.Equal(i.Equip != null, i.Group == ItemGroup.Wearable));
        Assert.All(Content.Items.All, i => Assert.Equal(i.Gear && i.Equip == null, i.Group == ItemGroup.Tool));
        // Salvage smelts into materials; what is never carried is caught or broken, a stack of one.
        Assert.All(Content.Items.All.Where(i => i.Group == ItemGroup.Salvage), i => Assert.NotNull(i.Yields));
        Assert.All(Content.Items.All.Where(i => i.Group == ItemGroup.Other), i => Assert.Equal(1, i.Stack));
        Assert.Equal(ItemGroup.Salvage, Content.Items.Get("scrapChunk").Group);
        Assert.Equal(ItemGroup.Material, Content.Items.Get("scrap").Group);
        Assert.InRange(Inventory.Hotbar, 1, Inventory.Slots - 1);
        Assert.Equal(0, Inventory.Slots % Inventory.Hotbar);
    }

    [Fact]
    public void WhatHoldsChargeIsAStackOfOneNamedInNoCostAndAStationFillsItInWholeTicks()
    {
        AllPositive(typeof(Charge));
        var holding = Content.Items.All.Where(i => i.Capacity != null).ToList();
        Assert.NotEmpty(holding);
        Assert.All(holding, i => Assert.Equal(1, i.Stack));
        Assert.All(holding, i => Assert.True(i.Capacity > 0, i.Id));
        // Batteries are worn on the belt; a drone holds its own and flies on it (0062).
        Assert.All(holding.Where(i => i.Id != Content.Logistics.Drone), i => Assert.Equal(Charge.Belt, i.Equip));
        Assert.Contains(holding, i => i.Id == Content.Logistics.Drone);
        Assert.Contains(Inventory.Equipment, kind => kind == Charge.Belt);
        // Paying never takes what holds charge, so no charge is lost to a recipe, a build or a cable; but the drone, whose
        // battery becomes its own: made empty, as a crafted battery is, the battery's charge is lost there.
        var costs = Crafts.All.Where(c => c.Item != Content.Logistics.Drone).Select(c => c.Cost)
            .Concat(AllBuilds().Select(b => b.Cost))
            .Append(Content.Power.CableCost);
        Assert.All(costs, cost => Assert.DoesNotContain(cost, c => Content.Items.Get(c.Item).Capacity != null));
        // A station puts in what it draws: it fills every store in whole ticks at full power.
        foreach (var station in Machines.All.Where(m => m.Charges))
        {
            Assert.True(station.Draws > 0 && station.Slots > 0, station.Kind);
            Assert.Null(station.Inputs);
            Assert.All(holding, i => Assert.Equal(0, i.Capacity!.Value % (station.Draws!.Value * Charge.Scale)));
        }
        // Consumers draw as machines do, in whole power units; a store is no consumer.
        Assert.All(Content.Items.All.Where(i => i.Draw != null), i => Assert.True(i.Draw > 0, i.Id));
        Assert.DoesNotContain(Content.Items.All, i => i.Draw != null && i.Capacity != null);
    }

    [Fact]
    public void TheHarpoonShootsFasterThanItReelsAndStopsAPieceWithinTheHandsReachInsideItsRange()
    {
        AllPositive(typeof(Harpoon));
        Assert.True(Harpoon.ReelSpeed < Harpoon.RodSpeed);
        Assert.True(Harpoon.RodSpeed < Harpoon.Range);
        Assert.True(Harpoon.Stop < Hero.Reach);
        Assert.True(Harpoon.Stop < Harpoon.Range);
        Assert.True(Harpoon.RodSpeed < Harpoon.ReturnSpeed);
    }

    [Fact]
    public void AReelFollowsAHeroThatMovesFartherThanThePieceIsHeldBeforeTheGunNoMoreOftenThanItTakesToArrive()
    {
        AllPositive(typeof(Harpoon));
        Assert.True(Harpoon.FollowBeyond < Harpoon.Stop);
        Assert.True(Harpoon.FollowTicks < Harpoon.CooldownTicks);
    }

    [Fact]
    public void TheGrapplePullsTheHeroFasterThanItWalksAndStopsItWithinTheHandsReachOfTheHook()
    {
        // Per tick against the hero's per step: 20 ticks a second, 60 steps.
        Assert.True(Harpoon.PullSpeed > Hero.Walk * 3);
        Assert.True(Harpoon.PullStop < Hero.Reach);
        Assert.True(Harpoon.PullStop < Harpoon.Stop);
        Assert.True(Harpoon.PullSpeed < Harpoon.RodSpeed);
    }

    [Fact]
    public void TheHarpoonIsAToolOfOneCraftedAtThePodAndOpenedByAnalysingWhatItIsMadeOf()
    {
        Assert.True(Content.Items.TryGet(Harpoon.Item, out var harpoon));
        Assert.Equal(1, harpoon!.Stack);
        Assert.True(Crafts.TryGet(Harpoon.Item, out var craft));
        Assert.Equal([("tube", 1), ("coil", 1), ("wire", 4)], craft!.Cost);
        Assert.DoesNotContain(Harpoon.Item, Content.Research.StartKnown);
    }

    [Fact]
    public void SalvageSpawnsOnTheWrecksGridItsLeadRightOfTheHull()
    {
        int right = Wreck.Rows.Max(row => row.TrimEnd().Length - 1);
        Assert.True(right + DebrisSpec.Lead < Wreck.Rows.Max(row => row.Length));
    }

    [Fact]
    public void CentresTheBuildZoneInTheWorldExactlyWithRoomForTheWreckInIt()
    {
        var (world, zone) = ((Content.World.Width, Content.World.Height), (Content.World.ZoneWidth, Content.World.ZoneHeight));
        Assert.InRange(zone.ZoneWidth, 1, world.Width);
        Assert.InRange(zone.ZoneHeight, 1, world.Height);
        // An even difference, so the zone and the wreck sit in the very middle, not a cell off it.
        int wreckWidth = Wreck.Rows.Max(row => row.Length);
        Assert.All(new[] { world.Width - zone.ZoneWidth, world.Height - zone.ZoneHeight, world.Width - wreckWidth, world.Height - Wreck.Rows.Count }, d => Assert.Equal(0, d % 2));
        Assert.True(wreckWidth <= zone.ZoneWidth && Wreck.Rows.Count <= zone.ZoneHeight);
    }

    [Fact]
    public void LeavesBandsOfColdBetweenTheFieldAndTheWorldsEdgeThatEmptyAFullSuitThere()
    {
        AllPositive(typeof(Field));
        // Room past the field every way, so its line is crossed before the grid's wall.
        Assert.True(Content.World.ZoneWidth + 2 * Field.Margin < Content.World.Width);
        Assert.True(Content.World.ZoneHeight + 2 * Field.Margin < Content.World.Height);
        Assert.True(Field.EdgeCost <= Content.Crew.Suit);
        Assert.InRange(Content.Crew.ReportEvery, 1, Content.Crew.PassOut);
    }

    // Builds

    [Fact]
    public void BuildsCostWhatAnEmptyInventoryHoldsWallsIncluded()
    {
        foreach (var cost in AllBuilds().Select(b => b.Cost))
            Assert.True(SlotsFor(cost) <= Inventory.Slots);
    }

    [Fact]
    public void BuildsCostWholeUnitsOfRealItemsSomethingForEachAndTakeWholeTicks()
    {
        foreach (var (cost, buildTicks) in AllBuilds())
        {
            Assert.NotEmpty(cost);
            foreach (var (item, amount) in cost)
            {
                Assert.True(Content.Items.TryGet(item, out _), item);
                Assert.Null(Loot.Of(item));
                Assert.True(amount > 0, item);
            }
            Assert.True(buildTicks > 0);
            // A tick of work at least for each item, so every part a build is paid in has a tick (0063).
            Assert.True(buildTicks >= cost.Sum(c => c.Count), $"{buildTicks} ticks for {cost.Sum(c => c.Count)} items");
        }
    }

    [Fact]
    public void BuildsComeApartFasterThanTheyGoUpByAWholeFactor() => Assert.True(Builds.DeconstructSpeedup > 1);

    /// <summary>
    /// Drones (0062): every number positive and whole; a bay is a charging station of slots on the power grid, a drone an
    /// item of a stack of one holding charge, made at the assembler; a drone crosses at most a cell a tick, launches short
    /// of full, and full covers any job its bay may find and the way home.
    /// </summary>
    [Fact]
    public void ABayChargesItsSlotsADroneIsABatteryOfItsOwnAndEveryLogisticsNumberIsPositive()
    {
        AllPositive(typeof(Content.Logistics));
        var bay = Machines.Get(Content.Logistics.Bay);
        Assert.True(bay.Slots > 0 && bay.Draws > 0 && bay.Plug != null && bay.Charges);
        Assert.Equal((null, null), (bay.Inputs, bay.FuelSlot));
        var drone = Content.Items.Get(Content.Logistics.Drone);
        Assert.Equal((1, Charge.Drone, null), (drone.Stack, drone.Capacity, drone.Draw));
        Assert.Contains(Stations.Get("assembler").Rows, row => row.Contains(Content.Logistics.Drone));
        Assert.InRange(Content.Logistics.Speed, 1, Cell);
        Assert.InRange(Content.Logistics.LaunchAt, 1, Charge.Drone - 1);
        // Corner to corner of the zone three times, to the source, the target and home, and the two cells spared.
        int ticks = (Cell + Content.Logistics.Speed - 1) / Content.Logistics.Speed;
        int across = 4 * Content.Logistics.Reach + bay.Width + bay.Height - 2;
        Assert.True(Charge.Drone >= (3 * across + 2) * ticks * Content.Logistics.Draw * Charge.Scale);
    }

    // Air

    [Fact]
    public void AirIsInWholeUnitsAndATankTakesAtLeastOneCanister()
    {
        AllPositive(typeof(Air));
        Assert.True(Air.Canister <= Air.TankCapacity);
    }

    // Power

    [Fact]
    public void PowerIsInWholeUnitsAndACableCostsSomethingAndSpansMoreThanACell()
    {
        Assert.True(Content.Power.MaxSpan > 1);
        foreach (var (item, amount) in Content.Power.CableCost)
        {
            // What the BuildCost type held in TypeScript: items there are.
            Assert.True(Content.Items.TryGet(item, out _), item);
            Assert.True(amount > 0, item);
        }
        foreach (var m in Machines.All)
            foreach (int? value in (int?[])[m.Makes, m.Draws])
                if (value != null) Assert.True(value > 0, m.Kind);
    }

    [Fact]
    public void PowerGivesEveryMachineMakingOrDrawingPowerOnePlugInsideItsPictureAndNoOtherAPlug()
    {
        foreach (var m in Machines.All)
        {
            bool powered = m.Makes != null || m.Draws != null;
            Assert.True(m.Plug != null == powered, m.Kind);
            if (m.Plug is not { } plug) continue;
            Assert.True(plug.X >= 0 && plug.X < m.Width * 16, m.Kind);
            Assert.True(plug.Y >= 0 && plug.Y < m.Height * 16, m.Kind);
        }
    }

    [Fact]
    public void PowerLetsTheWrecksOnePanelRunItsGeneratorInFull()
    {
        Assert.True(Machines.Get("solarPanel").Makes >= Machines.Get("generator").Draws);
        Assert.NotEmpty(Wreck.Cables);
    }

    // Crew

    [Fact]
    public void CrewBreathesInWholeUnitsAndTicksWithASuitThatLastsSomeBreaths()
    {
        AllPositive(typeof(Content.Crew));
        Assert.True(Content.Crew.Refill < Content.Crew.Suit);
        // By the pod a calm breath gains, even in vacuum; running there holds the suit at worst.
        Assert.True(Content.Crew.PodRefill > 1);
        Assert.True(Content.Crew.PodRefill >= Content.Crew.Exerted);
    }

    // Light

    [Fact]
    public void LightFadesInAirMoreThroughMachinesAndACellThickWallLetsNextToNoneThrough()
    {
        Assert.True(Light.Margin >= 0);
        Assert.True(Light.Rounds >= 1);
        foreach (float keep in (float[])[Light.Air, Light.Machine, Light.WallIn, Light.WallDeep, Light.WallOut])
        {
            Assert.True(keep > 0);
            Assert.True(keep < 1);
        }
        Assert.True(Light.Machine < Light.Air);
        // Through a wall a cell thick, into the cell beyond: a tenth at most.
        Assert.True(Light.WallIn * Light.WallOut * Light.Air < 0.1f);
    }

    [Fact]
    public void LightComesInColoursAbove0SourcesAtMostTwiceWhiteNeverLeavingACornerBlack()
    {
        foreach (var (r, g, b) in (List<(float, float, float)>)[Light.Star, Light.Lamp, Light.Fire])
            foreach (float n in (float[])[r, g, b])
            {
                Assert.True(n > 0);
                Assert.True(n <= 2);
            }
        foreach (float n in (float[])[Light.Ambient.R, Light.Ambient.G, Light.Ambient.B])
        {
            Assert.True(n > 0);
            Assert.True(n < 1);
        }
        Assert.True(Light.Particle > 0);
    }

    // Research

    /// <summary>Every blueprint by the name the build menu gives its card.</summary>
    private static IEnumerable<string> Blueprints()
    {
        static string Titled(string id) => id.Length == 0 ? "" : char.ToUpperInvariant(id[0]) + id[1..];
        return Materials.All.SelectMany(m => new[] { $"wall{Titled(m.Id)}", $"backWall{Titled(m.Id)}" })
            .Concat(Builds.All.Select(b => b.Id))
            .Concat(Machines.All.Where(m => !m.Fixed).Select(m => m.Kind));
    }

    [Fact]
    public void ResearchGrantsOnlyBlueprintsAndCraftsThereAreNamingNoneTwice()
    {
        var names = Blueprints().Concat(Crafts.All.Select(c => c.Item)).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
        foreach (var id in Content.Research.StartKnown) Assert.Contains(id, names);
    }

    // Crafts and stations

    /// <summary>
    /// Every craft is made at some queued station; the pod keeps its twelve and its list stops growing, new crafts
    /// going to machines (0057): the electronics, circuits and solar cells, to the circuit assembler alone, and soil to
    /// the Former alone (0060).
    /// </summary>
    [Fact]
    public void EveryCraftIsMadeSomewhereThePodAllButElectronicsDronesAndSoilAndEveryQueuedStationLaysEachOfItsCraftsOutOnce()
    {
        string[] electronics = ["circuit", "solarCell", "drone"];
        Assert.Equal(Crafts.All.Select(c => c.Item).Except([.. electronics, "soil"]).Order(StringComparer.Ordinal), Stations.Get("lifepod").Rows.SelectMany(r => r).Order(StringComparer.Ordinal));
        Assert.Equal(electronics, Stations.Get("assembler").Rows.SelectMany(r => r));
        Assert.Equal(["former"], Stations.All.Where(s => s.Rows.Any(r => r.Contains("soil"))).Select(s => s.Machine));
        Assert.All(Crafts.All, c => Assert.Contains(Stations.All, s => s.Rows.Any(r => r.Contains(c.Item))));
        foreach (var station in Stations.All.Where(s => !s.Fed))
        {
            var placed = station.Rows.SelectMany(row => row).ToList();
            Assert.Equal(placed.Count, placed.Distinct().Count());
            Assert.All(placed, craft => Assert.True(Crafts.TryGet(craft, out _), $"{station.Machine} lays out {craft}"));
            foreach (var row in station.Rows) Assert.InRange(row.Count, 1, Stations.RowMost);
        }
    }

    /// <summary>
    /// A queued station makes from a queue, with inputs enough for the most distinct items any craft of its needs, but
    /// the pod, paid by hand by design (0057); a fed one takes into its input and smelts at some heat; each has one
    /// energy at most, a machine's, and works every unit and smelt in whole ticks at full share.
    /// </summary>
    [Fact]
    public void StationsAreMachinesQueuedOrFedEachWorkingInWholeTicks()
    {
        Assert.Equal(Stations.All.Count, Stations.All.Select(s => s.Machine).Distinct().Count());
        foreach (var station in Stations.All)
        {
            Assert.True(Machines.TryGet(station.Machine, out var spec), station.Machine);
            Assert.NotNull(spec!.Slots);
            Assert.False(spec.Draws != null && spec.FuelSlot != null, station.Machine);
            Assert.True(station.Speed > 0, station.Machine);
            if (station.Fed)
            {
                Assert.Equal((0, 0), (station.QueueMost, station.Rows.Count));
                Assert.True(spec.Inputs >= 1, station.Machine);
                foreach (var item in Content.Items.All.Where(i => i.SmeltTicks != null))
                    Assert.Equal(0, item.SmeltTicks!.Value * 1000 % station.Speed);
            }
            else
            {
                Assert.True(station.QueueMost > 0, station.Machine);
                int most = station.Rows.SelectMany(r => r).Max(craft => Crafts.Get(craft).Cost.Select(c => c.Item).Distinct().Count());
                if (station.Machine == "lifepod") Assert.Equal(0, spec.Inputs);
                else Assert.True(spec.Inputs >= most, $"{station.Machine}: {spec.Inputs} inputs, a craft of {most} items");
                foreach (var craft in station.Rows.SelectMany(r => r)) Assert.Equal(0, Crafts.Get(craft).Ticks * 1000 % station.Speed);
            }
        }
        Assert.All(Content.Items.All, i => Assert.True(i.Heat > 0, i.Id));
        Assert.InRange(Stations.Get("lifepod").Speed, 1, 1000);
    }

    [Fact]
    public void CraftsMakeItemsOfWholeUnitsOfOtherRealItemsInWholeTicks()
    {
        foreach (var craft in Crafts.All)
        {
            Assert.True(Content.Items.TryGet(craft.Item, out _), craft.Item);
            Assert.NotEmpty(craft.Cost);
            foreach (var (item, amount) in craft.Cost)
            {
                Assert.True(Content.Items.TryGet(item, out _), item);
                Assert.NotEqual(craft.Item, item);
                Assert.True(amount > 0, item);
            }
            Assert.True(craft.Ticks > 0, craft.Item);
        }
    }

    /// <summary>Beyond content.test.ts: nothing costs what no player could ever get.</summary>
    [Fact]
    public void EveryItemACostNamesIsCaughtSmeltedOrCrafted()
    {
        var had = new HashSet<string>(StringComparer.Ordinal);
        // Every zone's: rock, which soil costs, is broken out of the band's asteroids, not the ship's stream.
        foreach (var (item, _) in DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds))
        {
            had.Add(item);
            foreach (var entry in Loot.Of(item) ?? []) had.Add(entry.Item);
        }
        foreach (var item in Content.Items.All)
            foreach (var (made, _) in item.Yields ?? []) had.Add(made);
        foreach (var craft in Crafts.All) had.Add(craft.Item);
        var costs = AllBuilds().Select(b => b.Cost).Concat(Crafts.All.Select(c => c.Cost)).Append(Content.Power.CableCost);
        foreach (var (item, _) in costs.SelectMany(c => c)) Assert.Contains(item, had);
    }

    [Fact]
    public void NoCraftIsMadeOfItselfThroughTheCraftsItsCostNames()
    {
        // Depth first over the crafts' costs: an item met again on the path in is a cycle.
        var done = new HashSet<string>(StringComparer.Ordinal);
        void Walk(string item, List<string> path)
        {
            Assert.False(path.Contains(item), $"a cycle: {string.Join(" > ", path.Append(item))}");
            if (done.Contains(item) || !Crafts.TryGet(item, out var craft)) return;
            path.Add(item);
            foreach (var (part, _) in craft.Cost) Walk(part, path);
            path.RemoveAt(path.Count - 1);
            done.Add(item);
        }
        foreach (var craft in Crafts.All) Walk(craft.Item, []);
    }

    /// <summary>
    /// What the wreck can come to, walking stations (0057): an item is reachable when it is salvage — what any
    /// zone's debris is or breaks into — or made at a reachable station from reachable items: crafted at a queued
    /// one that lays it out, smelted at a fed one hot enough. A station is reachable when it is built into the ship,
    /// or its blueprint is known and its cost reachable; under the opening rule (0027) a blueprint or craft is
    /// known when granted or when every item of its cost is reachable, and so analysed. Debris of the kinds
    /// <paramref name="never"/> is left out, as if it never drifted by.
    /// </summary>
    private static (HashSet<string> Items, HashSet<string> Known) Reachable(IReadOnlyList<Station> stations, params string[] never)
    {
        var had = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (item, _) in DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).Where(k => !never.Contains(k.Item)))
        {
            had.Add(item);
            foreach (var entry in Loot.Of(item) ?? []) had.Add(entry.Item);
        }
        var known = new HashSet<string>(Content.Research.StartKnown, StringComparer.Ordinal);
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => (b.Id, b.Cost))
            .Concat(Crafts.All.Select(c => (Id: c.Item, c.Cost))).ToList();
        var built = new HashSet<string>(StringComparer.Ordinal);
        for (bool more = true; more;)
        {
            more = false;
            foreach (var (id, _) in recipes.Where(r => !known.Contains(r.Id) && r.Cost.All(c => had.Contains(c.Item))))
                more |= known.Add(id);
            foreach (var station in stations)
            {
                var spec = Machines.Get(station.Machine);
                if (spec.Fixed || (known.Contains(station.Machine) && spec.Cost.All(c => had.Contains(c.Item))))
                    more |= built.Add(station.Machine);
            }
            foreach (var station in stations.Where(s => built.Contains(s.Machine)))
            {
                var made = station.Fed
                    ? Content.Items.All.Where(i => had.Contains(i.Id) && i.Yields != null && i.Process == station.Process && i.Heat <= station.Heat).SelectMany(i => i.Yields!).Select(y => y.Item)
                    : station.Rows.SelectMany(r => r).Where(c => known.Contains(c) && Crafts.Get(c).Cost.All(p => had.Contains(p.Item)));
                foreach (var item in made.ToList()) more |= had.Add(item);
            }
        }
        return (had, known);
    }

    /// <summary>Every craft and blueprint opens from the wreck's start, by analysing what is caught or made at its stations.</summary>
    [Fact]
    public void EveryCraftAndBlueprintOpensFromTheWrecksStartByAnalysingWhatItsStationsMake()
    {
        var (_, known) = Reachable(Stations.All);
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => b.Id).Concat(Crafts.All.Select(c => c.Item));
        Assert.Equal([], recipes.Where(id => !known.Contains(id)).ToArray());
    }

    /// <summary>The invariant: every item there is can be had from the wreck, caught or made at a station reachable.</summary>
    [Fact]
    public void EveryItemIsReachableFromTheWreck()
    {
        var (had, _) = Reachable(Stations.All);
        Assert.Equal([], Content.Items.All.Select(i => i.Id).Where(id => !had.Contains(id)).ToArray());
    }

    /// <summary>
    /// The farm walks from salvage (0060): seeds from supply pods and soil clods, soil pressed at the Former or broken
    /// out of clods, the grow capsule opened by what its cost is made of, biomass grown in it and biofuel smelted from
    /// that. Without the capsule, neither biomass nor biofuel; without the Former, soil only from clods; without both,
    /// no soil and no capsule.
    /// </summary>
    [Fact]
    public void TheFarmIsReachableSeedsFromSalvageSoilAtTheFormerBiomassInTheCapsuleBiofuelSmelted()
    {
        var (had, known) = Reachable(Stations.All);
        Assert.Contains("seed", had);
        foreach (var item in new[] { "seed", "soil", "biomass", "biofuel" }) Assert.Contains(item, had);
        Assert.Contains("growCapsule", known);
        var (noCapsule, _) = Reachable([.. Stations.All.Where(s => s.Machine != "growCapsule")]);
        Assert.DoesNotContain("biomass", noCapsule);
        Assert.DoesNotContain("biofuel", noCapsule);
        var noFormer = Stations.All.Where(s => s.Machine != "former").ToList();
        Assert.Contains("soil", Reachable(noFormer).Items);
        var (noSoil, opened) = Reachable(noFormer, "soilClod");
        Assert.DoesNotContain("soil", noSoil);
        Assert.DoesNotContain("growCapsule", opened);
    }

    /// <summary>
    /// Seeds have two sources, supply pods and soil clods: either alone still reaches them, and nothing else gives
    /// them. A clod guarantees its seeds; a pod holds them one time in two.
    /// </summary>
    [Fact]
    public void SeedsComeFromSupplyPodsAndSoilClodsEitherAloneReachingThem()
    {
        Assert.Equal(["supplyPod", "soilClod"], Loot.All.Where(l => l.Entries.Any(e => e.Item == "seed")).Select(l => l.Container));
        Assert.Contains("seed", Reachable(Stations.All, "supplyPod").Items);
        Assert.Contains("seed", Reachable(Stations.All, "soilClod").Items);
        Assert.DoesNotContain("seed", Reachable(Stations.All, "supplyPod", "soilClod").Items);
        Assert.Null(Loot.Of("soilClod")!.Single(e => e.Item == "seed").OneIn);
        Assert.NotNull(Loot.Of("supplyPod")!.Single(e => e.Item == "seed").OneIn);
    }

    /// <summary>
    /// The soil clod: wreckage about as frail as the stream's frailest, the tank cluster and the cable rack, so the axe
    /// breaks it in a few hits; it breaks into 1 to 3 seeds and 1 to 2 soil, always, and nothing else; and it drifts
    /// only in the stream by the ship, rarely: one spawn in 50 or more of it, beside the supply pods.
    /// </summary>
    [Fact]
    public void TheSoilClodIsFrailWreckageOfTheStreamGivingSeedsAndALittleSoilAlways()
    {
        int hp = Wreckage.HpOf("soilClod")!.Value;
        Assert.InRange(hp, Wreckage.HpOf("tankCluster")!.Value, Wreckage.HpOf("cableRack")!.Value);
        Assert.InRange(hp / Weapons.Axe.Damage, 2, 5);
        Assert.Equal([new LootEntry("seed", 1, 3), new LootEntry("soil", 1, 2)], Loot.Of("soilClod"));
        Assert.Equal(1, Content.Items.Get("soilClod").Stack);
        var zones = DebrisSpec.Zones.Where(z => z.Tiers.Any(t => t.Kinds.Any(k => k.Item == "soilClod"))).ToList();
        Assert.Equal(new[] { DebrisSpec.Ship.Id }, zones.Select(z => z.Id));
        int weight = DebrisSpec.Kinds.Single(k => k.Item == "soilClod").Weight, all = DebrisSpec.Kinds.Sum(k => k.Weight);
        Assert.True(weight * 50 <= all, $"{weight} of {all}");
        Assert.True(weight < DebrisSpec.Kinds.Single(k => k.Item == "supplyPod").Weight);
    }

    /// <summary>Heat walks too: the furnace smelts rock into silicon, but glass is had only once the smelter is.</summary>
    [Fact]
    public void GlassIsReachableOnlyThroughTheElectricSmelterSiliconWithoutIt()
    {
        var (had, _) = Reachable([.. Stations.All.Where(s => s.Machine != "smelter")]);
        Assert.Contains("silicon", had);
        Assert.DoesNotContain("glass", had);
        Assert.Contains("glass", Reachable(Stations.All).Items);
    }

    /// <summary>
    /// The solar panel walks the whole chain (0057): rock smelted into silicon, silicon into glass at the smelter, both
    /// with wire into solar cells at the circuit assembler, which salvaged circuits build. Without the assembler, or
    /// the smelter, neither a cell nor a panel; circuits still come from salvage.
    /// </summary>
    [Fact]
    public void SolarCellsAndTheSolarPanelAreReachableOnlyThroughTheAssemblerAndTheSmelter()
    {
        var (had, known) = Reachable(Stations.All);
        Assert.Contains("solarCell", had);
        Assert.Contains("solarPanel", known);
        Assert.Contains(("solarCell", 4), Machines.Get("solarPanel").Cost);
        foreach (var without in new[] { "assembler", "smelter" })
        {
            var (some, opened) = Reachable([.. Stations.All.Where(s => s.Machine != without)]);
            Assert.DoesNotContain("solarCell", some);
            Assert.DoesNotContain("solarPanel", opened);
            Assert.Contains("circuit", some);
        }
    }

    [Fact]
    public void ResearchAnalysesInWholeTicks() => Assert.True(Content.Research.Ticks > 0);

    // What TypeScript's types guaranteed, which C#'s strings do not.

    [Fact]
    public void EveryMachineALegendCharacterDrawsIsAMachine()
    {
        foreach (var (c, _) in Legend.Map)
            if (Legend.MachineOf(c) is { } kind) Assert.True(Machines.TryGet(kind, out _), kind);
    }
}
