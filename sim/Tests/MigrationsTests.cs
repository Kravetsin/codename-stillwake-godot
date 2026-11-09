using System.Text.Json.Nodes;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Old saves keep loading (0031): a fixture of every format since <see cref="Migrations.Oldest"/>, the
/// wreck played a while, upgrades, loads and plays on. A format bump without its step and its fixture
/// fails here. Set <c>STILLWAKE_WRITE_FIXTURE=1</c> to write the fixture of this build's format. Format 28 halved
/// the cell (0104) and refuses every save before it.
/// </summary>
public class MigrationsTests
{
    private static string Fixture(int format) => Path.Combine(AppContext.BaseDirectory, "Fixtures", $"format-{format}.json");

    /// <summary>The wreck played a few hundred ticks with commands its modules draw: what a player's save holds.</summary>
    private static string Played()
    {
        var m = new Registry();
        var world = World.Create(1, Registry.Wreck, m.All);
        var rng = new Rng(new RngState { S = 2026 });
        for (int i = 0; i < 400; i++) world.Step(ModulesTests.RandomCommands(world, rng));
        return world.Save();
    }

    [Fact]
    public void HasAStepForEveryFormatSinceTheOldestAndAFixtureOfEach()
    {
        if (Environment.GetEnvironmentVariable("STILLWAKE_WRITE_FIXTURE") == "1")
        {
            string path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", $"format-{World.SaveFormat}.json");
            File.WriteAllText(path, Played());
        }
        Assert.True(Migrations.Steps.Count == World.SaveFormat - Migrations.Oldest, $"{Migrations.Steps.Count} steps for formats {Migrations.Oldest} to {World.SaveFormat}");
        for (int format = Migrations.Oldest; format <= World.SaveFormat; format++)
            Assert.True(File.Exists(Fixture(format)), $"no fixture of format {format}: run the tests once with STILLWAKE_WRITE_FIXTURE=1 before the bump");
    }

    [Fact]
    public void UpgradesASaveOfEveryFormatSoItLoadsAndPlaysOn()
    {
        for (int format = Migrations.Oldest; format <= World.SaveFormat; format++)
        {
            string save = File.ReadAllText(Fixture(format));
            Assert.Equal(format, Migrations.FormatOf(save));
            var m = new Registry();
            var world = World.Load(Migrations.Upgrade(save), m.All);
            for (int i = 0; i < 600; i++) world.Step([]);
            Assert.Equal(World.SaveFormat, Migrations.FormatOf(world.Save()));
        }
    }

    [Fact]
    public void TheFluffAWorldHadIsGoneOnUpgradingAndTheOthersStay()
    {
        var old = JsonNode.Parse("""{"surfaces":[{"slices":{"creatures":{"creatures":[{"species":"fluff"},{"species":"loper"},{"species":"fluff"}]}}}]}""")!.AsObject();
        var kept = Migrations.Steps[41 - Migrations.Oldest](old)["surfaces"]![0]!["slices"]!["creatures"]!["creatures"]!.AsArray();
        Assert.Equal(["loper"], kept.Select(c => c!["species"]!.GetValue<string>()));
    }

    [Fact]
    public void ACapbackMidPounceStopsOnUpgradingAndOtherCreaturesActOn()
    {
        var old = JsonNode.Parse("""{"slices":{},"surfaces":[{"slices":{"creatures":{"creatures":[{"species":"capback","act":{"move":0,"left":3},"vx":900,"vy":-40},{"species":"loper","act":{"move":0,"left":3},"vx":900}]}}}]}""")!.AsObject();
        var kept = Migrations.Steps[54 - Migrations.Oldest](old)["surfaces"]![0]!["slices"]!["creatures"]!["creatures"]!.AsArray();
        Assert.Null(kept[0]!["act"]);
        Assert.Equal((0, 0), (kept[0]!["vx"]!.GetValue<int>(), kept[0]!["vy"]!.GetValue<int>()));
        Assert.Equal(3, kept[1]!["act"]!["left"]!.GetValue<int>());
        Assert.Equal(900, kept[1]!["vx"]!.GetValue<int>());
    }

