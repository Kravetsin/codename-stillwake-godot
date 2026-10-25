using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Xunit;
using Spec = Stillwake.Content;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Tests;

/// <summary>Aim, reach and what the hands send: apps/game/src/features/crew/aim.test.ts, one for one.</summary>
public class AimTests
{
    private const int Unit = Hero.Unit, Block = HeroTests.Block;

    /// <summary>A hero whose body is centred on the middle of the block (x, y), a map character two cells square (0104).</summary>
    private static Hero HeroOn(int x, int y) => new(x * Block + Unit, y * Block + Unit + Hero.MiddleUp, 0, 0, false, 1);

    /// <summary>A hero whose body is centred on the middle of cell (x, y).</summary>
    private static Hero CellHero(int x, int y) => new(x * Unit + Unit / 2, y * Unit + Unit / 2 + Hero.MiddleUp, 0, 0, false, 1);

    /// <summary>The middle of the top-left cell of the block (x, y): a door's or a conduit's, as a map doubles them.</summary>
    private static (double X, double Y) Centre(int x, int y) => (2 * x + 0.5, 2 * y + 0.5);

    /// <summary>The world <paramref name="map"/> draws, a character a 16 px block.</summary>
    private static TestWorld Drawn(string map) => new(Layout.Parse(map).Doubled());

    private static readonly Placement IronWall = Placement.Wall("iron");

    /// <summary>A hall nine wide with rows enough inside for <paramref name="cells"/> blocks, seven to a row.</summary>
    private static string Hall(int cells) =>
        string.Join("\n", new[] { "#########" }.Concat(Enumerable.Repeat("#.......#", (cells + 6) / 7)).Append("#########"));

    /// <summary>The top-left cell of the <paramref name="i"/>-th block inside a hall, row by row.</summary>
    private static int Inside(int i) => 2 * (1 + i / 7) * 18 + 2 * (1 + i % 7);

    /// <summary><paramref name="map"/> with a crate drawn below it, and peer 0 carrying scrap, iron and tubes.</summary>
    private static TestWorld WithStore(string map) =>
        new TestWorld(Layout.Parse([.. Layout.Parse(map).Rows, "KK"]).Doubled()).Give(0, ("scrap", 360), ("iron", 60), ("tube", 20));

    // Aiming.

    private const string Corridor = """
        ########
        #..D-v-#
        ########
        """;

    /// <summary>The top-left cell of the corridor's block (x, y): sixteen cells wide.</summary>
    private static int At(int x, int y) => 2 * y * 16 + 2 * x;

    [Fact]
    public void OffersToOpenOrShutADoorOrAValveUnderThePointer()
    {
        var m = Drawn(Corridor).M;
        var hero = HeroOn(2, 1);
        Assert.Equal(new Aim(At(3, 1), true, new Use(new SetDoor(At(3, 1), true), null)), Aiming.AimAt(m, 0, hero, Centre(3, 1)));
        Assert.Null(Aiming.AimAt(m, 0, hero, Centre(4, 1))?.Press);
        Assert.Equal(new SetValve(At(5, 1), false), Aiming.AimAt(m, 0, HeroOn(4, 1), Centre(5, 1))?.Press?.Command);
    }

    [Fact]
    public void PressesADoorOrValveIntoTheOppositeOfHowThisPeerShowsItItsOwnSetsInFlightMade()
    {
        var m = new Registry();
        var client = new Client(World.Create(1, Layout.Parse(Corridor).Doubled(), m.All), new Fed(), 1);
        client.Send(new SetDoor(At(3, 1), true));
        client.Send(new SetValve(At(5, 1), false));
        var shown = Predicted.Grid(client, m.Grid, m.Pipes);
        var door = Aiming.AimAt(m, 0, HeroOn(2, 1), Centre(3, 1), shown: shown);
        Assert.Equal(new SetDoor(At(3, 1), false), door?.Press?.Command);
        Assert.Equal("use.closeDoor", Prompts.Of(m, door)?.Action);
        Assert.Equal(new SetValve(At(5, 1), true), Aiming.AimAt(m, 0, HeroOn(4, 1), Centre(5, 1), shown: shown)?.Press?.Command);
        // Without what this peer shows, as the world stands: its sets have not run yet.
        Assert.Equal(new SetDoor(At(3, 1), true), Aiming.AimAt(m, 0, HeroOn(2, 1), Centre(3, 1))?.Press?.Command);
    }

