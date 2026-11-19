using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The light drone (light-drone, drone-companion): crafted at the pod of scrap, a right click in hand pairs a hero with it for good,
/// the item taken; before that a hero's own slots are the hotbar alone, after it the bag rows and what upgrades add.
/// </summary>
public class LightDroneTests
{
    /// <summary>The pod, and a crate beside it.</summary>
    private const string Room = """
        ##########
        #........#
        #.OOO....#
        #.OOO....#
        #.OOO.KK.#
        ##########
        """;

    private static readonly string Drone = LightDrone.Item;
    private static readonly Items.Place FirstBag = new(Inventory.Hotbar);

    /// <summary>The room with peer 0's hero in at the pod, unpaired, carrying <paramref name="cost"/> after its kit; everything known, every upgrade too.</summary>
    private static Scenario Aboard(params (string Item, int Count)[] cost) => Aboard(Layout.Parse(Room), cost);

    private static Scenario Aboard(Layout layout, params (string Item, int Count)[] cost)
    {
        var w = new Scenario(layout, doubled: true) { Unpaired = true };
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        return cost.Length > 0 ? w.GiveItems(0, cost) : w;
    }

    private static int Pod(Scenario w) => w.M.Grid.MachinesOf("lifepod")[0].Id;

    private static int Carried(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);

    /// <summary>Eight full stacks of as many materials, after the kit's screwdriver: the hotbar full.</summary>
    private static (string Item, int Count)[] EightStacks() =>
        [.. Content.Items.All.Where(i => i.Group == ItemGroup.Material && i.Stack > 1 && i.Id != "scrap").Take(8).Select(i => (i.Id, i.Stack))];

    [Fact]
    public void ThePodWakesTheKitsCoreInFiveRawChunksAndTheWorkbenchNot()
    {
        Assert.Equal([(LightDrone.Core, 1), ("scrapChunk", 5)], Crafts.Get(Drone).Cost);
        Assert.Contains(Drone, Stations.Get("lifepod").Rows.SelectMany(r => r));
        Assert.DoesNotContain(Drone, Stations.Get("workbench").Rows.SelectMany(r => r));
        var w = Aboard(("scrapChunk", 10));
        Assert.Equal(1, Carried(w, LightDrone.Core));
        int pod = Pod(w);
        w.Do(new Crafting.Craft(pod, Drone, 1)).Run(Crafts.Get(Drone).Ticks * 1000 / Stations.Get("lifepod").Speed + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(Drone, 1), w.M.Items.Crate(pod)[0]);
        Assert.Equal((0, 5), (Carried(w, LightDrone.Core), Carried(w, "scrapChunk")));
    }

    /// <summary>One core a hero, so one drone: with chunks to spare, a second is refused, and two at once too.</summary>
    [Fact]
    public void TheCoreMakesOneDroneNoMore()
    {
        var w = Aboard(("scrapChunk", 20));
        int pod = Pod(w);
        w.Do(new Crafting.Craft(pod, Drone, 2)).Run();
        Assert.Equal(["items.notEnough"], w.Refusals.Select(r => r.Reason));
        w.Do(new Crafting.Craft(pod, Drone, 1)).Run().Do(new Crafting.Craft(pod, Drone, 1)).Run();
        Assert.Equal(["items.notEnough", "items.notEnough"], w.Refusals.Select(r => r.Reason));
        Assert.Single(w.M.Crafting.Queue(pod));
    }

