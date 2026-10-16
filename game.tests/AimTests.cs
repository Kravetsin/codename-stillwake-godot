using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Aim, reach and what the hands send: apps/game/src/features/crew/aim.test.ts, one for one.</summary>
public class AimTests
{
    private const int Unit = Hero.Unit;

    /// <summary>A hero whose body is centred on the middle of cell (x, y).</summary>
    private static Hero HeroOn(int x, int y) => new(x * Unit + Unit / 2, y * Unit + Unit / 2 + Hero.MiddleUp, 0, 0, false, 1);

    private static (double X, double Y) Centre(int x, int y) => (x + 0.5, y + 0.5);

    private static readonly Placement IronWall = Placement.Wall("iron");

    /// <summary>A hall nine wide with rows enough inside for <paramref name="cells"/> cells, seven to a row.</summary>
    private static string Hall(int cells) =>
        string.Join("\n", new[] { "#########" }.Concat(Enumerable.Repeat("#.......#", (cells + 6) / 7)).Append("#########"));

    /// <summary>The <paramref name="i"/>-th cell inside a hall, row by row.</summary>
    private static int Inside(int i) => (1 + i / 7) * 9 + 1 + i % 7;

    /// <summary><paramref name="map"/> with a crate drawn below it, and peer 0 carrying scrap and iron.</summary>
    private static TestWorld WithStore(string map) =>
        new TestWorld(Layout.Parse([.. Layout.Parse(map).Rows, "KK"])).Give(0, ("scrap", 360), ("iron", 60));

    // Aiming.

    private const string Corridor = """
        ########
        #..D.v.#
        ########
        """;

    private static int At(int x, int y) => y * 8 + x;

    [Fact]
    public void OffersToToggleADoorOrAValveUnderThePointer()
    {
        var m = new TestWorld(Corridor).M;
        var hero = HeroOn(2, 1);
        Assert.Equal(new Aim(At(3, 1), true, new Use(new ToggleDoor(At(3, 1)), null)), Aiming.AimAt(m, 0, hero, Centre(3, 1)));
        Assert.Null(Aiming.AimAt(m, 0, hero, Centre(4, 1))?.Press);
        Assert.Equal(new ToggleValve(At(5, 1)), Aiming.AimAt(m, 0, HeroOn(4, 1), Centre(5, 1))?.Press?.Command);
    }

    [Fact]
    public void WorksTheDoorWhenAValveSharesItsCell()
    {
        var w = new TestWorld(Corridor);
        // A valve built into the door's cell, by hand, for free.
        w.Step(new SetCreative(true), new PlacePlan(At(3, 1), new Placement("conduit", "valve")));
        while (w.M.Building.Plans.Count > 0) w.Step(new Work(1));
        Assert.True(w.M.Grid.ConduitAt(At(3, 1)) is Conduit.ValveOpen or Conduit.ValveClosed);
        Assert.Equal(new ToggleDoor(At(3, 1)), Aiming.AimAt(w.M, 0, HeroOn(2, 1), Centre(3, 1))?.Press?.Command);
    }

    [Fact]
    public void PutsACanisterIntoATankOnAPressOrCarriesWhyItCannot()
    {
        var w = new TestWorld("""
            ########
            #TT....#
            #TT....#
            ########
            """);
        var tank = w.M.Grid.MachineAt(At(1, 1))!;
        // Carrying none, the hero is told why.
        Assert.Equal(new Use(new LoadCanister(tank.Id), "items.notEnough"), Aiming.AimAt(w.M, 0, HeroOn(3, 2), Centre(2, 2))?.Press);
        // In creative mode a canister is free.
        w.Step(new SetCreative(true));
        Assert.Null(Aiming.AimAt(w.M, 0, HeroOn(3, 2), Centre(2, 2))?.Press?.Problem);
    }