    [Fact]
    public void WorksTheDoorWhenAValveSharesItsCell()
    {
        var w = Drawn(Corridor);
        // A valve made on a joint in the door's cell, for free.
        w.Step(new SetCreative(true), new LayPipe(At(3, 1), At(4, 1)), new MakeValve(At(3, 1), true));
        Assert.NotNull(w.M.Pipes.ValveAt(At(3, 1)));
        Assert.Equal(new SetDoor(At(3, 1), true), Aiming.AimAt(w.M, 0, HeroOn(2, 1), Centre(3, 1))?.Press?.Command);
    }

    [Fact]
    public void PutsACanisterIntoATankOnAPressOrCarriesWhyItCannot()
    {
        var w = Drawn("""
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
        // A character a cell: the reach is measured to cells' middles.
        var open = new TestWorld(Layout.Parse([new string('.', 16), new string('.', 16), new string('.', 16)])).M;
        var hero = CellHero(1, 1);
        const int cellsAway = Spec.Hero.Reach / Unit;
        (double, double) Middle(int x, int y) => (x + 0.5, y + 0.5);
        Assert.True(Aiming.AimAt(open, 0, hero, Middle(1 + cellsAway, 1))?.InReach);
        Assert.False(Aiming.AimAt(open, 0, hero, Middle(2 + cellsAway, 1))?.InReach);
        Assert.False(Aiming.AimAt(open, 0, hero, Middle(1 + cellsAway, 2))?.InReach);
    }

    [Fact]
    public void WorksTheOldestPlanInACellAndLeavesETheDoorUnderIt()
    {
        var w = Drawn(Corridor);
        w.Step(new Deconstruct(At(3, 1), "structure"), new Deconstruct(At(3, 1), "back"));
        Assert.Equal(
            new Aim(At(3, 1), true, new Use(new SetDoor(At(3, 1), true), null), new WorkAim(1, null)),
            Aiming.AimAt(w.M, 0, HeroOn(2, 1), Centre(3, 1)));
    }

    [Fact]
    public void CarriesTheSimsReasonWhenEOnABlueprintWouldBeRefused()
    {
        // An iron wall planned for a hero carrying nothing.
        var w = Drawn(Hall(1));
        w.Step(new PlacePlan(Inside(0), IronWall));
        Assert.Equal(new WorkAim(1, "items.notEnough"), Aiming.AimAt(w.M, 0, HeroOn(2, 1), Centre(1, 1))?.Work);
    }

    [Fact]
    public void WorksAPlanItCanBeforeAnOlderOneItCannot()
    {
        // Nine wide, eighteen cells. A tank is planned, and the back wall behind its corner marked: two plans in that cell.
        var w = new TestWorld(Layout.Parse(["#########", "#.......#", "#.......#", "#########"]).Doubled());
        const int pipe = 2 * 18 + 12;
        w.Step(new PlacePlan(pipe, new Placement("machine", "tank")), new Deconstruct(pipe, "back"));
        var q = w.M.Building;
        Assert.Equal([(1, "items.notEnough"), (2, (string?)null)], q.PlansAt(pipe).Select(p => (p.Id, q.WorkProblem(p.Id, 0))));
        Assert.Equal(new WorkAim(2, null), Aiming.AimAt(w.M, 0, HeroOn(5, 1), Centre(6, 1))?.Work);
    }

    [Fact]
    public void KeepsWorkingABlueprintItHasPaidIntoNotPassingToTheNextInItsCell()
    {
        // A wall planned and the back wall behind it marked, in one cell, for a hero carrying scrap.
        var w = WithStore(Hall(1));
        w.Step(new PlacePlan(Inside(0), Placement.Wall("scrap")), new Deconstruct(Inside(0), "back"));
        var hero = HeroOn(2, 1);
        Assert.Equal(new WorkAim(1, null), Aiming.AimAt(w.M, 0, hero, Centre(1, 1))?.Work);
        // Its first part paid and begun (0063): held on, the hands work the wall on.
        w.Step(new Work(1));
        Assert.Equal(1, w.M.Building.Parts(1)?.In);
        Assert.Equal(new WorkAim(1, null), Aiming.AimAt(w.M, 0, hero, Centre(1, 1))?.Work);
    }

    [Fact]
    public void AimsAtNothingOffTheGrid()
    {
        var m = Drawn(Corridor).M;
        var hero = HeroOn(2, 1);
        Assert.Null(Aiming.AimAt(m, 0, hero, (-0.5, 1)));
        Assert.Null(Aiming.AimAt(m, 0, hero, (4, 6.4)));
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
        // At the door, two cells thick, its block whole (0104).
        Assert.Equal(46 * Block + Spec.Hero.Width / 2, h.X);
        Use(h, 45, 22);
        h = Run(h, HeroTests.Left, 900);
        // Into room B, torn open to space, and out past its torn edge as far as the tether lets the hero.
        // The tether cuts a move back to its length rounded toward its anchor: within a thousandth of a cell.
        var tied = h.Anchor!.Value;
        Assert.InRange(tied.X, HeroTests.TornEdge, HeroTests.TornEdge + Spec.Hero.Walk);
        Assert.InRange(h.X, tied.X - Spec.Hero.Tether, tied.X - Spec.Hero.Tether + 1);

        // Back the other way, the airlock's inner doorway stops the hero, until opened.
        h = Run(h, HeroTests.Right, 1200);
        Assert.Equal(54 * Block - Spec.Hero.Width / 2, h.X);
        Use(h, 54, 22);
        h = Run(h, HeroTests.Right, 60);
        Assert.Equal(57 * Block - Spec.Hero.Width / 2, h.X);
    }

    [Fact]
    public void LetsTheHeroThroughADoorItsOwnToggleOpenedBeforeTheHostsTickRunsIt()
    {
        var m = new Registry();
        var (_, client) = LocalSession.Create(World.Create(1, HeroTests.Wreck, m.All));
        Ship ShipNow() => Ship.Of(m.Grid, m.Rooms, cell => Predicted.Grid(client, m.Grid, m.Pipes).StructureAt(m.Grid, cell));
        var h = Hero.At(ShipNow(), HeroTests.Spawn);
        for (int i = 0; i < 120; i++) h = h.Step(HeroTests.Left, ShipNow());
        if (Aiming.AimAt(m, 0, h, Centre(45, 22))?.Press is { } press) client.Send(press.Command);
        // Sent, not released: the world still has the doorway shut.
        Assert.Contains(Predicted.Grid(client, m.Grid, m.Pipes).Structure!, shown => shown.Value != m.Grid.StructureAt(shown.Key));
        for (int i = 0; i < 900; i++) h = h.Step(HeroTests.Left, ShipNow());
        // Into room B, torn open to space, and out past its torn edge as far as the tether lets the hero.
        // The tether cuts a move back to its length rounded toward its anchor: within a thousandth of a cell.
        var tied = h.Anchor!.Value;
        Assert.InRange(tied.X, HeroTests.TornEdge, HeroTests.TornEdge + Spec.Hero.Walk);
        Assert.InRange(h.X, tied.X - Spec.Hero.Tether, tied.X - Spec.Hero.Tether + 1);
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
        // The crate sits on the row below the room's floor, from column 0: its block's row, its cell its top left.
        int row = crate.Cell / Room.M.Grid.Width / 2;
        var aim = Aiming.AimAt(Room.M, 0, HeroOn(2, row - 1), Centre(1, row));
        Assert.Equal(new Aim(crate.Cell + 2, true, Crate: crate.Id), aim);
        var opened = Aiming.UsesOf(aim, pressed: true, held: true, 3, -1);
        Assert.Equal(((int?)crate.Id, -1), (opened.Open, opened.WorkedOnTick));
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
        // Its right column, the fourth cell, the reach's cells off.
        Assert.True(Aiming.CrateInReach(Room.M.Grid, CellHero(3 + reach, row), crate.Id));
        Assert.False(Aiming.CrateInReach(Room.M.Grid, CellHero(4 + reach, row), crate.Id));
        Assert.False(Aiming.CrateInReach(Room.M.Grid, CellHero(1, row), crate.Id + 1));
    }

    // A piece of debris under the pointer.

    /// <summary>A hull of one block in the corner, for the debris to fly past, run until the first piece is out: 24 cells by 16.</summary>
    private static TestWorld Corner()
    {
        var w = new TestWorld(Layout.Parse(["#".PadRight(12), .. Enumerable.Repeat(new string(' ', 12), 7)]).Doubled());
        for (int i = 0; i < Spec.Debris.Every; i++) w.Step();
        return w;
    }

    /// <summary>The middle of cell (x, y).</summary>
    private static (double X, double Y) MiddleOf(int x, int y) => (x + 0.5, y + 0.5);

    [Fact]
    public void IsGrabbedOnAPressOrCarriesWhyTheSimWouldRefuseIt()
    {
        var w = Corner();
        var piece = w.M.Debris.Pieces[0];
        int x = piece.X / Unit, y = piece.Y / Unit;
        var grab = new Grab(piece.Id);
        // The sim knows no hero outside yet.
        Assert.Equal(new Use(grab, "debris.notOutside"), Aiming.AimAt(w.M, 0, CellHero(x - 4, y), MiddleOf(x, y))?.Press);
        w.Step(new Sim.Crew.Enter(y * 24 + x - 4));
        var aim = Aiming.AimAt(w.M, 0, CellHero(x - 4, y), MiddleOf(x, y));
        Assert.Equal(new Use(grab, null), aim?.Press);
        Assert.Equal(piece.Id, aim?.Piece);
        Assert.Equal([grab], Aiming.UsesOf(aim, pressed: true, held: true, 3, -1).Commands);
        // Grabbed, its grab in flight: the aim passes over it, so E cannot send it twice.
        var passed = Aiming.AimAt(w.M, 0, CellHero(x - 4, y), MiddleOf(x, y), new HashSet<int> { piece.Id });
        Assert.Equal((null, null), (passed?.Piece, passed?.Press));
    }

    [Fact]
    public void IsAimedAtFromBesideItWithinARadiusOfThePointerNotFromFarther()
    {
        var w = Corner();
        var piece = w.M.Debris.Pieces[0];
        var hero = CellHero(piece.X / Unit - 4, piece.Y / Unit);
        // Off to its left, inside the grid: it came in from the right edge.
        (double, double) Off(int by) => ((double)(piece.X - by) / Unit, (double)piece.Y / Unit);
        Assert.Equal(piece.Id, Aiming.AimAt(w.M, 0, hero, Off(Spec.Debris.AimRadius - 200))?.Piece);
        Assert.Null(Aiming.AimAt(w.M, 0, hero, Off(Spec.Debris.AimRadius + 200))?.Piece);
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
    /// The corner's first piece made a hull section, a scrap chunk two cells below its middle, and peer 0's
    /// hero reported out in space four cells left of it, its blade's reach: the wreck's middle and the hero.
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
            chunk["y"] = (int)first["y"]! + Block;
            pieces.Add(chunk);
        });
        var wreck = w.M.Debris.Pieces[0];
        int x = wreck.X / Unit, y = wreck.Y / Unit;
        w.Step(new Sim.Crew.Enter(y * 24 + x - 4));
        return (w, w.M.Debris.Pieces[0], CellHero(x - 4, y));
    }

    [Fact]
    public void AimsAtAWreckAnywhereOnItsPictureOverASmallPieceNearerThePointer()
    {
        var (w, wreck, hero) = Wrecked();
        var (pw, ph) = DebrisLooks.Wrecks["hullSection"];
        // Near its bottom left corner: the chunk below its middle is nearer, the wreck still wins.
        double x = (double)wreck.X / Unit - pw / (double)Camera.Cell / 2 + 0.4, y = (double)wreck.Y / Unit + ph / (double)Camera.Cell / 2 - 0.4;
        Assert.Equal(wreck.Id, Aiming.AimAt(w.M, 0, hero, (x, y))?.Piece);
        // Off its picture, the chunk below.
        Assert.Equal(99, Aiming.AimAt(w.M, 0, hero, ((double)wreck.X / Unit, (double)wreck.Y / Unit + ph / (double)Camera.Cell / 2 + 1.2))?.Piece);
    }

    [Fact]
    public void ChopsAWreckWithTheAxeAndBareHandedTriesToCatchItWhichIsTooBig()
    {
        var (w, wreck, hero) = Wrecked();
        var at = ((double)wreck.X / Unit, (double)wreck.Y / Unit);
        Assert.Equal(new Use(new Grab(wreck.Id), "debris.tooBig"), Aiming.AimAt(w.M, 0, hero, at)?.Press);
        var axe = Aiming.AimAt(w.M, 0, hero, at, axe: true);
        var fan = Aiming.FanOf(w.M, hero, Aiming.PointOf(at), Spec.Weapons.Axe.Item);
        Assert.Equal([wreck], fan.Pieces);
        Assert.Equal(new Use(fan.Wrecks, null), axe?.Press);
        Assert.Equal(new Prompt("use.chop", null), Prompts.Of(w.M, axe));
        // A press never chops: the left button held does, at the axe's pace.
        Assert.Empty(Aiming.UsesOf(axe, pressed: true, held: true, 5, -1).Commands);
        // The axe in hand, a small piece is still caught.
        var chunk = w.M.Debris.Pieces.Single(p => p.Id == 99);
        Assert.IsType<Grab>(Aiming.AimAt(w.M, 0, hero, ((double)chunk.X / Unit, (double)chunk.Y / Unit + 0.8), axe: true)?.Press?.Command);
    }

    [Fact]
    public void ReachesAWreckBareHandedByItsNearestEdgeNotItsMiddle()
    {
        var (w, wreck, _) = Wrecked();
        int halfW = DebrisLooks.Wrecks["hullSection"].W * Unit / (2 * Camera.Cell);
        int y = wreck.Y / Unit;
        // A hero whose reach just passes its left edge, though its middle is farther.
        var near = new Hero(wreck.X - halfW - Spec.Hero.Reach + 200, wreck.Y + Hero.MiddleUp, 0, 0, false, 1);
        var far = near with { X = near.X - 1_200 };
        var at = ((double)wreck.X / Unit, (double)y + 0.5);
        Assert.True(Aiming.AimAt(w.M, 0, near, at)!.InReach);
        Assert.False(Aiming.AimAt(w.M, 0, far, at)!.InReach);
    }

    // A tool's fan in space (the fan of rays): the first wreck each ray meets, as the sim's query says.

    private const int FieldWidth = 24;

    /// <summary>
    /// Open space <see cref="FieldWidth"/> by 14 blocks, peer 0's hero out in it on block (6, 7) facing right,
    /// and still pieces of <paramref name="items"/> with their middles where they say, in thousandths of a
    /// cell, ids from 1 in that order.
    /// </summary>
    private static (TestWorld W, Hero Hero) Field(params (string Item, int X, int Y)[] items)
    {
        var w = new TestWorld(Layout.Parse(["#".PadRight(FieldWidth), .. Enumerable.Repeat(new string(' ', FieldWidth), 13)]).Doubled());
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
        w.Step(new Sim.Crew.Enter(15 * 2 * FieldWidth + 13));
        return (w, HeroOn(6, 7));
    }

    /// <summary>The hand a hero's fan is cast from (<see cref="Aiming.HandOf"/>), as the figure's shoulder upright.</summary>
    private static Point HandOf(Hero hero) => Aiming.HandOf(hero);

    /// <summary>Half a wreck's own size across, in thousandths of a cell, from content.</summary>
    private static int HalfW(string kind) => Spec.Wreckage.SizeOf(kind)!.Value.W * Unit / 2;

    private static List<int> Struck(TestWorld w, Hero hero, Point at, string? tool = null) =>
        Aiming.FanOf(w.M, hero, at, tool ?? Spec.Weapons.Axe.Item).Pieces.Select(p => p.Id).ToList();

    [Fact]
    public void TheFanCastsFromTheShoulderAndAimsAtThePointer()
    {
        var hero = HeroOn(6, 7);
        Assert.Equal(new Point(hero.X, hero.Y - (int)(Rig.Shoulder / Rig.Cell * Unit)), HandOf(hero));
        var hand = HandOf(hero);
        Assert.Equal(0, Aiming.HeadingOf(hand, hand with { X = hand.X + 5_000 }));
        Assert.Equal(64, Aiming.HeadingOf(hand, hand with { Y = hand.Y + 5_000 }));
        Assert.Equal(128, Aiming.HeadingOf(hand, hand with { X = hand.X - 5_000 }));
        Assert.Equal(192, Aiming.HeadingOf(hand, hand with { Y = hand.Y - 5_000 }));
        Assert.Equal(32, Aiming.HeadingOf(hand, new Point(hand.X + 3_000, hand.Y + 3_000)));
    }

    /// <summary>Every kind of wreckage: each struck within the axe's range by its own size, and not past it.</summary>
    public static TheoryData<string> WreckKinds() => [.. Spec.Wreckage.All.Select(k => k.Item)];

    [Theory]
    [MemberData(nameof(WreckKinds))]
    public void TheAxesFanStrikesEveryKindOfWreckWithinItsRangeAndNoFarther(string kind)
    {
        var hand = HandOf(HeroOn(6, 7));
        int range = Spec.Weapons.Axe.Range, half = HalfW(kind);
        var far = hand with { X = hand.X + 10_000 };
        // Its near side a cell inside the range ahead, and a cell past it.
        var (near, hero) = Field((kind, hand.X + range - 1_000 + half, hand.Y));
        Assert.Equal([1], Struck(near, hero, far));
        var (beyond, _) = Field((kind, hand.X + range + 1_000 + half, hand.Y));
        Assert.Empty(Struck(beyond, hero, far));
        // Bare-handed, the pointer on its picture's far top corner aims at it, and just off it does not.
        var (pw, ph) = DebrisLooks.Wrecks[kind];
        var (w, _) = Field((kind, 29_000, 15_000));
        double right = 29 + pw / (2.0 * Camera.Cell), top = 15 - ph / (2.0 * Camera.Cell);
        Assert.Equal(1, Aiming.AimAt(w.M, 0, hero, (right - 0.2, top + 0.2))?.Piece);
        Assert.Null(Aiming.AimAt(w.M, 0, hero, (right + 0.8, top - 0.8))?.Piece);
    }

    [Fact]
    public void MissesAWreckBehindTheHeroOrOutOfRangeThoughThePointerIsOnIt()
    {
        var hand = HandOf(HeroOn(6, 7));
        var (behind, hero) = Field(("hullSection", hand.X - 1_000 - HalfW("hullSection"), hand.Y));
        Assert.Empty(Struck(behind, hero, hand with { X = hand.X + 6_000 }));
        Assert.Equal([1], Struck(behind, hero, hand with { X = hand.X - 6_000 }));
        var on = new Point(hand.X + 8_000 + HalfW("hullSection"), hand.Y);
        var (w, _) = Field(("hullSection", on.X, on.Y));
        Assert.Empty(Struck(w, hero, on));
        // Nothing aimed at, no prompt: the fan would cut air.
        var aim = Aiming.AimAt(w.M, 0, hero, ((double)on.X / Unit, (double)on.Y / Unit), axe: true);
        Assert.Null(aim?.Piece);
        Assert.Null(Prompts.Of(w.M, aim));
        Assert.True(aim?.Fan?.IsEmpty);
    }

    [Fact]
    public void StrikesEachWreckItsRaysMeetOnceAWreckBehindAnotherNotAndNeverASmallPiece()
    {
        var hand = HandOf(HeroOn(6, 7));
        // Two wrecks up and down the fan, a scrap chunk on the line to the pointer, and a rack behind the upper one.
        var (w, hero) = Field(
            ("cableRack", hand.X + 3_000, hand.Y - 2_000),
            ("scrapChunk", hand.X + 1_400, hand.Y),
            ("cableRack", hand.X + 3_000, hand.Y + 2_500),
            ("cableRack", hand.X + 7_000, hand.Y - 2_000));
        var ahead = hand with { X = hand.X + 6_000 };
        Assert.Equal([1, 3], Struck(w, hero, ahead));
        // Aimed at, the first of them is outlined, with the axe's prompt.
        var aim = Aiming.AimAt(w.M, 0, hero, ((double)ahead.X / Unit, (double)ahead.Y / Unit), axe: true);
        Assert.Equal((1, true), (aim?.Piece, aim?.InReach));
        Assert.Equal(new Prompt("use.chop", null), Prompts.Of(w.M, aim));
    }

    [Fact]
    public void StrikesNoWreckAboard()
    {
        // The hero in a room, a wreck just outside its wall: nothing is struck from inside the hull.
        var w = new TestWorld(Layout.Parse(["#####".PadRight(FieldWidth), "#...#".PadRight(FieldWidth), "#...#".PadRight(FieldWidth), "#####".PadRight(FieldWidth), .. Enumerable.Repeat(new string(' ', FieldWidth), 6)]).Doubled());
        for (int i = 0; i < Spec.Debris.Every; i++) w.Step();
        var aboard = HeroOn(3, 2);
        var hand = HandOf(aboard);
        w.Edit(root =>
        {
            var piece = root["slices"]!["debris"]!["pieces"]![0]!;
            (piece["item"], piece["x"], piece["y"], piece["speed"]) = ("hullSection", hand.X + 2_000 + HalfW("hullSection"), hand.Y, 0);
        });
        Assert.Empty(Struck(w, aboard, hand with { X = hand.X + 6_000 }));
        Assert.Null(Aiming.AimAt(w.M, 0, aboard, (12.0, (double)hand.Y / Unit), axe: true)?.Piece);
    }

    [Fact]
    public void TheDrillsBeamReachesFartherAndSaysWhenTheBeltIsFlat()
    {
        var hand = HandOf(HeroOn(6, 7));
        string drill = Spec.Weapons.Drill.Item;
        var (w, hero) = Field(("asteroid", hand.X + 5_300 + HalfW("asteroid"), hand.Y));
        var at = ((double)hand.X / Unit + 10, (double)hand.Y / Unit);
        // Past the axe's range, within the drill's.
        Assert.Empty(Struck(w, hero, Aiming.PointOf(at)));
        Assert.Equal([1], Struck(w, hero, Aiming.PointOf(at), drill));
        var aim = Aiming.AimAt(w.M, 0, hero, at, drill: drill);
        var fan = Aiming.FanOf(w.M, hero, Aiming.PointOf(at), drill);
        Assert.Equal(new Use(fan.Wrecks, "items.flat"), aim?.Press);
        Assert.Equal(1, aim?.Piece);
        Assert.Equal(new Prompt("use.chop", "items.flat"), Prompts.Of(w.M, aim));
        // In creative mode it needs no battery.
        w.Step(new SetCreative(true));
        Assert.Equal(new Use(fan.Wrecks, null), Aiming.AimAt(w.M, 0, hero, at, drill: drill)?.Press);
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
        var (w, hero) = Field(("hullSection", 37_000, 7_000));
        var swing = new AxeSwing();
        var point = new Point(18_000, 18_000);
        int begun = 0, strikes = 0;
        var fans = new List<Fan>();
        for (int frame = 0; frame < 60; frame++)
        {
            var step = swing.Advance(1000.0 / 60, frame == 0, point);
            begun += step.Begun ? 1 : 0;
            if (!step.Hits) continue;
            strikes++;
            fans.Add(Aiming.FanOf(w.M, hero, swing.At, Spec.Weapons.Axe.Item));
        }
        Assert.Equal((1, 1), (begun, strikes));
        Assert.True(Assert.Single(fans).IsEmpty);
    }

    // What E sends.

    private static readonly SetDoor Door = new(3, true);
    private static readonly Aim OnDoor = new(3, true, new Use(Door, null));
    private static readonly Aim OnPlan = new(4, true, Work: new WorkAim(7, null));
    private static readonly Work WorkOn = new(7);

    [Fact]
    public void OpensADoorOnceOnAPressAndNothingWhileEStaysHeld()
    {
        Assert.Equal([Door], Aiming.UsesOf(OnDoor, pressed: true, held: true, 10, -1).Commands);
        Assert.Empty(Aiming.UsesOf(OnDoor, pressed: false, held: true, 11, -1).Commands);
    }

    [Fact]
    public void SendsOneWorkForEveryTickReleasedWhileEIsHeldOnAPlan()
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
    private const int SmallPicture = 1_000, SmallBox = 1_500, Bite = 300;

    [Fact]
    public void HooksTheNearestPieceTheRodMeetsNotTheFirstSpawned()
    {
        var (w, _) = Field(("hullSection", 24_000, 15_000), ("scrapChunk", 20_000, 15_000));
        Assert.Equal(2, Aiming.Hooks(w.M, 0, (13_000, 15_000), (28_000, 15_000), 0)?.Piece);
        Assert.Equal(1, Aiming.Hooks(w.M, 0, (28_000, 15_000), (13_000, 15_000), 0)?.Piece);
        var (section, _) = Field(("hullSection", 24_000, 15_000));
        // It enters the section's box at its margin and sticks just inside its picture.
        Assert.Equal((24_000.0 - HalfW("hullSection") + Bite, 15_000.0), Aiming.Hooks(section.M, 0, (13_000, 15_000), (28_000, 15_000), 0)?.At);
    }

    [Fact]
    public void HooksASmallPieceInABoxOfItsPictureAndAMarginStickingOnThePicture()
    {
        var (w, _) = Field(("scrapChunk", 20_000, 15_000));
        Assert.Equal((20_000.0 - SmallPicture + Bite, 15_000.0), Aiming.Hooks(w.M, 0, (13_000, 15_000), (28_000, 15_000), 0)?.At);
        // Passing just inside the margin above it, or stopping just short of it.
        Assert.Equal(1, Aiming.Hooks(w.M, 0, (13_000, 15_000 - SmallBox + 20), (28_000, 15_000 - SmallBox + 20), 0)?.Piece);
        Assert.Null(Aiming.Hooks(w.M, 0, (13_000, 15_000 - SmallBox - 20), (28_000, 15_000 - SmallBox - 20), 0));
        Assert.Null(Aiming.Hooks(w.M, 0, (13_000, 15_000), (20_000 - SmallBox - 20, 15_000), 0));
    }

    [Fact]
    public void PassesOverAPieceTheSimWouldNotLetTheHeroReel()
    {
        // The first lies over the hull's one block: not in space. The second is.
        var (w, _) = Field(("scrapChunk", 1_000, 1_000), ("scrapChunk", 6_000, 1_000));
        Assert.Equal(2, Aiming.Hooks(w.M, 0, (12_000, 1_000), (0, 1_000), 0)?.Piece);
        Assert.Equal("debris.notOutside", w.M.Debris.ReelProblem(0, 1));
        // A peer with no hero out reels nothing.
        Assert.Null(Aiming.Hooks(w.M, 5, (12_000, 1_000), (0, 1_000), 0));
    }

    [Fact]
    public void MeetsAPieceWhereItIsDrawnBetweenTicksReeledOrDrifting()
    {
        var (w, _) = Field(("scrapChunk", 20_000, 15_000));
        // Reeled 8000 to the left: 10 ticks at 800, one of them run with the reel.
        const int Reels = Stillwake.Content.Harpoon.ReelSpeed;
        w.Step(new Reel(1, 12_000, 15_000));
        var piece = w.M.Debris.Pieces.Single();
        Assert.Equal((20_000 - Reels, 8_000 / Reels - 1), (piece.X, piece.Reel!.Left));
        Assert.Equal((20_000 - Reels * 1.5, 15_000.0), PieceDrawn.At(piece, 0.5));
        // Its box drawn halfway reaches half a tick's reel further left than at the tick: a rod passing there meets it then only.
        const int Edge = 20_000 - Reels * 3 / 2 - SmallBox + 100;
        Assert.Equal(1, Aiming.Hooks(w.M, 0, (Edge, 0), (Edge, 20_000), 0.5)?.Piece);
        Assert.Null(Aiming.Hooks(w.M, 0, (Edge, 0), (Edge, 20_000), 0));
    }

    [Fact]
    public void WithTheHarpoonAimsAtNoWreckButStillCatchesASmallPiece()
    {
        var (w, wreck, hero) = Wrecked();
        var at = ((double)wreck.X / Unit, (double)wreck.Y / Unit);
        Assert.Null(Aiming.AimAt(w.M, 0, hero, at, gun: true)?.Piece);
        var chunk = w.M.Debris.Pieces.Single(p => p.Id == 99);
        Assert.IsType<Grab>(Aiming.AimAt(w.M, 0, hero, ((double)chunk.X / Unit, (double)chunk.Y / Unit + 0.8), gun: true)?.Press?.Command);
    }

    // The grapple (0044): the rod grips walls, floors and closed doors.

    /// <summary>A corridor of back walls, a ladder, an open and a closed door, a floor: a character a block, its middle row the top cells' (0104).</summary>
    private static readonly Ship Gripped = HeroTests.ShipFrom([
        "###########",
        "#.H.d.D.=.#",
        "###########",
    ]);

    [Fact]
    public void GripsWallsFloorsAndClosedDoorsNotBackWallsOpenDoorsOrLadders()
    {
        // Back walls, a ladder and an open door: the rod flies through.
        Assert.Null(Aiming.Grapples(Gripped, (3_000, 2_500), (11_800, 2_500)));
        Assert.Equal((12_000.0 + Bite, 2_500.0), Aiming.Grapples(Gripped, (3_000, 2_500), (19_800, 2_500))?.At);
        Assert.Equal((16_000.0 + Bite, 2_500.0), Aiming.Grapples(Gripped, (15_000, 2_500), (17_800, 2_500))?.At);
        Assert.Equal((20_000.0 + Bite, 2_500.0), Aiming.Grapples(Gripped, (19_000, 2_500), (24_000, 2_500))?.At);
        // Going down and left into the bottom wall, the cell it enters first.
        var down = Aiming.Grapples(Gripped, (11_000, 3_000), (9_000, 5_000));
        Assert.Equal(0.5, down?.T);
        Assert.True(Aiming.Grips(Gripped, (12_500, 2_500)));
        Assert.False(Aiming.Grips(Gripped, (8_500, 2_500)));
        Assert.False(Aiming.Grips(Gripped, (-1_000, 2_500)));
    }

    [Fact]
    public void MeetsTheNearerOfAPieceAndTheShipsStructure()
    {
        var (w, _) = Field(("scrapChunk", 20_000, 15_000));
        Ship WallAt(int column) => new(2 * FieldWidth, 28, cell => cell % (2 * FieldWidth) == column ? Structure.Wall : Structure.Empty, _ => true);
        Assert.Equal(new RodHit(null, (16_000.0 + Bite, 15_000.0)), Aiming.RodMeets(w.M, WallAt(16), 0, (13_000, 15_000), (28_000, 15_000), 0));
        Assert.Equal(new RodHit(1, (20_000.0 - SmallPicture + Bite, 15_000.0)), Aiming.RodMeets(w.M, WallAt(24), 0, (13_000, 15_000), (28_000, 15_000), 0));
        Assert.Null(Aiming.RodMeets(w.M, WallAt(40), 0, (13_000, 0), (28_000, 0), 0));
    }
}
