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

    [Fact]
    public void ItemsSmeltInAFurnaceWhoseOutputsTakeAnyOneItemsYields()
    {
        var furnace = Machines.Get("furnace");
        int outputs = furnace.Slots!.Value - furnace.Inputs!.Value;
        foreach (var spec in Content.Items.All)
            if (spec.Yields != null) Assert.True(SlotsFor(spec.Yields) <= outputs, spec.Id);
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

    [Fact]
    public void SalvageSmeltsInWholeTicksIntoWholeAmountsOfItemsThatAreNotSalvage()
    {
        foreach (var spec in Content.Items.All)
        {
            Assert.True(spec.Yields != null == (spec.SmeltTicks != null), spec.Id);
            if (spec.Yields == null) continue;
            Assert.True(spec.SmeltTicks > 0, spec.Id);
            Assert.NotEmpty(spec.Yields);
            foreach (var (item, amount) in spec.Yields)
            {
                // What the Yields type held in TypeScript: materials and canisters only.
                Assert.True(Materials.TryGet(item, out _) || item == "canister", item);
                Assert.True(Content.Items.TryGet(item, out var yielded), item);
                Assert.Null(yielded!.Yields);
                Assert.True(amount > 0, item);
            }
            // Taken apart in its own slot, one piece yields what that slot and an empty inventory hold.
            Assert.True(SlotsFor(spec.Yields) <= Inventory.Slots, spec.Id);
        }
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
        Assert.True(Inside(around.View, around.RingFrom) && around.RingFrom.X <= around.RingTo.X && around.RingFrom.Y <= around.RingTo.Y);
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
    public void WreckageDriftsByBreaksIntoLootTakesMoreThanOneHitAndNoneOfItIsCaughtByHand()
    {
        Assert.NotEmpty(Wreckage.All);
        Assert.Equal(Wreckage.All.Count, Wreckage.All.Select(w => w.Item).Distinct().Count());
        foreach (var (item, hp) in Wreckage.All)
        {
            Assert.Contains(item, DebrisSpec.Kinds.Select(k => k.Item));
            Assert.True(Content.Items.TryGet(item, out var spec), item);
            // Never carried: broken where it drifts, a stack of one only to be named.
            Assert.Equal(1, spec!.Stack);
            Assert.NotEmpty(Loot.Of(item) ?? []);
            Assert.True(hp > Axe.Damage, item);
        }
    }

    [Fact]
    public void CircuitsComeFromCableRacksAloneOneEachAlwaysAndTheSolarPanelTakesSeveral()
    {
        var sources = Loot.All.Where(l => l.Entries.Any(e => e.Item == "circuit")).Select(l => l.Container);
        Assert.Equal(["cableRack"], sources);
        Assert.Equal(new LootEntry("circuit", 1, 1), Loot.Of("cableRack")!.Single(e => e.Item == "circuit"));
        Assert.False(Crafts.TryGet("circuit", out _));
        Assert.Null(Content.Items.Get("circuit").Yields);
        int panel = Machines.Get("solarPanel").Cost.Single(c => c.Item == "circuit").Count;
        Assert.True(panel > 1, $"the panel takes {panel}");
    }

    [Fact]
    public void TheAxeIsAToolCraftedAtThePodKnownFromTheStartThatHitsAndSwingsInWholeUnits()
    {
        Assert.True(Axe.Damage > 0);
        Assert.True(Axe.SwingTicks > 0);
        Assert.True(Content.Items.TryGet(Axe.Item, out var axe));
        Assert.Equal(1, axe!.Stack);
        Assert.True(Crafts.TryGet(Axe.Item, out _));
        Assert.Contains(Axe.Item, Content.Research.StartKnown);
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
    public void WhatHoldsChargeIsAStackOfOneNamedInNoCostAndAStationFillsItInWholeTicks()
    {
        AllPositive(typeof(Charge));
        var holding = Content.Items.All.Where(i => i.Capacity != null).ToList();
        Assert.NotEmpty(holding);
        Assert.All(holding, i => Assert.Equal(1, i.Stack));
        Assert.All(holding, i => Assert.True(i.Capacity > 0, i.Id));
        Assert.All(holding, i => Assert.Equal(Charge.Belt, i.Equip));
        Assert.Contains(Inventory.Equipment, kind => kind == Charge.Belt);
        // Paying never takes what holds charge, so no charge is lost to a recipe, a build or a cable.
        var costs = Crafts.All.Select(c => c.Cost)
            .Concat(AllBuilds().Select(b => b.Cost))
            .Append(Content.Power.CableCost);
        Assert.All(costs, cost => Assert.DoesNotContain(cost, c => Content.Items.Get(c.Item).Capacity != null));
        foreach (var station in Machines.All.Where(m => m.Charges != null))
        {
            Assert.True(station.Charges > 0 && station.Draws > 0 && station.Slots > 0, station.Kind);
            Assert.Null(station.Inputs);
            Assert.All(holding, i => Assert.Equal(0, i.Capacity!.Value % station.Charges!.Value));
        }
        Assert.DoesNotContain(Content.Items.All, i => i.Electric && i.Capacity != null);
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
        Assert.Equal([("planks", 1), ("frame", 1), ("tube", 1), ("coil", 1), ("wire", 4)], craft!.Cost);
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
        }
    }

    [Fact]
    public void BuildsComeApartFasterThanTheyGoUpByAWholeFactor() => Assert.True(Builds.DeconstructSpeedup > 1);

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

    // Crafts

    [Fact]
    public void EveryCraftHasOnePlaceInTheFabricatorAndEveryPlaceIsACraft()
    {
        var placed = Crafts.Rows.SelectMany(row => row).ToList();
        Assert.Equal(Crafts.All.Select(c => c.Item).Order(StringComparer.Ordinal), placed.Order(StringComparer.Ordinal));
        foreach (var row in Crafts.Rows) Assert.InRange(row.Count, 1, Crafts.RowMost);
    }

    [Fact]
    public void CraftsAtThePodAtAShareOfFullSpeedEveryUnitInWholeTicks()
    {
        Assert.InRange(Crafts.PodSpeed, 1, 1000);
        foreach (var craft in Crafts.All) Assert.Equal(0, craft.Ticks * 1000 % Crafts.PodSpeed);
    }

    [Fact]
    public void CraftsMakeItemsOfWholeUnitsOfOtherRealItemsInWholeTicks()
    {
        Assert.True(Content.Crafting.QueueMost > 0);
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
        foreach (var (item, _) in DebrisSpec.Kinds)
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
    /// Under the opening rule (0027), from what the wreck grants: what is caught, smelted or crafted is
    /// analysed, which opens crafts, which make more to analyse; every craft and blueprint opens so.
    /// </summary>
    [Fact]
    public void EveryCraftAndBlueprintOpensFromTheWrecksStartByAnalysingWhatIsCaughtSmeltedOrCrafted()
    {
        var had = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (item, _) in DebrisSpec.Kinds)
            foreach (var got in Loot.Of(item)?.Select(e => e.Item) ?? [item]) had.Add(got);
        var known = new HashSet<string>(Content.Research.StartKnown, StringComparer.Ordinal);
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => (b.Id, b.Cost))
            .Concat(Crafts.All.Select(c => (Id: c.Item, c.Cost))).ToList();
        for (bool more = true; more;)
        {
            more = false;
            foreach (var item in Content.Items.All.Where(i => had.Contains(i.Id)).SelectMany(i => i.Yields ?? []).ToList())
                more |= had.Add(item.Item);
            foreach (var (id, cost) in recipes.Where(r => !known.Contains(r.Id) && r.Cost.All(c => had.Contains(c.Item))))
                more |= known.Add(id);
            foreach (var craft in Crafts.All.Where(c => known.Contains(c.Item)))
                more |= had.Add(craft.Item);
        }
        Assert.Equal([], recipes.Select(r => r.Id).Where(id => !known.Contains(id)).ToArray());
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
