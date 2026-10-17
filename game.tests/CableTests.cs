using Stillwake.Content;
using Stillwake.Game.Features.Power;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A cable as drawn: apps/game/src/render/cable.test.ts, one for one.</summary>
public class CableShapeTests
{
    [Fact]
    public void HangsBelowTheLineBetweenItsEndsInsideDeepestInTheMiddleAndIsStraightOutside()
    {
        var hanging = CableShape.CablePoints((0, 0), (10, 0), true);
        Assert.Equal((0.0, 0.0), hanging[0]);
        Assert.Equal((10.0, 0.0), hanging[^1]);
        var middle = hanging[hanging.Count >> 1];
        Assert.True(middle.Y > 1);
        Assert.Equal(middle.Y, hanging.Max(p => p.Y));
        Assert.All(CableShape.CablePoints((0, 0), (10, 0), false), p => Assert.Equal(0, p.Y));
    }

    [Fact]
    public void MeasuresHowFarAPointIsFromIt()
    {
        var line = CableShape.CablePoints((0, 0), (10, 0), false);
        Assert.Equal(0.3, CableShape.DistanceTo(line, (5, 0.3)), 6);
        Assert.Equal(2, CableShape.DistanceTo(line, (12, 0)), 6);
    }

    [Fact]
    public void MeetsAMachineAtItsSocketAnAnchorAtItsMiddle()
    {
        var w = new TestWorld(Layout.Parse(["#GG.....", ".GG....."]));
        // The generator covers columns 1–2, rows 0–1; its top-left names it.
        var plug = Machines.Get("generator").Plug!.Value;
        Assert.Equal((1 + plug.X / 16.0, plug.Y / 16.0), CableShape.EndOf(w.M.Grid, 1));
        Assert.Equal((0.5, 0.5), CableShape.EndOf(w.M.Grid, 0));
    }

    // A panel out in space over a room's roof, a lamp under the roof inside; the roof is row 2.
    private static readonly string[] Roofed = ["PPP     ", "        ", "########", "#..L...#", "#......#", "########"];

    private static TestWorld With(string[] rows, ((int, int), (int, int)) cable) => new(Layout.Parse(rows, null, [cable]));

    [Fact]
    public void IsHeldWhereItGoesThroughFloatingOutsideAndHangingAboard()
    {
        var w = With(Roofed, ((0, 0), (3, 3)));
        var (pieces, feeds) = CableShape.PiecesOf(w.M.Grid, w.M.Rooms, 0, 3 * 8 + 3);
        Assert.Equal([false, true], pieces.Select(p => p.Hangs));
        Assert.Single(feeds);
        // In the roof's row, where the pieces meet.
        Assert.Equal(2, (int)Math.Floor(feeds[0].Y));
        Assert.Equal(feeds[0], pieces[0].To);
        Assert.Equal(feeds[0], pieces[1].From);
    }

    [Fact]
    public void IsOneHangingPieceBetweenTwoThingsAboard()
    {
        var w = With(["########", "#.L..L.#", "#......#", "########"], ((2, 1), (5, 1)));
        var (pieces, feeds) = CableShape.PiecesOf(w.M.Grid, w.M.Rooms, 8 + 2, 8 + 5);
        Assert.True(Assert.Single(pieces).Hangs);
        Assert.Empty(feeds);
    }
}

/// <summary>Cables as ropes: apps/game/src/features/power/ropes.test.ts, one for one.</summary>
public class CableRopesTests
{
    /// <summary>A room with a lamp in each top corner and a cable between them, hanging across the room.</summary>
    private static readonly string[] Rows = ["##########", "#L......L#", "#........#", "#........#", "##########"];
    private static readonly TestWorld Sim = new(Layout.Parse(Rows, [], [((1, 1), (8, 1))]));
    private static readonly List<(double X, double Y)> Rest = CableShape.ShapeOf(Sim.M.Grid, Sim.M.Rooms, 10 + 1, 10 + 8);

