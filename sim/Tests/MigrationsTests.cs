using Xunit;

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
            for (int i = 0; i < 100; i++) world.Step([]);
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
        Assert.All(new[] { 1, 2, 3 }, x => Assert.NotNull(m.Grid.BackAt(2 * 6 + x)));
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
