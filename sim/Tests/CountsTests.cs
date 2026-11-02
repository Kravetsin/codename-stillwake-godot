using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Research;
using Stillwake.Sim.Trade;
using Xunit;
using Craft = Stillwake.Sim.Crafting.Craft;
using Cut = Stillwake.Sim.Power.Cut;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>What the crew has obtained and sold, for good (docs/plans/board.md): counted where a stack is born, never when one only moves or a payment comes back.</summary>
public class CountsTests
{
    /// <summary>A room on the left, space right of it, as <c>DebrisTests</c>' map.</summary>
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

    private const string Open = """
        #########
        #.......#
        #.......#
        #.......#
        #########
        """;

    private static readonly string Axe = Weapons.Axe.Item;

    private static int Got(Scenario w, string item) => w.M.Knowledge.ObtainedOf(item);

    private static int Carried(Scenario w, string item, int peer = 0) => w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    private static int Lying(Scenario w, string item) => w.M.Items.Floor.Sum(s => s.Item == item ? s.Count : 0);

    /// <summary>The first piece out, made a <paramref name="kind"/>, and hero 0 in space by it.</summary>
    private static Scenario Out(string kind)
    {
        var w = new Scenario(Space).Run(Content.Debris.Every);
        w.M.Debris.Pieces[0].Item = kind;
        return w.Do(new Enter(10 + 2 + 20)).Run();
    }

    /// <summary>Soil over turf with an acacia standing at column 5, played under a planet's rules, as <c>FloraTests</c>' meadow.</summary>
    private static Scenario Meadow()
    {
        var layout = Layout.Parse("""
            ........
            ........
            ########
            ########
            """).WithMaterials("""
            ........
            ........
            ssssssss
            tttttttt
            """) with { Flora = [(1 * 8 + 5, "acacia")] };
        return new Scenario(layout.OnPlanet(), doubled: true);
    }

    /// <summary>A room with a workbench and no power, knowing everything.</summary>
    private static (Scenario W, int Bench) Workshop()
    {
        var w = new Scenario(Layout.Parse(["##########", "#II......#", "##########"]), doubled: true);
        return (w, w.M.Grid.MachinesOf("workbench").Single().Id);
    }

    // What counts.

