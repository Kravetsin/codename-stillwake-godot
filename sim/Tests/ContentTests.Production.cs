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
        Assert.Equal(["crush", "grow", "smelt"], processes);
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
    /// Salvage smelts into materials and canisters, never more salvage; rock and asteroid ore are crushed into gravel, iron
    /// ore and quartz, which smelt on into glass, iron and silicon (0057, smelt-chances): whole amounts in whole ticks,
    /// byproducts too, each step of a chain as hot as the one before it or hotter, never back into what it came from.
    /// </summary>
    [Fact]
    public void SmeltingMakesWholeAmountsInWholeTicksOfItemsThatAreNotSalvageAndChainsOnlyUpInHeat()
    {
        string[] refined = ["silicon", "glass", "biomass", "biofuel", "copper", "ironOre", "gravel", "quartz"];
        foreach (var spec in Content.Items.All)
        {
            Assert.True(spec.Yields != null == (spec.SmeltTicks != null), spec.Id);
            if (spec.Yields == null) continue;
            Assert.True(spec.SmeltTicks > 0, spec.Id);
            Assert.NotEmpty(spec.Yields);
            foreach (var (item, amount) in spec.Yields.Concat((spec.Byproducts ?? []).Select(b => (b.Item, b.Max))))
            {
                // What the Yields type held in TypeScript, materials and canisters, then silicon, glass, biomass, biofuel and
                // copper, and the crusher's iron ore, gravel and quartz.
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
        var chained = Content.Items.All.Where(i => i.Yields != null && Content.Items.All.Any(f =>
            f.Yields?.Any(y => y.Item == i.Id) == true || f.Byproducts?.Any(b => b.Item == i.Id) == true)).ToList();
        Assert.Equal(["ironOre", "quartz", "gravel", "biomass"], chained.Select(i => i.Id));
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
    /// Every craft is made at some queued station (0057): the pod gives what a castaway is given, the suit and the
    /// screwdriver (0046, 0107); the workbench the basic parts and the first tools; the Former those parts again, and
    /// alone frames, the jetpack, the battery, the drill, soil (0060), the motor of a planet's copper, the canister of its
    /// ice and the cape of its cloth (2026-10-27-the-cape-is-worn); the electronics, circuits, solar cells and the deck drive, the circuit
    /// assembler alone; the arc crafts the arc bench (0099, 2026-10-27-the-railgun).
    /// </summary>
    [Fact]
    public void EveryCraftIsMadeSomewhereThePodTheSuitTheWorkbenchWhatIsAssembledTheFormerWhatIsStampedAndEveryQueuedStationLaysEachOfItsCraftsOutOnce()
    {
        string[] pod = [Content.Helmet.Basic, Content.Suit.Basic, Screwdrivers.Basic.Item];
        string[] parts = ["wire", "plate", "gear", "tube", "coil", "rotor"];
        string[] tools = ["fireAxe", "harpoon", "nailer", "nail", "helmetLamp", "jetpack", "drill", "cape", "fuelPump"];
        string[] electronics = ["circuit", "solarCell", "battery", "drone", "deckDrive", "processor", "display", "transceiver", "igniter"], arc = ["arcModule", "accumulator", "railgun"];
        string[] formed = ["frame", "motor", "soil", "canister", "nozzle"];
        Assert.Empty(Crafts.All.Select(c => c.Item).Except([.. pod, .. parts, .. tools, .. formed, .. electronics, .. arc]));
        Assert.Equal(pod, Stations.Get("lifepod").Rows.SelectMany(r => r));
        Assert.Equal(parts.Concat(tools).Order(StringComparer.Ordinal), Stations.Get("workbench").Rows.SelectMany(r => r).Order(StringComparer.Ordinal));
        Assert.Equal(parts.Concat(formed).Order(StringComparer.Ordinal), Stations.Get("former").Rows.SelectMany(r => r).Order(StringComparer.Ordinal));
        Assert.All(tools, item => Assert.Equal(["workbench"], Stations.All.Where(s => s.Rows.Any(r => r.Contains(item))).Select(s => s.Machine)));
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
    /// shard in its cost, and the bench is built of what the pod, the
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
                // The pod and the workbench are paid by hand, so they take nothing in.
                if (station.Machine is "lifepod" or "workbench") Assert.Equal(0, spec.Inputs);
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
            // The basic helmet, body and screwdriver alone are free: what the pod gives (2026-10-27-suits-are-worn, 0107).
            if (craft.Item != Content.Suit.Basic && craft.Item != Content.Helmet.Basic && craft.Item != Screwdrivers.Basic.Item) Assert.NotEmpty(craft.Cost);
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
        had.UnionWith(WrecksOwn);
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
    /// What only the wreck's own machines give, taken apart: the photosensitive panel of its solar panel, had once, never
    /// made. Every item is had of something else besides.
    /// </summary>
    private static readonly string[] WrecksOwn = ["photoPanel"];

    /// <summary>
    /// What the wreck can come to, walking stations (0057): an item is reachable when it is salvage — what any
    /// zone's debris is or breaks into — or made at a reachable station from reachable items: crafted at a queued
    /// one that lays it out, smelted at a fed one hot enough. A station is reachable when it is built into the ship,
    /// or its blueprint is known and its cost reachable. A tier opens when the one before it did and its deposit can be had
    /// (2026-10-30-trade-and-tech); its blocks are bought then, credits not walked, every price being earned by selling; what they
    /// open is used when its cost is had. Debris of the kinds
    /// <paramref name="never"/> is left out, as if it never drifted by, and so are a planet's items of them. Aboard only: what a
    /// planet gives is not had. What only the wreck's own machines give (<see cref="WrecksOwn"/>) is had from the start.
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
        var had = new HashSet<string>(WrecksOwn, StringComparer.Ordinal);
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
        var known = new HashSet<string>(Content.Research.StartKnown.Concat(Content.Research.KnownTakenApart), StringComparer.Ordinal);
        var built = new HashSet<string>(StringComparer.Ordinal);
        var done = new HashSet<string>(StringComparer.Ordinal);
        for (bool more = true; more;)
        {
            more = false;
            foreach (var node in Content.Research.Nodes.Where(n => !done.Contains(n.Id) && n.Needs.All(done.Contains)))
            {
                if (!node.Deposit.All(d => had.Contains(d.Item))) continue;
                more |= done.Add(node.Id);
                if (!node.Gate) more |= known.Add(node.Id);
            }
            foreach (var station in stations)
            {
                var spec = Machines.Get(station.Machine);
                if (spec.Fixed || (known.Contains(station.Machine) && spec.Cost.All(c => had.Contains(c.Item))))
                    more |= built.Add(station.Machine);
            }
            foreach (var station in stations.Where(s => built.Contains(s.Machine)))
            {
                var made = station.Fed
                    ? Content.Items.All.Where(i => had.Contains(i.Id) && i.Yields != null && i.Process == station.Process && i.Heat <= station.Heat).SelectMany(i => i.Yields!.Select(y => y.Item).Concat((i.Byproducts ?? []).Select(b => b.Item)))
                    : station.Rows.SelectMany(r => r).Where(c => known.Contains(c) && Crafts.Get(c).Cost.All(p => had.Contains(p.Item)));
                foreach (var item in made.ToList()) more |= had.Add(item);
            }
        }
        return (had, known);
    }

    /// <summary>Every craft and blueprint opens from the wreck's start, walking the tree on what is caught, dug on a planet or made at its stations.</summary>
    [Fact]
    public void EveryCraftAndBlueprintOpensFromTheWrecksStartWalkingTheTree()
    {
        var (_, known) = Reachable(Stations.All, planet: true);
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => b.Id).Concat(Crafts.All.Select(c => c.Item));
        Assert.Equal([], recipes.Where(id => !known.Contains(id)).ToArray());
    }

    /// <summary>Every blueprint and craft is known from the start or opened by exactly one block; a block opens nothing else.</summary>
    [Fact]
    public void EveryBlueprintAndCraftIsInExactlyOneBlockOrKnownFromTheStart()
    {
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => b.Id).Concat(Crafts.All.Select(c => c.Item)).Distinct().Order(StringComparer.Ordinal).ToArray();
        var opened = Content.Research.Nodes.Where(n => !n.Gate && Upgrades.Of(n.Id) == null).Select(n => n.Id).Concat(Content.Research.StartKnown).Concat(Content.Research.KnownTakenApart).ToList();
        Assert.Equal(recipes, opened.Distinct().Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(opened.Count, opened.Distinct().Count());
    }

    /// <summary>
    /// The gates before the Former are paid by hand at a workbench (first-hour-pace): in seconds of its work, however many things
    /// a gate asks for, each no more than it takes a hero to sit through.
    /// </summary>
    [Theory]
    [InlineData("firstMetal", 0)]
    [InlineData("press", 330)]
    [InlineData("machinery", 960)]
    public void AGateBeforeTheFormerIsAtMostSoManySecondsOfTheWorkbench(string gate, int seconds)
    {
        int speed = Stations.Get("workbench").Speed;
        int Ticks(string item) => Crafts.TryGet(item, out var craft) && Stations.Get("workbench").Rows.Any(r => r.Contains(item))
            ? craft.Ticks * 1000 / speed + craft.Cost.Sum(c => c.Count * Ticks(c.Item))
            : 0;
        int total = Content.Research.Node(gate)!.Deposit.Sum(d => d.Count * Ticks(d.Item));
        Assert.True(total <= seconds * 20, $"{gate}: {total / 20} s of the workbench");
    }

    /// <summary>Losing one station shuts what is made at it and everything after: the walk bites.</summary>
    [Fact]
    public void WithoutAStationAWholeStageStaysShut()
    {
        var (had, known) = Reachable([.. Stations.All.Where(s => s.Machine != "former")]);
        Assert.DoesNotContain("frame", had);
        Assert.DoesNotContain("crusher", known);
        Assert.DoesNotContain("dropPod", known);
    }

    /// <summary>Selling refined pays: a priced craft or a fixed yield is worth more than what goes in, or nothing would be worth processing. Soil is dug, its craft for a ship with no ground.</summary>
    [Fact]
    public void EveryPricedCraftAndYieldSellsForMoreThanItsPricedInputs()
    {
        foreach (var craft in Crafts.All.Where(c => Market.PriceOf(c.Item) != null && c.Item != "soil"))
        {
            int inputs = craft.Cost.Sum(c => (Market.PriceOf(c.Item) ?? 0) * c.Count);
            Assert.True(Market.PriceOf(craft.Item) > inputs, $"{craft.Item}: {Market.PriceOf(craft.Item)} for {inputs}");
        }
        foreach (var item in Content.Items.All.Where(i => i.Yields != null && Market.PriceOf(i.Id) != null))
        {
            int yields = item.Yields!.Sum(y => (Market.PriceOf(y.Item) ?? 0) * y.Count);
            // A byproduct pays what it does on average: crushing ore pays by its quartz, gravel now being cheap.
            yields += item.Byproducts?.Sum(b => (Market.PriceOf(b.Item) ?? 0) * b.Min / (b.OneIn ?? 1)) ?? 0;
            if (yields > 0) Assert.True(yields > Market.PriceOf(item.Id) * item.SmeltTakes, $"{item.Id}: {item.SmeltTakes} at {Market.PriceOf(item.Id)} yield {yields}");
        }
    }

    /// <summary>What no one buys: wreckage whole, tools, what is worn, what stores or flies by itself, the arc's, the photosensitive panel nothing makes.</summary>
    private static readonly string[] Unpriced = ["arcShard", "arcModule", "supplyPod", "cargoCrate", "hullSection", "cargoContainer", "machineBlock", "cableRack", "tankCluster", "shieldSlab", "soilClod", "asteroid", "asteroidSmall", "asteroidLarge", "asteroidRich", "engineBlock", "cargoHold", "podCluster", "photoPanel", "drone", "screwdriver", "fireAxe", "harpoon", "nailer", "railgun", "drill", "jetpack", "helmetLamp", "battery", "accumulator", "helmet", "suit", "cape"];

    /// <summary>Everything is priced or named here as what no one buys; raw salvage is, so the first credits need nothing made.</summary>
    [Fact]
    public void EveryItemIsPricedOrNamedUnpricedAndTheStreamsChunkIsPriced()
    {
        Assert.NotNull(Market.PriceOf("scrapChunk"));
        Assert.Contains("scrapChunk", DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).SelectMany(k => (Loot.Of(k.Item) ?? []).Select(e => e.Item).Append(k.Item)));
        var unpriced = Content.Items.All.Where(i => Market.PriceOf(i.Id) == null).Select(i => i.Id).ToList();
        Assert.True(unpriced.All(Unpriced.Contains), string.Join(", ", unpriced.Select(id => $"\"{id}\"")));
        Assert.All(Unpriced, id => Assert.Null(Market.PriceOf(id)));
    }

    /// <summary>
    /// Every line of the tree tells the truth: a block's crafts and blueprints are made only of what the start, its ring or a ring further in opens, at a
    /// station they open; what wreckage gives needs no one to open it.
    /// </summary>
    [Fact]
    public void NothingIsOpenedBeforeWhatItIsMadeOf()
    {
        var (given, _) = Reachable([]);
        var prints = Stillwake.Sim.Research.Blueprints.All.ToDictionary(b => b.Id);
        var tabs = Content.Research.Tabs.Select(t => t.Id).ToList();
        // A ring is bought in any order: a block may take what its own ring opens, or what is further in, never further out.
        HashSet<string> Behind(TechNode node) => new(Content.Research.StartKnown.Concat(Content.Research.Nodes
            .Where(n => tabs.IndexOf(n.Tab) < tabs.IndexOf(node.Tab) || (n.Tab == node.Tab && n.Ring <= node.Ring)).Where(n => !n.Gate).Select(n => n.Id)), StringComparer.Ordinal);
        foreach (var node in Content.Research.Nodes.Where(n => !n.Gate && Upgrades.Of(n.Id) == null))
        {
            var opened = Behind(node);
            string id = node.Id;
            {
                bool craft = Crafts.TryGet(id, out var recipe);
                var cost = craft ? recipe!.Cost : prints[id].Cost;
                foreach (var (item, _) in cost)
                    Assert.True(!Crafts.TryGet(item, out _) || given.Contains(item) || opened.Contains(item), $"{id} of {item}, not behind {node.Id}");
                if (craft && cost.Count > 0)
                    Assert.True(Stations.All.Any(s => s.Rows.Any(r => r.Contains(id)) && (Machines.Get(s.Machine).Fixed || opened.Contains(s.Machine))), $"{id}: no station behind {node.Id}");
            }
        }
    }

    /// <summary>Every need names a node listed before it: the order the window looks for what to do next.</summary>
    [Fact]
    public void EveryNeedIsANodeListedBeforeIt()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in Content.Research.Nodes)
        {
            Assert.All(node.Needs, need => Assert.True(seen.Contains(need), $"{node.Id} needs {need}"));
            seen.Add(node.Id);
        }
    }

    /// <summary>The formula, pinned on a chain worked by hand: a chunk of 20 smelted in 3 seconds into 4 scrap, 2 of it wired in 1.5.</summary>
    [Fact]
    public void AStepIsWorthItsInputsAndTimeMarkedUpPerUnitRoundedUp()
    {
        // What the ground gives for a swing is worth next to nothing, whatever a recipe would make of it.
        Assert.Equal(1, Market.PriceOf("soil"));
        Assert.Equal(1, Market.PriceOf("rock"));
        Assert.Equal(20, Market.WorthOf("scrapChunk"));
        Assert.Equal(8, Market.WorthOf("scrap")); // (20 + 3) x 1.25 / 4 = 7.19
        Assert.Equal(22, Market.WorthOf("wire")); // (2 x 8 + 1.5) x 1.25 = 21.875
        Assert.Equal(116, Market.WorthOf("coil")); // (4 x 22 + 4.5) x 1.25 = 115.625
    }

    /// <summary>First blueprints come for a few chunks; the engine is the dearest of all, so it stays the end of the tree.</summary>
    [Fact]
    public void TheFirstBlocksCostAFewChunksAndTheEngineMost()
    {
        var prices = Content.Research.Nodes.Where(n => !n.Gate).ToDictionary(n => n.Id, Stillwake.Sim.Research.Prices.Of);
        Assert.All(new[] { "workbench", "furnace", "wire" }, id => Assert.InRange(prices[id], 1, 12 * Market.WorthOf("scrapChunk")));
        Assert.Equal(prices.Values.Max(), prices["engine"]);
        Assert.Single(prices.Values, prices["engine"]);
    }

    /// <summary>The first credits need nothing bought: no softlock can stand between the wreck's start and its first sale.</summary>
    [Fact]
    public void TheFirstCreditsNeedNothingBought()
    {
        var (had, known) = Reachable([]);
        Assert.Contains("tradeStation", known);
        Assert.All(Machines.Get("tradeStation").Cost, c => Assert.Contains(c.Item, had));
        Assert.Contains("scrapChunk", had);
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
        Assert.Contains("magnetite", Crafts.Get("motor").Cost.Select(c => c.Item));
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
        string[] planetary = ["accumulator", "arcBench", "arcLamp", "arcModule", "backWallAlloy", "backWallHide", "backWallMica", "backWallStone", "canister", "cape", "engine", "fuelPump", "igniter", "motor", "nozzle", "railgun", "wallAlloy", "wallHide", "wallMica", "wallStone"];
        var (had, known) = Reachable(Stations.All);
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => (b.Id, b.Cost)).Concat(Crafts.All.Select(c => (Id: c.Item, c.Cost)));
        var locked = recipes.Where(r => !known.Contains(r.Id) || !r.Cost.All(c => had.Contains(c.Item))).Select(r => r.Id).Distinct().Order(StringComparer.Ordinal).ToArray();
        // The whole list in the message, so a failure names every recipe that moved, not the first few.
        Assert.True(locked.SequenceEqual(planetary.Order(StringComparer.Ordinal)), $"opened only on a planet: {string.Join(", ", locked.Select(id => $"\"{id}\""))}");
        Assert.Contains("dropPod", known);
        Assert.Contains(Builds.Platform, known);
    }

    /// <summary>
    /// The farm walks from salvage (0060): seeds from supply pods and soil clods, soil pressed at the Former or broken
    /// out of clods, the grow capsule bought once that is had, biomass grown in it and biofuel smelted from
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

    /// <summary>
    /// Heat walks too: aboard, the crusher's quartz and gravel are had without the smelter, but silicon and glass only once
    /// it is; without the crusher none of them, the furnace too cold for what it would have made.
    /// </summary>
    [Fact]
    public void SiliconAndGlassAreReachableOnlyThroughTheCrusherAndTheElectricSmelter()
    {
        var (had, _) = Reachable([.. Stations.All.Where(s => s.Machine != "smelter")]);
        Assert.All(["quartz", "gravel"], item => Assert.Contains(item, had));
        Assert.All(["silicon", "glass"], item => Assert.DoesNotContain(item, had));
        var (uncrushed, _) = Reachable([.. Stations.All.Where(s => s.Machine != "crusher")]);
        Assert.All(["quartz", "gravel", "silicon", "glass"], item => Assert.DoesNotContain(item, uncrushed));
        Assert.All(["silicon", "glass"], item => Assert.Contains(item, Reachable(Stations.All).Items));
    }

    /// <summary>
    /// Solar cells walk the whole chain (0057): asteroid ore crushed into quartz and gravel, smelted into silicon and
    /// glass at the smelter, both with wire into solar cells at the circuit assembler, which salvaged circuits build.
    /// Without the assembler, the smelter or the crusher, no cell; circuits still come from salvage.
    /// </summary>
    [Fact]
    public void SolarCellsAreReachableOnlyThroughTheAssemblerTheSmelterAndTheCrusher()
    {
        Assert.Contains("solarCell", Reachable(Stations.All).Items);
        Assert.Contains("solarCell", Machines.Get("solarPanel").Cost.Select(c => c.Item));
        foreach (var without in new[] { "assembler", "smelter", "crusher" })
        {
            var (some, _) = Reachable([.. Stations.All.Where(s => s.Machine != without)]);
            Assert.DoesNotContain("solarCell", some);
            Assert.Contains("circuit", some);
        }
    }

    /// <summary>
    /// The photosensitive panel (2026-10-29-photo-panel) is had only of a solar panel taken apart: no debris, loot, smelt,
    /// craft, growth or creature gives one. The wreck starts knowing the solar panel, so its own panel builds again, and no
    /// second one.
    /// </summary>
    [Fact]
    public void OnlyASolarPanelTakenApartGivesAPhotosensitivePanelAndTeachesTheSolarPanel()
    {
        Assert.Equal(["photoPanel"], WrecksOwn);
        Assert.Contains(("photoPanel", 1), Machines.Get("solarPanel").Cost);
        Assert.DoesNotContain("solarPanel", Content.Research.StartKnown);
        Assert.Equal(["solarPanel"], Content.Research.KnownTakenApart);
        var given = DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).Select(k => k.Item)
            .Concat(DebrisSpec.Zones.SelectMany(z => z.Tiers).SelectMany(t => t.Kinds).SelectMany(k => Loot.Of(k.Item) ?? []).Select(e => e.Item))
            .Concat(Content.Items.All.SelectMany(i => (i.Yields ?? []).Select(y => y.Item).Concat((i.Byproducts ?? []).Select(b => b.Item))))
            .Concat(Crafts.All.Select(c => c.Item))
            .Concat(Flora.Every.SelectMany(k => k.Yields).Select(y => y.Item))
            .Concat(Content.Creatures.All.SelectMany(s => s.Yields).Select(y => y.Item));
        Assert.DoesNotContain("photoPanel", given);
        var recipes = Stillwake.Sim.Research.Blueprints.All.Select(b => (b.Id, b.Cost)).Concat(Crafts.All.Select(c => (Id: c.Item, c.Cost)));
        Assert.Equal(["solarPanel"], recipes.Where(r => r.Cost.Any(c => c.Item == "photoPanel")).Select(r => r.Id));
    }
}