    [Fact]
    public void ReachesCellsWhoseMiddleIsAtMostTheReachFromTheMiddleOfTheBody()
    {
        var open = new TestWorld("""
            ..........
            ..........
            ..........
            """).M;
        var hero = HeroOn(1, 1);
        const int cellsAway = Spec.Hero.Reach / Unit;
        Assert.True(Aiming.AimAt(open, 0, hero, Centre(1 + cellsAway, 1))?.InReach);
        Assert.False(Aiming.AimAt(open, 0, hero, Centre(2 + cellsAway, 1))?.InReach);
        Assert.False(Aiming.AimAt(open, 0, hero, Centre(1 + cellsAway, 2))?.InReach);
    }

    [Fact]
    public void WorksTheOldestPlanInACellAndLeavesETheDoorUnderIt()
    {
        var w = new TestWorld(Corridor);
        w.Step(new Deconstruct(At(3, 1), "structure"), new PlacePlan(At(3, 1), new Placement("conduit", "pipe")));
        Assert.Equal(
            new Aim(At(3, 1), true, new Use(new ToggleDoor(At(3, 1)), null), new WorkAim(1, null)),
            Aiming.AimAt(w.M, 0, HeroOn(2, 1), Centre(3, 1)));
    }

    [Fact]
    public void CarriesTheSimsReasonWhenEOnABlueprintWouldBeRefused()
    {
        // An iron wall planned for a hero carrying nothing.
        var w = new TestWorld(Hall(1));
        w.Step(new PlacePlan(Inside(0), IronWall));
        Assert.Equal(new WorkAim(1, "items.notEnough", Delivers: true), Aiming.AimAt(w.M, 0, HeroOn(2, 1), Centre(1, 1))?.Work);
    }

    [Fact]
    public void WorksAPlanItCanBeforeAnOlderOneItCannot()
    {
        // Nine wide. A tank is planned over a pipe, and the pipe is marked: two plans in cell 15.
        var w = new TestWorld(Layout.Parse(["#########", "#.....-.#", "#.......#", "#########"]));
        w.Step(new PlacePlan(15, new Placement("machine", "tank")), new Deconstruct(15, "conduit"));
        var q = w.M.Building;
        Assert.Equal([(1, "items.notEnough"), (2, (string?)null)], q.PlansAt(15).Select(p => (p.Id, q.WorkProblem(p.Id, 0))));
        Assert.Equal(new WorkAim(2, null), Aiming.AimAt(w.M, 0, HeroOn(5, 1), Centre(6, 1))?.Work);
    }

    [Fact]
    public void PassesOverABlueprintDeliveredToInThisHoldOfEToTheNextInItsCell()
    {
        // A wall and a pipe through it, both planned in one cell, for a hero carrying scrap.
        var w = WithStore(Hall(1));
        w.Step(new PlacePlan(Inside(0), Placement.Wall("scrap")), new PlacePlan(Inside(0), new Placement("conduit", "pipe")));
        var hero = HeroOn(2, 1);
        Assert.Equal(new WorkAim(1, null, Delivers: true), Aiming.AimAt(w.M, 0, hero, Centre(1, 1))?.Work);
        var next = Aiming.AimAt(w.M, 0, hero, Centre(1, 1), new HashSet<int>(), new HashSet<int> { 1 });
        Assert.Equal(new WorkAim(2, null, Delivers: true), next?.Work);
    }

    [Fact]
    public void AimsAtNothingOffTheGrid()
    {
        var m = new TestWorld(Corridor).M;
        var hero = HeroOn(2, 1);
        Assert.Null(Aiming.AimAt(m, 0, hero, (-0.5, 1)));
        Assert.Null(Aiming.AimAt(m, 0, hero, (2, 3.2)));
        Assert.Null(Aiming.AimAt(m, 0, hero, (m.Grid.Width + 0.5, 1)));
    }

    // The wreck, worked with E.