    [Fact]
    public void ACatchCountsWhatItGave()
    {
        var w = Out("scrapChunk").Do(new Grab(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((1, 1), (Got(w, "scrapChunk"), Carried(w, "scrapChunk")));
    }

    [Fact]
    public void ACatchWithAFullBagCountsWhatFloatedOff()
    {
        var w = Out("scrapChunk");
        w.GiveItems(0, ("canister", (Inventory.Slots - Inventory.Kit.Count) * Content.Items.Get("canister").Stack));
        w.Do(new Grab(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((1, 0, 1), (Got(w, "scrapChunk"), Carried(w, "scrapChunk"), Lying(w, "scrapChunk")));
    }

    [Fact]
    public void WhatATakingApartGivesCounts()
    {
        var w = new Scenario(Layout.Parse(["#KK...#", "#.....#", "#######"]), doubled: true);
        w.Do(new Deconstruct(w.At(1, 0), "machine")).Run();
        for (int i = 0; i < Costs.ApartTicks(Machines.Get("crate").BuildTicks); i++) w.Do(new Work(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Grid.MachinesOf("crate"));
        Assert.All(Machines.Get("crate").Cost, c => Assert.Equal((c.Count, c.Count), (Got(w, c.Item), Carried(w, c.Item))));
    }

    [Fact]
    public void WhatADigGivesCounts()
    {
        var w = Meadow();
        var cost = w.M.Grid.WallAt(w.At(0, 2))!.Cost;
        Assert.NotEmpty(cost);
        w.Do(HewTesting.Dig(w, w.At(0, 2), Axe)).Run();
        Assert.Null(w.M.Grid.WallAt(w.At(0, 2)));
        Assert.All(cost, c => Assert.Equal(c.Count, Got(w, c.Item)));
    }

    [Fact]
    public void WhatAFellingGivesCounts()
    {
        var w = Meadow();
        for (int hit = 0; hit < Flora.Get("acacia").Strength / Weapons.Axe.BlockDamage; hit++) w.Do(HewTesting.Dig(w, w.At(5, 0), Axe)).Run();
        Assert.Null(w.M.Building.PlantAt(w.At(5, 0)));
        Assert.All(Flora.Get("acacia").Yields, y => Assert.Equal(y.Count, Got(w, y.Item)));
    }

    [Fact]
    public void WhatADigOrAFallDropsForNobodyCounts()
    {
        var w = Meadow();
        var cost = w.M.Grid.WallAt(w.At(5, 2))!.Cost;
        // A shot from a peer with no hero here: the wall breaks, and the tree it held falls, for nobody.
        Assert.True(w.M.Building.Hit(7, w.At(5, 2), 100_000));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(FloorStack.Nobody, s.By));
        Assert.All(cost, c => Assert.Equal(Lying(w, c.Item), Got(w, c.Item)));
        Assert.All(Flora.Get("acacia").Yields, y => Assert.Equal(Lying(w, y.Item), Got(w, y.Item)));
        Assert.NotEqual(0, Got(w, Flora.Get("acacia").Yields[0].Item));
    }

    [Fact]
    public void WhatAKillGivesCounts()
    {
        string room = string.Join('\n', [new string('#', 48), .. Enumerable.Repeat("#" + new string('.', 46) + "#", 3), new string('#', 48)]);
        var w = CreaturesTesting.Planet(room).HeroAt(20, 3).HeroAt(2, 3, peer: 1);
        var c = w.Loper(23, 3).Still();
        for (int i = 0; i < 400 && !c.Dead; i++) w.Do(new Strike(c.Id, Axe) { From = 1 }).Run();
        Assert.True(c.Dead);
        Assert.NotEmpty(w.M.Knowledge.Obtained);
        Assert.All(w.M.Knowledge.Obtained, t => Assert.Equal(Lying(w, t.Item), t.Count));
    }

    [Fact]
    public void WhatAWreckBrokenGivesCounts()
    {
        var w = Out("tankCluster");
        Assert.True(w.M.Debris.Hit(0, 1, 100_000));
        Assert.NotEmpty(w.M.Knowledge.Obtained);
        Assert.All(w.M.Knowledge.Obtained, t => Assert.Equal(Lying(w, t.Item), t.Count));
    }

    [Fact]
    public void ACraftPaidFromTheQueueCountsWhatItMadeOnceItIsMade()
    {
        var (w, bench) = Workshop();
        w.GiveItems(0, ("scrap", 10));
        w.Do(new Craft(bench, "wire", 1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(0, Got(w, "wire"));
        w.Run(Content.Crafts.Get("wire").Ticks * 1000 / Stations.Get("workbench").Speed);
        Assert.Equal(1, w.M.Items.Crate(bench).Sum(s => s?.Item == "wire" ? s.Count : 0));
        Assert.Equal((1, 0), (Got(w, "wire"), Got(w, "scrap")));
    }

    [Fact]
    public void ACraftFedFromTheInputsCountsWhatItMade()
    {
        string[] rows = ["PPP         ", "            ", "############", "#MM........#", "#MM........#", "############"];
        var w = new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), doubled: true).GiveItems(0, ("iron", 10));
        int former = w.M.Grid.MachinesOf("former").Single().Id;
        w.Do(new Move(new Slot(0), new Target(null, former), 10)).Run();
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Run(Content.Crafts.Get("plate").Ticks);
        Assert.Empty(w.Refusals);
        Assert.Equal((1, 0), (Got(w, "plate"), Got(w, "iron")));
    }

    [Fact]
    public void ASmeltCountsItsYieldsAndWhatItDrewBesides()
    {
        string[] rows = ["PPP         ", "            ", "############", "#kk........#", "#kk........#", "############"];
        var w = new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), doubled: true).GiveItems(0, ("asteroidOre", 10));
        int crusher = w.M.Grid.MachinesOf("crusher").Single().Id;
        w.Do(new Move(new Slot(0), new Target(0, crusher), 10)).Run();
        for (int i = 0; i < 20_000 && w.M.Items.Crate(crusher)[0] != null; i++) w.Run();
        int Made(string item) => w.M.Items.Crate(crusher).Sum(s => s?.Item == item ? s.Count : 0);
        Assert.Equal(0, Made("asteroidOre"));
        Assert.Equal((10, 10), (Got(w, "ironOre"), Made("ironOre")));
        Assert.Equal((20, 20), (Got(w, "gravel"), Made("gravel")));
        Assert.True(Made("quartz") > 0, "no byproduct drawn");
        Assert.Equal(Made("quartz"), Got(w, "quartz"));
        Assert.Equal(0, Got(w, "asteroidOre"));
    }

    // What does not.

    [Fact]
    public void ACraftCancelledCountsNothing()
    {
        var (w, bench) = Workshop();
        w.GiveItems(0, ("scrap", 10));
        w.Do(new Craft(bench, "wire", 1)).Run();
        w.Do(new Cancel(bench, 1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((10, 0), (Carried(w, "scrap"), Got(w, "scrap")));
    }

    [Fact]
    public void APlanCancelledCountsNothing()
    {
        var w = new Scenario(Open).GiveItems(0, ("plate", 4));
        w.Do(new PlacePlan(w.At(1, 1), Placement.Wall("iron"))).Run();
        w.Do(new Work(1)).Run();
        w.Do(new CancelPlan(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((1, 0), (Lying(w, "plate"), Got(w, "plate")));
    }

    [Fact]
    public void PipeAndValveRefundsCountNothing()
    {
        var w = new Scenario("""
            VV..............
            VV..............
            """, cells: true).GiveItems(0, ("tube", 20), ("gear", 4));
        int a = w.At(0, 0), joint = w.At(6, 0);
        w.Do(new LayPipe(a, joint)).Do(new MakeValve(joint, true)).Run();
        w.Do(new MakeValve(joint, false)).Run();
        w.Do(new CutPipe(joint, a)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((20, 4), (Carried(w, "tube"), Carried(w, "gear")));
        Assert.Empty(w.M.Knowledge.Obtained);
    }

    [Fact]
    public void ACableRefundCountsNothing()
    {
        string[] rows = ["PPP         ", "            ", "############", "#GG........#", "#GG........#", "############"];
        var w = new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), doubled: true);
        w.Do(new Cut(w.At(1, 3), w.At(0, 0))).Run();
        Assert.Empty(w.Refusals);
        Assert.True(Carried(w, "wire") > 0);
        Assert.Empty(w.M.Knowledge.Obtained);
    }

    [Fact]
    public void TheKitCountsNothing()
    {
        var w = new Scenario(Space) { Bare = true };
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        Assert.Equal(1, Carried(w, Inventory.Kit[0].Item));
        Assert.Empty(w.M.Knowledge.Obtained);
    }

    [Fact]
    public void MovingDroppingPickingUpAndSortingCountNothing()
    {
        var w = new Scenario(Open).GiveItems(0, ("scrap", 10));
        w.Do(new Enter(w.At(2, 3))).Do(new Enter(w.At(3, 3)) { From = 1 }).Run();
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "scrap");
        w.Do(new Move(new Slot(from), new Target(Inventory.Hotbar + 3), 4)).Run();
        w.Do(new Sort()).Run();
        w.Do(new Drop(new Slot(w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "scrap")), 4, w.At(3, 3), 0)).Run();
        w.Do(new PickUp(w.M.Items.Floor[0].Id) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(4, Carried(w, "scrap", peer: 1));
        Assert.Empty(w.M.Knowledge.Obtained);
    }

    [Fact]
    public void DeathCountsNothing()
    {
        var w = DeathTests.DieAt(DeathTests.Room().GiveItems(0, ("scrap", 30)), DeathTests.Inside);
        Assert.Equal(30, Lying(w, "scrap"));
        Assert.Empty(w.M.Knowledge.Obtained);
    }

    [Fact]
    public void ATransferCountsNothing()
    {
        var w = new Scenario(Space);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Surfaces.TestStrip.Id)).Run();
        w.GiveItems(0, ("scrap", 5)).Do(new Transfer(1)).Run();
        Assert.Equal(5, w.On(1).Items.Inventory(0).Sum(s => s?.Item == "scrap" ? s.Count : 0));
        Assert.Empty(w.M.Knowledge.Obtained);
    }

    [Fact]
    public void ASpawnCountsNothing()
    {
        var w = new Scenario(Layout.Empty).Do(new SetCreative(true)).Do(new Spawn("iron", 10)).Run();
        Assert.Equal(10, Carried(w, "iron"));
        Assert.Empty(w.M.Knowledge.Obtained);
    }

    // The list.

    [Fact]
    public void ObtainedKeepsTheOrderFirstObtainedAndALoadRebuildsItsLookUp()
    {
        var w = new Scenario(Layout.Empty);
        var knowledge = w.M.Knowledge;
        knowledge.Obtain("wire", 2);
        knowledge.Obtain("iron", 1);
        knowledge.Obtain("wire", 3);
        Assert.Equal([new Tally("wire", 5), new Tally("iron", 1)], knowledge.Obtained);
        w.Reload();
        Assert.Equal((5, 1, 0), (Got(w, "wire"), Got(w, "iron"), Got(w, "plate")));
        w.M.Knowledge.Obtain("iron", 1);
        Assert.Equal([new Tally("wire", 5), new Tally("iron", 2)], w.M.Knowledge.Obtained);
    }

    [Fact]
    public void ACountOfNoneOrLessIsIgnored()
    {
        var w = new Scenario(Layout.Empty);
        foreach (int count in new[] { 0, -3 })
        {
            w.M.Knowledge.Obtain("wire", count);
            w.M.Knowledge.Sell("wire", count);
        }
        Assert.Empty(w.M.Knowledge.Obtained);
        Assert.Equal(0, w.M.Knowledge.SoldOf("wire"));
        Assert.Empty(((KnowledgeState)w.M.Knowledge.Slice).Sold);
    }

    [Fact]
    public void AnotherSurfacesCountReachesTheWorldsList()
    {
        var w = new Scenario(Space);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Surfaces.TestStrip.Id)).Run();
        w.On(1).Items.GiveOrDrop(0, [("scrap", 2)], w.On(1).Crew.Spawn);
        Assert.Equal(2, w.On(1).Knowledge.ObtainedOf("scrap"));
        Assert.Equal(2, Got(w, "scrap"));
    }