    /// <summary>What a gate held goes and what it opened stays: granted things, the credits, a bought bag's slot.</summary>
    [Fact]
    public void TheGatesDepositsAreGoneOnUpgradingAndWhatTheyOpenedStays()
    {
        var old = JsonNode.Parse(File.ReadAllText(Fixture(42)))!.AsObject();
        var knowledge = old["slices"]!["knowledge"]!.AsObject();
        knowledge["credits"] = 77;
        knowledge["deposits"] = new JsonArray(new JsonObject { ["gate"] = "press", ["item"] = "wire", ["count"] = 5 });
        var granted = knowledge["granted"]!.AsArray().Select(g => g!.GetValue<string>()).Append("bag1").Distinct().Order(StringComparer.Ordinal).ToList();
        knowledge["granted"] = new JsonArray([.. granted.Select(g => (JsonNode)g)]);

        var upgraded = JsonNode.Parse(Migrations.Upgrade(old.ToJsonString()))!["slices"]!["knowledge"]!.AsObject();
        Assert.False(upgraded.ContainsKey("deposits"));
        Assert.Equal(77, upgraded["credits"]!.GetValue<int>());
        Assert.Equal(granted, upgraded["granted"]!.AsArray().Select(g => g!.GetValue<string>()));

        var m = new Registry();
        World.Load(Migrations.Upgrade(old.ToJsonString()), m.All);
        Assert.True(m.Items.SlotOpen(0, new Items.Place(Content.Inventory.Start)));
    }

    [Fact]
    public void MovesAPipeEndingAtATanksOldPortToItsNewOne()
    {
        // A format-45 save's pipe from the tank's old output on its left, the cell its input has now: from its right after the upgrade.
        var w = new Scenario("TTTT....\nTTTT....\nTTTT....\nTTTT....", cells: true).GiveItems(0, ("tube", 20));
        w.Do(new Pipes.LayPipe(w.At(0, 2), w.At(6, 2))).Run();
        var save = JsonNode.Parse(w.World.Save())!.AsObject();
        save["format"] = 45;
        w.Reload(Migrations.Upgrade(save.ToJsonString()));
        Assert.Equal([new Pipes.PipeSpan(w.At(3, 2), w.At(6, 2), true)], w.M.Pipes.Spans);
    }

    [Fact]
    public void GivesAChargerHoldingABatteryItsSecondSlotEmpty()
    {
        // A format-46 save's charger held one slot; after the upgrade it holds two, its battery in the first.
        var w = new Scenario("CC\nCC\nCC\nCC", cells: true);
        int charger = w.M.Grid.MachinesOf("charger").Single().Id;
        w.Put(0, new Items.Stack("battery", 1, 7), crate: charger);
        var save = JsonNode.Parse(w.World.Save())!.AsObject();
        save["format"] = 46;
        foreach (var held in save["slices"]!["items"]!["crates"]!.AsArray()) held!["slots"]!.AsArray().RemoveAt(1);
        w.Reload(Migrations.Upgrade(save.ToJsonString()));
        Assert.Equal([new Items.Stack("battery", 1, 7), null], w.M.Items.Crate(charger));
    }

    [Fact]
    public void GivesADroneBayItsFuelSlotEmpty()
    {
        // A format-47 save's bay held four slots; after the upgrade it holds five, its drones where they were.
        var w = new Scenario("RRRR\nRRRR\nRRRR\nRRRR", cells: true);
        int bay = w.M.Grid.MachinesOf("droneBay").Single().Id;
        w.Put(1, new Items.Stack("drone", 1, 7), crate: bay);
        var save = JsonNode.Parse(w.World.Save())!.AsObject();
        save["format"] = 47;
        foreach (var held in save["slices"]!["items"]!["crates"]!.AsArray()) held!["slots"]!.AsArray().RemoveAt(4);
        w.Reload(Migrations.Upgrade(save.ToJsonString()));
        Assert.Equal([null, new Items.Stack("drone", 1, 7), null, null, null], w.M.Items.Crate(bay));

        // On a surface too: a bay there padded, a crate of another kind left as it was.
        static JsonObject Slices(string kind) => JsonNode.Parse(
            """{"grid":{"machines":[{"id":3,"kind":"KIND"}]},"items":{"crates":[{"id":3,"slots":[null,null,null,null]}]}}""".Replace("KIND", kind))!.AsObject();
        var old = new JsonObject { ["slices"] = Slices("crate"), ["surfaces"] = new JsonArray(new JsonObject { ["slices"] = Slices("droneBay") }) };
        var upgraded = Migrations.Steps[47 - Migrations.Oldest](old);
        Assert.Equal(4, upgraded["slices"]!["items"]!["crates"]![0]!["slots"]!.AsArray().Count);
        Assert.Equal(5, upgraded["surfaces"]![0]!["slices"]!["items"]!["crates"]![0]!["slots"]!.AsArray().Count);
    }

