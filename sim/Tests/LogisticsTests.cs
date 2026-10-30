using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;
using Structure = Stillwake.Content.Structure;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Drone logistics (0062): with nothing set, drones keep the machines in their bay's zone going: an input holding an
/// item refilled with it below half a stack, a queued station's head entry fed from its inputs given what it needs,
/// what machines make carried to crates; job after job until there is no work or their charge runs low.
/// </summary>
public class LogisticsTests
{
    /// <summary>
    /// Two rooms aired from one tank, a closed door between: the bay (1) and a crate (4) on the left, the furnace (3) and
    /// a crate (7) on the right. Width 24.
    /// </summary>
    internal static readonly string[] Rooms =
    [
        "########################",
        "#.........D............#",
        "#RR....TT.D...FF.......#",
        "#RR.KK.TTVD..VFF..KK...#",
        "########################",
    ];

    internal static readonly string[] RoomsUnder = ["", "", "       o", "       -------"];

    internal const int Bay = 1, Furnace = 3, Left = 4, Right = 7;
    private static readonly int Fuel = Content.Machines.Get("furnace").FuelSlot!.Value;
    private static readonly int Output = Content.Machines.Get("furnace").Inputs!.Value;

    /// <summary>The same two rooms over a solar panel under the hull, cabled to the bay: it charges its drones. Its ids as theirs.</summary>
    private static readonly string[] Powered = [.. Rooms, "                        ", "PPP                     "];

    /// <summary>A drone as an item holding <paramref name="charge"/>, stored.</summary>
    internal static Stack Drone(int charge = Content.Charge.Drone) => new(Tune.Drone, 1, charge);

    /// <summary>
    /// A world of <paramref name="rows"/> run until every room with a vent is aired, each bay given <paramref name="drones"/>
    /// drones holding <paramref name="charge"/>.
    /// </summary>
    internal static Scenario Aired(string[] rows, string[]? under = null, int drones = 2, IReadOnlyList<((int, int), (int, int))>? cables = null, int charge = Content.Charge.Drone)
    {
        var w = new Scenario(AirTesting.Vaporized(rows, under, cables), doubled: true);
        for (int i = 0; i < 2000 && !AllAired(w); i++) w.Run();
        Assert.True(AllAired(w));
        foreach (var bay in w.M.Grid.MachinesOf(Tune.Bay)) Dock(w, bay.Id, drones, charge);
        return w;
    }

    /// <summary>Puts <paramref name="drones"/> drones holding <paramref name="charge"/> in bay <paramref name="bay"/>'s first slots.</summary>
    internal static Scenario Dock(Scenario w, int bay, int drones, int charge = Content.Charge.Drone)
    {
        for (int i = 0; i < drones; i++) w.Put(i, Drone(charge), crate: bay);
        return w;
    }

    /// <summary>Whether every room with a vent in it is aired.</summary>
    private static bool AllAired(Scenario w)
    {
        var vented = w.M.Grid.MachinesOf("vent").Select(v => w.M.Rooms.RoomAt(v.Cell)).ToHashSet();
        return w.M.Rooms.All.Where(r => vented.Contains(r.Id)).All(r => w.M.Atmosphere.StatusOf(r.Id) == Atmosphere.RoomStatus.Aired);
    }

    internal static Scenario TwoRooms(int drones = 2, int charge = Content.Charge.Drone) => Aired(Rooms, RoomsUnder, drones, charge: charge);

    private static Scenario PoweredRooms(int drones, int charge) => Aired(Powered, RoomsUnder, drones, [((0, 6), (1, 2))], charge);

    /// <summary>Puts each of <paramref name="stacks"/> in its own slot of machine <paramref name="crate"/>, from the first, unmerged.</summary>
    internal static Scenario Spread(Scenario w, int crate, params (string Item, int Count)[] stacks)
    {
        for (int i = 0; i < stacks.Length; i++) w.Put(i, new Stack(stacks[i].Item, stacks[i].Count), crate: crate);
        return w;
    }

    internal static int Count(IReadOnlyList<Stack?> slots, string item) => slots.Sum(s => s?.Item == item ? s.Count : 0);
    internal static int Docked(Scenario w, int bay) => Count(w.M.Items.Crate(bay), Tune.Drone);
    internal static int OnFloor(Scenario w, string item) => w.M.Items.Floor.Where(s => s.Item == item).Sum(s => s.Count);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Held(Scenario w, int machine, int slot, string item) => w.M.Items.Crate(machine)[slot] is { } s && s.Item == item ? s.Count : 0;

    /// <summary>The cells of the map's character at (<paramref name="x"/>, <paramref name="y"/>): its block, doubled (0104).</summary>
    internal static int[] Block(Scenario w, int x, int y) =>
        [w.At(x, y), w.At(x, y) + 1, w.At(x, y) + w.Layout.Width, w.At(x, y) + w.Layout.Width + 1];

    /// <summary>The map's column <paramref name="cell"/> lies in, a character a block.</summary>
    private static int Column(Scenario w, int cell) => cell % w.Layout.Width / w.Scale;

    /// <summary>Runs until <paramref name="done"/> holds, at most <paramref name="ticks"/>; whether it did.</summary>
    private static bool RunUntil(Scenario w, Func<bool> done, int ticks = 3000)
    {
        for (int i = 0; i < ticks && !done(); i++) w.Run();
        return done();
    }

    // What is wanted.

    /// <summary>A stack of scrap chunks: what a furnace's input is kept at.</summary>
    private static readonly int Chunks = Content.Items.Get("scrapChunk").Stack;

    /// <summary>A stack of planks, and what a fuel slot holding ten wants of it: the rest of the stack.</summary>
    private static readonly int Planks = Content.Items.Get("planks").Stack, PlanksWanted = Planks - 10;

    /// <summary>A third of the planks wanted and a few more: three slots of it hold a little over what is wanted.</summary>
    private static readonly int PlanksThird = PlanksWanted / 3 + 2, PlanksOver = 3 * PlanksThird - PlanksWanted;