    private static void Sync(CableRopes ropes, TestWorld w) => ropes.Sync(w.M.Grid, w.M.Rooms, w.M.Power.Cables);

    private static void Run(CableRopes ropes, double seconds, IReadOnlyList<Body>? bodies = null)
    {
        for (int i = 0; i < seconds * 60; i++) ropes.Update(Rope.StepMs, bodies ?? []);
    }

    private static IReadOnlyList<(double X, double Y)> PointsOf(CableRopes ropes) => ropes.Lines().FirstOrDefault()?.Points ?? [];

    /// <summary>How far the cable is from its shape at rest, at its farthest point.</summary>
    private static double OffRest(CableRopes ropes) =>
        PointsOf(ropes).Select((p, i) => Hypot(p.X - Rest[i].X, p.Y - Rest[i].Y)).Max();

    private static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);

    private static void Near((double X, double Y) expected, (double X, double Y) actual)
    {
        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
    }

    [Fact]
    public void HangAtRestFromTheStartWhereTheBuildingLayerPicksThem()
    {
        var ropes = new CableRopes();
        Sync(ropes, Sim);
        Run(ropes, 3);
        Assert.True(OffRest(ropes) < 0.15);
    }

    [Fact]
    public void LaidLaterFallFromStraightIntoTheSameHang()
    {
        var ropes = new CableRopes();
        Sync(ropes, new TestWorld(Layout.Parse(Rows)));
        Sync(ropes, Sim);
        Assert.True(OffRest(ropes) > 0.5);
        Run(ropes, 10);
        Assert.True(OffRest(ropes) < 0.15);
    }

    [Fact]
    public void SwayAsAHeroWalksThroughThemAndComeBackToRest()
    {
        var ropes = new CableRopes();
        Sync(ropes, Sim);
        var middle = Rest[Rest.Count >> 1];
        double step = Hero.Walk / 1000.0;
        double moved = 0;
        // Across the room at walking speed, the body's middle at the cable's height.
        for (double x = 1; x < 9; x += step)
        {
            var hero = new Body(x - 0.475, x + 0.475, middle.Y - 1.3, middle.Y + 1.3, step, 0);
            ropes.Update(Rope.StepMs, [hero]);
            moved = Math.Max(moved, OffRest(ropes));
        }
        Assert.True(moved > 0.05);
        Run(ropes, 20);
        Assert.True(OffRest(ropes) < 0.15);
    }

    [Fact]
    public void AreLeftAloneByAHeroStandingInThem()
    {
        var (alone, beside) = (new CableRopes(), new CableRopes());
        Sync(alone, Sim);
        Sync(beside, Sim);
        var middle = Rest[Rest.Count >> 1];
        var hero = new Body(middle.X - 0.5, middle.X + 0.5, middle.Y - 1, middle.Y + 1, 0, 0);
        Run(alone, 2);
        Run(beside, 2, [hero]);
        Assert.Equal(PointsOf(alone), PointsOf(beside));
    }

    [Fact]
    public void FollowAHandThatCarriesAnEndAndGoBackToTheirNodeOnceLetGo()
    {
        var ropes = new CableRopes();
        Sync(ropes, Sim);
        var hand = (4.5, 3.5);
        ropes.Hold(new Held(10 + 8, null, hand));
        Run(ropes, 2);
        Near(hand, PointsOf(ropes)[^1]);
        Near(Rest[0], PointsOf(ropes)[0]);
        ropes.Hold(null);
        Run(ropes, 10);
        Assert.True(OffRest(ropes) < 0.15);
    }

    [Fact]
    public void CarriedToAnotherNodeWaitWhereLetGoUntilTheWorldMovesThemThenHangThere()
    {
        // The cable ran from the lamp at 11 to the wall at 28; let go on the lamp at 18, it stays there.
        var before = new TestWorld(Layout.Parse(Rows, [], [((1, 1), (8, 2))]));
        var ropes = new CableRopes();
        Sync(ropes, before);
        Run(ropes, 2);
        var lamp = Rest[^1];
        ropes.Land(10 + 8, new Held(20 + 8, null, lamp));
        Run(ropes, 1);
        Near(lamp, PointsOf(ropes)[^1]);
        // The world has it there: it goes on as the same rope, not a jump to another shape, and hangs.
        var carried = PointsOf(ropes).ToList();
        Sync(ropes, Sim);
        Assert.Equal(carried, PointsOf(ropes));
        Run(ropes, 10);
        Assert.True(OffRest(ropes) < 0.15);
    }

    [Fact]
    public void LetGoOnANodeTheWorldNeverMovesThemToGoBackToWhereTheyWere()
    {
        var ropes = new CableRopes();
        Sync(ropes, Sim);
        Run(ropes, 2);
        var away = (4.5, 3.5);
        ropes.Land(20 + 4, new Held(10 + 8, null, away));
        Run(ropes, 1);
        Near(away, PointsOf(ropes)[^1]);
        Run(ropes, 10);
        Assert.True(OffRest(ropes) < 0.15);
    }

    [Fact]
    public void LaidByHandAreDrawnAsTheRopeTheyWereLaidAsAndGoOnAsItOnceTheWorldHasThem()
    {
        var ropes = new CableRopes();
        Sync(ropes, new TestWorld(Layout.Parse(Rows)));
        var from = Rest[0];
        var to = Rest[^1];
        // Laid from the far lamp, 18, to the near one, 11: the rope runs the other way to the cable.
        var rope = CableRopes.LayingRope(to);
        for (int i = 0; i < 60; i++) rope.Update(to, from, Rope.StepMs, CableRopes.HangFor);
        ropes.Adopt(new Laid(10 + 8, 10 + 1, to, from, rope));
        Assert.Equal([(18, 11)], ropes.Lines().Select(l => (l.A, l.B)));
        var laid = rope.Points.Select(k => (k.X, k.Y)).Reverse().ToList();
        Sync(ropes, Sim);
        Assert.Equal(laid, PointsOf(ropes));
        Run(ropes, 10);
        Assert.True(OffRest(ropes) < 0.15);
    }

    // Beyond ropes.test.ts: what a screen faster than the ropes' 60 steps a second shows, and cables through the hull.

    [Fact]
    public void MoveOnEveryFrameAtAnyRefreshRateNotOnceAStep()
    {
        // Laid later, the cable falls into its hang: drawn 240 times a second, every frame shows it lower.
        var ropes = new CableRopes();
        Sync(ropes, new TestWorld(Layout.Parse(Rows)));
        Sync(ropes, Sim);
        var last = PointsOf(ropes)[CableShape.Steps / 2];
        for (int frame = 0; frame < 40; frame++)
        {
            ropes.Update(Rope.StepMs / 4, []);
            var now = PointsOf(ropes)[CableShape.Steps / 2];
            Assert.True(now.Y > last.Y, $"frame {frame}: {now.Y} after {last.Y}");
            last = now;
        }
    }

    [Fact]
    public void LetGoBetweenTwoStepsGoOnFromWhereTheyWereDrawn()
    {
        var before = new TestWorld(Layout.Parse(Rows, [], [((1, 1), (8, 2))]));
        var ropes = new CableRopes();
        Sync(ropes, before);
        ropes.Land(10 + 8, new Held(20 + 8, null, Rest[^1]));
        // Frames a third of a step long: the world has the cable there while it is drawn between steps.
        for (int frame = 0; frame < 100; frame++) ropes.Update(Rope.StepMs / 3, []);
        var drawn = PointsOf(ropes).ToList();
        Sync(ropes, Sim);
        Assert.All(PointsOf(ropes).Zip(drawn), p => Near(p.Second, p.First));
    }

    /// <summary>A panel out in space over a room's roof, a lamp under the roof inside; the roof is row 2.</summary>
    private static readonly string[] Roofed = ["PPP     ", "        ", "########", "#..L...#", "#......#", "########"];

    [Fact]
    public void CarriedFromThroughTheHullToANodeOnItGoOnFromWhereTheyWereDrawn()
    {
        // The panel's cable ran in through the roof to the lamp at 27; its end is let go on the roof at 21.
        var through = new TestWorld(Layout.Parse(Roofed, [], [((0, 0), (3, 3))]));
        var onRoof = new TestWorld(Layout.Parse(Roofed, [], [((0, 0), (5, 2))]));
        Assert.NotEqual(
            CableShape.PiecesOf(through.M.Grid, through.M.Rooms, 0, 27).Pieces.Count,
            CableShape.PiecesOf(onRoof.M.Grid, onRoof.M.Rooms, 0, 21).Pieces.Count);
        var ropes = new CableRopes();
        Sync(ropes, through);
        Run(ropes, 2);
        var roof = CableShape.EndOf(onRoof.M.Grid, 21);
        ropes.Land(21, new Held(27, null, roof));
        Run(ropes, 1);
        var carried = PointsOf(ropes).ToList();
        // The world has it there, its pieces changed: it goes on along the line it was drawn as, not from a new shape.
        Sync(ropes, onRoof);
        var now = PointsOf(ropes);
        Near(carried[0], now[0]);
        Near(carried[^1], now[^1]);
        Assert.All(now, p => Assert.True(CableShape.DistanceTo(carried, p) < 0.01, $"{p} is off the line"));
    }
}

