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

    /// <summary><paramref name="map"/> with a crate drawn below it, and peer 0 carrying scrap, iron and tubes.</summary>
    private static TestWorld WithStore(string map) =>
        new TestWorld(Layout.Parse([.. Layout.Parse(map).Rows, "KK"])).Give(0, ("scrap", 360), ("iron", 60), ("tube", 20));

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
        // A wall and a pipe through it, both planned in one cell, for a hero carrying scrap and tubes.
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
        h = Run(h, HeroTests.Left, 900);
        // Into room B, torn open to space, and out past its torn edge as far as the tether lets the hero.
        // The tether cuts a move back to its length rounded toward its anchor: within a thousandth of a cell.
        Assert.InRange(h.X, HeroTests.TornEdge - Spec.Hero.Tether, HeroTests.TornEdge - Spec.Hero.Tether + 1);

        // Back the other way, the airlock's inner doorway stops the hero, until opened.
        h = Run(h, HeroTests.Right, 1200);
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
        Assert.Contains(Predicted.Grid(client, m.Grid).Structure!, shown => shown.Value != m.Grid.StructureAt(shown.Key));
        for (int i = 0; i < 900; i++) h = h.Step(HeroTests.Left, ShipNow());
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
    /// hero reported out in space two cells left of it, its blade's reach: the wreck's middle and the hero.
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
        w.Step(new Sim.Crew.Enter(y * 12 + x - 2));
        return (w, w.M.Debris.Pieces[0], HeroOn(x - 2, y));
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
        Assert.Equal(new Use(new Hit(wreck.Id, Spec.Weapons.Axe.Item), null), axe?.Press);
        Assert.Equal(new Prompt("use.chop", null), Prompts.Of(w.M, axe));
        // A press never chops: the left button held does, at the axe's pace.
        Assert.Empty(Aiming.UsesOf(axe, pressed: true, held: true, 5, -1).Commands);
        // The axe in hand, a small piece is still caught.
        var chunk = w.M.Debris.Pieces.Single(p => p.Id == 99);
        Assert.IsType<Grab>(Aiming.AimAt(w.M, 0, hero, ((double)chunk.X / Unit, (double)chunk.Y / Unit + 0.4), axe: true)?.Press?.Command);
    }

    [Fact]
    public void ReachesAWreckBareHandedByItsNearestEdgeNotItsMiddle()
    {
        var (w, wreck, _) = Wrecked();
        int halfW = DebrisLooks.Wrecks["hullSection"].W * Unit / 32;
        int y = wreck.Y / Unit;
        // A hero whose reach just passes its left edge, though its middle is farther.
        var near = new Hero(wreck.X - halfW - Spec.Hero.Reach + 100, wreck.Y + Hero.MiddleUp, 0, 0, false, 1);
        var far = near with { X = near.X - 600 };
        var at = ((double)wreck.X / Unit, (double)y + 0.5);
        Assert.True(Aiming.AimAt(w.M, 0, near, at)!.InReach);
        Assert.False(Aiming.AimAt(w.M, 0, far, at)!.InReach);
    }

    // The axe's blade (0039): what a swing's downstroke touches as it strikes.

    private const int FieldWidth = 24;

    /// <summary>
    /// Open space <see cref="FieldWidth"/> by 14 cells, peer 0's hero out in it on cell (6, 7) facing right,
    /// and still pieces of <paramref name="items"/> with their middles where they say, in thousandths of a
    /// cell, ids from 1 in that order.
    /// </summary>
    private static (TestWorld W, Hero Hero) Field(params (string Item, int X, int Y)[] items)
    {
        var w = new TestWorld(Layout.Parse(["#".PadRight(FieldWidth), .. Enumerable.Repeat(new string(' ', FieldWidth), 13)]));
        for (int i = 0; i < Spec.Debris.Every; i++) w.Step();
        w.Edit(root =>
        {
            var debris = root["slices"]!["debris"]!;
            var pieces = debris["pieces"]!.AsArray();
            var model = pieces[0]!.DeepClone();
            pieces.Clear();
            for (int i = 0; i < items.Length; i++)
            {
                var piece = model.DeepClone();
                (piece["id"], piece["item"], piece["x"], piece["y"], piece["speed"]) = (i + 1, items[i].Item, items[i].X, items[i].Y, 0);
                pieces.Add(piece);
            }
            debris["nextId"] = items.Length + 1;
        });
        w.Step(new Sim.Crew.Enter(7 * FieldWidth + 6));
        return (w, HeroOn(6, 7));
    }

    /// <summary>The hero's shoulder upright, in thousandths of a cell: where the blade's reach is measured from.</summary>
    private static Point ShoulderOf(Hero hero) => new(hero.X, hero.Y - (int)(Rig.Shoulder / Rig.Cell * Unit));

    /// <summary>Half a hull section's aim box across: its picture and the margin.</summary>
    private static readonly int HalfW = DebrisLooks.Wrecks["hullSection"].W * Unit / 32 + 250;

    private static List<int> Struck(TestWorld w, Hero hero, Point at) => Aiming.Struck(w.M, hero, at).Select(p => p.Id).ToList();

    /// <summary>
    /// How far ahead of the shoulder the edge of <paramref name="look"/> gets, in thousandths of a cell,
    /// swung level: its ends where the figure draws them (<see cref="AxeSwing.EdgeAt"/>), every tenth of a
    /// degree of the downstroke from the blade on the line to <see cref="AxeSwing.Through"/> past it.
    /// </summary>
    private static int ReachOf(HeldLook look)
    {
        double far = 0, lead = AxeSwing.Lead(look);
        for (double lift = 0; lift <= AxeSwing.Through; lift += Math.PI / 1800)
        {
            var (a, b) = AxeSwing.EdgeAt(look, lead + lift);
            far = Math.Max(far, Math.Max(a.X, b.X));
        }
        return (int)(far / Rig.Cell * Unit);
    }

    /// <summary>The wreck <paramref name="look"/> swung level strikes with its near side <paramref name="ahead"/> of the shoulder, if it does.</summary>
    private static List<int> StruckAhead(int ahead, HeldLook? look = null)
    {
        var shoulder = ShoulderOf(HeroOn(6, 7));
        var (w, hero) = Field(("hullSection", shoulder.X + ahead + HalfW, shoulder.Y));
        return Aiming.Struck(w.M, hero, shoulder with { X = shoulder.X + 5_000 }, look).Select(p => p.Id).ToList();
    }

    [Fact]
    public void HitsAWreckTheDrawnEdgeSweepsAndMissesOneJustBeyondIt()
    {
        var shoulder = ShoulderOf(HeroOn(6, 7));
        // Its near edge 0.8 of a cell ahead, level with the shoulder: struck, the pointer on it or short of it.
        var (w, hero) = Field(("hullSection", shoulder.X + 800 + HalfW, shoulder.Y));
        Assert.Equal([1], Struck(w, hero, new Point(shoulder.X + 3_000, shoulder.Y)));
        Assert.Equal([1], Struck(w, hero, new Point(shoulder.X + 500, shoulder.Y)));
        Assert.Equal([new Hit(1, Spec.Weapons.Axe.Item)], Aiming.HitsOf(w.M, 0, hero, new Point(shoulder.X + 500, shoulder.Y)));
        // The edge's tip reaches about an arm and the axe, held square to it (0040); a wreck just short of
        // it is struck, one just past it is not.
        int reach = ReachOf(HeldLook.FireAxe);
        Assert.InRange(reach, 1_500, 1_750);
        Assert.Equal([1], StruckAhead(reach - 50));
        Assert.Empty(StruckAhead(reach + 50));
    }

    /// <summary>Every kind of wreckage, two cells square to four by two: each aimed at and struck by its own picture's size.</summary>
    public static TheoryData<string> WreckKinds() => [.. Spec.Wreckage.All.Select(k => k.Item)];

    [Theory]
    [MemberData(nameof(WreckKinds))]
    public void AimsAtAndStrikesEveryKindOfWreckByItsOwnPictureAndNoFarther(string kind)
    {
        var (pw, ph) = DebrisLooks.Wrecks[kind];
        int halfW = pw * Unit / 32 + 250, halfH = ph * Unit / 32 + 250;
        var shoulder = ShoulderOf(HeroOn(6, 7));
        int reach = ReachOf(HeldLook.FireAxe);
        var far = shoulder with { X = shoulder.X + 5_000 };
        // Its near side just inside the blade's reach ahead, and just past it.
        var (near, hero) = Field((kind, shoulder.X + reach - 50 + halfW, shoulder.Y));
        Assert.Equal([1], Struck(near, hero, far));
        var (beyond, _) = Field((kind, shoulder.X + reach + 50 + halfW, shoulder.Y));
        Assert.Empty(Struck(beyond, hero, far));
        // Its top a cell below the shoulder, chopped downward.
        var (below, _) = Field((kind, shoulder.X + 500, shoulder.Y + 1_000 + halfH));
        Assert.Equal([1], Struck(below, hero, shoulder with { Y = shoulder.Y + 3_000 }));
        // Bare-handed, the pointer on its picture's far top corner aims at it, and just off it does not.
        var (w, _) = Field((kind, 14_500, 7_500));
        double right = 14.5 + pw / 32.0, top = 7.5 - ph / 32.0;
        Assert.Equal(1, Aiming.AimAt(w.M, 0, hero, (right - 0.1, top + 0.1))?.Piece);
        Assert.Null(Aiming.AimAt(w.M, 0, hero, (right + 0.4, top - 0.4))?.Piece);
    }

    [Fact]
    public void ReachesFartherGrippedLowerOnTheHaft()
    {
        // The same axe held at the very end of its haft: the edge, and with it the reach, moves out.
        var atTheEnd = HeldLook.FireAxe with { Grip = (2, 7) };
        int reach = ReachOf(HeldLook.FireAxe), longer = ReachOf(atTheEnd);
        Assert.True(longer > reach + 100, $"{longer} against {reach}");
        Assert.Empty(StruckAhead(reach + 50));
        Assert.Equal([1], StruckAhead(reach + 50, atTheEnd));
        Assert.Empty(StruckAhead(longer + 50, atTheEnd));
    }

    [Fact]
    public void MissesAWreckFourCellsOffThoughThePointerIsOnIt()
    {
        var shoulder = ShoulderOf(HeroOn(6, 7));
        var (w, hero) = Field(("hullSection", shoulder.X + 4_000 + HalfW, shoulder.Y));
        var on = new Point(shoulder.X + 4_000 + HalfW, shoulder.Y);
        Assert.Empty(Aiming.HitsOf(w.M, 0, hero, on));
        // Nothing outlined, no prompt: the blade would cut air.
        var aim = Aiming.AimAt(w.M, 0, hero, ((double)on.X / Unit, (double)on.Y / Unit), axe: true);
        Assert.Null(aim?.Piece);
        Assert.Null(Prompts.Of(w.M, aim));
    }

    [Fact]
    public void MissesAWreckBehindTheHeroOrInTheRaisedStretchOverItsHead()
    {
        var shoulder = ShoulderOf(HeroOn(6, 7));
        var ahead = shoulder with { X = shoulder.X + 3_000 };
        // Behind, as near as one ahead that is struck.
        var (behind, hero) = Field(("hullSection", shoulder.X - 1_000 - HalfW, shoulder.Y));
        Assert.Empty(Struck(behind, hero, ahead));
        Assert.Equal([1], Struck(behind, hero with { Facing = -1 }, shoulder with { X = shoulder.X - 3_000 }));
        // Over the head, where the blade is raised before it comes down ahead: struck only swung up at it.
        var (over, _) = Field(("hullSection", shoulder.X, shoulder.Y - 600 - 1_250));
        Assert.Empty(Struck(over, hero, ahead));
        Assert.Equal([1], Struck(over, hero, shoulder with { Y = shoulder.Y - 3_000 }));
    }

    [Fact]
    public void HitsEachOfTwoWrecksTheStrokeMeetsOnceAndNeverASmallPiece()
    {
        var shoulder = ShoulderOf(HeroOn(6, 7));
        // One along the line to the pointer, one lower, a scrap chunk on the line between them and the hero.
        var (w, hero) = Field(
            ("hullSection", shoulder.X + 2_500, shoulder.Y - 1_000),
            ("scrapChunk", shoulder.X + 700, shoulder.Y),
            ("hullSection", shoulder.X + 2_500, shoulder.Y + 1_500));
        var ahead = shoulder with { X = shoulder.X + 3_000 };
        Assert.Equal([new Hit(1, Spec.Weapons.Axe.Item), new Hit(3, Spec.Weapons.Axe.Item)], Aiming.HitsOf(w.M, 0, hero, ahead));
        // Aimed at, the first of them is outlined, with the axe's prompt.
        var aim = Aiming.AimAt(w.M, 0, hero, (9.9, (double)shoulder.Y / Unit - 0.2), axe: true);
        Assert.Equal((1, true), (aim?.Piece, aim?.InReach));
        Assert.Equal(new Prompt("use.chop", null), Prompts.Of(w.M, aim));
    }

    [Fact]
    public void ChopsBelowAndAboveTowardThePointer()
    {
        var shoulder = ShoulderOf(HeroOn(6, 7));
        // Their near sides a cell below and above the shoulder.
        var (w, hero) = Field(("hullSection", shoulder.X + 500, shoulder.Y + 1_000 + 1_250), ("hullSection", shoulder.X + 500, shoulder.Y - 1_000 - 1_250));
        Assert.Equal([new Hit(1, Spec.Weapons.Axe.Item)], Aiming.HitsOf(w.M, 0, hero, shoulder with { Y = shoulder.Y + 3_000 }));
        Assert.Equal([new Hit(2, Spec.Weapons.Axe.Item)], Aiming.HitsOf(w.M, 0, hero, shoulder with { Y = shoulder.Y - 3_000 }));
    }

    [Fact]
    public void StrikesNothingAboard()
    {
        // The hero in a room, a wreck just outside its wall: nothing is touched from inside the hull.
        var w = new TestWorld(Layout.Parse(["#####".PadRight(FieldWidth), "#...#".PadRight(FieldWidth), "#...#".PadRight(FieldWidth), "#####".PadRight(FieldWidth), .. Enumerable.Repeat(new string(' ', FieldWidth), 6)]));
        for (int i = 0; i < Spec.Debris.Every; i++) w.Step();
        var aboard = HeroOn(3, 2);
        var shoulder = ShoulderOf(aboard);
        w.Edit(root =>
        {
            var piece = root["slices"]!["debris"]!["pieces"]![0]!;
            (piece["item"], piece["x"], piece["y"], piece["speed"]) = ("hullSection", shoulder.X + 1_000 + HalfW, shoulder.Y, 0);
        });
        Assert.Empty(Aiming.Struck(w.M, aboard, shoulder with { X = shoulder.X + 3_000 }));
        Assert.Null(Aiming.AimAt(w.M, 0, aboard, (6.0, (double)shoulder.Y / Unit), axe: true)?.Piece);
        // The same reach out in space strikes it.
        Assert.Single(Aiming.Struck(w.M, aboard with { X = aboard.X + 1_000 * 3 }, shoulder with { X = shoulder.X + 5_000 }));
    }

    // The drill's bit (0054): aimed at the pointer, it touches what its point reaches.

    /// <summary>Half an asteroid's aim box across: its picture and the margin.</summary>
    private static readonly int RockHalf = DebrisLooks.Wrecks["asteroid"].W * Unit / 32 + 250;

    /// <summary>How far ahead of the shoulder the drill's point gets, aimed level, in thousandths of a cell, as the figure holds it.</summary>
    private static int DrillReach()
    {
        var look = HeldLook.Drill;
        var (_, tip) = AxeSwing.EdgeAt(look, HarpoonShot.ArmFor(look, 200, 0));
        return (int)(tip.X / Rig.Cell * Unit);
    }

    [Fact]
    public void TouchesAWreckTheDrillsPointReachesAimedAtItAndNotOneJustBeyond()
    {
        var shoulder = ShoulderOf(HeroOn(6, 7));
        int reach = DrillReach();
        // An arm and a stubby drill: a little over a cell.
        Assert.InRange(reach, 1_000, 1_600);
        var far = shoulder with { X = shoulder.X + 5_000 };
        foreach (var kind in new[] { "asteroid", "hullSection" })
        {
            int half = DebrisLooks.Wrecks[kind].W * Unit / 32 + 250;
            var (near, hero) = Field((kind, shoulder.X + reach - 60 + half, shoulder.Y));
            Assert.Equal([1], Aiming.Touched(near.M, hero, far).Select(p => p.Id));
            var (beyond, _) = Field((kind, shoulder.X + reach + 60 + half, shoulder.Y));
            Assert.Empty(Aiming.Touched(beyond.M, hero, far));
        }
        // Aimed away from it, up or behind, the bit touches nothing.
        var (w, h) = Field(("asteroid", shoulder.X + reach - 60 + RockHalf, shoulder.Y));
        Assert.Empty(Aiming.Touched(w.M, h, shoulder with { Y = shoulder.Y - 5_000 }));
    }

    [Fact]
    public void AimsTheDrillAtWhatItsBitTouchesAndSaysWhenTheBeltIsFlat()
    {
        var shoulder = ShoulderOf(HeroOn(6, 7));
        var (w, hero) = Field(("asteroid", shoulder.X + DrillReach() - 60 + RockHalf, shoulder.Y));
        var at = ((double)shoulder.X / Unit + 5, (double)shoulder.Y / Unit);
        var aim = Aiming.AimAt(w.M, 0, hero, at, drill: true);
        Assert.Equal(new Use(new Hit(1, Spec.Weapons.Drill.Item), "debris.flat"), aim?.Press);
        Assert.Equal(1, aim?.Piece);
        Assert.Equal(new Prompt("use.chop", "debris.flat"), Prompts.Of(w.M, aim));
        Assert.Empty(Aiming.HitsOf(w.M, 0, Aiming.Touched(w.M, hero, Aiming.PointOf(at)), Spec.Weapons.Drill.Item));
        // In creative mode it needs no battery.
        w.Step(new SetCreative(true));
        Assert.Equal(new Use(new Hit(1, Spec.Weapons.Drill.Item), null), Aiming.AimAt(w.M, 0, hero, at, drill: true)?.Press);
        Assert.Equal([new Hit(1, Spec.Weapons.Drill.Item)], Aiming.HitsOf(w.M, 0, Aiming.Touched(w.M, hero, Aiming.PointOf(at)), Spec.Weapons.Drill.Item));
    }

    [Fact]
    public void HitsWithTheDrillOnTheTickItStartsThenEveryPaceTicksAndOwesNoBurstAfterAStall()
    {
        const int pace = 2;
        var drill = new DrillPace();
        var due = new List<int>();
        // Three frames a tick, as at 60 frames a second: a hit on 10, 12, 14.
        for (int frame = 0; frame < 18; frame++)
            if (drill.Due(10 + frame / 3, runs: true, pace)) due.Add(10 + frame / 3);
        Assert.Equal([10, 12, 14], due);
        // A frame a tick and a half: it keeps the pace, catching up a tick it fell behind.
        Assert.True(drill.Due(16, true, pace));
        Assert.False(drill.Due(17, true, pace));
        Assert.True(drill.Due(19, true, pace));
        Assert.True(drill.Due(20, true, pace));
        // A stall of ten ticks: one hit, then the pace again from there.
        Assert.True(drill.Due(30, true, pace));
        Assert.False(drill.Due(31, true, pace));
        Assert.True(drill.Due(32, true, pace));
        // Let go, it stops; held again, it hits at once.
        Assert.False(drill.Due(33, false, pace));
        Assert.True(drill.Due(33, true, pace));
        // A load took the tick back: it starts afresh.
        Assert.True(drill.Due(5, true, pace));
    }

    [Fact]
    public void ASwingAtAirBeginsStrikesOnceAndSendsNothing()
    {
        var (w, hero) = Field(("hullSection", 18_500, 3_500));
        var swing = new AxeSwing();
        var point = new Point(9_000, 9_000);
        int begun = 0, strikes = 0;
        var sent = new List<Hit>();
        for (int frame = 0; frame < 60; frame++)
        {
            var step = swing.Advance(1000.0 / 60, frame == 0, point);
            begun += step.Begun ? 1 : 0;
            if (!step.Hits) continue;
            strikes++;
            sent.AddRange(Aiming.HitsOf(w.M, 0, hero, swing.At));
        }
        Assert.Equal((1, 1), (begun, strikes));
        Assert.Empty(sent);
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

    // The harpoon's rod (0044): what it meets on its way, and where it sticks.

    /// <summary>Half a small piece's picture across, and its box with the margin.</summary>
    private const int SmallPicture = 500, SmallBox = 750, Bite = 150;

    [Fact]
    public void HooksTheNearestPieceTheRodMeetsNotTheFirstSpawned()
    {
        var (w, _) = Field(("hullSection", 12_000, 7_500), ("scrapChunk", 10_000, 7_500));
        Assert.Equal(2, Aiming.Hooks(w.M, 0, (6_500, 7_500), (14_000, 7_500), 0)?.Piece);
        Assert.Equal(1, Aiming.Hooks(w.M, 0, (14_000, 7_500), (6_500, 7_500), 0)?.Piece);
        var (section, _) = Field(("hullSection", 12_000, 7_500));
        // It enters the section's box at its margin and sticks just inside its picture.
        Assert.Equal((12_000.0 - HalfW + 250 + Bite, 7_500.0), Aiming.Hooks(section.M, 0, (6_500, 7_500), (14_000, 7_500), 0)?.At);
    }

    [Fact]
    public void HooksASmallPieceInABoxOfItsPictureAndAMarginStickingOnThePicture()
    {
        var (w, _) = Field(("scrapChunk", 10_000, 7_500));
        Assert.Equal((10_000.0 - SmallPicture + Bite, 7_500.0), Aiming.Hooks(w.M, 0, (6_500, 7_500), (14_000, 7_500), 0)?.At);
        // Passing just inside the margin above it, or stopping just short of it.
        Assert.Equal(1, Aiming.Hooks(w.M, 0, (6_500, 7_500 - SmallBox + 10), (14_000, 7_500 - SmallBox + 10), 0)?.Piece);
        Assert.Null(Aiming.Hooks(w.M, 0, (6_500, 7_500 - SmallBox - 10), (14_000, 7_500 - SmallBox - 10), 0));
        Assert.Null(Aiming.Hooks(w.M, 0, (6_500, 7_500), (10_000 - SmallBox - 10, 7_500), 0));
    }

    [Fact]
    public void PassesOverAPieceTheSimWouldNotLetTheHeroReel()
    {
        // The first lies over the hull's one cell: not in space. The second is.
        var (w, _) = Field(("scrapChunk", 500, 500), ("scrapChunk", 3_000, 500));
        Assert.Equal(2, Aiming.Hooks(w.M, 0, (6_000, 500), (0, 500), 0)?.Piece);
        Assert.Equal("debris.notOutside", w.M.Debris.ReelProblem(0, 1));
        // A peer with no hero out reels nothing.
        Assert.Null(Aiming.Hooks(w.M, 5, (6_000, 500), (0, 500), 0));
    }

    [Fact]
    public void MeetsAPieceWhereItIsDrawnBetweenTicksReeledOrDrifting()
    {
        var (w, _) = Field(("scrapChunk", 10_000, 7_500));
        // Reeled 4000 to the left: 16 ticks at 250, one of them run with the reel.
        w.Step(new Reel(1, 6_000, 7_500));
        var piece = w.M.Debris.Pieces.Single();
        Assert.Equal((9_750, 15), (piece.X, piece.Reel!.Left));
        Assert.Equal((9_625.0, 7_500.0), PieceDrawn.At(piece, 0.5));
        // Its box drawn halfway reaches 125 further left than at the tick: a rod passing there meets it then only.
        const int Edge = 9_625 - SmallBox + 50;
        Assert.Equal(1, Aiming.Hooks(w.M, 0, (Edge, 0), (Edge, 10_000), 0.5)?.Piece);
        Assert.Null(Aiming.Hooks(w.M, 0, (Edge, 0), (Edge, 10_000), 0));
    }

    [Fact]
    public void WithTheHarpoonAimsAtNoWreckButStillCatchesASmallPiece()
    {
        var (w, wreck, hero) = Wrecked();
        var at = ((double)wreck.X / Unit, (double)wreck.Y / Unit);
        Assert.Null(Aiming.AimAt(w.M, 0, hero, at, gun: true)?.Piece);
        var chunk = w.M.Debris.Pieces.Single(p => p.Id == 99);
        Assert.IsType<Grab>(Aiming.AimAt(w.M, 0, hero, ((double)chunk.X / Unit, (double)chunk.Y / Unit + 0.4), gun: true)?.Press?.Command);
    }

    // The grapple (0044): the rod grips walls, floors and closed doors.

    private static readonly Ship Gripped = HeroTests.ShipFrom([
        "###########",
        "#.H.d.D.=.#",
        "###########",
    ]);

    [Fact]
    public void GripsWallsFloorsAndClosedDoorsNotBackWallsOpenDoorsOrLadders()
    {
        // Back walls, a ladder and an open door: the rod flies through.
        Assert.Null(Aiming.Grapples(Gripped, (1_500, 1_500), (5_900, 1_500)));
        Assert.Equal((6_000.0 + Bite, 1_500.0), Aiming.Grapples(Gripped, (1_500, 1_500), (9_900, 1_500))?.At);
        Assert.Equal((8_000.0 + Bite, 1_500.0), Aiming.Grapples(Gripped, (7_500, 1_500), (8_900, 1_500))?.At);
        Assert.Equal((10_000.0 + Bite, 1_500.0), Aiming.Grapples(Gripped, (9_500, 1_500), (12_000, 1_500))?.At);
        // Going down and left into the bottom wall, the cell it enters first.
        var down = Aiming.Grapples(Gripped, (5_500, 1_500), (4_500, 2_500));
        Assert.Equal(0.5, down?.T);
        Assert.True(Aiming.Grips(Gripped, (6_500, 1_500)));
        Assert.False(Aiming.Grips(Gripped, (4_500, 1_500)));
        Assert.False(Aiming.Grips(Gripped, (-500, 1_500)));
    }

    [Fact]
    public void MeetsTheNearerOfAPieceAndTheShipsStructure()
    {
        var (w, _) = Field(("scrapChunk", 10_000, 7_500));
        Ship WallAt(int column) => new(FieldWidth, 14, cell => cell % FieldWidth == column ? Structure.Wall : Structure.Empty, _ => true);
        Assert.Equal(new RodHit(null, (8_000.0 + Bite, 7_500.0)), Aiming.RodMeets(w.M, WallAt(8), 0, (6_500, 7_500), (14_000, 7_500), 0));
        Assert.Equal(new RodHit(1, (10_000.0 - SmallPicture + Bite, 7_500.0)), Aiming.RodMeets(w.M, WallAt(12), 0, (6_500, 7_500), (14_000, 7_500), 0));
        Assert.Null(Aiming.RodMeets(w.M, WallAt(20), 0, (6_500, 0), (14_000, 0), 0));
    }
}
