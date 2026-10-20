using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>A world made of surfaces (0076): commands routed to their sender's surface, a second surface opened, heroes moved and carried.</summary>
public class SurfacesTests
{
    private const string Map = """
        ..........
        .########.
        .#..D...#.
        .########.
        """;

    /// <summary>A ship with a room heroes wake in at its S, and space round it.</summary>
    private const string Ship = """
        ............
        .##########.
        .#..D..S..#.
        .##########.
        ............
        """;

    private static readonly string Strip = Content.Surfaces.TestStrip.Id;
    private static readonly int StripWidth = Content.Surfaces.TestStrip.Rows[0].Length;
    private static readonly int Belt = Content.Inventory.Slots + 2;
    private static readonly string Drill = Content.Weapons.Drill.Item;

    /// <summary>The ship of <see cref="Ship"/> with the strip opened, hero 0 aboard at its S.</summary>
    private static Scenario Opened()
    {
        var w = new Scenario(Ship);
        return w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Strip)).Run();
    }

    /// <summary>Where peer <paramref name="peer"/>'s hero and items are: every surface holding any of them, by id.</summary>
    private static List<int> Holding(Scenario w, int peer) => Holding(w.World, w.On, peer);

    private static List<int> Holding(World world, int peer) => Holding(world, s => Registry.On(world, s), peer);

    private static List<int> Holding(World world, Func<int, Registry> on, int peer)
    {
        var held = new List<int>();
        foreach (var s in world.Surfaces)
        {
            var m = on(s.Id);
            if (m.Crew.HeroOf(peer) != null || m.Items.Inventory(peer).Any(x => x != null) || m.Items.Running(peer) != null) held.Add(s.Id);
        }
        return held;
    }

    // Slice 1: routing.

    [Fact]
    public void AWorldIsTheShipAloneWithEveryPeerOnIt()
    {
        var w = new Scenario(Map);
        Assert.Equal([new Surface(0, Surface.Ship, 0)], w.World.Surfaces);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Equal(0, w.World.SurfaceOf(7));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void ACommandUnstampedOrStampedWithTheShipReachesTheShipsModules(int? surface)
    {
        var w = new Scenario(Map);
        w.Do(new SetDoor(w.At(4, 2), true) { From = 3, Surface = surface }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.DoorOpen, w.M.Grid.StructureAt(w.At(4, 2)));
    }

    [Fact]
    public void ACommandStampedOnASurfaceItsSenderIsNotOnIsRefusedBeforeAnyModuleSeesIt()
    {
        var w = new Scenario(Map);
        var still = new Scenario(Map);
        // Not a door: the grid would refuse it grid.noDoor, had it seen it.
        w.Do(new SetDoor(w.At(2, 2), true) { From = 1, Surface = 1 }).Run();
        still.Run();
        Assert.Equal(World.Elsewhere, Assert.Single(w.Refusals).Reason);
        Assert.Equal(still.World.Save(), w.World.Save());
    }

    [Fact]
    public void AStampedCommandCarriesItsSurfaceOverTheWireAndAnUnstampedOneNothing()
    {
        var w = new Scenario(Map);
        var text = w.World.Encode(new SetDoor(12, true) { From = 2, Surface = 0 });
        Assert.Equal("""{"type":"grid.setDoor","cell":12,"open":true,"from":2,"surface":0}""", text);
        Assert.Equal(new SetDoor(12, true) { From = 2, Surface = 0 }, w.World.Decode(text));
        Assert.DoesNotContain("surface", w.World.Encode(new SetDoor(12, true)));
    }

    [Fact]
    public void TheWorldsOwnCommandsGoOverTheWireByTheirNames()
    {
        var w = new Scenario(Map);
        Assert.Equal("""{"type":"world.open","kind":"testStrip","from":1}""", w.World.Encode(new OpenSurface(Strip) { From = 1 }));
        Assert.Equal("""{"type":"world.transfer","to":1,"cell":null,"from":1}""", w.World.Encode(new Transfer(1) { From = 1 }));
        Assert.Equal(new Transfer(1, 5) { From = 1 }, w.World.Decode(w.World.Encode(new Transfer(1, 5) { From = 1 })));
    }

    [Fact]
    public void ASaveLoadsAndSavesByteForByteAndAShipAloneHasNoKeyForSurfaces()
    {
        // The fixture of this format, with two strips opened, loads and saves byte for byte; a hash is of those bytes. A ship
        // alone saves as before surfaces.
        string before = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"format-{World.SaveFormat}.json"));
        Assert.Equal(before, World.Load(before, new Registry().All).Save());
        var root = JsonNode.Parse(new Scenario(Ship).Run(5).World.Save())!.AsObject();
        Assert.Equal(["format", "tick", "rng", "slices"], root.Select(p => p.Key));
    }

    // Slice 2: a second surface.

    [Fact]
    public void OpensTheStripFromItsMapSeededByTheWorldAndMovesAHeroThereAndBack()
    {
        var w = Opened();
        var strip = Assert.Single(w.World.Surfaces, s => s.Id == 1);
        Assert.Equal(Strip, strip.Kind);
        Assert.NotEqual(0u, strip.Seed);
        var m = w.On(1);
        Assert.Equal(StripWidth, m.Grid.Width);
        Assert.Equal(27 * StripWidth + 14, m.Crew.Spawn);
        // Its own modules, the world's globals shared.
        Assert.NotSame(w.M.Grid, m.Grid);
        Assert.Same(w.M.Players, m.Players);
        Assert.Same(w.M.Knowledge, m.Knowledge);

        w.Do(new Transfer(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(1, w.World.SurfaceOf(0));
        Assert.Null(w.M.Crew.HeroOf(0));
        Assert.Equal(m.Crew.Spawn, m.Crew.HeroOf(0)!.Cell);

        w.Do(new Transfer(0, w.At(3, 2)) { Surface = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Null(m.Crew.HeroOf(0));
        Assert.Equal(w.At(3, 2), w.M.Crew.HeroOf(0)!.Cell);
    }

    [Fact]
    public void RefusesATransferToASurfaceThereIsNotOrTheOneItsSenderIsOn()
    {
        var w = new Scenario(Ship);
        w.Do(new Transfer(1)).Do(new Transfer(0)).Do(new Transfer(-1)).Run();
        Assert.Equal([World.NoSurface, World.There, World.NoSurface], w.Refusals.Select(r => r.Reason));
        Assert.Equal(["format", "tick", "rng", "slices"], JsonNode.Parse(w.World.Save())!.AsObject().Select(p => p.Key));
    }

    [Fact]
    public void EverySurfaceRunsEveryTickInIdOrder()
    {
        var w = Opened().Run(10);
        // Items count their own ticks: the strip, opened on the first, has run every tick since with the ship.
        Assert.Equal(((ItemsState)w.M.Items.Slice).Ticks, ((ItemsState)w.On(1).Items.Slice).Ticks);
    }

    [Fact]
    public void ANewSurfaceKeepsTheCreativeModeOfTheSurfaceItWasOpenedFrom()
    {
        var w = new Scenario(Ship);
        w.Do(new SetCreative(true)).Do(new OpenSurface(Strip)).Run();
        Assert.True(w.On(1).Items.Creative);
        w.Do(new SetCreative(false)).Do(new OpenSurface(Strip)).Run();
        Assert.False(w.On(2).Items.Creative);
    }

    [Fact]
    public void CarriesTheHerosSuitAndThrustTheInventoryAndARunningDrillAcross()
    {
        var w = Opened();
        w.GiveItems(0, (Drill, 1), ("scrap", 30)).Put(Belt, new Stack("battery", 1, 500_000)).Wear(0, Content.Jetpack.Item);
        w.Do(new Exert(true)).Do(new Run(Drill, true)).Run(Content.Crew.BreathEvery);
        var hero = w.M.Crew.HeroOf(0)!;
        var before = (hero.Suit, hero.Exerting, hero.ThrustTicks);
        var inventory = w.M.Items.Inventory(0).ToList();

        w.Do(new Transfer(1)).Run();
        var m = w.On(1);
        var there = m.Crew.HeroOf(0)!;
        Assert.Equal(before, (there.Suit, there.Exerting, there.ThrustTicks));
        Assert.Equal(Drill, m.Items.Running(0));
        // The drill drew its tick on the strip: the battery paid it there, everything else as it was.
        Assert.Equal(inventory.Where((_, i) => i != Belt), m.Items.Inventory(0).Where((_, i) => i != Belt));
        int draw = Content.Items.Get(Drill).Draw!.Value * Content.Charge.Scale;
        Assert.Equal(inventory[Belt]!.Charge - draw, m.Items.Inventory(0)[Belt]!.Charge);
        Assert.Equal([1], Holding(w, 0));
        Assert.Null(w.M.Items.Running(0));
        Assert.Empty(w.Stored().Inventories);
    }

    [Fact]
    public void ACommandSentFromTheShipLandingAfterItsHeroLeftIsRefusedElsewhere()
    {
        var w = Opened();
        w.Do(new Transfer(1) { Surface = 0 }).Do(new SetDoor(w.At(4, 2), true) { Surface = 0 }).Run();
        Assert.Equal([World.Elsewhere], w.Refusals.Select(r => r.Reason));
        Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(w.At(4, 2)));
    }

    [Fact]
    public void TheHostsUnstampedFarewellFindsAHeroOnTheStripAndItsSeatWaitsAboard()
    {
        var w = Opened().GiveItems(0, ("scrap", 7));
        w.Do(new Transfer(1)).Run();
        w.Do(new Leave()).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.On(1).Crew.HeroOf(0));
        Assert.Null(w.M.Players.ColourOf(0));
        // Back aboard, its inventory with it: coming back, it wakes at the ship's spawn with what it had.
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Equal(new Stack("scrap", 7), w.M.Items.Inventory(0)[0]);
        w.Do(new Enter(w.M.Crew.Spawn) { Surface = 0 }).Run();
        Assert.Empty(w.Refusals);
        Assert.NotNull(w.M.Crew.HeroOf(0));
        Assert.Equal([0], Holding(w, 0));
    }

    [Fact]
    public void AHeroDyingOnTheStripWakesAtTheShipsPodItsBodyLeftOnTheStrip()
    {
        var w = Opened().GiveItems(0, ("scrap", 9), (Content.Weapons.Axe.Item, 1));
        w.Do(new Transfer(1)).Run();
        var m = w.On(1);
        // Up in the sky, its suit all but empty: the next breath passes it out.
        int sky = 5 * StripWidth + 14;
        w.Do(new Enter(sky) { Surface = 1 }).Run();
        m.Crew.HeroOf(0)!.Suit = 1;
        w.Run(Content.Crew.BreathEvery);
        Assert.True(m.Crew.HeroOf(0)!.Out > 0);
        var body = Assert.Single(m.Items.Floor);
        Assert.Equal(("scrap", 9, 0), (body.Item, body.Count, body.Body!.Peer));

        w.Run(Content.Crew.PassOut);
        Assert.Equal(0, w.World.SurfaceOf(0));
        Assert.Null(m.Crew.HeroOf(0));
        var hero = w.M.Crew.HeroOf(0)!;
        Assert.Equal((w.M.Crew.Spawn, Content.Crew.Suit, 0), (hero.Cell, hero.Suit, hero.Out));
        // The gear it kept came with it; the body stays on the strip.
        Assert.Equal(new Stack(Content.Weapons.Axe.Item, 1), w.M.Items.Inventory(0).First(s => s != null));
        Assert.Single(m.Items.Floor);
        Assert.Equal([0], Holding(w, 0));
    }

    [Fact]
    public void TwoHeroesOnTwoSurfacesNeverWearOneColour()
    {
        var w = Opened();
        w.Do(new Transfer(1)).Run();
        // Hero 1 comes in aboard while hero 0, in the first colour, is on the strip.
        w.Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run();
        Assert.Equal(0, w.M.Players.ColourOf(0));
        Assert.Equal(1, w.M.Players.ColourOf(1));
        w.Do(new PickColour(0) { From = 1 }).Run();
        Assert.Equal(["crew.colourTaken"], w.Refusals.Select(r => r.Reason));
        // Together again, still two colours.
        w.Do(new Transfer(0) { Surface = 1 }).Run();
        Assert.Equal([(0, 0), (1, 1)], w.M.Crew.Heroes.Select(h => (h.Id, w.M.Players.ColourOf(h.Id) ?? -1)));
    }

    [Fact]
    public void WhatIsLearnedOnOneSurfaceIsKnownOnEvery()
    {
        var w = Opened();
        w.M.Knowledge.Learn("wire");
        Assert.Equal(["wire"], w.On(1).Research.Analysed);
        Assert.Equal(w.M.Research.Recipes().Select(r => r.Known), w.On(1).Research.Recipes().Select(r => r.Known));
    }

    [Fact]
    public void SavesLoadsAndPlaysOnAWorldOfTwoSurfacesToTheSameHash()
    {
        var w = Opened().GiveItems(0, ("scrap", 3));
        w.Do(new Transfer(1)).Do(new Enter(w.M.Crew.Spawn) { From = 2 }).Run(30);
        var save = w.World.Save();
        var root = JsonNode.Parse(save)!.AsObject();
        Assert.Equal(["format", "tick", "rng", "slices", "surfaces", "where"], root.Select(p => p.Key));
        Assert.Equal("""[{"peer":0,"surface":1}]""", root["where"]!.ToJsonString());

        var copy = new Scenario(Ship).Reload(save);
        Assert.Equal(save, copy.World.Save());
        Assert.Equal(1, copy.World.SurfaceOf(0));
        Assert.Equal([1], Holding(copy, 0));
        for (int i = 0; i < 60; i++)
        {
            Command[] commands = i % 20 == 0 ? [new Enter(i) { From = 0 }, new Exert(i % 40 == 0) { From = 2 }, new OpenSurface(Strip)] : [];
            w.World.Step(commands);
            copy.World.Step(commands);
        }
        Assert.Equal(5, copy.World.Surfaces.Count);
        Assert.Equal(w.World.Hash(), copy.World.Hash());
    }

    /// <summary>
    /// A seat's hero, inventory and run are on its surface and nowhere else, through random play with surfaces opened
    /// and heroes moved; and a transfer carries every item: a world stepped with it holds what its copy stepped without
    /// it does, item for item.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property KeepsEverySeatsOwnOnItsSurfaceAloneAndLosesNoItemCrossing()
    {
        var gen = from seed in Gen.Choose(1, 100_000)
                  from ticks in Gen.Choose(20, 200)
                  from peer in Gen.Choose(0, 4)
                  select (seed, ticks, peer);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var m = new Registry();
            var world = World.Create((uint)t.seed, Layout.Parse(Ship), m.All);
            world.Step([new OpenSurface(Strip), .. Enumerable.Range(0, 5).Select(p => new Transfer(1) { From = p })]);
            var rng = new Rng(new RngState { S = (uint)t.seed });
            for (int i = 0; i < t.ticks; i++)
            {
                world.Step([.. ModulesTests.RandomCommands(world, rng), .. Seed(world, rng)]);
                for (int p = 0; p < 5; p++)
                    if (Holding(world, p).Any(s => s != world.SurfaceOf(p)))
                        return false.Label($"tick {i}: peer {p} on {world.SurfaceOf(p)} holds things on {string.Join(", ", Holding(world, p))}");
            }
            var copy = World.Load(world.Save(), new Registry().All);
            int to = (world.SurfaceOf(t.peer) + 1) % world.Surfaces.Count;
            world.Step([new Transfer(to) { From = t.peer }]);
            copy.Step([]);
            return (Totals(world).SequenceEqual(Totals(copy)) && world.SurfaceOf(t.peer) == to).Label($"to {to}: {string.Join(",", Totals(world))} against {string.Join(",", Totals(copy))}");
        });
    }

    /// <summary>Now and then something to carry, put straight into a peer's inventory on its surface: random play makes few items.</summary>
    private static IEnumerable<Command> Seed(World world, Rng rng)
    {
        if (rng.Below(10) != 0) yield break;
        int peer = (int)rng.Below(5);
        var items = Registry.On(world, world.SurfaceOf(peer)).Items;
        if (items.FullFor(peer, [("scrap", 5)]) == null) items.Give(peer, [("scrap", 5)]);
    }

    /// <summary>Every item in a world, on every surface, in inventories, machines and on the floor, by item, sorted.</summary>
    private static List<(string Item, int Count)> Totals(World world)
    {
        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        void Add(Stack? s)
        {
            if (s != null) counts[s.Item] = counts.GetValueOrDefault(s.Item) + s.Count;
        }
        foreach (var surface in world.Surfaces)
        {
            var m = Registry.On(world, surface.Id);
            var state = (ItemsState)m.Items.Slice;
            foreach (var held in state.Inventories) foreach (var s in held.Slots) Add(s);
            foreach (var held in state.Crates) foreach (var s in held.Slots) Add(s);
            foreach (var f in state.Floor) Add(new Stack(f.Item, f.Count));
        }
        return counts.Select(c => (c.Key, c.Value)).ToList();
    }
}
