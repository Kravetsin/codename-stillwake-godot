using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Drone logistics (0062): bays launch docked drones to bring what requesters lack from providers and storage, and
/// what fed stations make to storage, flying through air and doors and dropping where they stand with no way on.
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
    private static readonly int Door = 10 + 2 * 24;
    private static readonly int Fuel = Content.Machines.Get("furnace").FuelSlot!.Value;

    /// <summary>A world of <paramref name="rows"/> run until every room with a vent is aired, its bay given <paramref name="drones"/> drones.</summary>
    internal static Scenario Aired(string[] rows, string[]? under = null, int drones = 2, IReadOnlyList<((int, int), (int, int))>? cables = null)
    {
        var w = new Scenario(Layout.Parse(rows, under, cables));
        for (int i = 0; i < 2000 && !AllAired(w); i++) w.Run();
        Assert.True(AllAired(w));
        foreach (var bay in w.M.Grid.MachinesOf(Tune.Bay)) w.FillCrate(bay.Id, (Tune.Drone, drones));
        return w;
    }

    /// <summary>Whether every room with a vent in it is aired.</summary>
    private static bool AllAired(Scenario w)
    {
        var vented = w.M.Grid.MachinesOf("vent").Select(v => w.M.Rooms.RoomAt(v.Cell)).ToHashSet();
        return w.M.Rooms.All.Where(r => vented.Contains(r.Id)).All(r => w.M.Atmosphere.StatusOf(r.Id) == Atmosphere.RoomStatus.Aired);
    }

    internal static Scenario TwoRooms(int drones = 2) => Aired(Rooms, RoomsUnder, drones);

    internal static int Count(IReadOnlyList<Stack?> slots, string item) => slots.Sum(s => s?.Item == item ? s.Count : 0);
    internal static int Docked(Scenario w, int bay) => Count(w.M.Items.Crate(bay), Tune.Drone);
    internal static int OnFloor(Scenario w, string item) => w.M.Items.Floor.Where(s => s.Item == item).Sum(s => s.Count);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    private static Scenario Request(Scenario w, int machine, params (string Item, int Count)[] requests) =>
        w.Do(new SetRequests(machine, [.. requests.Select(r => new Request(r.Item, r.Count))])).Run();

    private static Scenario Provide(Scenario w, int crate) => w.Do(new SetRole(crate, Role.Provider)).Run();

    /// <summary>Runs until <paramref name="done"/> holds, at most <paramref name="ticks"/>; whether it did.</summary>
    private static bool RunUntil(Scenario w, Func<bool> done, int ticks = 3000)
    {
        for (int i = 0; i < ticks && !done(); i++) w.Run();
        return done();
    }

    [Fact]
    public void AFurnaceRequestingChunksGetsThemFromAProviderSmeltsThemAndItsScrapIsPushedToStorage()
    {
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 10), ("planks", 10));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 10), ("planks", 5));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        Assert.Equal(1, Docked(w, Bay));
        Assert.True(RunUntil(w, () => Count(w.M.Items.Crate(Right), "scrap") == 40, 4000));
        // The planks it burns are brought again: the request is what it keeps, not what it gets once.
        Assert.Equal(0, Count(w.M.Items.Crate(Left), "scrapChunk"));
        Assert.True(Count(w.M.Items.Crate(Left), "planks") < 5);
        // Every chunk smelted and its scrap carried off; the drones back home.
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0));
        Assert.Equal(0, Count(w.M.Items.Crate(Furnace), "scrapChunk") + Count(w.M.Items.Crate(Furnace), "scrap"));
        Assert.Equal(2, Docked(w, Bay));
        Assert.Empty(w.M.Items.Floor);
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void AMachineWithNoRequestGetsNothingAndTheDronesStayDocked()
    {
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 10), ("planks", 10));
        Provide(w, Left);
        w.Run(300);
        Assert.Equal(2, Docked(w, Bay));
        Assert.Empty(w.M.Logistics.Drones);
        Assert.All(w.M.Items.Crate(Furnace), Assert.Null);
    }

    [Fact]
    public void ProvidersAreTakenFromBeforeStorageEvenWhenStorageIsNearer()
    {
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 4)).FillCrate(Right, ("scrapChunk", 10));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 6));
        Assert.True(RunUntil(w, () => w.M.Items.HeldFor(Furnace, "scrapChunk") + w.M.Items.Crate(Furnace).Skip(2).Count(s => s != null) > 0 && w.M.Logistics.Drones.Count == 0));
        // The provider's four first; storage, nearer, only for the two still short. The furnace holds no fuel, so smelts nothing.
        Assert.Equal((0, 8, 6), (Count(w.M.Items.Crate(Left), "scrapChunk"), Count(w.M.Items.Crate(Right), "scrapChunk"), w.M.Items.HeldFor(Furnace, "scrapChunk")));
    }

    [Fact]
    public void TwoDronesNeverDeliverPastARequest()
    {
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 10), ("scrapChunk", 10));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 7));
        for (int i = 0; i < 600; i++)
        {
            w.Run();
            int held = w.M.Items.HeldFor(Furnace, "scrapChunk");
            int coming = w.M.Logistics.Drones.Where(d => d.Target == Furnace && d.Leg != Leg.Home).Sum(d => d.Cargo?.Count ?? d.Count);
            Assert.True(held + coming <= 7, $"tick {i}: {held} held and {coming} coming");
        }
        Assert.Equal(7, w.M.Items.HeldFor(Furnace, "scrapChunk"));
        Assert.Equal(13, Count(w.M.Items.Crate(Left), "scrapChunk"));
        Assert.Equal(2, Docked(w, Bay));
    }

    [Fact]
    public void ADeliveryIsCappedByTheStackAndByTheRoomAtTheTarget()
    {
        // Sixty planks wanted: a drone carries one stack of fifty, the slot's and the stack's limit.
        var w = TwoRooms(drones: 1).FillCrate(Left, ("planks", 50), ("planks", 50));
        Provide(w, Left);
        Request(w, Furnace, ("planks", 60));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        Assert.Equal(50, w.M.Logistics.Drones.Single().Count);
        // The fuel slot takes fifty: the ten still short have no room, so nothing more flies.
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0));
        w.Run(200);
        Assert.Equal((50, 50), (w.M.Items.HeldFor(Furnace, "planks"), Count(w.M.Items.Crate(Left), "planks")));
        Assert.Empty(w.M.Logistics.Drones);

        // Forty-five there by hand and sixty wanted: only the five that fit are carried.
        var v = TwoRooms(drones: 1).FillCrate(Left, ("planks", 50)).GiveItems(0, ("planks", 45));
        Provide(v, Left);
        v.Do(new Move(new Slot(0), new Target(Fuel, Furnace), 45)).Run();
        Request(v, Furnace, ("planks", 60));
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.Count > 0, 20));
        Assert.Equal(5, v.M.Logistics.Drones.Single().Count);
    }

    /// <summary>Wide: a bay on the left with a provider crate (2), a furnace (3) in its zone and a furnace (4) beyond it, and a second bay (5) on the right.</summary>
    private static readonly string[] Wide =
    [
        "##############################################",
        "#............................................#",
        "#RR....TT......FF.....FF.......RR............#",
        "#RR.KK.TTV.....FF.....FF.......RR............#",
        "##############################################",
    ];

    private static readonly string[] WideUnder = ["", "", "       o", "       ---"];

    [Fact]
    public void AMachineOutsideTheZoneIsIgnoredAndOneInsideTwoZonesGetsOneDelivery()
    {
        var w = Aired(Wide, WideUnder, drones: 0);
        int left = w.M.Grid.MachinesOf(Tune.Bay)[0].Id, right = w.M.Grid.MachinesOf(Tune.Bay)[1].Id;
        int crate = w.M.Grid.MachinesOf("crate").Single().Id;
        var furnaces = w.M.Grid.MachinesOf("furnace");
        int near = furnaces[0].Id, far = furnaces[1].Id;
        Assert.Null(w.M.Logistics.ZoneOf(crate));
        Assert.True(w.M.Logistics.ZoneOf(left) is { Right: 18 });
        w.FillCrate(left, (Tune.Drone, 2)).FillCrate(crate, ("scrapChunk", 10), ("scrapChunk", 10));
        Provide(w, crate);
        // The far furnace is past the left bay's reach, and the right bay has no drones: nothing reaches it.
        Request(w, far, ("scrapChunk", 5));
        w.Run(400);
        Assert.Equal(0, w.M.Items.HeldFor(far, "scrapChunk"));
        Assert.False(w.M.Logistics.InReach(far, "scrapChunk"));
        Assert.Empty(w.M.Logistics.Drones);

        // The near one is in both zones, both bays stocked: one delivery of what it lacks.
        w.FillCrate(right, (Tune.Drone, 2));
        Request(w, near, ("scrapChunk", 5));
        Assert.True(w.M.Logistics.InReach(near, "scrapChunk"));
        w.Run(600);
        Assert.Equal(5, w.M.Items.HeldFor(near, "scrapChunk"));
        Assert.Equal(15, Count(w.M.Items.Crate(crate), "scrapChunk"));
        Assert.Empty(w.M.Items.Floor);
        Assert.Equal((2, 2), (Docked(w, left), Docked(w, right)));
    }

    [Fact]
    public void ADronePassesAClosedDoorAndTheDoorStaysClosed()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 3));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 3));
        bool through = false;
        for (int i = 0; i < 400; i++)
        {
            w.Run();
            Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(Door));
            through |= w.M.Logistics.Drones.Any(d => d.Cell % 24 == 10);
        }
        Assert.True(through);
        Assert.Equal(3, w.M.Items.HeldFor(Furnace, "scrapChunk"));
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
    public void AnAirlessAirlockBetweenTheBayAndAMachineMeansTheJobIsSkipped()
    {
        var w = Aired(Airlock, AirlockUnder);
        int crate = w.M.Grid.MachinesOf("crate").Single().Id, furnace = w.M.Grid.MachinesOf("furnace").Single().Id;
        Assert.False(w.M.Atmosphere.AiredAt(12 + 2 * 25));
        w.FillCrate(crate, ("scrapChunk", 10));
        Provide(w, crate);
        Request(w, furnace, ("scrapChunk", 5));
        w.Run(300);
        Assert.Empty(w.M.Logistics.Drones);
        Assert.Equal(2, Docked(w, w.M.Grid.MachinesOf(Tune.Bay).Single().Id));
        Assert.False(w.M.Logistics.InReach(furnace, "scrapChunk"));
    }

    [Fact]
    public void ARoomVentedMidFlightMakesTheDroneDropWithItsCargo()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 6));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        // Taken from the crate, still in the left room: its roof torn open.
        Assert.True(w.M.Logistics.Drones.Single().Cell % 24 < 10);
        w.Do(new ClearAt(3, "structure"));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 100));
        Assert.Equal((1, 6), (OnFloor(w, Tune.Drone), OnFloor(w, "scrapChunk")));
        Assert.Equal((0, 0), (Docked(w, Bay), w.M.Items.HeldFor(Furnace, "scrapChunk")));
    }

    [Fact]
    public void ASourceEmptiedByHandSendsTheDroneHomeWithNothing()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 6));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        // A hero takes the stack the drone is flying to.
        w.Do(new Move(new Slot(0, Left), new Target(), 6)).Run();
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 200));
        Assert.Equal(1, Docked(w, Bay));
        Assert.Equal(0, w.M.Items.HeldFor(Furnace, "scrapChunk"));
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void ATargetFilledByHandGetsWhatFitsAndTheRestLiesOnTheFloorAtTheTarget()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6)).GiveItems(0, ("scrapChunk", 7));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 6));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        // Seven put in by hand while the six are on their way: three fit.
        w.Do(new Move(new Slot(0), new Target(0, Furnace), 7)).Run();
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count == 0, 400));
        Assert.Equal(10, w.M.Items.HeldFor(Furnace, "scrapChunk"));
        var left = w.M.Items.Floor.Single(s => s.Item == "scrapChunk");
        Assert.Equal(3, left.Count);
        Assert.True(left.Cell % 24 is 14 or 15, $"lying at {left.Cell % 24}");
        Assert.Equal(1, Docked(w, Bay));
    }

    [Fact]
    public void AWallBuiltAcrossThePathMakesTheDroneGoAnotherWayOrDropWithNone()
    {
        var w = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 6));
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        // Rows two and three walled off right of the door: the way over them, row one, is still open.
        w.Do(new Place(12 + 2 * 24, Placement.Wall())).Do(new Place(12 + 3 * 24, Placement.Wall())).Run();
        bool over = false;
        Assert.True(RunUntil(w, () =>
        {
            over |= w.M.Logistics.Drones.Any(d => d.Cell == 12 + 24);
            return w.M.Logistics.Drones.Count == 0;
        }, 400));
        Assert.True(over);
        Assert.Equal(6, w.M.Items.HeldFor(Furnace, "scrapChunk"));

        // The same, with row one walled too: no way at all, so it drops.
        var v = TwoRooms(drones: 1).FillCrate(Left, ("scrapChunk", 6));
        Provide(v, Left);
        Request(v, Furnace, ("scrapChunk", 6));
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.SingleOrDefault()?.Cargo != null, 400));
        foreach (int y in new[] { 1, 2, 3 }) v.Do(new Place(12 + y * 24, Placement.Wall()));
        Assert.True(RunUntil(v, () => v.M.Logistics.Drones.Count == 0, 400));
        Assert.Equal((1, 6, 0), (OnFloor(v, Tune.Drone), OnFloor(v, "scrapChunk"), v.M.Items.HeldFor(Furnace, "scrapChunk")));
    }

    /// <summary>The main rooms with the furnace a Former, powered by a panel under the stars, and storage on the right.</summary>
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

    private static Scenario Former(out int former, out int provider, out int storage)
    {
        var w = Aired(Pressing, PressingUnder, cables: [((0, 0), (14, 4))]);
        former = w.M.Grid.MachinesOf("former").Single().Id;
        var crates = w.M.Grid.MachinesOf("crate");
        (provider, storage) = (crates[0].Id, crates[1].Id);
        return w;
    }

    [Fact]
    public void AFormersEndlessEntryFedFromItsInputsIsFedByItsRequests()
    {
        var w = Former(out int former, out int provider, out _).FillCrate(provider, ("iron", 100));
        Provide(w, provider);
        w.Do(new Craft(former, "plate", 1, FromInputs: true)).Run();
        w.Do(new SetEndless(former, 1, true)).Run();
        Request(w, former, ("iron", 10));
        w.Run(1500);
        int plates = Count(w.M.Items.Crate(former), "plate");
        Assert.True(plates >= 10, $"{plates} plates");
        Assert.True(w.M.Items.HeldFor(former, "iron") <= 10);
        // Every iron is in the crate, the inputs, a drone or a plate, made or begun.
        int begun = w.M.Crafting.Progress(former) > 0 ? 2 : 0;
        Assert.Equal(100 - 2 * plates - begun - w.M.Items.HeldFor(former, "iron") - w.M.Logistics.Drones.Sum(d => d.Cargo?.Count ?? 0),
            Count(w.M.Items.Crate(provider), "iron"));
    }

    [Fact]
    public void AQueuedStationsOutputsAreNeverPushedToStorage()
    {
        var w = Former(out int former, out _, out int storage).GiveItems(0, ("iron", 10));
        w.Do(new Craft(former, "gear", 3)).Run(400);
        Assert.Equal(3, Count(w.M.Items.Crate(former), "gear"));
        w.Run(300);
        Assert.Equal(3, Count(w.M.Items.Crate(former), "gear"));
        Assert.Equal(0, Count(w.M.Items.Crate(storage), "gear"));
        Assert.Empty(w.M.Logistics.Drones);
    }

    [Fact]
    public void AQueuedStationsOutputsStillProvideForARequest()
    {
        var w = Former(out int former, out int provider, out _).GiveItems(0, ("iron", 10));
        w.Do(new SetRole(provider, Role.Requester)).Run();
        w.Do(new Craft(former, "gear", 3)).Run(400);
        Request(w, provider, ("gear", 2));
        w.Run(400);
        Assert.Equal((2, 1), (Count(w.M.Items.Crate(provider), "gear"), Count(w.M.Items.Crate(former), "gear")));
    }

    [Fact]
    public void SetRoleIsRefusedForWhatIsNoCrateAndLeavingRequesterClearsTheRequests()
    {
        var w = TwoRooms();
        w.Do(new SetRole(Furnace, Role.Provider)).Do(new SetRole(99, Role.Requester)).Do(new SetRole(Bay, Role.Provider)).Run();
        Assert.Equal(["logistics.notCrate", "logistics.notCrate", "logistics.notCrate"], Reasons(w));
        Assert.Equal(Role.Storage, w.M.Logistics.RoleOf(Furnace));

        w.Do(new SetRole(Left, Role.Requester)).Run();
        Request(w, Left, ("iron", 5), ("plate", 3));
        Assert.Equal(2, w.M.Logistics.RequestsOf(Left).Count);
        // Requester to requester keeps them; to provider clears them.
        w.Do(new SetRole(Left, Role.Requester)).Run();
        Assert.Equal(2, w.M.Logistics.RequestsOf(Left).Count);
        w.Do(new SetRole(Left, Role.Provider)).Run();
        Assert.Equal(Role.Provider, w.M.Logistics.RoleOf(Left));
        Assert.Empty(w.M.Logistics.RequestsOf(Left));
        w.Do(new SetRole(Left, Role.Storage)).Run();
        Assert.Equal(Role.Storage, w.M.Logistics.RoleOf(Left));
        Assert.Equal(3, w.Refusals.Count);
    }

    /// <summary>Every kind of machine once, aired: which may hold requests and how many.</summary>
    private static readonly string[] EveryMachine =
    [
        "####################################",
        "#...................gg.........OOO.#",
        "#.FF.MM.EE.YY.AA.BB.gg.C....RR.OOO.#",
        "#.FF.MM.EE.YY.AA.BB.gg.C.KK.RR.OOO.#",
        "####################################",
    ];

    [Theory]
    [InlineData("furnace", 2)]
    [InlineData("former", 2)]
    [InlineData("smelter", 1)]
    [InlineData("assembler", 4)]
    [InlineData("growCapsule", 1)]
    [InlineData("burnerGenerator", 1)]
    [InlineData("analyzer", 0)]
    [InlineData("charger", 0)]
    [InlineData("lifepod", 0)]
    [InlineData("droneBay", 0)]
    [InlineData("crate", 0)]
    public void RequestsGoOnMachinesThatTakeItemsInToWorkThemUpToTheirInputs(string kind, int most)
    {
        var w = new Scenario(Layout.Parse(EveryMachine));
        int id = w.M.Grid.MachinesOf(kind).Single().Id;
        Assert.Equal(most, w.M.Logistics.RequestsMost(id));
        var asked = Enumerable.Range(0, most + 1).Select(i => ("iron", i + 1)).ToArray();
        Request(w, id, asked);
        Request(w, id, asked[..Math.Max(1, most)]);
        if (most == 0) Assert.Equal(["logistics.cannotRequest", "logistics.cannotRequest"], Reasons(w));
        else
        {
            Assert.Equal(["logistics.tooMany"], Reasons(w));
            Assert.Equal(most, w.M.Logistics.RequestsOf(id).Count);
        }
    }

    [Fact]
    public void ARequesterCrateHoldsUpToItsMostAndRequestsOnAMachineNoLongerThereAreSkipped()
    {
        var w = TwoRooms().FillCrate(Right, ("iron", 20));
        w.Do(new SetRole(Left, Role.Requester)).Run();
        Request(w, Left, [.. Enumerable.Range(0, Tune.RequestsMost + 1).Select(i => ("iron", i + 1))]);
        Request(w, 99, ("iron", 1));
        Assert.Equal(["logistics.tooMany", "logistics.cannotRequest"], Reasons(w));
        Request(w, Left, ("iron", 5));
        w.Run(300);
        Assert.Equal(5, Count(w.M.Items.Crate(Left), "iron"));
        // Taken apart (emptied first), its requests stay and are skipped: nothing flies.
        w.Do(new Move(new Slot(0, Left), new Target(), 5)).Run();
        w.Do(new ClearAt(w.M.Grid.MachineById(Left)!.Cell, "machine")).Run(300);
        Assert.Empty(w.M.Logistics.Drones);
        Assert.Single(w.M.Logistics.RequestsOf(Left));
        Assert.Equal(15, Count(w.M.Items.Crate(Right), "iron"));
    }

    [Fact]
    public void SavedLoadedAndPlayedOnMidFlightItHashesTheSame()
    {
        var w = TwoRooms().FillCrate(Left, ("scrapChunk", 10), ("planks", 10));
        Provide(w, Left);
        Request(w, Furnace, ("scrapChunk", 10), ("planks", 5));
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
        Assert.True(Count(m.Items.Crate(Right), "scrap") > 0);
    }
}
