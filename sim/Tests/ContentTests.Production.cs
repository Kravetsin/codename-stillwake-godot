using System.Reflection;
using Stillwake.Content;
using Stillwake.Game;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>Smelting, fuel, crafts, stations, research and what is reachable from the wreck.</summary>
public partial class ContentTests
{
    /// <summary>
    /// Every fed station's outputs take the most any one smelt of its process makes, with the most of itself it may give
    /// back and every byproduct at its most (<c>ItemsModule.MostMade</c>): a grow capsule's two, biomass and a seed's stack (0060).
    /// </summary>
    [Fact]
    public void ItemsSmeltAtEveryFedStationOfTheirProcessWhoseOutputsTakeTheMostOneSmeltMakes()
    {
        foreach (var station in Stations.All.Where(s => s.Fed))
        {
            var spec = Machines.Get(station.Machine);
            int outputs = spec.Slots!.Value - spec.Inputs!.Value;
            foreach (var item in Content.Items.All.Where(i => i.Yields != null && i.Process == station.Process))
                Assert.True(SlotsFor(Sim.Items.ItemsModule.MostMade(item)) <= outputs, $"{station.Machine}: {item.Id}");
        }
    }

    /// <summary>
    /// A smelt takes from one up to a stack of its input, its input slot holding them all; byproducts are loot rolls of
    /// what is worked, a one-in of two or more where given (one in one is a yield), a count never negative.
    /// </summary>
    [Fact]
    public void SmeltsTakeAtMostAStackAndByproductsAreRollsOfWhatIsWorked()
    {
        foreach (var item in Content.Items.All)
        {
            Assert.InRange(item.SmeltTakes, 1, item.Stack);
            if (item.SmeltTakes > 1) Assert.NotNull(item.Yields);
            if (item.Byproducts is not { } byproducts) continue;
            Assert.NotNull(item.Yields);
            foreach (var by in byproducts)
            {
                Assert.True(Content.Items.TryGet(by.Item, out _), $"{item.Id}: {by.Item}");
                Assert.InRange(by.Min, 0, by.Max);
                Assert.True(by.Max > 0, $"{item.Id}: {by.Item}");
                Assert.True(by.OneIn is null or >= 2, $"{item.Id}: {by.Item}");
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
        Assert.Equal(["spore", "seed"], Content.Items.All.Where(i => i.Process == "grow").Select(i => i.Id));
        Assert.Equal(["growCapsule"], Stations.All.Where(s => s.Process == "grow").Select(s => s.Machine));
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
            Assert.NotEqual(ItemGroup.Salvage, spec.Group);
            // A fuel that smelts (fat) smelts into a fuel burning longer, so smelting it is worth the fire.
            foreach (var (made, _) in spec.Yields ?? []) Assert.True(Content.Items.Get(made).BurnTicks > spec.BurnTicks, spec.Id);
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
        string[] refined = ["silicon", "glass", "biomass", "biofuel", "copper"];
        foreach (var spec in Content.Items.All)
        {
            Assert.True(spec.Yields != null == (spec.SmeltTicks != null), spec.Id);
            if (spec.Yields == null) continue;
            Assert.True(spec.SmeltTicks > 0, spec.Id);
            Assert.NotEmpty(spec.Yields);
            foreach (var (item, amount) in spec.Yields)
            {
                // What the Yields type held in TypeScript, materials and canisters, then silicon, glass, biomass, biofuel and copper.
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
    /// going to machines (0057): the electronics, circuits, solar cells and the deck drive, to the circuit assembler alone, and soil to
    /// the Former alone (0060), as the motor of a planet's copper and the cape of its cloth (2026-10-27-the-cape-is-worn); the arc crafts to
    /// the arc bench (0099, 2026-10-27-the-railgun).
    /// </summary>
    [Fact]
    public void EveryCraftIsMadeSomewhereThePodAllButElectronicsDronesSoilMotorsAndTheCapeAndEveryQueuedStationLaysEachOfItsCraftsOutOnce()
    {
        string[] electronics = ["circuit", "solarCell", "drone", "deckDrive", "processor", "display", "transceiver", "igniter"], formed = ["soil", "motor", "nozzle", "fuelPump", "cape"], arc = ["arcModule", "accumulator", "railgun"];
        Assert.Equal(Crafts.All.Select(c => c.Item).Except([.. electronics, .. formed, .. arc]).Order(StringComparer.Ordinal), Stations.Get("lifepod").Rows.SelectMany(r => r).Order(StringComparer.Ordinal));
        Assert.Equal(arc, Stations.Get("arcBench").Rows.SelectMany(r => r));
        Assert.Equal(electronics, Stations.Get("assembler").Rows.SelectMany(r => r));
        Assert.All(formed, item => Assert.Equal(["former"], Stations.All.Where(s => s.Rows.Any(r => r.Contains(item))).Select(s => s.Machine)));
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
    /// The arc bench is the arc branch's station (0099): every craft of a sky shard sits there and nowhere else, each holds a
    /// shard in its cost, so analysing one opens them together with the bench, and the bench is built of what the pod, the
    /// Former and salvage give, and a shard: nothing it makes is needed to build it.
    /// </summary>
    [Fact]
    public void EveryArcCraftSitsAtTheArcBenchHoldsAShardAndTheBenchIsBuiltWithoutThem()
    {
        var bench = Stations.Get("arcBench");
        var crafts = bench.Rows.SelectMany(r => r).ToList();
        Assert.Equal(["arcModule", "accumulator", "railgun"], crafts);
        Assert.All(crafts, c => Assert.Contains(Crafts.Get(c).Cost, e => e.Item == Content.Ruins.Shard));
        Assert.Equal(crafts, Crafts.All.Where(c => c.Cost.Any(e => e.Item == Content.Ruins.Shard)).Select(c => c.Item));
        Assert.All(crafts, c => Assert.Equal(["arcBench"], Stations.All.Where(s => s.Rows.Any(r => r.Contains(c))).Select(s => s.Machine)));
        var spec = Machines.Get("arcBench");
        Assert.True(spec is { Tunes: true, Draws: null, Makes: null, FuelSlot: null, Inputs: 1, Width: 4, Height: 2 });
        Assert.Contains(spec.Cost, e => e.Item == Content.Ruins.Shard);
        // Built of what exists before it: none of its cost is made at the bench itself.
        Assert.All(spec.Cost, e => Assert.DoesNotContain(e.Item, crafts));
        Assert.All(spec.Cost.Where(e => e.Item != Content.Ruins.Shard), e => Assert.Contains(Stations.All.Where(s => s.Machine != "arcBench"), s => s.Rows.Any(r => r.Contains(e.Item))));
        Assert.Equal(["arcBench"], Machines.All.Where(m => m.Tunes).Select(m => m.Kind));
        // The accumulator: three batteries' charge, worn on the belt as one is.
        Assert.True(Content.Items.Get("accumulator") is { Stack: 1, Equip: "belt", Gear: true, Capacity: Charge.Accumulator });
        Assert.Equal(3 * Charge.Battery, Charge.Accumulator);
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
                // The arc bench is paid by hand too; its one input is its tool's (0099).
                else if (spec.Tunes) Assert.Equal(1, spec.Inputs);
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
            // The basic helmet and body alone are free (2026-10-27-suits-are-worn).
            if (craft.Item != Content.Suit.Basic && craft.Item != Content.Helmet.Basic) Assert.NotEmpty(craft.Cost);
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
        // What a planet's growth gives (0080), and what creatures drop.
        foreach (var (item, _) in Flora.Every.SelectMany(k => k.Yields)) had.Add(item);
        foreach (var entry in Content.Creatures.All.SelectMany(s => s.Yields)) had.Add(entry.Item);
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
    /// <paramref name="never"/> is left out, as if it never drifted by, and so are a planet's items of them. Aboard only: what a
    /// planet gives is not had.
    /// </summary>
    private static (HashSet<string> Items, HashSet<string> Known) Reachable(IReadOnlyList<Station> stations, params string[] never) =>
        Reachable(stations, planet: false, never);

    /// <summary>
    /// <see cref="Reachable(IReadOnlyList{Station}, string[])"/>, and with <paramref name="planet"/> what a planet's ground
    /// and growth give besides (0077, 0080), dug and felled with the axe the wreck starts knowing, and what the creatures living
    /// on a planet type drop.
    /// </summary>
    private static (HashSet<string> Items, HashSet<string> Known) Reachable(IReadOnlyList<Station> stations, bool planet, params string[] never)
    {
        var had = new HashSet<string>(StringComparer.Ordinal);
        if (planet)
        {
            var hunted = PlanetTypes.All.SelectMany(t => t.Creatures).SelectMany(h => Content.Creatures.Get(h.Species).Yields).Select(y => (y.Item, 0));
            foreach (var (item, _) in Materials.All.Where(m => m.Breaks).SelectMany(m => m.Cost).Concat(Flora.Every.SelectMany(k => k.Yields)).Concat(hunted))
                if (!never.Contains(item)) had.Add(item);
        }
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

    /// <summary>Every craft and blueprint opens from the wreck's start, by analysing what is caught, dug on a planet or made at its stations.</summary>
    [Fact]
    public void EveryCraftAndBlueprintOpensFromTheWrecksStartByAnalysingWhatItsStationsMake()
    {
        var (_, known) = Reachable(Stations.All, planet: true);
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => b.Id).Concat(Crafts.All.Select(c => c.Item));
        Assert.Equal([], recipes.Where(id => !known.Contains(id)).ToArray());
    }

    /// <summary>The invariant: every item there is can be had from the wreck, caught, dug on a planet or made at a station reachable.</summary>
    [Fact]
    public void EveryItemIsReachableFromTheWreck()
    {
        var (had, _) = Reachable(Stations.All, planet: true);
        Assert.Equal([], Content.Items.All.Select(i => i.Id).Where(id => !had.Contains(id)).ToArray());
    }

    /// <summary>
    /// What the planet's caves give (0080) is had there alone: copper ore from its veins, smelted into copper at the furnace,
    /// magnetite from the heat's cracks over the bedrock, and the motor of both at the Former; ice, crystal, spores and mica.
    /// Aboard, none of them; and no motor without the deepest band's magnetite.
    /// </summary>
    [Fact]
    public void WhatTheCavesGiveComesFromAPlanetAloneAndTheMotorNeedsItsCopperAndItsDeepestMagnetite()
    {
        string[] planetary = ["copperOre", "copper", "magnetite", "motor", "ice", "crystal", "spore", "mica"];
        var aboard = Reachable(Stations.All).Items;
        Assert.All(planetary, item => Assert.DoesNotContain(item, aboard));
        var (down, known) = Reachable(Stations.All, planet: true);
        Assert.All(planetary, item => Assert.Contains(item, down));
        Assert.Contains("motor", known);
        Assert.DoesNotContain("motor", Reachable([.. Stations.All.Where(s => s.Machine != "former")], planet: true).Items);
        Assert.Equal([("copper", 1)], Content.Items.Get("copperOre").Yields);
        Assert.Equal(1, Content.Items.Get("copperOre").Heat);
        Assert.Contains(("magnetite", 1), Crafts.Get("motor").Cost);
        // Magnetite lies only in the heat's cracks, and they only in the band over the bedrock.
        var magnetic = Flora.Cave.Where(k => k.Yields.Any(y => y.Item == "magnetite")).ToList();
        Assert.Equal(["heatCrack"], magnetic.Select(k => k.Id));
        Assert.All(magnetic, k => Assert.Equal([0, 0, 0], k.Chances.Take(PlanetTypes.First.Bands.Count)));
    }

    /// <summary>
    /// Hide and fat come off a planet's grazers alone, so nothing before the first landing is made of them: aboard neither,
    /// nor the cape of the hide; on a planet both, and the cape. Without its hide, no cape; fat smelts into biofuel there.
    /// </summary>
    [Fact]
    public void HideAndFatComeOffAPlanetsGrazersAloneAndTheCapeNeedsTheHide()
    {
        var (aboard, opened) = Reachable(Stations.All);
        Assert.DoesNotContain("hide", aboard);
        Assert.DoesNotContain("fat", aboard);
        Assert.DoesNotContain(Cape.Item, opened);
        var (down, known) = Reachable(Stations.All, planet: true);
        Assert.Contains("hide", down);
        Assert.Contains("fat", down);
        Assert.Contains(Cape.Item, known);
        Assert.DoesNotContain(Cape.Item, Reachable(Stations.All, planet: true, "hide").Items);
        Assert.Equal([("biofuel", 1)], Content.Items.Get("fat").Yields);
        // The grazers drop them, the sentry its scrap.
        Assert.All(Content.Creatures.All.Where(s => s.Faction == Content.Creatures.Grazer), s => Assert.Equal(2, s.Yields.Count(y => y.Item is "hide" or "fat")));
        Assert.DoesNotContain(Content.Creatures.Sentry.Yields, y => y.Item is "hide" or "fat");
    }

    /// <summary>
    /// The engine is the last thing opened (2026-10-25-the-engine-through-the-hull): never aboard, only of a planet's
    /// parts, and with any one of the ruins' alloy, the deepest band's magnetite or the deep's crystal missing, neither it nor
    /// the part made of it. Its card is the ship tab's last.
    /// </summary>
    [Fact]
    public void TheEngineOpensLastOfAPlanetsAlloyMagnetiteAndCrystal()
    {
        Assert.DoesNotContain("engine", Reachable(Stations.All).Known);
        Assert.Contains("engine", Reachable(Stations.All, planet: true).Known);
        foreach (var (missing, part) in new[] { ("alloyScrap", "nozzle"), ("magnetite", "fuelPump"), ("crystal", "igniter") })
        {
            var (had, known) = Reachable(Stations.All, planet: true, missing);
            Assert.DoesNotContain(part, had);
            Assert.DoesNotContain("engine", known);
        }
        var ship = Machines.All.Where(m => m.Menu?.Tab == MenuTabs.Ship).Select(m => m.Menu!.Order)
            .Concat(Builds.All.Where(b => b.Menu?.Tab == MenuTabs.Ship).Select(b => b.Menu!.Order)).ToList();
        Assert.Equal(ship.Max(), Machines.Get("engine").Menu!.Order);
        Assert.Single(Machines.All, m => m.Menu?.Order == ship.Max() && m.Menu.Tab == MenuTabs.Ship);
    }

    /// <summary>
    /// The way down is had aboard: every blueprint and craft opens from what space gives alone but those named here, made of
    /// a planet's things, and the drop pod and its platform among the rest. A recipe that comes to need a planet's item before
    /// the first landing, a softlock, shows as a change to this list.
    /// </summary>
    [Fact]
    public void EverythingButAPlanetsOwnOpensFromWhatSpaceGivesAloneTheDropPodAmongThem()
    {
        string[] planetary = ["accumulator", "arcBench", "arcLamp", "arcModule", "backWallAlloy", "cape", "engine", "fuelPump", "igniter", "motor", "nozzle", "railgun", "wallAlloy"];
        var (_, known) = Reachable(Stations.All);
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => b.Id).Concat(Crafts.All.Select(c => c.Item));
        var locked = recipes.Where(id => !known.Contains(id)).Distinct().Order(StringComparer.Ordinal).ToArray();
        // The whole list in the message, so a failure names every recipe that moved, not the first few.
        Assert.True(locked.SequenceEqual(planetary.Order(StringComparer.Ordinal)), $"opened only on a planet: {string.Join(", ", locked.Select(id => $"\"{id}\""))}");
        Assert.Contains("dropPod", known);
        Assert.Contains(Builds.Platform, known);
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
}