    [Fact]
    public void PutsWhatASalvageDroneCaughtInItsNet()
    {
        // One carrying a piece's loot, one a stack it lifted, one still on its way out.
        var old = JsonNode.Parse(
            """
            {"slices":{"logistics":{"drones":[
              {"id":1,"leg":1,"piece":7,"caught":"cargoCrate","load":[{"item":"plate","count":2}]},
              {"id":2,"leg":1,"load":[{"item":"wire","count":5}]},
              {"id":3,"leg":0,"piece":9}]}},"surfaces":[]}
            """)!.AsObject();
        var drones = Migrations.Steps[48 - Migrations.Oldest](old)["slices"]!["logistics"]!["drones"]!.AsArray();
        Assert.Equal("""[{"kind":"cargoCrate","piece":7}]""", drones[0]!["net"]!.ToJsonString());
        Assert.False(drones[0]!.AsObject().ContainsKey("caught"));
        Assert.False(drones[0]!.AsObject().ContainsKey("piece"));
        Assert.Equal("""[{"kind":"wire","piece":0}]""", drones[1]!["net"]!.ToJsonString());
        Assert.Equal(9, drones[2]!["piece"]!.GetValue<int>());
        Assert.False(drones[2]!.AsObject().ContainsKey("net"));
    }

    [Fact]
    public void StandsAWorkbenchAndAnArcBenchBuiltOrPlannedAsTallAsTheirPicturesOnTheFloorTheyStoodOn()
    {
        // Format-49 benches two cells tall at row 6, planned ones at row 16, a crate left where it was; on a surface too.
        static JsonObject Slices() => JsonNode.Parse(
            """
            {"grid":{"width":10,"machines":[{"id":1,"kind":"workbench","cell":62},{"id":2,"kind":"crate","cell":66},{"id":3,"kind":"arcBench","cell":64}]},
             "building":{"plans":[{"id":1,"cell":162,"place":{"layer":"machine","kind":"workbench"}},{"id":2,"cell":170,"place":{"layer":"machine","kind":"crate"}},{"id":3,"cell":164,"place":{"layer":"machine","kind":"arcBench"}},{"id":4,"cell":166,"clear":"machine"}]}}
            """)!.AsObject();
        var old = new JsonObject { ["slices"] = Slices(), ["surfaces"] = new JsonArray(new JsonObject { ["slices"] = Slices() }) };
        var upgraded = Migrations.Steps[49 - Migrations.Oldest](old);
        foreach (var slices in new[] { upgraded["slices"]!, upgraded["surfaces"]![0]!["slices"]! })
        {
            Assert.Equal([32, 66, 44], slices["grid"]!["machines"]!.AsArray().Select(m => m!["cell"]!.GetValue<int>()));
            Assert.Equal([132, 170, 144, 166], slices["building"]!["plans"]!.AsArray().Select(p => p!["cell"]!.GetValue<int>()));
        }
    }

    [Fact]
    public void StandsARadarAntennaBuiltOrPlannedAsATallMastOnTheRoofItStoodOn()
    {
        // Format-51 antennas four cells tall at row 6, a planned one at row 16, a crate left where it was; on a surface too.
        static JsonObject Slices() => JsonNode.Parse(
            """
            {"grid":{"width":10,"machines":[{"id":1,"kind":"radarAntenna","cell":62},{"id":2,"kind":"crate","cell":66}]},
             "building":{"plans":[{"id":1,"cell":162,"place":{"layer":"machine","kind":"radarAntenna"}},{"id":2,"cell":170,"place":{"layer":"machine","kind":"crate"}},{"id":3,"cell":166,"clear":"machine"}]}}
            """)!.AsObject();
        var old = new JsonObject { ["slices"] = Slices(), ["surfaces"] = new JsonArray(new JsonObject { ["slices"] = Slices() }) };
        var upgraded = Migrations.Steps[51 - Migrations.Oldest](old);
        foreach (var slices in new[] { upgraded["slices"]!, upgraded["surfaces"]![0]!["slices"]! })
        {
            Assert.Equal([42, 66], slices["grid"]!["machines"]!.AsArray().Select(m => m!["cell"]!.GetValue<int>()));
            Assert.Equal([142, 170, 166], slices["building"]!["plans"]!.AsArray().Select(p => p!["cell"]!.GetValue<int>()));
        }
    }