    [Fact]
    public void VersionMovesWithWhatIsObtainedOrNewlyGranted()
    {
        var w = new Scenario(Layout.Empty with { Known = [] });
        var knowledge = w.M.Knowledge;
        int was = knowledge.Version;
        knowledge.Obtain("wire", 0);
        knowledge.Sell("wire", 1);
        Assert.Equal(was, knowledge.Version);
        knowledge.Obtain("wire", 1);
        Assert.Equal(was + 1, knowledge.Version);
        knowledge.Grant("wire");
        Assert.Equal(was + 2, knowledge.Version);
        knowledge.Grant("wire");
        Assert.Equal(was + 2, knowledge.Version);
        Assert.Equal(["wire"], knowledge.GrantedIds);
    }

    [Fact]
    public void ASaveWithNeitherListLoadsWithBothEmpty()
    {
        var w = new Scenario(Layout.Empty);
        w.M.Knowledge.Obtain("wire", 2);
        w.M.Knowledge.Sell("wire", 1);
        var older = JsonNode.Parse(w.World.Save())!.AsObject();
        var slice = older["slices"]!["knowledge"]!.AsObject();
        Assert.True(slice.Remove("obtained"));
        Assert.True(slice.Remove("sold"));
        w.Reload(older.ToJsonString()).Run(10);
        Assert.Empty(w.M.Knowledge.Obtained);
        Assert.Equal((0, 0), (Got(w, "wire"), w.M.Knowledge.SoldOf("wire")));
    }

