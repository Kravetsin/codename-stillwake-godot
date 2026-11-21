using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The light drone (light-drone, drone-companion): a hero touching the alien core with E is paired with a shard of it for good, the core
/// staying for the next; once the floor under the core breaks, every hero is paired as it stands. Before pairing a hero's own slots are
/// the hotbar alone, after it the bag rows and what upgrades add.
/// </summary>
public class LightDroneTests
{
    /// <summary>The pod, a crate beside it, and the alien core on the floor in the corner.</summary>
    private const string Room = """
        ###########
        #.........#
        #.OOO.....#
        #.OOO.....#
        #.OOO.KK.?#
        ###########
        """;

    private static readonly string Drone = LightDrone.Item;
    private static readonly Items.Place FirstBag = new(Inventory.Hotbar);

    /// <summary>The room with peer 0's hero in at the pod, unpaired; everything known, every upgrade too.</summary>
    private static Scenario Aboard() => Aboard(Layout.Parse(Room));

    private static Scenario Aboard(Layout layout)
    {
        var w = new Scenario(layout, doubled: true) { Unpaired = true };
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        return w;
    }

    private static int Core(Scenario w) => w.M.Grid.MachinesOf(LightDrone.CoreMachine)[0].Id;

    /// <summary>Eight full stacks of as many materials, after the kit's screwdriver: the hotbar full.</summary>
    private static (string Item, int Count)[] EightStacks() =>
        [.. Content.Items.All.Where(i => i.Group == ItemGroup.Material && i.Stack > 1 && i.Id != "scrap").Take(8).Select(i => (i.Id, i.Stack))];

    [Fact]
    public void TheCoreLiesInTheWreckFixedAndTheDroneIsNoCraftNorInTheKit()
    {
        var wreck = new Registry();
        World.Create(1, Registry.Wreck, wreck.All);
        Assert.Single(wreck.Grid.MachinesOf(LightDrone.CoreMachine));
        var spec = Machines.Get(LightDrone.CoreMachine);
        Assert.True(spec.Fixed);
        Assert.True(spec.Is(MachineTrait.Wakes));
        Assert.Null(spec.Menu);
        Assert.DoesNotContain(Crafts.All, c => c.Item == Drone);
        Assert.DoesNotContain(Stations.All.SelectMany(s => s.Rows).SelectMany(r => r), r => r == Drone);
        Assert.DoesNotContain(Inventory.Kit, k => k.Item == Drone);
    }

    [Fact]
    public void IsWornNowhereDrawsNothingAndIsKeptAtDeath()
    {
        var spec = Content.Items.Get(Drone);
        Assert.Null(spec.Equip);
        Assert.DoesNotContain("mount", Inventory.Equipment);
        Assert.Null(spec.Draw);
        Assert.True(spec.Gear);
    }

