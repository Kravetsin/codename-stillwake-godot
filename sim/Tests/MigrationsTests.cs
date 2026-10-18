using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using Xunit;
using Move = Stillwake.Sim.Items.Move;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Old saves keep loading (0031): a fixture of every format since <see cref="Migrations.Oldest"/>, the
/// wreck played a while, upgrades, loads and plays on. A format bump without its step and its fixture
/// fails here. Set <c>STILLWAKE_WRITE_FIXTURE=1</c> to write the fixture of this build's format.
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
        for (int i = 0; i < 400; i++) world.Step(ModulesTests.RandomCommands(m, rng));
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
    public void TakesTheBackWallFromBehindAPanelOutInSpaceSoItSeesTheStars()
    {
        // The fixture's cables are cut by its random commands: its panel feeds nothing, but stands on the stars.
        var m = new Registry();
        World.Load(Migrations.Upgrade(File.ReadAllText(Fixture(5))), m.All);
        var panel = m.Grid.Machines.Single(x => x.Kind == "solarPanel");
        Assert.All(Enumerable.Range(0, 3), dx => Assert.Null(m.Grid.BackAt(panel.Cell + dx)));
        // Every other back wall of the wreck stands as it did.
        Assert.NotNull(m.Grid.BackAt(panel.Cell + 3 * m.Grid.Width));
    }

    [Fact]
    public void LeavesTheBackWallBehindAPanelInARoomWhichMadeNoPowerBefore()
    {
        var layout = Layout.Parse(["      ", "######", "#PPP.#", "######"]);
        var root = System.Text.Json.Nodes.JsonNode.Parse(World.Create(1, layout, new Registry().All).Save())!.AsObject();
        root["format"] = 5;
        var grid = root["slices"]!["grid"]!;
        var back = Runs.Cells(grid["back"]!);
        var backMaterial = Runs.Cells(grid["backMaterial"]!);
        foreach (int x in new[] { 1, 2, 3 }) (back[2 * 6 + x], backMaterial[2 * 6 + x]) = (1, Content.Materials.Get("scrap").Code);
        grid["back"] = Runs.Node(back);
        grid["backMaterial"] = Runs.Node(backMaterial);
        var m = new Registry();
        World.Load(Migrations.Upgrade(root.ToJsonString()), m.All);
        // Moved into the middle of the world since (format 10).
        var (ox, oy) = Layout.OffsetInWorld(6, 4);
        Assert.All(new[] { 1, 2, 3 }, x => Assert.NotNull(m.Grid.BackAt((2 + oy) * m.Grid.Width + x + ox)));
    }

    [Fact]
    public void NamesAFiresMachineWhereItNamedItsFurnaceSoAFireBurnsOn()
    {
        // A furnace's fire, saved as format 7 named it: read as soon as it loads.
        var layout = Layout.Parse(["######", "#FF..#", "#FF..#", "######"]);
        var root = System.Text.Json.Nodes.JsonNode.Parse(World.Create(1, layout, new Registry().All).Save())!.AsObject();
        root["format"] = 7;
        root["slices"]!["furnace"]!["fires"] = new System.Text.Json.Nodes.JsonArray(
            new System.Text.Json.Nodes.JsonObject { ["furnace"] = 1, ["fuel"] = 120, ["item"] = null, ["progress"] = 0 });
        var m = new Registry();
        World.Load(Migrations.Upgrade(root.ToJsonString()), m.All);
        Assert.True(m.Furnace.At(1).Burning);
    }

    /// <summary>
    /// <paramref name="save"/>, of this build, as format 13 kept it: what a furnace smelts on its fire, in ticks,
    /// a fire kept with no fuel while it holds work, and crafting's slice after building's.
    /// </summary>
    private static string AsFormat13(string save)
    {
        var root = JsonNode.Parse(save)!.AsObject();
        root["format"] = 13;
        var slices = root["slices"]!.AsObject();
        var crafting = slices["crafting"]!.AsObject();
        var smelts = crafting["smelts"]!.AsArray().ToDictionary(s => (int)s!["machine"]!);
        var fires = slices["furnace"]!["fires"]!.AsArray().Select(f => (int)f!["machine"]!).Union(smelts.Keys).Order()
            .Select(machine =>
            {
                var fire = slices["furnace"]!["fires"]!.AsArray().FirstOrDefault(f => (int)f!["machine"]! == machine);
                var smelt = smelts.GetValueOrDefault(machine);
                return (JsonNode)new JsonObject
                {
                    ["machine"] = machine,
                    ["fuel"] = fire == null ? 0 : (int)fire["fuel"]!,
                    ["item"] = smelt == null ? null : (string)smelt["item"]!,
                    ["progress"] = smelt == null ? 0 : (int)smelt["progress"]! / 1000,
                };
            }).ToArray();
        slices["furnace"]!["fires"] = new JsonArray(fires);
        crafting.Remove("smelts");
        var order = slices.Select(p => p.Key).Where(k => k != "crafting").ToList();
        order.Insert(order.IndexOf("building") + 1, "crafting");
        var moved = order.Select(k => (k, slices[k]!.DeepClone())).ToList();
        slices.Clear();
        foreach (var (key, slice) in moved) slices[key] = slice;
        return root.ToJsonString();
    }

    [Fact]
    public void MovesWhatAFireWasSmeltingIntoCraftingSoTheFurnaceSmeltsOnWhereItWas()
    {
        // A sealed, aired room with two furnaces: one burning part-way through a chunk, one cold part-way through another.
        var w = new Scenario(Layout.Parse(["############", "#FF.TT..FF.#", "#FF.TTV.FF.#", "############"], ["", "    o", "    ---"]))
            .GiveItems(0, ("scrapChunk", 9), ("planks", 2));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(8, 1)); i++) w.Run();
        int hot = w.M.Grid.MachineAt(w.At(1, 1))!.Id, cold = w.M.Grid.MachineAt(w.At(8, 1))!.Id;
        // One plank smelts six chunks and part of a seventh: the cold one's fire goes out with work on it.
        w.Do(new Move(new Items.Place(0), new Target(0, cold), 8)).Do(new Move(new Items.Place(1), new Target(1, cold), 1)).Run(420);
        w.Do(new Move(new Items.Place(0), new Target(0, hot), 1)).Do(new Move(new Items.Place(1), new Target(1, hot), 1)).Run(25);
        Assert.True(w.M.Furnace.At(hot) is { Burning: true, Progress: > 0 });
        Assert.True(w.M.Furnace.At(cold) is { Burning: false, Progress: > 0 });
        string save = w.World.Save();
        var was = (w.M.Furnace.At(hot), w.M.Furnace.At(cold));

        string old = AsFormat13(save);
        Assert.Contains("\"item\":\"scrapChunk\",\"progress\":", old);
        Assert.Equal(save, Migrations.Upgrade(old));
        var m = new Registry();
        World.Load(Migrations.Upgrade(old), [.. m.All, new Builder(m)]);
        Assert.Equal(was, (m.Furnace.At(hot), m.Furnace.At(cold)));
    }

    /// <summary>The format <see cref="Migrations.StationInputs"/> upgrades a save into: the Former's and the assembler's inputs.</summary>
    private static readonly int InputsFormat = Migrations.Oldest + 1 + Migrations.Steps.ToList().IndexOf(Migrations.StationInputs);

    [Fact]
    public void GivesTheFormerAndTheAssemblerTheirInputsEmptyInFrontSoWhatTheyMadeStaysInTheirOutputsAndEntriesLoadPaid()
    {
        // A Former and an assembler on a panel each, both having made something, each with a paid entry queued.
        var w = new Scenario(Layout.Parse(
                ["PPP PPP     ", "            ", "############", "#MM.YY.....#", "#MM.YY.....#", "############"], null,
                [((0, 0), (1, 3)), ((4, 0), (4, 3))]))
            .GiveItems(0, ("iron", 20), ("silicon", 4), ("wire", 10));
        int former = w.M.Grid.MachinesOf("former").Single().Id, assembler = w.M.Grid.MachinesOf("assembler").Single().Id;
        w.Do(new Crafting.Craft(former, "gear", 2)).Do(new Crafting.Craft(assembler, "circuit", 1)).Run(200);
        w.Do(new Crafting.Craft(former, "plate", 3)).Do(new Crafting.Craft(assembler, "circuit", 2)).Run(5);
        string save = w.World.Save();
        var was = (w.M.Items.Crate(former).ToList(), w.M.Items.Crate(assembler).ToList(), w.M.Crafting.Queue(former).ToList(), w.M.Crafting.Queue(assembler).ToList());
        Assert.Equal(new Stack("gear", 2), w.M.Items.Crate(former)[Content.Machines.Get("former").Inputs!.Value]);

        // As the format before kept it: only the outputs, and entries with no feed.
        var root = JsonNode.Parse(save)!.AsObject();
        root["format"] = InputsFormat - 1;
        foreach (var held in root["slices"]!["items"]!["crates"]!.AsArray())
        {
            int id = (int)held!["id"]!;
            if (id != former && id != assembler) continue;
            var slots = held["slots"]!.AsArray();
            int inputs = Content.Machines.Get(id == former ? "former" : "assembler").Inputs!.Value;
            for (int i = 0; i < inputs; i++)
            {
                Assert.Null(slots[0]);
                slots.RemoveAt(0);
            }
        }
        string old = root.ToJsonString();
        Assert.DoesNotContain("fromInputs", old);
        Assert.Equal(save, Migrations.Upgrade(old));
        var m = new Registry();
        World.Load(Migrations.Upgrade(old), [.. m.All, new Builder(m)]);
        Assert.Equal(was.Item1, m.Items.Crate(former));
        Assert.Equal(was.Item2, m.Items.Crate(assembler));
        Assert.Equal(was.Item3, m.Crafting.Queue(former));
        Assert.Equal(was.Item4, m.Crafting.Queue(assembler));
        Assert.All(m.Crafting.Queue(former).Concat(m.Crafting.Queue(assembler)), e => Assert.False(e.FromInputs || e.Endless));
    }

    [Fact]
    public void GivesEveryInventoryOfAFormat8SaveItsEquipmentEmptyAndNoHeroAThrust()
    {
        string old = File.ReadAllText(Fixture(8));
        var before = System.Text.Json.Nodes.JsonNode.Parse(old)!["slices"]!["items"]!["inventories"]!.AsArray();
        Assert.All(before, held => Assert.Equal(Content.Inventory.Slots, held!["slots"]!.AsArray().Count));
        var m = new Registry();
        World.Load(Migrations.Upgrade(old), m.All);
        Assert.NotEmpty(before);
        foreach (var held in before)
        {
            int peer = (int)held!["id"]!;
            var slots = m.Items.Inventory(peer);
            Assert.Equal(Content.Inventory.Total, slots.Count);
            // What it carried stays in its own slots, where it was.
            for (int i = 0; i < Content.Inventory.Slots; i++)
                Assert.Equal((string?)held["slots"]![i]?["item"], slots[i]?.Item);
            Assert.All(slots.Skip(Content.Inventory.Slots), Assert.Null);
        }
        Assert.All(m.Crew.Heroes, hero => Assert.Equal((false, 0), (hero.Thrusting, hero.ThrustTicks)));
    }

    /// <summary>A save brought by the steps up to format <paramref name="to"/>, as JSON.</summary>
    private static JsonObject UpgradedTo(string save, int to)
    {
        var root = JsonNode.Parse(save)!.AsObject();
        for (int f = Migrations.FormatOf(save); f < to; f++)
        {
            root = Migrations.Steps[f - Migrations.Oldest](root);
            root["format"] = f + 1;
        }
        return root;
    }

    /// <summary>
    /// Every cell and position <paramref name="before"/>, a save of format 9, holds is where <paramref name="after"/>,
    /// it upgraded to 10, holds it: moved by the offset that centres the old grid in the world, a cell off
    /// the old grid at -1, nothing in the layers but what was moved; its grid of the world's size and zone.
    /// The offset, for the caller to check.
    /// </summary>
    private static (int X, int Y) AssertMovedIntoTheWorld(JsonNode before, JsonNode after)
    {
        var (was, now) = (before["slices"]!, after["slices"]!);
        var (oldGrid, grid) = (was["grid"]!, now["grid"]!);
        int w = (int)oldGrid["width"]!, h = (int)oldGrid["height"]!, width = (int)grid["width"]!;
        Assert.Equal((Content.World.Width, Content.World.Height), (width, (int)grid["height"]!));
        int ox = (width - w) / 2, oy = (Content.World.Height - h) / 2;
        int Moved(int c) => c < 0 || c >= w * h ? -1 : (c / w + oy) * width + c % w + ox;
        int Cell(JsonNode? n) => Moved((int)n!);
        void Same(IEnumerable<JsonNode?> old, IEnumerable<JsonNode?> moved, Func<JsonNode?, int> how) =>
            Assert.Equal(old.Select(how).ToList(), moved.Select(n => (int)n!).ToList());
        IEnumerable<JsonNode?> Each(JsonNode slice, string list, string key) => slice[list]!.AsArray().Select(o => o![key]);

        foreach (var layer in new[] { "structure", "material", "conduit", "back", "backMaterial" })
        {
            var (old, cells) = (Runs.Cells(oldGrid[layer]!), Runs.Cells(grid[layer]!));
            Assert.Equal(Content.World.Width * Content.World.Height, cells.Length);
            for (int c = 0; c < old.Length; c++)
                if (old[c] != cells[Moved(c)]) Assert.Fail($"{layer} at {c}: {old[c]} moved as {cells[Moved(c)]}");
            Assert.Equal(old.Count(v => v != 0), cells.Count(v => v != 0));
        }
        var zone = grid["zone"]!;
        Assert.Equal(Layout.WorldZone, new Box((int)zone["left"]!, (int)zone["top"]!, (int)zone["right"]!, (int)zone["bottom"]!));
        Same(Each(oldGrid, "machines", "cell"), Each(grid, "machines", "cell"), Cell);
        var (oldItems, items) = (was["items"]!, now["items"]!);
        Same(Each(oldItems, "floor", "cell"), Each(items, "floor", "cell"), Cell);
        Same(Each(oldItems, "floor", "x"), Each(items, "floor", "x"), n => (int)n! + ox * 1000);
        Same(Each(oldItems, "floor", "y"), Each(items, "floor", "y"), n => (int)n! + oy * 1000);
        foreach (var end in new[] { "a", "b" }) Same(Each(was["power"]!, "cables", end), Each(now["power"]!, "cables", end), Cell);
        Same(Each(was["building"]!, "plans", "cell"), Each(now["building"]!, "plans", "cell"), Cell);
        Same(was["atmosphere"]!["aired"]!.AsArray(), now["atmosphere"]!["aired"]!.AsArray(), Cell);
        var (oldCrew, crew) = (was["crew"]!, now["crew"]!);
        Assert.Equal(Cell(oldCrew["spawn"]), (int)crew["spawn"]!);
        Assert.Equal(oldCrew["pod"] == null ? null : Cell(oldCrew["pod"]), (int?)crew["pod"]);
        Same(Each(oldCrew, "heroes", "cell"), Each(crew, "heroes", "cell"), Cell);
        var (oldDebris, debris) = (was["debris"]!, now["debris"]!);
        foreach (var x in new[] { "x", "start", "end" }) Same(Each(oldDebris, "pieces", x), Each(debris, "pieces", x), n => (int)n! + ox * 1000);
        Same(Each(oldDebris, "pieces", "y"), Each(debris, "pieces", "y"), n => (int)n! + oy * 1000);
        (int, int)? Reel(JsonNode? r, int dx, int dy) => r == null ? null : ((int)r["toX"]! + dx, (int)r["toY"]! + dy);
        Assert.Equal(Each(oldDebris, "pieces", "reel").Select(r => Reel(r, ox * 1000, oy * 1000)).ToList(),
            Each(debris, "pieces", "reel").Select(r => Reel(r, 0, 0)).ToList());
        return (ox, oy);
    }

    /// <summary>The rooms of a save's grid, by the cell each starts at, and their volumes, as a load finds them.</summary>
    private static List<(int Id, int Volume)> RoomsOf(JsonNode save)
    {
        var grid = save["slices"]!["grid"]!;
        var (_, rooms) = RoomsModule.Find((int)grid["width"]!, (int)grid["height"]!, Runs.Cells(grid["structure"]!), Runs.Cells(grid["back"]!));
        return rooms.Select(r => (r.Id, r.Volume)).ToList();
    }

    /// <summary>The rooms <paramref name="m"/> finds, by the cell each starts at, and their volumes.</summary>
    private static List<(int Id, int Volume)> RoomsOf(Registry m) => m.Rooms.All.Select(r => (r.Id, r.Volume)).ToList();

    [Fact]
    public void MovesEveryCellOfAnOldWreckIntoTheMiddleOfTheWorldItsRoomsAndAirAsTheyWere()
    {
        for (int format = Migrations.Oldest; format < 10; format++)
        {
            var before = UpgradedTo(File.ReadAllText(Fixture(format)), 9);
            var after = UpgradedTo(before.ToJsonString(), 10);
            // The wreck's 192×96 grid, centred in the world's 2048×1024.
            Assert.Equal((928, 464), AssertMovedIntoTheWorld(before, after));
            var m = new Registry();
            World.Load(Migrations.Upgrade(after.ToJsonString()), m.All);
            Assert.Equal(RoomsOf(before).Select(r => ((r.Id / 192 + 464) * 2048 + r.Id % 192 + 928, r.Volume)), RoomsOf(m));
            Assert.Equal(before["slices"]!["atmosphere"]!["aired"]!.AsArray().Count, m.Atmosphere.AiredCells.Count);
        }
    }

    /// <summary>
    /// A small world of the reference tile, played a while with every module's commands so it holds
    /// rooms, air, heroes, plans, cables, floor stacks and debris, saved as format 9 had it: upgraded,
    /// every saved cell and position is at its shifted place, and it loads with the same rooms and air.
    /// </summary>
    [Property(MaxTest = 12)]
    public Property MovesEverySavedCellOfARandomSmallWorldToItsPlaceInTheWorld()
    {
        var worlds = from tilesX in Gen.Choose(1, 4)
                     from tilesY in Gen.Choose(1, 3)
                     from margin in Gen.Choose(0, 12)
                     from extra in Gen.Choose(0, 9)
                     from seed in Gen.Choose(1, 10_000)
                     from ticks in Gen.Choose(0, 300)
                     select (tilesX, tilesY, margin, extra, seed, ticks);
        return Prop.ForAll(worlds.ToArbitrary(), c =>
        {
            int shipWidth = 8 * c.tilesX, shipHeight = 8 * c.tilesY;
            var layout = BudgetTests.Reference(shipWidth + 2 * c.margin + c.extra, shipHeight + 2 * c.margin + c.extra, shipWidth, shipHeight, c.margin);
            var m = new Registry();
            var world = World.Create((uint)c.seed, layout, m.All);
            var rng = new Rng(new RngState { S = (uint)c.seed });
            for (int i = 0; i < c.ticks; i++) world.Step(ModulesTests.RandomCommands(m, rng));
            var before = JsonNode.Parse(world.Save())!.AsObject();
            before["format"] = 9;
            before["slices"]!["grid"]!.AsObject().Remove("zone");
            var after = UpgradedTo(before.ToJsonString(), 10);
            var (ox, oy) = AssertMovedIntoTheWorld(before, after);
            var loaded = new Registry();
            World.Load(Migrations.Upgrade(after.ToJsonString()), loaded.All);
            Assert.Equal(RoomsOf(before).Select(r => ((r.Id / layout.Width + oy) * Content.World.Width + r.Id % layout.Width + ox, r.Volume)), RoomsOf(loaded));
            Assert.Equal(m.Atmosphere.AiredCells.Count, loaded.Atmosphere.AiredCells.Count);
        });
    }

    [Fact]
    public void LeavesASaveOfThisFormatAsItIsAndOneTooOldForTheLoadToRefuse()
    {
        string save = Played();
        Assert.Same(save, Migrations.Upgrade(save));
        string old = save.Replace($"\"format\":{World.SaveFormat}", "\"format\":2");
        Assert.Equal(old, Migrations.Upgrade(old));
        Assert.Throws<InvalidDataException>(() => World.Load(old, new Registry().All));
    }
}