    [Fact]
    public void KeepsTheHeroBehindClosedDoorsUntilTheyAreToggledThroughTheSession()
    {
        var m = new Registry();
        var (host, client) = LocalSession.Create(World.Create(1, HeroTests.Wreck, m.All));
        Ship ShipNow() => Ship.Of(m.Grid, m.Rooms);
        Hero Run(Hero hero, HeroInput input, int steps)
        {
            for (int i = 0; i < steps; i++) hero = hero.Step(input, ShipNow());
            return hero;
        }
        void Use(Hero hero, int x, int y)
        {
            var aim = Aiming.AimAt(m, 0, hero, Centre(x, y));
            Assert.True(aim?.InReach);
            if (aim?.Press is { } press) client.Send(press.Command);
            host.Advance();
            client.Update();
        }

        // Along room A, where the hero wakes: the closed doorway to room B stops the hero, until opened.
        var h = Run(Hero.At(ShipNow(), HeroTests.Spawn), HeroTests.Left, 120);
        Assert.Equal(46 * Unit + Spec.Hero.Width / 2, h.X);
        Use(h, 45, 22);
        h = Run(h, HeroTests.Left, 300);
        // Into room B, torn open to space, and out past its torn edge as far as the tether lets the hero.
        // The tether cuts a move back to its length rounded toward its anchor: within a thousandth of a cell.
        Assert.InRange(h.X, HeroTests.TornEdge - Spec.Hero.Tether, HeroTests.TornEdge - Spec.Hero.Tether + 1);

        // Back the other way, the airlock's inner doorway stops the hero, until opened.
        h = Run(h, HeroTests.Right, 400);
        Assert.Equal(54 * Unit - Spec.Hero.Width / 2, h.X);
        Use(h, 54, 22);
        h = Run(h, HeroTests.Right, 60);
        Assert.Equal(57 * Unit - Spec.Hero.Width / 2, h.X);
    }

    [Fact]
    public void LetsTheHeroThroughADoorItsOwnToggleOpenedBeforeTheHostsTickRunsIt()
    {
        var m = new Registry();
        var (_, client) = LocalSession.Create(World.Create(1, HeroTests.Wreck, m.All));
        Ship ShipNow() => Ship.Of(m.Grid, m.Rooms, cell => Predicted.Grid(client, m.Grid).StructureAt(m.Grid, cell));
        var h = Hero.At(ShipNow(), HeroTests.Spawn);
        for (int i = 0; i < 120; i++) h = h.Step(HeroTests.Left, ShipNow());
        if (Aiming.AimAt(m, 0, h, Centre(45, 22))?.Press is { } press) client.Send(press.Command);
        // Sent, not released: the world still has the doorway shut.
        Assert.NotEqual(m.Grid.StructureLayer.ToArray(), Predicted.Grid(client, m.Grid).Structure);
        for (int i = 0; i < 300; i++) h = h.Step(HeroTests.Left, ShipNow());
        // Into room B, torn open to space, and out past its torn edge as far as the tether lets the hero.
        // The tether cuts a move back to its length rounded toward its anchor: within a thousandth of a cell.
        Assert.InRange(h.X, HeroTests.TornEdge - Spec.Hero.Tether, HeroTests.TornEdge - Spec.Hero.Tether + 1);
    }

    // A crate under the pointer.

    private static readonly TestWorld Room = WithStore("""
        ##########
        #........#
        #........#
        #........#
        ##########
        """);

    [Fact]
    public void OpensOnAPressWithinReachAndOnNoPressOrOutOfReach()
    {
        var crate = Room.M.Grid.Machines.First(c => c.Kind == "crate");
        // The crate sits on the row below the room's floor, from column 0.
        int row = crate.Cell / Room.M.Grid.Width;
        var aim = Aiming.AimAt(Room.M, 0, HeroOn(2, row - 1), Centre(1, row));
        Assert.Equal(new Aim(crate.Cell + 1, true, Crate: crate.Id), aim);
        var opened = Aiming.UsesOf(aim, pressed: true, held: true, 3, -1);
        Assert.Equal(((int?)crate.Id, (int?)null, -1), (opened.Open, opened.Served, opened.WorkedOnTick));
        Assert.Empty(opened.Commands);
        Assert.Null(Aiming.UsesOf(aim, pressed: false, held: true, 3, -1).Open);
        var far = Aiming.AimAt(Room.M, 0, HeroOn(8, row - 7), Centre(1, row));
        Assert.Null(Aiming.UsesOf(far, pressed: true, held: true, 3, -1).Open);
    }

