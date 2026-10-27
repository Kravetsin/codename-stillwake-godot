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
    public void AFormat34SaveLoadsEveryHeroAndEveryPeerWithoutOneSuitedTheirAirAsItWasAndTheLampOffTheHead()
    {
        // The fixture with peer 9 gone, its inventory kept for its seat (2026-10-27-suits-are-worn).
        var save = JsonNode.Parse(File.ReadAllText(Fixture(34)))!.AsObject();
        var inventories = save["slices"]!["items"]!["inventories"]!.AsArray();
        var gone = new JsonArray();
        for (int i = 0; i < Content.Inventory.Total - 2; i++) gone.Add(i == Content.Inventory.Slots ? new JsonObject { ["item"] = Content.HelmetLamp.Item, ["count"] = 1 } : null);
        inventories.Add(new JsonObject { ["id"] = 9, ["slots"] = gone });
        var heroes = save["slices"]!["crew"]!["heroes"]!.AsArray().Select(h => (Id: h!["id"]!.GetValue<int>(), Suit: h["suit"]!.GetValue<int>())).ToList();
        Assert.NotEmpty(heroes);

        var m = new Registry();
        World.Load(Migrations.Upgrade(save.ToJsonString()), m.All);
        foreach (var (id, suit) in heroes)
        {
            Assert.Equal((Content.Helmet.Basic, Content.Suit.Basic), (m.Items.WornIn(id, Content.Helmet.Slot), m.Items.WornIn(id, Content.Suit.Slot)));
            Assert.Equal(suit, m.Crew.HeroOf(id)!.Air);
        }
        Assert.Equal(Content.Suit.Basic, m.Items.WornIn(9, Content.Suit.Slot));
        // The lamp the head held moved to its own slot.
        Assert.Equal(Content.HelmetLamp.Item, m.Items.WornIn(9, Content.Helmet.Mount));
    }

    [Fact]
    public void AFormat33SaveLetsItsCreaturesGoAndKeepsTheirIdsAndClock()
    {
        // The fixture's strip holding a loper in its old mode (2026-10-26-creatures-are-composed).
        var save = JsonNode.Parse(File.ReadAllText(Fixture(33)))!.AsObject();
        save["surfaces"]![0]!["slices"]!["creatures"] = JsonNode.Parse("""
            {"creatures":[{"id":7,"species":"loper","x":5000,"y":9000,"vx":0,"vy":0,"damage":2,"mode":2,"modeLeft":30,"seenAt":40}],
             "nextId":8,"clock":41}
            """);
        string upgraded = Migrations.Upgrade(save.ToJsonString());
        var creatures = JsonNode.Parse(upgraded)!["surfaces"]![0]!["slices"]!["creatures"]!;
        Assert.Empty(creatures["creatures"]!.AsArray());
        Assert.Equal((8, 41), (creatures["nextId"]!.GetValue<int>(), creatures["clock"]!.GetValue<int>()));
        // And it loads and plays on, its ids never reused.
        var m = new Registry();
        var world = World.Load(upgraded, m.All);
        for (int i = 0; i < 600; i++) world.Step([]);
        Assert.True(JsonNode.Parse(world.Save())!["surfaces"]![0]!["slices"]!["creatures"]!["nextId"]!.GetValue<int>() >= 8);
    }

    [Fact]
    public void LeavesASaveOfThisFormatAsItIsAndOneFromBeforeTheHalvedCellForTheLoadToRefuse()
    {
        string save = Played();
        Assert.Same(save, Migrations.Upgrade(save));
        // The last format of the 16 px cell (0104): its cells another scale's, it loads no more.
        string old = save.Replace($"\"format\":{World.SaveFormat}", "\"format\":27");
        Assert.Equal(old, Migrations.Upgrade(old));
        Assert.Throws<InvalidDataException>(() => World.Load(old, new Registry().All));
    }
}