    // Sales.

    private static (Scenario W, int Id) Trading(char station)
    {
        string[] rows = ["##########", $"#{station}{station}......#", $"#{station}{station}......#", "##########"];
        var w = new Scenario(Layout.Parse(rows, null), doubled: true);
        return (w, w.M.Grid.MachineAt(w.At(1, 1))!.Id);
    }

    [Fact]
    public void ACapsuleSentByHandCountsWhatItCarriedOnce()
    {
        var (w, id) = Trading('$');
        string unpriced = Content.Items.All.First(i => Market.PriceOf(i.Id) == null).Id;
        w.FillCrate(id, ("wire", 3), (unpriced, 1));
        w.Do(new SendCapsule(id)).Run();
        Assert.Equal((3, 0), (w.M.Knowledge.SoldOf("wire"), w.M.Knowledge.SoldOf(unpriced)));
        w.Run(Market.CapsuleFlight);
        Assert.Equal(3, w.M.Knowledge.SoldOf("wire"));
        Assert.Empty(w.M.Knowledge.Obtained);
    }

    [Fact]
    public void AnAutoStationCountsWhatItSendsOnce()
    {
        var (w, id) = Trading('A');
        w.FillCrate(id, ("wire", 2));
        w.Run(Market.CapsuleFlight + 1);
        Assert.Equal(2, w.M.Knowledge.SoldOf("wire"));
    }