    [Fact]
    public void StaysInReachWhileAnyCellOfItIsAndNotOnceItIsTakenApart()
    {
        var crate = Room.M.Grid.Machines.First(c => c.Kind == "crate");
        int row = crate.Cell / Room.M.Grid.Width;
        const int reach = Spec.Hero.Reach / Unit;
        Assert.True(Aiming.CrateInReach(Room.M.Grid, HeroOn(1 + reach, row), crate.Id));
        Assert.False(Aiming.CrateInReach(Room.M.Grid, HeroOn(2 + reach, row), crate.Id));
        Assert.False(Aiming.CrateInReach(Room.M.Grid, HeroOn(1, row), crate.Id + 1));
    }

    // A piece of debris under the pointer.

    /// <summary>A hull of one cell in the corner, for the debris to fly past, run until the first piece is out.</summary>
    private static TestWorld Corner()
    {
        var w = new TestWorld(Layout.Parse(["#".PadRight(12), .. Enumerable.Repeat(new string(' ', 12), 7)]));
        for (int i = 0; i < Spec.Debris.Every; i++) w.Step();
        return w;
    }

    [Fact]
    public void IsGrabbedOnAPressOrCarriesWhyTheSimWouldRefuseIt()
    {
        var w = Corner();
        var piece = w.M.Debris.Pieces[0];
        int x = piece.X / Unit, y = piece.Y / Unit;
        var grab = new Grab(piece.Id);
        // The sim knows no hero outside yet.
        Assert.Equal(new Use(grab, "debris.notOutside"), Aiming.AimAt(w.M, 0, HeroOn(x - 2, y), Centre(x, y))?.Press);
        w.Step(new Sim.Crew.Enter(y * 12 + x - 2));
        var aim = Aiming.AimAt(w.M, 0, HeroOn(x - 2, y), Centre(x, y));
        Assert.Equal(new Use(grab, null), aim?.Press);
        Assert.Equal(piece.Id, aim?.Piece);
        Assert.Equal([grab], Aiming.UsesOf(aim, pressed: true, held: true, 3, -1).Commands);
        // Grabbed, its grab in flight: the aim passes over it, so E cannot send it twice.
        var passed = Aiming.AimAt(w.M, 0, HeroOn(x - 2, y), Centre(x, y), new HashSet<int> { piece.Id });
        Assert.Equal((null, null), (passed?.Piece, passed?.Press));
    }

    [Fact]
    public void IsAimedAtFromBesideItWithinARadiusOfThePointerNotFromFarther()
    {
        var w = Corner();
        var piece = w.M.Debris.Pieces[0];
        var hero = HeroOn(piece.X / Unit - 2, piece.Y / Unit);
        // Off to its left, inside the grid: it came in from the right edge.
        (double, double) Off(int by) => ((double)(piece.X - by) / Unit, (double)piece.Y / Unit);
        Assert.Equal(piece.Id, Aiming.AimAt(w.M, 0, hero, Off(Spec.Debris.AimRadius - 100))?.Piece);
        Assert.Null(Aiming.AimAt(w.M, 0, hero, Off(Spec.Debris.AimRadius + 100))?.Piece);
    }

    [Fact]
    public void IsNotAimedAtWhileTheHeroIsAboardItDriftsPastBehindTheHull()
    {
        var w = new TestWorld(Registry.Wreck);
        for (int i = 0; i < 4 * Spec.Debris.Every && w.M.Debris.Pieces.Count == 0; i++) w.Step();
        var piece = w.M.Debris.Pieces[0];
        var aboard = Hero.At(Ship.Of(w.M.Grid, w.M.Rooms), w.M.Crew.Spawn);
        Assert.Null(Aiming.AimAt(w.M, 0, aboard, ((double)piece.X / Unit, (double)piece.Y / Unit))?.Piece);
    }

    // A wreck (0034): aimed at by its picture, chopped with the axe, too big to catch.

