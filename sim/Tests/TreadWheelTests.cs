using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Stillwake.Sim.Landing;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The treadmill wheel (the power-chain ADR): a seat that makes power, 4 a tick, anywhere, only while an awake rider
/// runs in it; counted with the panels in <c>Steady</c>, so while it covers its network no fire lights.
/// </summary>
public class TreadWheelTests
{
    /// <summary>A room three characters tall: the wheel on its floor, cabled to a lamp; inside, where no panel would make a thing.</summary>
    private static readonly string[] Room =
    [
        "############",
        "#.........L#",
        "#www.......#",
        "#www.......#",
        "#www.......#",
        "############",
    ];

    /// <summary>
    /// The burner generator's aired room with the wheel on its roof, both cabled to a lamp and to <paramref name="more"/> lamps more,
    /// the network drawing one more than that.
    /// </summary>
    private static Layout Fired(int more) => AirTesting.Vaporized(
        ["    www       ", "    www       ", "    www       ", "##############", "#BB.TT..LLLLL#", "#BB.TTV......#", "##############"],
        ["", "", "", "", "    o", "    ---"],
        [((4, 0), (8, 4)), ((1, 4), (8, 4)), .. Enumerable.Range(9, more).Select(x => ((x, 4), (8, 4)))]);

    private static readonly int Makes = Machines.Get("treadWheel").Makes!.Value;
    private static Scenario World() => new Scenario(Layout.Parse(Room, null, [((1, 2), (10, 1))]), doubled: true);
    private static int Wheel(Scenario w) => w.M.Grid.MachinesOf("treadWheel").Single().Id;
    private static int Supply(Scenario w) => w.M.Power.NetworkOf(w.M.Grid.MachineById(Wheel(w))!.Cell)!.Supply;
    private static int Lamp(Scenario w) => w.M.Grid.MachinesOf("lamp")[0].Id;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>Peer <paramref name="peer"/>'s hero in the room, seated in the wheel.</summary>
    private static Scenario Ridden(Scenario w, int peer = 0) =>
        w.Do(new Enter(w.At(5, 4)) { From = peer }).Run().Do(new Board(Wheel(w)) { From = peer }).Run();

    [Fact]
    public void IsASixBySixDrumOfPlanksGearsACoilAndScrapThatSeatsAHeroAndMakesFourAnywhere()
    {
        var spec = Machines.Get("treadWheel");
        Assert.Equal((6, 6, 4, true), (spec.Width, spec.Height, Makes, spec.Anywhere));
        Assert.Equal(["planks", "gear", "coil", "scrap"], spec.Cost.Select(c => c.Item));
        Assert.Equal(MachineTrait.Seats, spec.Traits);
        Assert.Equal(MenuTabs.Power, spec.Menu!.Tab);
    }