    [Fact]
    public void TurnsTheOrdersMetInTurnIntoTheLevelAndCountsTheNextFromWhatIsSoldNow()
    {
        // Format-57 sales: the first two orders met and the fourth's circuits, so level 2 (the third unmet stops the count).
        static JsonObject Old(string sold) => JsonNode.Parse(
            "{\"slices\":{\"knowledge\":{\"granted\":[],\"credits\":0" + sold + "}},\"surfaces\":[]}")!.AsObject();
        static JsonNode Knowledge(string sold) => Migrations.Steps[57 - Migrations.Oldest](Old(sold))["slices"]!["knowledge"]!;

        var met = Knowledge(""","sold":[{"item":"circuit","count":20},{"item":"gear","count":20},{"item":"glass","count":5},{"item":"iron","count":20},{"item":"planks","count":20},{"item":"plate","count":30},{"item":"scrapChunk","count":40}]""");
        Assert.Equal("""{"level":2,"base":[{"item":"frame","count":0},{"item":"glass","count":5},{"item":"silicon","count":0}],"baseWorth":0}""", met["reputation"]!.ToJsonString());
        Assert.Equal("0", met["soldWorth"]!.ToJsonString());

        var none = Knowledge("");
        Assert.Equal("""{"level":0,"base":[{"item":"iron","count":0},{"item":"planks","count":0},{"item":"scrapChunk","count":0}],"baseWorth":0}""", none["reputation"]!.ToJsonString());

        var partWay = Knowledge(""","sold":[{"item":"iron","count":20},{"item":"scrapChunk","count":40}]""");
        Assert.Equal("""{"level":0,"base":[{"item":"iron","count":20},{"item":"planks","count":0},{"item":"scrapChunk","count":40}],"baseWorth":0}""", partWay["reputation"]!.ToJsonString());
    }

    [Fact]
    public void RefusesAFieldThisBuildDoesNotKnowAndReadsOneMissingAsItsDefaultSoAnAddedFieldNeedsNoBump()
    {
        var newer = JsonNode.Parse(Played())!.AsObject();
        newer["slices"]!["power"]!["fromANewerBuild"] = 1;
        var e = Assert.Throws<InvalidDataException>(() => World.Load(newer.ToJsonString(), new Registry().All));
        Assert.Contains("power", e.Message);

        // An older save of this format, from before power kept its stores (0101): it loads with none and plays on.
        var older = JsonNode.Parse(Played())!.AsObject();
        Assert.True(older["slices"]!["power"]!.AsObject().Remove("stores"));
        var world = World.Load(older.ToJsonString(), new Registry().All);
        for (int i = 0; i < 100; i++) world.Step([]);
        Assert.Empty(JsonNode.Parse(world.Save())!["slices"]!["power"]!["stores"]!.AsArray());
    }

    [Fact]
    public void LeavesASaveOfThisFormatAsItIsAndOneFromBeforeTheHalvedCellForTheLoadToRefuse()
    {
        string save = Played();
        Assert.Same(save, Migrations.Upgrade(save));
        // The last format of the 16 px cell (0104), and the last before trade and the tree (2026-10-30-trade-and-tech): they load no more.
        foreach (int format in new[] { 27, Migrations.Oldest - 1 })
        {
            string old = save.Replace($"\"format\":{World.SaveFormat}", $"\"format\":{format}");
            Assert.Equal(old, Migrations.Upgrade(old));
            Assert.Throws<InvalidDataException>(() => World.Load(old, new Registry().All));
        }
    }