    /// <summary>The core is gear, kept through a death, and sold to no one; the drone's recipe waits for the chunks alone.</summary>
    [Fact]
    public void TheCoreIsKeptAtDeathUnsoldAndInEveryKit()
    {
        Assert.True(Content.Items.Get(LightDrone.Core).Gear);
        Assert.Null(Market.PriceOf(LightDrone.Core));
        Assert.Contains((LightDrone.Core, 1), Inventory.Kit);
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
    public void PairingTakesOneDroneForGoodAndOpensTheBag()
    {
        var w = Aboard((Drone, 2));
        Assert.False(w.M.Players.HasDrone(0));
        Assert.False(w.M.Items.SlotOpen(0, FirstBag));
        w.Do(new PairDrone { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Players.HasDrone(0));
        Assert.Equal(1, Carried(w, Drone));
        Assert.True(w.M.Items.SlotOpen(0, FirstBag));
        // A second drone is refused, and stays carried: no second pairing, no gift.
        w.Do(new PairDrone { From = 0 }).Run();
        Assert.Equal(["crew.hasDrone"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(1, Carried(w, Drone));
    }

    [Fact]
    public void PairingIsRefusedWithNoHeroOnePassedOutOrNoDroneCarried()
    {
        var none = new Scenario(Room) { Unpaired = true };
        none.Do(new PairDrone { From = 0 }).Run();
        var passedOut = Aboard((Drone, 1));
        passedOut.Do(new Succumb { From = 0 }).Run().Do(new PairDrone { From = 0 }).Run();
        var empty = Aboard();
        empty.Do(new PairDrone { From = 0 }).Run();
        Assert.Equal(["crew.noHero"], none.Refusals.Select(r => r.Reason));
        Assert.Equal(["crew.noHero"], passedOut.Refusals.Select(r => r.Reason));
        Assert.Equal(["items.notEnough"], empty.Refusals.Select(r => r.Reason));
        Assert.All(new[] { none, passedOut, empty }, w => Assert.False(w.M.Players.HasDrone(0)));
    }

    [Fact]
    public void CreativePairsForFreeAndOpensEverySlotPairedOrNot()
    {
        var w = Aboard();
        w.Do(new SetCreative(true) { From = 0 }).Run();
        Assert.True(w.M.Items.SlotOpen(0, new Items.Place(Inventory.Slots - 1)));
        w.Do(new PairDrone { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Players.HasDrone(0));
        Assert.Equal(0, Carried(w, Drone));
    }

    [Fact]
    public void BeforePairingAPickUpPastTheHotbarIsRefusedAndWhatIsGivenPastItDrops()
    {
        var w = Aboard(EightStacks());
        int cell = w.M.Crew.HeroOf(0)!.Cell;
        w.M.Items.LayDown(new Stack("scrap", 5), cell);
        w.Do(new PickUp(w.M.Items.Floor[0].Id) { From = 0 }).Run();
        Assert.Equal(["items.full"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(5, Assert.Single(w.M.Items.Floor).Count);
        w.M.Items.GiveOrDrop(0, [("scrap", 3)], cell);
        Assert.Equal(0, Carried(w, "scrap"));
        Assert.Equal(8, w.M.Items.Floor.Where(s => s.Item == "scrap").Sum(s => s.Count));

        // Paired, the same pick-up goes into the bag.
        var paired = new Scenario(Room);
        paired.Do(new Enter(paired.M.Crew.Spawn)).Run().GiveItems(0, EightStacks());
        paired.M.Items.LayDown(new Stack("scrap", 5), cell);
        paired.Do(new PickUp(paired.M.Items.Floor[0].Id) { From = 0 }).Run();
        Assert.Empty(paired.Refusals);
        Assert.Equal(5, Carried(paired, "scrap"));
    }

    [Fact]
    public void PairingOpensTheStartAndTheBagUpgradesBought()
    {
        // Nothing known but two bag upgrades bought.
        var w = Aboard(Layout.Parse(Room) with { Known = [] }, (Drone, 1));
        w.M.Knowledge.Grant("bag1");
        w.M.Knowledge.Grant("bag2");
        Assert.False(w.M.Items.SlotOpen(0, FirstBag));
        w.Do(new PairDrone { From = 0 }).Run();
        Assert.True(w.M.Items.SlotOpen(0, new Items.Place(Inventory.Start + 1)));
        Assert.False(w.M.Items.SlotOpen(0, new Items.Place(Inventory.Start + 2)));
        // The equipment stays with the body, paired or not.
        Assert.True(Aboard().M.Items.SlotOpen(0, new Items.Place(Inventory.Slots)));
    }

    [Fact]
    public void APairingOutlivesDeath()
    {
        var w = Aboard((Drone, 1));
        w.Do(new PairDrone { From = 0 }).Run().Do(new Succumb { From = 0 }).Run(Content.Crew.PassOut + 1);
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
            .#..D..S..#.
            .##########.
            ............
            """;
        var w = new Scenario(Ship) { Unpaired = true }.GiveItems(0, (Drone, 2));
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new PairDrone()).Do(new OpenSurface(Content.Surfaces.TestStrip.Id)).Run();
        w.Do(new Transfer(1)).Run();
        Assert.Equal(1, w.World.SurfaceOf(0));
        Assert.True(w.On(1).Players.HasDrone(0));
        Assert.True(w.On(1).Items.SlotOpen(0, FirstBag));
        w.Do(new PairDrone()).Run();
        Assert.Equal(["crew.hasDrone"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void AFriendJoiningLaterPaysItsDroneFromTheCrewsCrates()
    {
        // Peer 0 paired an hour ago; peer 1 wakes bare with its own core and pays the chunks from the crate (craft-from-crates).
        var w = new Scenario(Room) { Unpaired = true };
        int pod = Pod(w), crate = w.M.Grid.MachinesOf("crate")[0].Id;
        w.FillCrate(crate, ("scrapChunk", 15));
        w.Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run();
        w.Do(new Crafting.Craft(pod, Drone, 1) { From = 1 }).Run(Crafts.Get(Drone).Ticks * 1000 / Stations.Get("lifepod").Speed + 1);
        Assert.Equal(new Stack("scrapChunk", 10), w.M.Items.Crate(crate)[0]);
        w.Do(new Move(new Items.Place(0, pod), new Target(), 1) { From = 1 }).Run();
        w.Do(new PairDrone { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Players.HasDrone(1));
        Assert.False(w.M.Players.HasDrone(0));
        Assert.True(w.M.Items.SlotOpen(1, FirstBag));
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