    /// <summary>
    /// The corner's first piece made a hull section, a scrap chunk a cell below its middle, and peer 0's
    /// hero reported out in space three cells left of it: the wreck's middle and the hero.
    /// </summary>
    private static (TestWorld W, Piece Wreck, Hero Hero) Wrecked()
    {
        var w = Corner().Edit(root =>
        {
            var pieces = root["slices"]!["debris"]!["pieces"]!.AsArray();
            var first = pieces[0]!;
            first["item"] = "hullSection";
            var chunk = first.DeepClone();
            chunk["id"] = 99;
            chunk["item"] = "scrapChunk";
            chunk["y"] = (int)first["y"]! + Unit;
            pieces.Add(chunk);
        });
        var wreck = w.M.Debris.Pieces[0];
        int x = wreck.X / Unit, y = wreck.Y / Unit;
        w.Step(new Sim.Crew.Enter(y * 12 + x - 3));
        return (w, w.M.Debris.Pieces[0], HeroOn(x - 3, y));
    }

    [Fact]
    public void AimsAtAWreckAnywhereOnItsPictureOverASmallPieceNearerThePointer()
    {
        var (w, wreck, hero) = Wrecked();
        var (pw, ph) = DebrisLooks.Wrecks["hullSection"];
        // Near its bottom left corner: the chunk below its middle is nearer, the wreck still wins.
        double x = (double)wreck.X / Unit - pw / 16.0 / 2 + 0.2, y = (double)wreck.Y / Unit + ph / 16.0 / 2 - 0.2;
        Assert.Equal(wreck.Id, Aiming.AimAt(w.M, 0, hero, (x, y))?.Piece);
        // Off its picture, the chunk below.
        Assert.Equal(99, Aiming.AimAt(w.M, 0, hero, ((double)wreck.X / Unit, (double)wreck.Y / Unit + ph / 16.0 / 2 + 0.6))?.Piece);
    }

    [Fact]
    public void ChopsAWreckWithTheAxeAndBareHandedTriesToCatchItWhichIsTooBig()
    {
        var (w, wreck, hero) = Wrecked();
        var at = ((double)wreck.X / Unit, (double)wreck.Y / Unit);
        Assert.Equal(new Use(new Grab(wreck.Id), "debris.tooBig"), Aiming.AimAt(w.M, 0, hero, at)?.Press);
        var axe = Aiming.AimAt(w.M, 0, hero, at, axe: true);
        Assert.Equal(new Use(new Hit(wreck.Id), null), axe?.Press);
        Assert.Equal(new Prompt("use.chop", null), Prompts.Of(w.M, axe));
        // A press never chops: the left button held does, at the axe's pace.
        Assert.Empty(Aiming.UsesOf(axe, pressed: true, held: true, 5, -1).Commands);
        // The axe in hand, a small piece is still caught.
        var chunk = w.M.Debris.Pieces.Single(p => p.Id == 99);
        Assert.IsType<Grab>(Aiming.AimAt(w.M, 0, hero, ((double)chunk.X / Unit, (double)chunk.Y / Unit + 0.4), axe: true)?.Press?.Command);
    }

    [Fact]
    public void ReachesAWreckByItsNearestEdgeNotItsMiddle()
    {
        var (w, wreck, _) = Wrecked();
        int halfW = DebrisLooks.Wrecks["hullSection"].W * Unit / 32;
        int y = wreck.Y / Unit;
        // A hero whose reach just passes its left edge, though its middle is farther.
        var near = new Hero(wreck.X - halfW - Spec.Hero.Reach + 100, wreck.Y + Hero.MiddleUp, 0, 0, false, 1);
        var far = near with { X = near.X - 600 };
        var at = ((double)wreck.X / Unit, (double)y + 0.5);
        Assert.True(Aiming.AimAt(w.M, 0, near, at, axe: true)!.InReach);
        Assert.False(Aiming.AimAt(w.M, 0, far, at, axe: true)!.InReach);
    }