    [Fact]
    public void DropsAQueuedCanisterAndItsGrantSoTheFormerWorksOnWhatCameAfterIt()
    {
        // A format-50 Former at work on canisters of ice, plates queued after them; after the upgrade only the plates are left, begun afresh.
        string[] rows = ["PPP         ", "            ", "############", "#MM.GG.....#", "#MM.GG.....#", "############"];
        var w = new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), doubled: true).GiveItems(0, ("iron", 40));
        int former = w.M.Grid.MachinesOf("former").Single().Id;
        w.Do(new Crafting.Craft(former, "plate", 2)).Run();
        var save = JsonNode.Parse(w.World.Save())!.AsObject();
        save["format"] = 50;
        var queue = save["slices"]!["crafting"]!["queues"]!.AsArray().Single(q => q!["machine"]!.GetValue<int>() == former)!;
        queue["entries"]!.AsArray().Insert(0, new JsonObject { ["id"] = 9, ["recipe"] = "canister", ["count"] = 3, ["by"] = 0 });
        queue["progress"] = 5000;
        save["slices"]!["knowledge"]!["granted"]!.AsArray().Insert(0, "canister");

        w.Reload(Migrations.Upgrade(save.ToJsonString()));
        Assert.Equal(["plate"], w.M.Crafting.Queue(former).Select(e => e.Recipe));
        Assert.Equal(0, w.M.Crafting.Progress(former));
        Assert.DoesNotContain("canister", w.M.Knowledge.GrantedIds);
        w.Run(Content.Crafts.Get("plate").Ticks * 2 + 5);
        Assert.Equal(2, w.M.Items.Crate(former).Sum(s => s?.Item == "plate" ? s.Count : 0));
    }

    [Fact]
    public void DropsQueuedGearAndItsBlueprintsSoTheStationWorksOnWhatCameAfter()
    {
        // A format-59 Former at work on a power driver, a scanner queued after the plates; after the upgrade only the plates are left, begun afresh.
        string[] rows = ["PPP         ", "            ", "############", "#MM.GG.....#", "#MM.GG.....#", "############"];
        var w = new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), doubled: true).GiveItems(0, ("iron", 40));
        int former = w.M.Grid.MachinesOf("former").Single().Id;
        w.Do(new Crafting.Craft(former, "plate", 2)).Run();
        var save = JsonNode.Parse(w.World.Save())!.AsObject();
        save["format"] = 59;
        var queue = save["slices"]!["crafting"]!["queues"]!.AsArray().Single(q => q!["machine"]!.GetValue<int>() == former)!;
        queue["entries"]!.AsArray().Insert(0, new JsonObject { ["id"] = 9, ["recipe"] = "powerDriver", ["count"] = 1, ["by"] = 0 });
        queue["entries"]!.AsArray().Add(new JsonObject { ["id"] = 10, ["recipe"] = "scanner", ["count"] = 2, ["by"] = 0 });
        queue["progress"] = 5000;
        foreach (var gear in Content.Research.GearSold) save["slices"]!["knowledge"]!["granted"]!.AsArray().Add(gear);

        w.Reload(Migrations.Upgrade(save.ToJsonString()));
        Assert.Equal(["plate"], w.M.Crafting.Queue(former).Select(e => e.Recipe));
        Assert.Equal(0, w.M.Crafting.Progress(former));
        Assert.All(Content.Research.GearSold, gear => Assert.DoesNotContain(gear, w.M.Knowledge.GrantedIds));
        w.Run(Content.Crafts.Get("plate").Ticks * 2 + 5);
        Assert.Equal(2, w.M.Items.Crate(former).Sum(s => s?.Item == "plate" ? s.Count : 0));
    }

    [Fact]
    public void ClearsCracksOfAFormatSixtySaveTheirDamageCountedBeforeStrengthsWereInTenths()
    {
        // A pickaxe blow cracks stone in a format-60 world; after the upgrade the wall stands whole, as it soon would have healed.
        string map = """
            ................
            ............####
            ............####
            ................
            """;
        var w = new Scenario(Layout.Parse(map).WithMaterials(map.Replace('#', 't')).OnPlanet()).GiveItems(0, (Content.Weapons.Pickaxe.Item, 1));
        int cell = 2 * w.M.Grid.Width + 12;
        w.Do(HewTesting.Dig(w, cell, Content.Weapons.Pickaxe.Item)).Run();
        Assert.NotEmpty(w.M.Building.Cracks);
        var save = JsonNode.Parse(w.World.Save())!.AsObject();
        save["format"] = 60;

        w.Reload(Migrations.Upgrade(save.ToJsonString()));
        Assert.Empty(w.M.Building.Cracks);
        Assert.NotNull(w.M.Grid.WallAt(cell));
    }
}
