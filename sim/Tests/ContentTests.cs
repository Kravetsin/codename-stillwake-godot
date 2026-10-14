using System.Reflection;
using Stillwake.Content;
using Stillwake.Game;
using Xunit;

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
        Builds.All.Select(b => (b.Cost, b.BuildTicks)).Concat(Machines.All.Select(m => (m.Cost, m.BuildTicks)));

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
            foreach (int value in (int[])[m.Strength, m.Leak, m.Cost, m.BuildTicks])
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
        foreach (int speed in (int[])[Hero.Walk, Hero.Climb, Hero.Swim]) Assert.True(speed < Cell);
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
    public void ItemsGoIntoInventoriesAndMachinesOfWholeSlotsAMachineTakingThemIntoItsFirstFew()
    {
        Assert.True(Inventory.Slots > 0);
        foreach (var m in Machines.All)
        {
            if (m.Slots != null) Assert.True(m.Slots > 0, m.Kind);
            if (m.Inputs == null) continue;
            Assert.NotNull(m.Slots);
            Assert.True(m.Inputs > 0 && m.Inputs < m.Slots, m.Kind);
        }
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
    public void FuelBreathesAFurnacesFireInWholeUnitsOnWholeBeatsItsFuelASlotItPutsThingsInto()
    {
        Constants(typeof(Content.Furnace));
        Assert.True(Content.Furnace.Breath > 0);
        Assert.True(Content.Furnace.BreathEvery > 0);
        Assert.True(Content.Furnace.FuelSlot > 0);
        Assert.True(Content.Furnace.FuelSlot < (Machines.Get("furnace").Inputs ?? 0));
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
        foreach (int value in (int[])[Debris.Every, Debris.Max, Debris.DriftMin, Debris.Margin, Debris.Lead, Debris.AimRadius])
            Assert.True(value > 0);
        Assert.True(Debris.Jitter >= 0);
        Assert.True(Debris.Jitter < Debris.Every);
        Assert.True(Debris.DriftMin <= Debris.DriftMax && Debris.DriftMax < 1000);
        foreach (var (item, weight) in Debris.Kinds)
        {
            bool opens = Loot.Of(item) != null;
            Assert.True(opens || Content.Items.Get(item).Yields != null, item);
            Assert.True(weight > 0, item);
        }
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
    public void SalvageSpawnsOnTheWrecksGridItsLeadRightOfTheHull()
    {
        int right = Wreck.Rows.Max(row => row.TrimEnd().Length - 1);
        Assert.True(right + Debris.Lead < Wreck.Rows.Max(row => row.Length));
    }

    // Builds

    [Fact]
    public void BuildsCostWhatAnEmptyInventoryHoldsWallsIncluded()
    {
        var walls = Materials.All.Select(m => (IReadOnlyList<(string, int)>)[(m.Id, m.Cost)]);
        foreach (var cost in AllBuilds().Select(b => b.Cost).Concat(walls))
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
        AllPositive(typeof(Crew));
        Assert.True(Crew.Refill < Crew.Suit);
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
            .Concat(Machines.All.Select(m => m.Kind));
    }

    [Fact]
    public void ResearchLearnsEveryBlueprintOnceKnownFromTheStartFromAMaterialOrFromASchematic()
    {
        var learned = Content.Research.StartKnown.Concat(Content.Research.Unlocks.SelectMany(u => u.Blueprints)).Concat(Content.Research.Schematics);
        Assert.Equal(Blueprints().Order(StringComparer.Ordinal), learned.Order(StringComparer.Ordinal));
        foreach (var (item, _) in Content.Research.Unlocks) Assert.True(Content.Items.TryGet(item, out _), item);
        Assert.True(Content.Items.TryGet("schematic", out _));
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