    [Fact]
    public void TouchingTheCorePairsForGoodOpensTheBagAndLeavesTheCoreForTheNext()
    {
        var w = Aboard();
        Assert.False(w.M.Players.HasDrone(0));
        Assert.False(w.M.Items.SlotOpen(0, FirstBag));
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Players.HasDrone(0));
        Assert.True(w.M.Items.SlotOpen(0, FirstBag));
        Assert.Equal(0, w.M.Items.Totals(0).Where(t => t.Item == Drone).Sum(t => t.Count));
        // Once paired, a second touch is refused; a friend joining later takes their own shard of the same core.
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run();
        Assert.Equal(["crew.hasDrone"], w.Refusals.Select(r => r.Reason));
        w.Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run().Do(new PairDrone(Core(w)) { From = 1 }).Run();
        Assert.Single(w.Refusals);
        Assert.True(w.M.Players.HasDrone(1));
        Assert.True(w.M.Items.SlotOpen(1, FirstBag));
        Assert.Single(w.M.Grid.MachinesOf(LightDrone.CoreMachine));
    }

    [Fact]
    public void PairingIsRefusedWithNoHeroOnePassedOutOrNoCoreThere()
    {
        var none = new Scenario(Room) { Unpaired = true };
        none.Do(new PairDrone(Core(none)) { From = 0 }).Run();
        var passedOut = Aboard();
        passedOut.Do(new Succumb { From = 0 }).Run().Do(new PairDrone(Core(passedOut)) { From = 0 }).Run();
        var pod = Aboard();
        pod.Do(new PairDrone(pod.M.Grid.MachinesOf("lifepod")[0].Id) { From = 0 }).Do(new PairDrone(0) { From = 0 }).Run();
        Assert.Equal(["crew.noHero"], none.Refusals.Select(r => r.Reason));
        Assert.Equal(["crew.noHero"], passedOut.Refusals.Select(r => r.Reason));
        Assert.Equal(["crew.noCore", "crew.noCore"], pod.Refusals.Select(r => r.Reason));
        Assert.All(new[] { none, passedOut, pod }, w => Assert.False(w.M.Players.HasDrone(0)));
    }

    /// <summary>The floor under the core broken, the core is lost: every hero without a drone is paired as it stands, a late one on waking, and the core gives no more.</summary>
    [Fact]
    public void TheFloorUnderTheCoreBrokenPairsEveryHeroAndTheCoreIsGone()
    {
        var w = Aboard();
        var core = w.M.Grid.MachineById(Core(w))!;
        var spec = Machines.Get(LightDrone.CoreMachine);
        Assert.False(w.M.Crew.CoreLost());
        w.M.Grid.Clear(core.Cell + spec.Height * w.M.Grid.Width + 1, "structure");
        w.Run();
        Assert.True(w.M.Crew.CoreLost());
        Assert.True(w.M.Players.HasDrone(0));
        w.Do(new Enter(w.M.Crew.Spawn) { From = 2 }).Run(2);
        Assert.True(w.M.Players.HasDrone(2));
        Assert.True(w.M.Items.SlotOpen(2, FirstBag));
        Assert.Equal("crew.noCore", w.M.Crew.PairProblem(3, core.Id) is "crew.noHero" ? "crew.noCore" : w.M.Crew.PairProblem(3, core.Id));
    }

    /// <summary>
    /// The floor under the core is taken apart like any floor, the core holding none: the core is gone from the map, every hero is
    /// paired, and a floor laid there again brings nothing back, its place free to build in.
    /// </summary>
    [Fact]
    public void TakingTheFloorUnderTheCoreApartLosesItForGoodAndFreesItsPlace()
    {
        var w = Aboard();
        var core = w.M.Grid.MachineById(Core(w))!;
        int under = core.Cell + Machines.Get(LightDrone.CoreMachine).Height * w.M.Grid.Width;
        w.Do(new SetCreative(true)).Do(new Building.SetInstant(true)).Do(new Building.Deconstruct(under, "structure")).Run(2);
        Assert.Empty(w.Refusals);
        Assert.False(w.M.Grid.Solid(under));
        Assert.True(w.M.Crew.CoreLost());
        Assert.True(w.M.Players.HasDrone(0));
        Assert.Empty(w.M.Grid.MachinesOf(LightDrone.CoreMachine));
        Assert.Null(w.M.Grid.MachineAt(core.Cell));
        // The floor laid back, the core stays lost: a hero waking later is still paired.
        w.Do(new Building.PlacePlan(under, new Grid.Placement("structure", "floor"))).Run();
        Assert.True(w.M.Grid.Solid(under));
        w.Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run(2);
        Assert.True(w.M.Crew.CoreLost());
        Assert.True(w.M.Players.HasDrone(1));
    }

    /// <summary>In the wreck itself, every cell of the floor under the core can be taken apart, and any one of them loses it.</summary>
    [Fact]
    public void InTheWreckEachCellOfTheCoresFloorIsTakenApartAndLosesIt()
    {
        var probe = new Scenario(Registry.Wreck) { Unpaired = true };
        var core = probe.M.Grid.MachineById(Core(probe))!;
        var spec = Machines.Get(LightDrone.CoreMachine);
        for (int x = 0; x < spec.Width; x++)
        {
            var w = new Scenario(Registry.Wreck) { Unpaired = true };
            w.Do(new Enter(w.M.Crew.Spawn)).Do(new SetCreative(true)).Do(new Building.SetInstant(true)).Run();
            int under = core.Cell + spec.Height * w.M.Grid.Width + x;
            w.Do(new Building.Deconstruct(under, "structure")).Run(2);
            Assert.Empty(w.Refusals);
            Assert.True(w.M.Crew.CoreLost(), $"the cell {x} under the core taken apart, the core stayed");
            Assert.True(w.M.Players.HasDrone(0));
        }
    }

    /// <summary>A map with no core never loses one: its heroes stay unpaired.</summary>
    [Fact]
    public void AMapWithNoCoreNeverPairsByItself()
    {
        var w = new Scenario("""
            ######
            #.OOO#
            #.OOO#
            #.OOO#
            ######
            """) { Unpaired = true };
        w.Do(new Enter(w.M.Crew.Spawn)).Run(5);
        Assert.False(w.M.Crew.CoreLost());
        Assert.False(w.M.Players.HasDrone(0));
    }

    [Fact]
    public void CreativeOpensEverySlotPairedOrNot()
    {
        var w = Aboard();
        w.Do(new SetCreative(true) { From = 0 }).Run();
        Assert.True(w.M.Items.SlotOpen(0, new Items.Place(Inventory.Slots - 1)));
        Assert.False(w.M.Players.HasDrone(0));
    }

    [Fact]
    public void BeforePairingAPickUpPastTheHotbarIsRefusedAndWhatIsGivenPastItDrops()
    {
        var w = Aboard().GiveItems(0, EightStacks());
        int cell = w.M.Crew.HeroOf(0)!.Cell;
        w.M.Items.LayDown(new Stack("scrap", 5), cell);
        w.Do(new PickUp(w.M.Items.Floor[0].Id) { From = 0 }).Run();
        Assert.Equal(["items.full"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(5, Assert.Single(w.M.Items.Floor).Count);
        w.M.Items.GiveOrDrop(0, [("scrap", 3)], cell);
        Assert.Equal(0, w.M.Items.Totals(0).Where(t => t.Item == "scrap").Sum(t => t.Count));
        Assert.Equal(8, w.M.Items.Floor.Where(s => s.Item == "scrap").Sum(s => s.Count));

        // Paired, the same pick-up goes into the bag.
        var paired = new Scenario(Room);
        paired.Do(new Enter(paired.M.Crew.Spawn)).Run().GiveItems(0, EightStacks());
        paired.M.Items.LayDown(new Stack("scrap", 5), cell);
        paired.Do(new PickUp(paired.M.Items.Floor[0].Id) { From = 0 }).Run();
        Assert.Empty(paired.Refusals);
        Assert.Equal(5, paired.M.Items.Totals(0).Where(t => t.Item == "scrap").Sum(t => t.Count));
    }

    [Fact]
    public void PairingOpensTheStartAndTheBagUpgradesBought()
    {
        // Nothing known but two bag upgrades bought.
        var w = Aboard(Layout.Parse(Room) with { Known = [] });
        w.M.Knowledge.Grant("bag1");
        w.M.Knowledge.Grant("bag2");
        Assert.False(w.M.Items.SlotOpen(0, FirstBag));
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run();
        Assert.True(w.M.Items.SlotOpen(0, new Items.Place(Inventory.Start + 1)));
        Assert.False(w.M.Items.SlotOpen(0, new Items.Place(Inventory.Start + 2)));
        // The equipment stays with the body, paired or not.
        Assert.True(Aboard().M.Items.SlotOpen(0, new Items.Place(Inventory.Slots)));
    }

    [Fact]
    public void APairingOutlivesDeath()
    {
        var w = Aboard();
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run().Do(new Succumb { From = 0 }).Run(Content.Crew.PassOut + 1);
        Assert.Equal(0, w.M.Crew.HeroOf(0)!.Out);
        Assert.True(w.M.Players.HasDrone(0));
        Assert.True(w.M.Items.SlotOpen(0, FirstBag));
    }

    [Fact]
    public void APairingIsThePlayersOnEverySurface()
    {
        const string Ship = """
            ............
            .##########.
            .#........#.
            .#?.D..S..#.
            .##########.
            ............
            """;
        var w = new Scenario(Ship) { Unpaired = true };
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new PairDrone(Core(w))).Do(new OpenSurface(Content.Surfaces.TestStrip.Id)).Run();
        w.Do(new Transfer(1)).Run();
        Assert.Equal(1, w.World.SurfaceOf(0));
        Assert.True(w.On(1).Players.HasDrone(0));
        Assert.True(w.On(1).Items.SlotOpen(0, FirstBag));
        w.Do(new PairDrone(0)).Run();
        Assert.Equal(["crew.hasDrone"], w.Refusals.Select(r => r.Reason));
    }

    /// <summary>The lamp and the scanner are the drone's modes, each opened by its node (light-drone): no item, so no wreck or crate gives one.</summary>
    [Theory]
    [InlineData("scanner")]
    [InlineData("droneLamp")]
    public void EachModeIsANodeNeitherAnItemNorLoot(string mode)
    {
        Assert.Contains(mode, (string[])[LightDrone.ScannerMode, LightDrone.LampMode]);
        Assert.False(Content.Items.TryGet(mode, out _));
        Assert.NotNull(Content.Research.Node(mode));
        Assert.DoesNotContain(mode, Content.Research.GearSold);
        Assert.DoesNotContain(Loot.All.SelectMany(t => t.Entries), e => e.Item == mode);
        Assert.DoesNotContain(mode, Content.Research.StartKnown);
    }

    /// <summary>
    /// Through random play on the wreck, creative switched and drones paired by the fuzz: a peer not paired and not creative has no
    /// own slot open past the hotbar, and a peer once paired stays paired.
    /// </summary>
    [Property(MaxTest = 12)]
    public Property AnUnpairedHeroHasTheHotbarAloneAndAPairingIsNeverUndone()
    {
        var gen = from seed in Gen.Choose(1, 100_000)
                  from ticks in Gen.Choose(50, 300)
                  select (seed, ticks);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var world = World.Create((uint)t.seed, Registry.Wreck, new Registry().All);
            var rng = new Rng(new RngState { S = (uint)t.seed });
            var paired = new SortedSet<int>();
            for (int i = 0; i < t.ticks; i++)
            {
                world.Step(ModulesTests.RandomCommands(world, rng));
                for (int p = 0; p < 5; p++)
                {
                    var m = Registry.On(world, world.SurfaceOf(p));
                    if (m.Players.HasDrone(p)) paired.Add(p);
                    else if (paired.Contains(p)) return false.Label($"tick {i}: peer {p}'s pairing undone");
                    else if (!m.Items.Creative(p) && Enumerable.Range(Inventory.Hotbar, Inventory.Slots - Inventory.Hotbar).Any(s => m.Items.SlotOpen(p, new Items.Place(s))))
                        return false.Label($"tick {i}: peer {p}, not paired, has a bag slot open");
                }
            }
            return true.ToProperty();
        });
    }
}