    [Fact]
    public void SoldStaysSortedAndAddsUp()
    {
        var (w, id) = Trading('$');
        w.FillCrate(id, ("wire", 1), ("scrapChunk", 2), ("frame", 1));
        w.Do(new SendCapsule(id)).Run(Market.CapsuleFlight);
        w.FillCrate(id, ("wire", 2));
        w.Do(new SendCapsule(id)).Run();
        Assert.Equal([new Tally("frame", 1), new Tally("scrapChunk", 2), new Tally("wire", 3)], ((KnowledgeState)w.M.Knowledge.Slice).Sold);
    }

    // How many are enough.

    [Theory]
    [InlineData("scrapChunk", 10)]
    [InlineData("scrap", 10)]
    [InlineData("wire", 10)]
    [InlineData("planks", 10)]
    [InlineData("iron", 10)]
    [InlineData("gear", 8)]
    [InlineData("tube", 8)]
    [InlineData("hullPlate", 5)]
    [InlineData("glass", 5)]
    [InlineData("plate", 4)]
    [InlineData("rotor", 3)]
    [InlineData("coil", 2)]
    [InlineData("canister", 2)]
    [InlineData("circuit", 2)]
    [InlineData("frame", 1)]
    [InlineData("solarCell", 1)]
    [InlineData("photoPanel", 1)]
    public void EnoughGoesByWorth(string item, int enough) => Assert.Equal(enough, Understanding.Enough(item));

    // Invariants: played by every module's fuzz, creative mode left off, so a spawn is refused.

    private static Arbitrary<uint> Seeds => Gen.Choose(1, int.MaxValue).Select(s => (uint)s).ToArbitrary();

    private static World Played(uint seed)
    {
        var world = World.Create(seed, Registry.Wreck, new Registry().All);
        var rng = new Rng(new RngState { S = seed });
        for (int i = 0; i < 600; i++) world.Step([.. ModulesTests.RandomCommands(world, rng).Where(c => c is not SetCreative)]);
        return world;
    }

    /// <summary>Every item in a slot or on the floor, on every surface.</summary>
    private static IEnumerable<string> Held(World world)
    {
        foreach (var surface in world.Surfaces)
        {
            var m = Registry.On(world, surface.Id);
            foreach (var floor in m.Items.Floor) yield return floor.Item;
            foreach (var machine in m.Grid.Machines)
                foreach (var stack in m.Items.Crate(machine.Id))
                    if (stack != null) yield return stack.Item;
            for (int peer = 0; peer < 5; peer++)
                foreach (var stack in m.Items.Inventory(peer))
                    if (stack != null) yield return stack.Item;
        }
    }

    [Property(MaxTest = 12)]
    public Property EverythingHeldHasACountOrCameWithTheKit() => Prop.ForAll(Seeds, seed =>
    {
        var world = Played(seed);
        var knowledge = Registry.On(world, 0).Knowledge;
        var strays = Held(world).Where(item => knowledge.ObtainedOf(item) == 0 && !Content.Inventory.Kit.Any(k => k.Item == item)).Distinct().ToList();
        Assert.True(strays.Count == 0, $"held with no count: {string.Join(", ", strays)}");
    });

    [Property(MaxTest = 12)]
    public Property ObtainedHoldsEachItemOnce() => Prop.ForAll(Seeds, seed =>
    {
        var obtained = Registry.On(Played(seed), 0).Knowledge.Obtained;
        Assert.Equal(obtained.Count, obtained.Select(t => t.Item).Distinct().Count());
        Assert.All(obtained, t => Assert.True(t.Count > 0));
    });
}