    [Fact]
    public void MakesFourOnlyWhileItsRiderRuns()
    {
        var w = World().Run();
        // Empty, it rests from its first construction.
        Assert.Equal(0, Supply(w));
        Ridden(w);
        Assert.Equal(Wheel(w), w.M.Landing.SeatOf(0));
        // Seated and still: nothing.
        w.Run(5);
        Assert.Equal(0, Supply(w));
        Assert.Equal(0, w.M.Power.Satisfaction(Lamp(w)));
        // Running: from the tick its word arrives.
        w.Do(new Exert(true)).Run();
        Assert.True(w.M.Landing.Turning(Wheel(w)));
        Assert.Equal(Makes, Supply(w));
        Assert.Equal(1000, w.M.Power.Satisfaction(Lamp(w)));
        // Stopping, it rests from the next construction.
        w.Do(new Exert(false)).Run();
        Assert.Equal(0, Supply(w));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void RestsOnceItsRiderLeavesOrPassesOut()
    {
        var w = Ridden(World().Run()).Do(new Exert(true)).Run();
        Assert.Equal(Makes, Supply(w));
        // Off the wheel, still running: a hero beside the wheel turns nothing.
        w.Do(new Alight()).Run();
        Assert.Null(w.M.Landing.SeatOf(0));
        Assert.True(w.M.Crew.HeroOf(0)!.Exerting);
        Assert.Equal(0, Supply(w));
        // Back in and running, then struck out cold: nothing from the next construction, the seat freed.
        w.Do(new Board(Wheel(w))).Run();
        Assert.Equal(Makes, Supply(w));
        ((CrewState)w.M.Crew.Slice).Heroes.Single(h => h.Id == 0).Out = 100;
        w.Run();
        Assert.False(w.M.Landing.Turning(Wheel(w)));
        Assert.Equal(0, Supply(w));
        Assert.Null(w.M.Landing.SeatOf(0));
    }

    /// <summary>A rider who leaves the game frees the seat, and the wheel rests from the next construction.</summary>
    [Fact]
    public void RestsOnceItsRiderLeavesTheGame()
    {
        var w = Ridden(World().Run()).Do(new Exert(true)).Run();
        Assert.Equal(Makes, Supply(w));
        w.Do(new Leave()).Run(2);
        Assert.Null(w.M.Landing.SeatOf(0));
        Assert.Equal(0, Supply(w));
        Assert.Empty(w.Refusals);
    }

    /// <summary>
    /// A wheel built while the world runs, where a cable from the lamp already ends, rests on the tick it stands, as one drawn on
    /// the map does from the first.
    /// </summary>
    [Fact]
    public void ABuiltWheelRestsFromTheTickItStands()
    {
        var w = new Scenario(Layout.Parse(Room, null, [((1, 2), (10, 1)), ((6, 2), (10, 1))]), doubled: true).Run();
        int drawn = Wheel(w);
        w.Do(new Building.SetInstant(true)).Do(new Building.PlacePlan(w.At(6, 2), new Grid.Placement("machine", "treadWheel"))).Run();
        var built = w.M.Grid.MachinesOf("treadWheel").Single(m => m.Id != drawn);
        Assert.Equal(0, w.M.Power.NetworkOf(built.Cell)!.Supply);
        // Cabled: ridden, it makes.
        w.Do(new Enter(w.At(9, 4))).Run().Do(new Board(built.Id)).Do(new Exert(true)).Run();
        Assert.Equal(Makes, w.M.Power.NetworkOf(built.Cell)!.Supply);
        Assert.Empty(w.Refusals);
    }

    /// <summary>Drawn on a map it stands on a back wall, as a fire does and a panel does not, so a room it is drawn in stays shut.</summary>
    [Fact]
    public void IsDrawnOnABackWallSoItsRoomStaysShut()
    {
        var w = World().Run();
        Assert.All(w.M.Rooms.All, r => Assert.Empty(r.Openings));
        Assert.Equal(Stillwake.Sim.Grid.Back.Wall, w.M.Grid.BackLayer[w.M.Grid.MachineById(Wheel(w))!.Cell]);
    }

    /// <summary>A solar panel in the same room makes nothing: the wheel's <c>Anywhere</c> is what lets it work indoors.</summary>
    [Fact]
    public void MakesIndoorsWhereAPanelWouldNot()
    {
        var w = new Scenario(Layout.Parse([.. Room[..1], "#PPP.....L.#", .. Room[2..]], null, [((1, 1), (9, 1))]), doubled: true).Run(2);
        Assert.Equal(0, w.M.Power.NetworkOf(w.At(1, 1))!.Supply);
        Assert.False(Machines.Get("solarPanel").Anywhere);
    }

    /// <summary>
    /// Its 4 counts in <c>Steady</c> as a panel's would: while it covers the network a burner stays cold, a draw of exactly 4
    /// included, past it one lights.
    /// </summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public void CountsInSteadySoABurnerLightsOnlyOnceTheDrawPassesIt(int more, bool heavy)
    {
        var w = new Scenario(Fired(more), doubled: true).GiveItems(0, ("planks", 4));
        int burner = w.M.Grid.MachinesOf("burnerGenerator").Single().Id;
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 4)); i++) w.Run();
        int wheel = Wheel(w);
        w.Do(new Enter(w.At(2, 2))).Run().Do(new Board(wheel)).Do(new Exert(true)).Run(2);
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "planks");
        w.Do(new Move(new Items.Place(from), new Target(Machines.Get("burnerGenerator").FuelSlot!.Value, burner), 2)).Run(3);
        var network = w.M.Power.NetworkOf(w.M.Grid.MachineById(wheel)!.Cell)!;
        Assert.Equal(Makes, network.Steady);
        Assert.Equal(heavy, w.M.Furnace.At(burner).Burning);
        Assert.Equal(heavy, w.M.Power.Needed(burner));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void LaunchesNothingAndSeatsOneRider()
    {
        var w = Ridden(World().Run());
        w.Do(new Launch()).Run();
        Assert.Equal(["landing.notSeated"], Reasons(w));
        Assert.Equal(Wheel(w), w.M.Landing.SeatOf(0));
        w.Do(new Enter(w.At(7, 4)) { From = 1 }).Run().Do(new Board(Wheel(w)) { From = 1 }).Run();
        Assert.Equal(["landing.notSeated", "landing.taken"], Reasons(w));
    }

    /// <summary>Two worlds fed the same hash alike, and one saved and loaded mid-ride plays on as the other.</summary>
    [Fact]
    public void PlaysOnAlikeSavedAndLoadedMidRide()
    {
        static Scenario Played(bool reload)
        {
            var w = Ridden(World().Run()).Do(new Exert(true)).Run(10);
            if (reload) w.Reload();
            w.Run(10).Do(new Exert(false)).Run(5);
            if (reload) w.Reload();
            return w.Do(new Exert(true)).Run(10);
        }
        var (played, loaded) = (Played(false), Played(true));
        Assert.Equal(Makes, Supply(loaded));
        Assert.Equal(played.World.Hash(), loaded.World.Hash());
        Assert.Equal(played.World.Save(), loaded.World.Save());
    }

    /// <summary>One turn of the property: a peer boards, alights, starts or stops running, or does nothing.</summary>
    public enum Act { Board, Alight, Run, Stop, Wait }

    /// <summary>
    /// Over any boards, alights, starts and stops: the wheel supplies on a tick exactly when that tick's construction saw a rider
    /// seated, awake and exerting. Proven to bite by dropping the exerting check from <c>Turning</c> once: the property failed.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property SuppliesOnlyOnTicksWhoseConstructionSawItsRiderRun() =>
        Prop.ForAll(Gen.Elements(Act.Board, Act.Alight, Act.Run, Act.Stop, Act.Wait).ListOf().Select(l => l.Take(60).ToArray()).ToArbitrary(), acts =>
        {
            var w = World().Run();
            int wheel = Wheel(w);
            w.Do(new Enter(w.At(5, 4))).Run();
            foreach (var act in acts)
            {
                Command? sent = act switch
                {
                    Act.Board => new Board(wheel),
                    Act.Alight => new Alight(),
                    Act.Run => new Exert(true),
                    Act.Stop => new Exert(false),
                    _ => null,
                };
                if (sent != null) w.Do(sent);
                w.Run();
                // Commands applied before construction and nothing between it and derivation changes seats or running.
                bool ran = w.M.Landing.SeatOf(0) == wheel && w.M.Crew.HeroOf(0) is { Out: 0, Exerting: true };
                if ((Supply(w) == Makes) != ran) return false.Label($"{act}: supply {Supply(w)}, ran {ran}");
            }
            return true.ToProperty();
        });
}