/// <summary>The hands on cables: apps/game/src/features/power/wiring.test.ts, one for one.</summary>
public class WiringTests
{
    // Ten wide. A machine is node 11; an anchor, node 15, has a cable to it; node 18 has none.
    private const int Machine = 11, Support = 15;
    private static readonly Line[] Lines = [new(Machine, Support, [(1.8, 1.5), (3.5, 1.7), (5.5, 1.5)])];
    private static bool IsMachine(int n) => n == Machine;

    [Fact]
    public void TakesACablesEndAtAMachinePressedNearItRatherThanTheMachine() =>
        Assert.Equal(new EndGrip(Machine, new(Machine, Support)), Wiring.GripAt((1.9, 1.6), Lines, Machine, IsMachine));

    [Fact]
    public void TakesASupportWithCablesOnItAndLaysFromAMachineOrABareAnchor()
    {
        Assert.Equal(new SupportGrip(Support), Wiring.GripAt((5.5, 1.5), Lines, Support, IsMachine));
        Assert.Equal(new LayGrip(Machine), Wiring.GripAt((1.2, 1.2), Lines, Machine, IsMachine));
        Assert.Equal(new LayGrip(18), Wiring.GripAt((8.5, 1.5), Lines, 18, IsMachine));
        Assert.Null(Wiring.GripAt((8.5, 3.5), Lines, null, IsMachine));
    }

    [Fact]
    public void HoldsACableLedTooFarAtTheCellNearestThePointerThatTakesIt()
    {
        // Along row 0 from column 0 to 9; columns 3 and 6 hold.
        bool Holds(int cell) => cell is 3 or 6;
        Assert.Equal(6, Wiring.SupportOnWay((0.5, 0.5), (9.5, 0.5), 10, Holds));
        Assert.Equal(3, Wiring.SupportOnWay((0.5, 0.5), (5.5, 0.5), 10, Holds));
        Assert.Null(Wiring.SupportOnWay((0.5, 0.5), (2.5, 0.5), 10, Holds));
    }

    [Fact]
    public void LightsANodeACableFitsOneTooFarGreyAndOneRefusedOtherwiseRed()
    {
        Assert.Equal(Features.Power.Light.Fits, Wiring.LightOf(null));
        Assert.Equal(Features.Power.Light.Far, Wiring.LightOf("power.tooLong"));
        Assert.Equal(Features.Power.Light.Far, Wiring.LightOf("power.taut"));
        Assert.Equal(Features.Power.Light.Refused, Wiring.LightOf("power.twice"));
        Assert.Equal(Features.Power.Light.Refused, Wiring.LightOf("items.notEnough"));
    }
}