    [Fact]
    public void SwingsAtOnceThenEveryAxePaceWhileHeldAndNoFasterForClicking()
    {
        var hit = new Hit(4);
        var aim = new Aim(3, true, new Use(hit, null), Piece: 4);
        int swung = -1;
        var sent = new List<int>();
        foreach (var (tick, held) in new[] { (10, true), (11, true), (19, true), (20, true), (21, false), (22, true), (30, true) })
        {
            var (command, now) = Aiming.Swing(aim, held, tick, swung);
            swung = now;
            if (command != null) sent.Add(tick);
        }
        Assert.Equal([10, 20, 30], sent);
        Assert.Equal((null, 5), Aiming.Swing(aim with { InReach = false }, true, 50, 5));
        Assert.Equal((null, 5), Aiming.Swing(aim with { Press = new Use(hit, "debris.notOutside") }, true, 50, 5));
        Assert.Equal((hit, 3), Aiming.Swing(aim, true, 3, 40));
    }

    // What E sends.

    private static readonly ToggleDoor Door = new(3);
    private static readonly Aim OnDoor = new(3, true, new Use(Door, null));
    private static readonly Aim OnPlan = new(4, true, Work: new WorkAim(7, null));
    private static readonly Aim ToPlan = OnPlan with { Work = new WorkAim(7, null, Delivers: true) };
    private static readonly Work WorkOn = new(7);

    [Fact]
    public void TogglesADoorOnceOnAPressAndNothingWhileEStaysHeld()
    {
        Assert.Equal([Door], Aiming.UsesOf(OnDoor, pressed: true, held: true, 10, -1).Commands);
        Assert.Empty(Aiming.UsesOf(OnDoor, pressed: false, held: true, 11, -1).Commands);
    }

    [Fact]
    public void DeliversToABlueprintOnceNamingItSoTheSweepPassesItBy()
    {
        var uses = Aiming.UsesOf(ToPlan, pressed: false, held: true, 5, -1);
        Assert.Equal([WorkOn], uses.Commands);
        Assert.Equal(((int?)null, (int?)7, -1), (uses.Open, uses.Served, uses.WorkedOnTick));
    }

    [Fact]
    public void SendsOneWorkForEveryTickReleasedWhileEIsHeldOnAPlanWithAllItNeeds()
    {
        int worked = -1;
        var sent = new List<int>();
        foreach (int tick in new[] { 5, 5, 6, 6, 8 })
        {
            var uses = Aiming.UsesOf(OnPlan, pressed: false, held: true, tick, worked);
            worked = uses.WorkedOnTick;
            sent.Add(uses.Commands.Count);
        }
        // One when the hold begins, none until the next tick, then one per tick, two after a skip.
        Assert.Equal([1, 0, 1, 0, 2], sent);
    }

    [Fact]
    public void CatchesUpAfterAStallAFewTicksAtMostAndStartsAfreshWhenALoadTakesTheTickBack()
    {
        Assert.Equal(5, Aiming.UsesOf(OnPlan, pressed: false, held: true, 100, 10).Commands.Count);
        var fresh = Aiming.UsesOf(OnPlan, pressed: false, held: true, 3, 50);
        Assert.Equal([WorkOn], fresh.Commands);
        Assert.Equal(3, fresh.WorkedOnTick);
    }

    [Fact]
    public void SendsNothingOutOfReachForAPlanTheSimWouldRefuseOrWithEUp()
    {
        var far = Aiming.UsesOf(OnPlan with { InReach = false }, pressed: true, held: true, 5, 4);
        Assert.Equal((0, -1), (far.Commands.Count, far.WorkedOnTick));
        var refused = OnPlan with { Work = new WorkAim(7, "items.notEnough") };
        Assert.Empty(Aiming.UsesOf(refused, pressed: false, held: true, 5, -1).Commands);
        var up = Aiming.UsesOf(OnPlan, pressed: false, held: false, 5, 4);
        Assert.Equal((0, -1), (up.Commands.Count, up.WorkedOnTick));
    }

    [Fact]
    public void PressesNothingTheSimWouldRefuse() =>
        Assert.Empty(Aiming.UsesOf(OnDoor with { Press = new Use(Door, "atmosphere.tankFull") }, pressed: true, held: true, 5, -1).Commands);

    [Fact]
    public void WorksAPlanInADoorCellOnAPressRatherThanTogglingTheDoor() =>
        Assert.Equal([WorkOn], Aiming.UsesOf(OnPlan with { Cell = 3 }, pressed: true, held: true, 5, -1).Commands);
}