    [Fact]
    public void AnInputHoldingChunksIsRefilledToAStackFromACrateOnceUnderHalf()
    {
        // Half a stack is not under half: nothing flies.
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 2 * Chunks)).Put(0, new Stack("scrapChunk", Chunks / 2), crate: Furnace);
        for (int i = 0; i < 300; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal((Chunks / 2, 2 * Chunks), (Held(w, Furnace, 0, "scrapChunk"), Count(w.M.Items.Crate(Left), "scrapChunk")));

        // One less is: one drone brings what makes a stack, and no fuel, the fuel slot being empty.
        int under = Chunks / 2 - 1;
        var v = TwoRooms().FillCrate(Left, ("scrapChunk", 2 * Chunks), ("planks", 50)).Put(0, new Stack("scrapChunk", under), crate: Furnace);
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.Count > 0, 20));
        Assert.Equal(Chunks - under, v.M.Logistics.Drones.Single().Count);
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.Count == 0));
        Assert.Equal((Chunks, Chunks + under, 0), (Held(v, Furnace, 0, "scrapChunk"), Count(v.M.Items.Crate(Left), "scrapChunk"), v.M.Items.HeldFor(Furnace, "planks")));
        Assert.Equal(2, Docked(v, Bay));
        Assert.Empty(v.Refusals);
    }

    [Fact]
    public void AFuelSlotHoldingPlanksIsRefilledAsAnInputIsAndTheEmptyInputBesideItIsLeftAlone()
    {
        var w = TwoRooms().FillCrate(Left, ("planks", 50), ("scrapChunk", 10)).Put(Fuel, new Stack("planks", 10), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Items.HeldFor(Furnace, "planks") >= 49 && w.M.Logistics.Drones.Count == 0, 600));
        // Lit by its fuel, it burns a plank as it is brought: but never a chunk, its input empty.
        Assert.Null(w.M.Items.Crate(Furnace)[0]);
        Assert.Equal(10, Count(w.M.Items.Crate(Left), "scrapChunk"));
    }

    [Fact]
    public void AMachineWithEveryInputEmptyIsLeftAloneAndTheDronesStayDocked()
    {
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 10), ("planks", 10)).FillCrate(Right, ("silicon", 50));
        for (int i = 0; i < 300; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal(2, Docked(w, Bay));
        Assert.All(w.M.Items.Crate(Furnace), Assert.Null);
    }

    [Fact]
    public void TwoDronesNeverBringMoreThanMakesAStack()
    {
        // A stack in each crate, split over two slots: the first drone brings all that is short, the second nothing.
        var w = TwoRooms().Put(0, new Stack("scrapChunk", 1), crate: Furnace);
        Spread(w, Left, ("scrapChunk", Chunks / 2), ("scrapChunk", Chunks / 2));
        Spread(w, Right, ("scrapChunk", Chunks / 2), ("scrapChunk", Chunks / 2));
        for (int i = 0; i < 600; i++)
        {
            w.Run();
            int held = w.M.Items.HeldFor(Furnace, "scrapChunk");
            int coming = w.M.Logistics.Drones.Where(d => d.Target == Furnace && d.Leg != Leg.Home).Sum(d => d.Cargo?.Count ?? d.Count);
            Assert.True(held + coming <= Chunks, $"tick {i}: {held} held and {coming} coming");
        }
        Assert.Equal(Chunks, w.M.Items.HeldFor(Furnace, "scrapChunk"));
        Assert.Equal(Chunks + 1, Count(w.M.Items.Crate(Left), "scrapChunk") + Count(w.M.Items.Crate(Right), "scrapChunk"));
        Assert.Equal(0, OnFloor(w, "scrapChunk"));
    }

    [Fact]
    public void AStackIsCarriedWholeTheRoomAtTheTargetCappingIt()
    {
        // Planks under half a stack: a drone carries what makes it one, more than any other carry.
        var w = TwoRooms(drones: 1).FillCrate(Left, ("planks", Planks)).Put(Fuel, new Stack("planks", 10), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        Assert.Equal(PlanksWanted, w.M.Logistics.Drones.Single().Count);
    }

    // One trip takes from every slot of its item.

    [Fact]
    public void OneTripTakesFromEverySlotOfTheItemInTheCrateAndFillsTheFuelToAStack()
    {
        // A third of what is wanted and a few more in each of three slots: one drone brings it all, the last slots first.
        var w = Spread(TwoRooms(drones: 1), Left, ("planks", PlanksThird), ("planks", PlanksThird), ("planks", PlanksThird)).Put(Fuel, new Stack("planks", 10), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        Assert.Equal(PlanksWanted, w.M.Logistics.Drones.Single().Count);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 600));
        Assert.Equal(Planks, w.M.Items.HeldFor(Furnace, "planks"));
        Assert.Equal([PlanksOver, 0, 0], w.M.Items.Crate(Left).Take(3).Select(s => s?.Count ?? 0));
        Assert.Equal(1, Docked(w, Bay));
        Assert.Empty(w.M.Items.Floor);
    }

    /// <summary>Two furnaces in one room with the bay and a crate, aired by a tank and a vent. Width 22.</summary>
    private static readonly string[] TwoFurnaces =
    [
        "######################",
        "#RR....FF...FF.......#",
        "#RR....FF...FF.TT....#",
        "#.KK...........TTV...#",
        "######################",
    ];

    private static readonly string[] TwoFurnacesUnder = ["", "", "               o", "               ---"];

    [Fact]
    public void TwoDronesShareTheItemsOfOneCrateByCountNeverTakingTwiceNorOverfilling()
    {
        var w = Aired(TwoFurnaces, TwoFurnacesUnder);
        int crate = w.M.Grid.MachinesOf("crate").Single().Id;
        var furnaces = w.M.Grid.MachinesOf("furnace").Select(f => f.Id).ToArray();
        // Fifteen planks more than one furnace wants, over three slots: the first gets all it wants, the other the fifteen left.
        Spread(w, crate, ("planks", PlanksThird), ("planks", PlanksThird), ("planks", PlanksWanted + 15 - 2 * PlanksThird));
        foreach (int f in furnaces) w.Put(Fuel, new Stack("planks", 10), crate: f);
        var counts = new List<int>();
        for (int i = 0; i < 800; i++)
        {
            w.Run();
            var going = w.M.Logistics.Drones.Where(d => d.Leg == Leg.Source).ToList();
            Assert.True(going.Sum(d => d.Count) <= Count(w.M.Items.Crate(crate), "planks"), $"tick {i}: more spoken for than the crate holds");
            foreach (var d in going)
                if (!counts.Contains(d.Id * 1000 + d.Count)) counts.Add(d.Id * 1000 + d.Count);
            foreach (int f in furnaces)
            {
                int coming = w.M.Logistics.Drones.Where(d => d.Target == f && d.Leg != Leg.Home).Sum(d => d.Cargo?.Count ?? d.Count);
                Assert.True(w.M.Items.HeldFor(f, "planks") + coming <= Planks, $"tick {i}: furnace {f} overfilled");
            }
        }
        Assert.Equal([PlanksWanted, 15], counts.Select(c => c % 1000));
        Assert.Equal((Planks, 25, 0), (w.M.Items.HeldFor(furnaces[0], "planks"), w.M.Items.HeldFor(furnaces[1], "planks"), Count(w.M.Items.Crate(crate), "planks")));
        Assert.Empty(w.M.Items.Floor);
        Assert.Equal(2, Docked(w, w.M.Grid.MachinesOf(Tune.Bay).Single().Id));
    }

    [Fact]
    public void ACrateSortedOrPartlyEmptiedMidFlightGivesTheDroneWhatItHoldsOfItsItemThen()
    {
        // Sorted while the drone flies: its planks merged into one slot among others, it still takes what it came for.
        var w = Spread(TwoRooms(drones: 1), Left, ("planks", PlanksThird), ("iron", 5), ("planks", PlanksThird), ("scrapChunk", 3), ("planks", PlanksThird))
            .Put(Fuel, new Stack("planks", 10), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        w.Do(new Sort(Left)).Run();
        Assert.Equal(3 * PlanksThird, w.M.Items.Crate(Left).Single(s => s?.Item == "planks")!.Count);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 600));
        Assert.Equal((Planks, PlanksOver, 5, 3), (w.M.Items.HeldFor(Furnace, "planks"), Count(w.M.Items.Crate(Left), "planks"), Count(w.M.Items.Crate(Left), "iron"), Count(w.M.Items.Crate(Left), "scrapChunk")));

        // A hand takes one of the three slots: the drone brings the two left, and the furnace, not short, gets no more.
        var v = Spread(TwoRooms(drones: 1), Left, ("planks", PlanksThird), ("planks", PlanksThird), ("planks", PlanksThird)).Put(Fuel, new Stack("planks", 10), crate: Furnace);
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.Count > 0, 20));
        v.Do(new Move(new Slot(1, Left), new Target(), PlanksThird)).Run();
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.Count == 0, 600));
        v.Run(200);
        Assert.Equal((10 + 2 * PlanksThird, 0, PlanksThird), (v.M.Items.HeldFor(Furnace, "planks"), Count(v.M.Items.Crate(Left), "planks"), Count(v.M.Items.Inventory(0), "planks")));
        Assert.Empty(v.M.Items.Floor);
    }

    /// <summary>The main rooms with the furnace a Former, powered by a panel under the stars, and two crates on the right.</summary>
    private static readonly string[] Pressing =
    [
        "PPP                     ",
        "                        ",
        "########################",
        "#.........D............#",
        "#RR....TT.D...MM.......#",
        "#RR.KK.TTVD..VMM..KK...#",
        "########################",
    ];

    private static readonly string[] PressingUnder = ["", "", "", "", "       o", "       -------"];

    private static Scenario Former(out int former, out int left, out int right)
    {
        var w = Aired(Pressing, PressingUnder, cables: [((0, 0), (14, 4))]);
        former = w.M.Grid.MachinesOf("former").Single().Id;
        var crates = w.M.Grid.MachinesOf("crate");
        (left, right) = (crates[0].Id, crates[1].Id);
        return w;
    }

    [Fact]
    public void AFormersEndlessHeadEntryFedFromItsInputsGetsAStackOfIronAndItsPlatesGoToACrate()
    {
        var w = Former(out int former, out int left, out int right).FillCrate(left, ("iron", 100));
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Run();
        w.Do(new SetEndless(former, 1, true)).Run();
        Assert.True(RunUntil(w, () => Count(w.M.Items.Crate(right), "plate") >= 5, 2000));
        // The whole hundred in one carry, short of the stack it wants; its plates to the crate nearest it, empty.
        Assert.Equal(0, Count(w.M.Items.Crate(left), "iron"));
        int plates = Count(w.M.Items.Crate(former), "plate") + Count(w.M.Items.Crate(right), "plate") + w.M.Logistics.Drones.Sum(d => d.Cargo?.Item == "plate" ? d.Cargo.Count : 0);
        int begun = w.M.Crafting.Progress(former) > 0 ? 2 : 0;
        Assert.Equal(100, 2 * plates + begun + w.M.Items.HeldFor(former, "iron") + w.M.Logistics.Drones.Sum(d => d.Cargo?.Item == "iron" ? d.Cargo.Count : 0));
    }

    [Fact]
    public void AHeadEntryOfThreeFromTheInputsWantsTheirCostAndAPaidOneNothing()
    {
        var w = Former(out int former, out int left, out _).FillCrate(left, ("iron", 100)).GiveItems(0, ("iron", 10));
        // A paid head: nothing is brought, though one fed from the inputs waits behind it.
        w.Do(new Craft(former, "gear", 2)).Do(new Craft(former, "plate", 3, FromInputs: true)).Run(30);
        Assert.Equal(100, Count(w.M.Items.Crate(left), "iron"));
        Assert.Empty(w.M.Logistics.Drones);
        // Once it is done the plates head the queue: six iron for three plates, no more.
        Assert.True(RunUntil(w, () => w.M.Crafting.Queue(former).Count == 0, 2000));
        Assert.Equal(94, Count(w.M.Items.Crate(left), "iron"));
        Assert.Equal(0, w.M.Items.HeldFor(former, "iron"));
    }

    /// <summary>A grow capsule (2) beside the bay (1) and a crate (3).</summary>
    private static readonly string[] Farm =
    [
        "##################",
        "#RR...gg.........#",
        "#RR...gg..TT.....#",
        "#.KK..gg..TTV....#",
        "##################",
    ];

    private static readonly string[] FarmUnder = ["", "", "          o", "          ---"];

    [Fact]
    public void AGrowCapsuleHoldingSeedsHasThemToppedUp()
    {
        var w = Aired(Farm, FarmUnder);
        int capsule = w.M.Grid.MachinesOf("growCapsule").Single().Id, crate = w.M.Grid.MachinesOf("crate").Single().Id;
        int seeds = Content.Items.Get("seed").Stack;
        w.FillCrate(crate, ("seed", seeds), ("soil", 20)).Put(0, new Stack("seed", 4), crate: capsule);
        Assert.True(RunUntil(w, () => w.M.Items.HeldFor(capsule, "seed") == seeds && w.M.Logistics.Drones.Count == 0, 600));
        Assert.Equal((4, 20), (Count(w.M.Items.Crate(crate), "seed"), Count(w.M.Items.Crate(crate), "soil")));
    }

    // Where what is made goes.

    [Fact]
    public void WhatAMachineMakesGoesToACrateHoldingItFirstElseToTheNearest()
    {
        // The right crate is nearer the furnace, but the left one holds scrap already.
        var w = TwoRooms().FillCrate(Left, ("scrap", 1)).Put(Output, new Stack("scrap", 8), crate: Furnace);
        Assert.True(RunUntil(w, () => Count(w.M.Items.Crate(Left), "scrap") == 9 && w.M.Logistics.Drones.Count == 0, 600));
        Assert.Equal(0, Count(w.M.Items.Crate(Right), "scrap"));

        var v = TwoRooms().Put(Output, new Stack("scrap", 8), crate: Furnace);
        Assert.True(RunUntil(v, () => Count(v.M.Items.Crate(Right), "scrap") == 8 && v.M.Logistics.Drones.Count == 0, 600));
    }

    [Fact]
    public void AFurnacesChunksAreSmeltedAndItsScrapCarriedOffAsItComesItsDronesChargingBetween()
    {
        var w = PoweredRooms(drones: 2, charge: Tune.LaunchAt).FillCrate(Left, ("scrapChunk", 10), ("scrapChunk", 10), ("planks", 50))
            .Put(0, new Stack("scrapChunk", 1), crate: Furnace).Put(Fuel, new Stack("planks", 30), crate: Furnace);
        Assert.True(RunUntil(w, () => Count(w.M.Items.Crate(Left), "scrapChunk") == 0, 4000));
        Assert.True(RunUntil(w, () => Count(w.M.Items.Crate(Right), "scrap") + Count(w.M.Items.Crate(Left), "scrap") == 84, 4000));
        Assert.Empty(w.M.Items.Floor);
        Assert.Empty(w.Refusals);
    }

    // The switch.

    [Fact]
    public void AMachineSwitchedOffIsNeitherFedNorEmptiedNorTakenFromAndOneSwitchedOnAgainIs()
    {
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", Chunks - 1)).Put(0, new Stack("scrapChunk", 1), crate: Furnace).Put(Output, new Stack("scrap", 8), crate: Furnace);
        w.Do(new SetServed(Furnace, false));
        for (int i = 0; i < 300; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.False(w.M.Logistics.Served(Furnace));
        Assert.Equal((1, 8), (Held(w, Furnace, 0, "scrapChunk"), Held(w, Furnace, Output, "scrap")));

        // The furnace on, the crate holding its chunks off: its scrap goes to the other crate, and no chunk comes.
        w.Do(new SetServed(Furnace, true)).Do(new SetServed(Left, false)).Run();
        Assert.True(RunUntil(w, () => Count(w.M.Items.Crate(Right), "scrap") == 8 && w.M.Logistics.Drones.Count == 0, 600));
        w.Run(200);
        Assert.Equal((1, Chunks - 1), (Held(w, Furnace, 0, "scrapChunk"), Count(w.M.Items.Crate(Left), "scrapChunk")));

        // The crate on again: its chunks are brought.
        w.Do(new SetServed(Left, true)).Run();
        Assert.True(RunUntil(w, () => Held(w, Furnace, 0, "scrapChunk") == Chunks, 600));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void TheSwitchIsRefusedForWhatIsNoMachineAndForABay()
    {
        var w = TwoRooms();
        w.Do(new SetServed(99, false)).Do(new SetServed(Bay, false)).Do(new SetServed(Furnace, false)).Do(new SetServed(Furnace, false)).Run();
        Assert.Equal(["logistics.noMachine", "logistics.bay"], Reasons(w));
        Assert.False(w.M.Logistics.Served(Furnace));
        Assert.True(w.M.Logistics.Served(Bay));
        w.Do(new SetServed(Furnace, true)).Do(new SetServed(Furnace, true)).Run();
        Assert.True(w.M.Logistics.Served(Furnace));
        Assert.Equal(2, w.Refusals.Count);
    }

    [Fact]
    public void DronesWorkCratesFedQueuedAndTradeStationsButNotBaysOrChargers()
    {
        var w = new Scenario(Layout.Parse(EveryMachine), doubled: true);
        string[] servable = ["crate", "furnace", "former", "smelter", "assembler", "growCapsule", "burnerGenerator", "lifepod", "tradeStation"];
        foreach (var m in w.M.Grid.Machines.Where(m => Content.Machines.Get(m.Kind).Slots != null))
            Assert.True(servable.Contains(m.Kind) == w.M.Logistics.Servable(m.Id), m.Kind);
    }

    /// <summary>Every kind of machine holding items once.</summary>
    private static readonly string[] EveryMachine =
    [
        "####################################",
        "#...................gg.........OOO.#",
        "#.FF.MM.EE.YY.$$.BB.gg.C....RR.OOO.#",
        "#.FF.MM.EE.YY.$$.BB.gg.C.KK.RR.OOO.#",
        "####################################",
    ];

    // Job after job.

    [Fact]
    public void ADroneTakesItsNextJobFromWhereItIsWithoutDockingAndFliesHomeWithNoneLeft()
    {
        // A chunk short at the furnace, and scrap in its outputs: one drone brings the chunks, then carries the scrap off.
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", Chunks - 2)).Put(0, new Stack("scrapChunk", 2), crate: Furnace).Put(Output, new Stack("scrap", 8), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        var jobs = new List<(int Target, string Item)>();
        var launched = new HashSet<int>();
        Assert.True(RunUntil(w, () =>
        {
            if (w.M.Logistics.Drones.SingleOrDefault() is { } d) launched.Add(d.Id);
            if (w.M.Logistics.Drones.SingleOrDefault() is { Leg: not Leg.Home } e && (jobs.Count == 0 || jobs[^1] != (e.Target, e.Item))) jobs.Add((e.Target, e.Item));
            return w.M.Logistics.Drones.Count == 0 && jobs.Count == 2;
        }, 800));
        Assert.Equal([(Furnace, "scrapChunk"), (Right, "scrap")], jobs);
        // Launched once: a drone gets a new id each launch.
        Assert.Single(launched);
        Assert.Equal((Chunks, 8, 1), (Held(w, Furnace, 0, "scrapChunk"), Count(w.M.Items.Crate(Right), "scrap"), Docked(w, Bay)));
    }

    /// <summary>What a drone spends over <paramref name="drawn"/> characters of the map, two cells each (0104), stored: at a power-tick a tick.</summary>
    private static int Cells(int drawn) => (2 * drawn * 1000 + Tune.Speed - 1) / Tune.Speed * Tune.Draw * Content.Charge.Scale;

    /// <summary>
    /// A drone with the furnace's chunks on board, left charge for the way there and home and a little: the furnace's
    /// scrap waits, its job and the way home after it more than the drone holds.
    /// </summary>
    private static Scenario Spent(Scenario w)
    {
        w.FillCrate(Left, ("scrapChunk", Chunks - 2)).Put(0, new Stack("scrapChunk", 2), crate: Furnace).Put(Output, new Stack("scrap", 8), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 200));
        var d = w.M.Logistics.Drones.Single();
        d.Stack = d.Stack with { Charge = Cells(31) };
        return w;
    }

    [Fact]
    public void ADroneThatCannotCoverItsNextJobAndTheWayHomeFliesHomeLowAndOneShortOfLaunchAtNeverLaunches()
    {
        var w = Spent(TwoRooms(drones: 1, charge: Tune.LaunchAt));
        bool low = false;
        Assert.True(RunUntil(w, () =>
        {
            low |= w.M.Logistics.Drones.Any(LogisticsModule.Low);
            return w.M.Logistics.Drones.Count == 0;
        }, 800));
        Assert.True(low);
        Assert.Equal((Chunks, 8, 1), (Held(w, Furnace, 0, "scrapChunk"), Held(w, Furnace, Output, "scrap"), Docked(w, Bay)));
        Assert.InRange(w.M.Items.Crate(Bay)[0]!.Charge, 1, Tune.LaunchAt - 1);
        // Short of what it launches on, and no power to charge it: it stays docked, though there is work.
        w.Run(400);
        Assert.Empty(w.M.Logistics.Drones);
        Assert.Equal(8, Held(w, Furnace, Output, "scrap"));
    }

    [Fact]
    public void ADockedDroneShortOfLaunchAtStaysDockedThoughItCouldCoverTheJobAndAtLaunchAtFlies()
    {
        var w = TwoRooms(drones: 1, charge: Tune.LaunchAt - 1).FillCrate(Left, ("scrapChunk", 10)).Put(0, new Stack("scrapChunk", 2), crate: Furnace);
        for (int i = 0; i < 300; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        w.Put(0, Drone(Tune.LaunchAt), crate: Bay);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, Tune.LookEvery));
    }

    [Fact]
    public void ADroneHomeLowRechargesInAPoweredBayAndFliesAgainOnceAtLaunchAt()
    {
        var w = Spent(PoweredRooms(drones: 1, charge: Tune.LaunchAt));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 800));
        int home = w.M.Items.Crate(Bay)[0]!.Charge;
        // Docked, the bay fills it by all it draws, the only drone short of full.
        w.Run(10);
        Assert.Equal(home + 10 * 4 * Content.Charge.Scale, w.M.Items.Crate(Bay)[0]!.Charge);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 2000));
        Assert.InRange(w.M.Logistics.Drones.Single().Stack.Charge, Tune.LaunchAt - Content.Charge.Scale, Tune.LaunchAt + Tune.LookEvery * 4 * Content.Charge.Scale);
        Assert.True(RunUntil(w, () => Held(w, Furnace, Output, "scrap") == 0 && w.M.Logistics.Drones.Count == 0, 800));
        Assert.Equal(8, Count(w.M.Items.Crate(Right), "scrap"));
    }

    [Fact]
    public void ABayChargesEveryDockedDroneShortOfFullSharingWhatItDraws()
    {
        var w = PoweredRooms(drones: 3, charge: 0).Put(3, Drone(), crate: Bay);
        w.Run();
        Assert.True(w.M.Power.Charges(Bay));
        var before = w.M.Items.Crate(Bay).Select(s => s!.Charge).ToArray();
        w.Run(10);
        var gained = w.M.Items.Crate(Bay).Select((s, i) => s!.Charge - before[i]).ToArray();
        // Four power-ticks a tick between the three short of full, the rest of the division to the first.
        Assert.Equal([13340, 13330, 13330, 0], gained);
    }

    // What an input remembers.

    /// <summary>
    /// A furnace lit on twenty planks with a chunk in, put there as a save holds it (no move remembered it), and no chunks
    /// anywhere else: it smelts it to nothing.
    /// </summary>
    private static Scenario BurntDown()
    {
        var w = TwoRooms().Put(0, new Stack("scrapChunk", 1), crate: Furnace).Put(Fuel, new Stack("planks", 20), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Items.Crate(Furnace)[0] == null, 600));
        return w;
    }

    [Fact]
    public void AnInputBurntToNothingRemembersItsItemAndIsRefilledOnceACrateHasIt()
    {
        var w = BurntDown();
        Assert.Equal("scrapChunk", w.M.Items.KeptAt(Furnace, 0));
        Assert.Equal("planks", w.M.Items.KeptAt(Furnace, Fuel));
        Assert.False(w.M.Logistics.Supplied(Furnace, "scrapChunk"));
        // Its scrap is carried off; no chunk is anywhere to bring.
        for (int i = 0; i < 200; i++) Assert.DoesNotContain(w.Run().M.Logistics.Drones, d => d.Item == "scrapChunk");
        // Chunks in the crate: drones bring a stack to the empty slot that remembers them.
        w.FillCrate(Left, ("scrapChunk", Chunks));
        Assert.True(w.M.Logistics.Supplied(Furnace, "scrapChunk"));
        Assert.True(RunUntil(w, () => Held(w, Furnace, 0, "scrapChunk") == Chunks, 600));
        Assert.Equal(0, Count(w.M.Items.Crate(Left), "scrapChunk"));
    }

    /// <summary>Automation is what makes credits flow (2026-10-30-trade-and-tech): drones keep a seeded slot stocked, never with what no one buys.</summary>
    [Fact]
    public void DronesKeepATradeStationsSeededSlotStockedAndBringNothingUnpriced()
    {
        string[] rows = [.. Rooms.Select(r => r.Replace("FF", "$$"))];
        var w = Aired(rows, RoomsUnder, 1, charge: Content.Charge.Drone);
        int station = w.M.Grid.MachinesOf("tradeStation")[0].Id;
        Assert.False(w.M.Logistics.Supplied(station, "wire"));
        w.Put(0, new Stack("wire", 1), crate: station);
        w.FillCrate(Left, ("wire", 100));
        Assert.True(RunUntil(w, () => Count(w.M.Items.Crate(Left), "wire") < 100, 900));
        Assert.True(RunUntil(w, () => w.M.Research.Credits > Content.Market.PriceOf("wire"), 900));
        // Held by hand, an unpriced stack stays, and none comes after it.
        w.Put(1, new Stack("arcShard", 1), crate: station);
        w.FillCrate(Right, ("arcShard", 4));
        w.Run(600);
        Assert.Equal(4, Count(w.M.Items.Crate(Right), "arcShard"));
        Assert.Equal(1, Count(w.M.Items.Crate(station), "arcShard"));
    }

    [Fact]
    public void ADeliveryGoesToTheEmptySlotRememberingItAndLeavesAnotherSlotsMemory()
    {
        string[] rows = [.. Rooms.Select(r => r.Replace("FF", "$$"))];
        var w = Aired(rows, RoomsUnder, 1, charge: Content.Charge.Drone);
        int station = w.M.Grid.MachinesOf("tradeStation")[0].Id;
        w.Put(0, new Stack("wire", 1), crate: station);
        w.Put(1, new Stack("gear", 1), crate: station);
        Assert.True(RunUntil(w, () => w.M.Items.Crate(station).All(s => s == null), 900));
        w.FillCrate(Right, ("gear", 1));
        Assert.True(RunUntil(w, () => Count(w.M.Items.Crate(Right), "gear") == 0 && w.M.Items.Crate(station).All(s => s == null), 1800));
        Assert.Equal("wire", w.M.Items.KeptAt(station, 0));
        Assert.Equal("gear", w.M.Items.KeptAt(station, 1));
    }

    [Fact]
    public void AnInputEmptiedByTheWriteThatTakesItsOnlyItemStillRemembersIt()
    {
        // A smelter writes its slots only as a smelt completes: its one quartz gone, nothing wrote it there before.
        string[] rows = [.. Pressing.Select(r => r.Replace("MM", "EE"))];
        var w = Aired(rows, PressingUnder, cables: [((0, 0), (14, 4))]);
        int smelter = w.M.Grid.MachinesOf("smelter").Single().Id;
        w.Put(0, new Stack("quartz", 1), crate: smelter);
        Assert.True(RunUntil(w, () => w.M.Items.Crate(smelter)[0] == null, 600));
        Assert.Equal("quartz", w.M.Items.KeptAt(smelter, 0));
    }

    [Fact]
    public void AHandTakingAllOfAnInputOutMakesItForgetAndDronesLeaveItAloneButPartOfItDoesNot()
    {
        // Taken out whole by a move: forgotten, and the crate's chunks stay where they are. A stack in at first, so no
        // look between the two moves finds it short.
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 10)).Put(0, new Stack("scrapChunk", Chunks), crate: Furnace);
        w.Do(new Move(new Slot(0, Furnace), new Target(), 1)).Run();
        Assert.Equal("scrapChunk", w.M.Items.KeptAt(Furnace, 0));
        w.Do(new Move(new Slot(0, Furnace), new Target(), Chunks - 1)).Run();
        Assert.Null(w.M.Items.KeptAt(Furnace, 0));
        for (int i = 0; i < 300; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal(10, Count(w.M.Items.Crate(Left), "scrapChunk"));

        // Thrown out whole: the same.
        var v = TwoRooms().FillCrate(Left, ("scrapChunk", 10)).Put(0, new Stack("scrapChunk", 4), crate: Furnace);
        v.Do(new Drop(new Slot(0, Furnace), 4, v.At(14, 3), 1)).Run();
        Assert.Null(v.M.Items.KeptAt(Furnace, 0));
        for (int i = 0; i < 300; i++) Assert.Empty(v.Run().M.Logistics.Drones);

        // Burnt down, it is not forgotten: only a hand forgets.
        var u = BurntDown();
        u.Do(new Move(new Slot(Fuel, Furnace), new Target(), 1)).Run();
        Assert.Equal(("scrapChunk", "planks"), (u.M.Items.KeptAt(Furnace, 0), u.M.Items.KeptAt(Furnace, Fuel)));
    }

    [Fact]
    public void WhatAnInputRemembersIsSavedAndLoaded()
    {
        var w = BurntDown();
        var m = new Registry();
        var copy = World.Load(w.World.Save(), [.. m.All, new Builder(m)]);
        Assert.Equal(w.World.Hash(), copy.Hash());
        Assert.Equal("scrapChunk", m.Items.KeptAt(Furnace, 0));
        // Loaded, it is refilled as before.
        var items = (ItemsState)m.Items.Slice;
        Stacks.SetCrate(items, Left, [new Stack("scrapChunk", 10), .. Enumerable.Repeat<Stack?>(null, 26)]);
        for (int i = 0; i < 600 && m.Items.Crate(Left).Any(s => s != null); i++) copy.Step([]);
        Assert.All(m.Items.Crate(Left), Assert.Null);
    }

    // Flying.

    [Fact]
    public void ADronePassesAClosedDoorAndTheDoorStaysClosed()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", Chunks - 3)).Put(0, new Stack("scrapChunk", 3), crate: Furnace);
        bool through = false;
        for (int i = 0; i < 400; i++)
        {
            w.Run();
            Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(w.At(10, 2)));
            through |= w.M.Logistics.Drones.Any(d => d.Cell % w.Layout.Width == w.At(10, 2) % w.Layout.Width);
        }
        Assert.True(through);
        Assert.Equal(Chunks, w.M.Items.HeldFor(Furnace, "scrapChunk"));
    }

    /// <summary>
    /// Two rooms one over the other, a hatch in the deck between: the furnace above on the right, the bay and a crate below.
    /// Aired with the hatch open, then shut, the crate holding all but the three chunks the furnace holds.
    /// </summary>
    private static readonly string[] Decks =
    [
        "##############",
        "#............#",
        "#..........FF#",
        "#..........FF#",
        "######o#######",
        "#............#",
        "#RR....TT....#",
        "#RR.KK.TTV...#",
        "##############",
    ];

    private static (Scenario W, int Hatch, int Furnace) Decked()
    {
        var w = Aired(Decks, ["", "", "", "", "", "", "       o", "       ---"], drones: 1);
        int hatch = w.At(6, 4), furnace = w.M.Grid.MachinesOf("furnace").Single().Id, crate = w.M.Grid.MachinesOf("crate").Single().Id;
        w.Do(new SetDoor(hatch, false)).Run();
        w.FillCrate(crate, ("scrapChunk", Chunks - 3)).Put(0, new Stack("scrapChunk", 3), crate: furnace);
        return (w, hatch, furnace);
    }

    [Fact]
    public void ADronePassesAClosedHatchAndTheHatchStaysClosed()
    {
        var (w, hatch, furnace) = Decked();
        bool through = false;
        for (int i = 0; i < 400; i++)
        {
            w.Run();
            Assert.Equal(Structure.HatchClosed, w.M.Grid.StructureAt(hatch));
            through |= w.M.Logistics.Drones.Any(d => d.Cell == hatch || d.Cell == hatch + 1);
        }
        Assert.True(through);
        Assert.Equal(Chunks, w.M.Items.HeldFor(furnace, "scrapChunk"));
    }

    [Fact]
    public void OfWaysAsShortADroneTakesTheOneKeepingOffTheCeilingAndClimbsAtTheHatch()
    {
        var (w, hatch, _) = Decked();
        int width = w.M.Grid.Width, under = w.At(1, 5) / width;
        for (int i = 0; i < 400; i++)
        {
            w.Run();
            // The lower room's two rows under its ceiling: only in the hatch's columns, rising through them.
            foreach (var d in w.M.Logistics.Drones)
                if (d.Cell / width is int row && row >= under && row < under + 2)
                    Assert.InRange(d.Cell % width, hatch % width, hatch % width + 1);
        }
        Assert.Equal(Chunks, w.M.Items.HeldFor(w.M.Grid.MachinesOf("furnace").Single().Id, "scrapChunk"));
    }

    /// <summary>Wide: a bay on the left with a crate (2), a furnace (3) in its zone and a furnace (4) beyond it, and a second bay (5) on the right,
    /// aired by two vaporizers: one cannot keep up with its leak (liquid-oxygen).</summary>
    private static readonly string[] Wide =
    [
        "##############################################",
        "#............................................#",
        "#RR....TT......FF.....FF.......RR........TT..#",
        "#RR.KK.TTV.....FF.....FF.......RR........TTV.#",
        "##############################################",
    ];

    private static readonly string[] WideUnder = ["", "", "       o                                 o", "       ---                               ---"];

    [Fact]
    public void AMachineOutsideTheZoneIsIgnoredAndOneInsideTwoZonesGetsOneDelivery()
    {
        var w = Aired(Wide, WideUnder, drones: 0);
        int left = w.M.Grid.MachinesOf(Tune.Bay)[0].Id, right = w.M.Grid.MachinesOf(Tune.Bay)[1].Id;
        int crate = w.M.Grid.MachinesOf("crate").Single().Id;
        var furnaces = w.M.Grid.MachinesOf("furnace");
        int near = furnaces[0].Id, far = furnaces[1].Id;
        Assert.Null(w.M.Logistics.ZoneOf(crate));
        // Its reach past the bay's right column, drawn at 2 and doubled.
        Assert.True(w.M.Logistics.ZoneOf(left) is { Right: 2 * 2 + 1 + Tune.Reach });
        Dock(w, left, 2).FillCrate(crate, ("scrapChunk", 2 * Chunks)).Put(0, new Stack("scrapChunk", 2), crate: far);
        // The far furnace is past the left bay's reach, and the right bay has no drones: nothing reaches it.
        for (int i = 0; i < 400; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal(2, w.M.Items.HeldFor(far, "scrapChunk"));

        // The near one is in both zones, both bays stocked: one delivery of what it lacks.
        Dock(w, right, 2).Put(0, new Stack("scrapChunk", 2), crate: near);
        w.Run(600);
        Assert.Equal(Chunks, w.M.Items.HeldFor(near, "scrapChunk"));
        Assert.Equal(Chunks + 2, Count(w.M.Items.Crate(crate), "scrapChunk"));
        Assert.Empty(w.M.Items.Floor);
        Assert.Equal((2, 2), (Docked(w, left), Docked(w, right)));
    }

    /// <summary>The furnace beyond an airlock with no vent: its vacuum stands between the bay and the furnace.</summary>
    private static readonly string[] Airlock =
    [
        "#########################",
        "#.........D...D.........#",
        "#RR....TT.D...D.FF......#",
        "#RR.KK.TTVD...D.FF.V....#",
        "#########################",
    ];

    private static readonly string[] AirlockUnder = ["", "", "       o", "       -------------"];

    [Fact]
    public void AnAirlessAirlockBetweenTheBayAndAMachineLeavesItUnreached()
    {
        var w = Aired(Airlock, AirlockUnder);
        int crate = w.M.Grid.MachinesOf("crate").Single().Id, furnace = w.M.Grid.MachinesOf("furnace").Single().Id;
        Assert.False(w.M.Atmosphere.AiredAt(w.At(12, 2)));
        w.FillCrate(crate, ("scrapChunk", 10)).Put(0, new Stack("scrapChunk", 2), crate: furnace);
        for (int i = 0; i < 300; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal(2, Docked(w, w.M.Grid.MachinesOf(Tune.Bay).Single().Id));
        Assert.False(w.M.Logistics.Reached(furnace));
        Assert.True(w.M.Logistics.Reached(crate));
    }

    [Fact]
    public void ARoomVentedMidFlightMakesTheDroneDropWithItsCargo()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6)).Put(0, new Stack("scrapChunk", 1), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        // Taken from the crate, still in the left room: its roof torn open.
        Assert.True(Column(w, w.M.Logistics.Drones.Single().Cell) < 10);
        foreach (int cell in Block(w, 3, 0)) w.Do(new ClearAt(cell, "structure"));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 100));
        Assert.Equal((1, 6), (OnFloor(w, Tune.Drone), OnFloor(w, "scrapChunk")));
        Assert.Equal((0, 1), (Docked(w, Bay), w.M.Items.HeldFor(Furnace, "scrapChunk")));
    }

    [Fact]
    public void ASourceEmptiedByHandSendsTheDroneHomeWithNothingWhenThereIsNoOtherWork()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6)).Put(0, new Stack("scrapChunk", 1), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        // A hero takes the stack the drone is flying to.
        w.Do(new Move(new Slot(0, Left), new Target(), 6)).Run();
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 200));
        Assert.Equal(1, Docked(w, Bay));
        Assert.Equal(1, w.M.Items.HeldFor(Furnace, "scrapChunk"));
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void ATargetFilledByHandGetsWhatFitsTheRestGoingOnToACrateWithRoomOrWithNoneOnTheFloorThere()
    {
        // Seven put in by hand while the rest of a stack is on its way: all but seven fit, and the seven go back on to the
        // crate still holding chunks, though the other is nearer.
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", Chunks)).Put(0, new Stack("scrapChunk", 2), crate: Furnace).GiveItems(0, ("scrapChunk", 7));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        w.Do(new Move(new Slot(0), new Target(0, Furnace), 7)).Run();
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 400));
        Assert.Equal((Chunks, 9, 0), (w.M.Items.HeldFor(Furnace, "scrapChunk"), Count(w.M.Items.Crate(Left), "scrapChunk"), Count(w.M.Items.Crate(Right), "scrapChunk")));
        Assert.Empty(w.M.Items.Floor);
        Assert.Equal(1, Docked(w, Bay));

        // Every crate switched off: no crate takes them, so they lie at the target.
        var v = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", Chunks)).Put(0, new Stack("scrapChunk", 2), crate: Furnace).GiveItems(0, ("scrapChunk", 7));
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        v.Do(new Move(new Slot(0), new Target(0, Furnace), 7)).Do(new SetServed(Left, false)).Do(new SetServed(Right, false)).Run();
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.Count == 0, 400));
        var left = v.M.Items.Floor.Single(s => s.Item == "scrapChunk");
        Assert.Equal(7, left.Count);
        Assert.True(Column(v, left.Cell) is 14 or 15, $"lying at {Column(v, left.Cell)}");
    }

    [Fact]
    public void AWallBuiltAcrossThePathMakesTheDroneGoAnotherWayOrHomeWithItsCargoOrDropWithNoWayHome()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6)).Put(0, new Stack("scrapChunk", 4), crate: Furnace);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        // Rows two and three walled off right of the door: the way over them, row one, is still open.
        foreach (int cell in Block(w, 12, 2).Concat(Block(w, 12, 3))) w.Do(new Place(cell, Placement.Wall()));
        w.Run();
        bool over = false;
        Assert.True(RunUntil(w, () =>
        {
            over |= w.M.Logistics.Drones.Any(d => Block(w, 12, 1).Contains(d.Cell));
            return w.M.Logistics.Drones.Count == 0;
        }, 400));
        Assert.True(over);
        Assert.Equal(10, w.M.Items.HeldFor(Furnace, "scrapChunk"));

        // The same, with row one walled too: no way to the furnace, so it flies home and lays its cargo at the bay.
        var v = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6)).Put(0, new Stack("scrapChunk", 4), crate: Furnace);
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        foreach (int y in new[] { 1, 2, 3 })
            foreach (int cell in Block(v, 12, y)) v.Do(new Place(cell, Placement.Wall()));
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.Count == 0, 400));
        Assert.Equal((1, 0, 6, 4), (Docked(v, Bay), OnFloor(v, Tune.Drone), OnFloor(v, "scrapChunk"), v.M.Items.HeldFor(Furnace, "scrapChunk")));
        Assert.All(v.M.Items.Floor, f => Assert.True(Column(v, f.Cell) is 1 or 2, $"lying at {Column(v, f.Cell)}"));

        // Walled off from home, the furnace still in reach: no way home, so it drops where it is, its cargo with it.
        var h = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6)).Put(0, new Stack("scrapChunk", 4), crate: Furnace);
        Assert.True(RunUntil(h, () => h.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        foreach (int y in new[] { 1, 2, 3 })
            foreach (int cell in Block(h, 3, y)) h.Do(new Place(cell, Placement.Wall()));
        Assert.True(RunUntil(h, () => h.M.Logistics.Drones.Count == 0, 400));
        Assert.Equal((0, 1, 6, 4), (Docked(h, Bay), OnFloor(h, Tune.Drone), OnFloor(h, "scrapChunk"), h.M.Items.HeldFor(Furnace, "scrapChunk")));

        // Walled in by the crate, home and the furnace both cut off: no way on, so it drops.
        var u = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6)).Put(0, new Stack("scrapChunk", 4), crate: Furnace);
        Assert.True(RunUntil(u, () => u.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        Assert.InRange(Column(u, u.M.Logistics.Drones.Single().Cell), 4, 5);
        foreach (int x in new[] { 3, 6 })
            foreach (int y in new[] { 1, 2, 3 })
                foreach (int cell in Block(u, x, y)) u.Do(new Place(cell, Placement.Wall()));
        Assert.True(RunUntil(u, () => u.M.Logistics.Drones.Count == 0, 400));
        Assert.Equal((0, 1, 6), (Docked(u, Bay), OnFloor(u, Tune.Drone), OnFloor(u, "scrapChunk")));
    }

    [Fact]
    public void SavedLoadedAndPlayedOnMidFlightItHashesTheSame()
    {
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 10), ("planks", 50)).Put(0, new Stack("scrapChunk", 1), crate: Furnace)
            .Put(Fuel, new Stack("planks", 10), crate: Furnace).Do(new SetServed(Right, false));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Any(d => d.Cargo != null) && w.M.Logistics.Drones.Count == 2
            && w.M.Logistics.Drones.Any(d => d.X % 1000 != 500 || d.Y % 1000 != 500), 400));
        var m = new Registry();
        var copy = World.Load(w.World.Save(), [.. m.All, new Builder(m)]);
        Assert.Equal(w.World.Hash(), copy.Hash());
        for (int i = 0; i < 600; i++)
        {
            w.World.Step([]);
            copy.Step([]);
        }
        Assert.Equal(w.World.Hash(), copy.Hash());
        Assert.False(m.Logistics.Served(Right));
        Assert.True(Count(m.Items.Crate(Left), "scrap") > 0);
    }
}
