using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A thrown weapon as drawn (companion-fights): it comes into the fist of the arm held out, never through the body to jump there.</summary>
public class ThrownDrawingTests
{
    private static readonly double Px = Rig.Cell / Hero.Unit;

    public static TheoryData<string, int, double, int> Throws()
    {
        var data = new TheoryData<string, int, double, int>();
        foreach (var spec in Spec.Abilities.Throws)
            foreach (int facing in new[] { 1, -1 })
                foreach (double aim in new[] { 0, -0.4, 0.4 })
                    foreach (int fps in new[] { 30, 60, 144 }) data.Add(spec.Item, facing, aim, fps);
        return data;
    }

    /// <summary>
    /// A whole throw along <paramref name="aim"/>: every frame's grip from the shoulder, ahead positive, and the fist's; a wall
    /// <paramref name="wall"/> cells ahead of it, or none, its hero going at <paramref name="moving"/> a step.
    /// </summary>
    private static (List<(Vec Grip, double Turn, ThrowLeg Leg)> Drawn, Vec Fist, double Turn, List<(Vec Grip, double Turn)> After) Thrown(string item, int facing, double aim, int fps,
        int? wall = null, (int X, int Y) moving = default)
    {
        double Frame = 1000.0 / fps;
        int walled = 48 + facing * (wall ?? 0) - (facing < 0 ? 1 : 0);
        string row = new([.. Enumerable.Range(0, 96).Select(x => wall != null && x == walled ? '#' : ' ')]);
        var w = new TestWorld(Layout.Parse([.. Enumerable.Repeat(row, 24)]), 1);
        var spec = Spec.Abilities.ThrowOf(item)!;
        var look = Strokes.Of(item)!.Look;
        var hero = new Hero(0, 0, 0, 0, false, facing, null);
        var raised = Aiming.HandOf(hero);
        hero = hero with { X = 48 * Hero.Unit - raised.X, Y = 12 * Hero.Unit + 500 - raised.Y };
        var hand = Aiming.HandOf(hero);
        var shoulder = new Vec(hand.X * Px, hand.Y * Px);
        var arm = AxeSwing.ArmAt(ThrownDrawing.ArmToward(Math.Cos(aim), Math.Sin(aim)), ThrownDrawing.OutElbow);
        var fist = Rig.FistOf(arm);
        double turn = Rig.HeldTurn(arm, look);
        HeldPlace Held() => new(new Vec(shoulder.X + fist.X * facing, shoulder.Y + fist.Y), facing > 0 ? turn : Math.PI - turn, facing);
        hero = hero with { Vx = moving.X, Vy = moving.Y };
        var own = DroneFlight.VelocityOf(hero.Vx, hero.Vy);
        var thrown = new WeaponThrow();
        var drawing = new ThrownDrawing();
        drawing.Step(Frame, look, spec, null, Held(), shoulder, own);
        Assert.True(thrown.Begin(new ThrowHands(item, true, false, 0), 500));
        Assert.True(thrown.Leave(hero, new Point(hand.X + (int)(facing * 40 * Hero.Unit * Math.Cos(aim)), hand.Y + (int)(40 * Hero.Unit * Math.Sin(aim)))));
        (Vec, double) Local(HeldPlace? at) => at is { } p
            ? (new Vec((p.Grip.X - shoulder.X) * facing, p.Grip.Y - shoulder.Y), facing > 0 ? p.Angle : Math.PI - p.Angle) : (fist, turn);
        var drawn = new List<(Vec, double, ThrowLeg)>();
        while (thrown.Flying && drawn.Count < 300)
        {
            var flight = new Flight(new Vec(thrown.At.X * Px, thrown.At.Y * Px), thrown.Count, thrown.Hauled);
            var place = drawing.Step(Frame, look, spec, flight, Held(), shoulder, own);
            Assert.NotNull(place);
            var (grip, angle) = Local(place);
            drawn.Add((grip, angle, thrown.Leg));
            double steps = Frame / Hero.StepMs;
            hero = hero with { X = hero.X + (int)(moving.X * steps), Y = hero.Y + (int)(moving.Y * steps) };
            var now = Aiming.HandOf(hero);
            shoulder = new Vec(now.X * Px, now.Y * Px);
            thrown.Step(Frame, w.M, hero, false, 0, (_, _, _) => false);
        }
        var after = new List<(Vec, double)>();
        for (int i = 0; i < 30; i++) after.Add(Local(drawing.Step(Frame, look, spec, null, Held(), shoulder, own)));
        return (drawn, fist, turn, after);
    }

    [Theory]
    [MemberData(nameof(Throws))]
    public void HauledHomeItsGripNeverPassesIntoTheBody(string item, int facing, double aim, int fps)
    {
        var (drawn, fist, _, _) = Thrown(item, facing, aim, fps);
        double Ahead(Vec at) => at.X * Math.Cos(aim) + at.Y * Math.Sin(aim);
        Assert.All(drawn.Where(d => d.Leg == ThrowLeg.Home), d => Assert.InRange(Ahead(d.Grip), Ahead(fist) - 3, double.PositiveInfinity));
    }

    [Theory]
    [MemberData(nameof(Throws))]
    public void ItIsCaughtInTheFistWithoutAJump(string item, int facing, double aim, int fps)
    {
        var (drawn, fist, _, after) = Thrown(item, facing, aim, fps);
        static double Between(Vec a, Vec b) => DroneFlight.Length(DroneFlight.Minus(a, b));
        // At 30 frames a second a frame is twice as far: what it may cover is as much more.
        double frames = Math.Max(1, 60.0 / fps);
        Assert.InRange(Between(drawn[^1].Grip, fist), 0, MostLeft * frames);
        Assert.InRange(Between(after[0].Grip, drawn[^1].Grip), 0, MostCatchStep * frames * frames);
        Assert.InRange(Between(after[^1].Grip, fist), 0, 0.5);
    }

    [Theory]
    [MemberData(nameof(Throws))]
    public void ItTurnsIntoTheHandWithoutASnap(string item, int facing, double aim, int fps)
    {
        var (drawn, _, turn, after) = Thrown(item, facing, aim, fps);
        var turns = drawn.Select(d => d.Turn).TakeLast(10).Concat(after.Select(a => a.Turn)).ToList();
        for (int i = 1; i < turns.Count; i++) Assert.InRange(Math.Abs(Poses.Apart(turns[i - 1], turns[i])), 0, MostTurn * Math.Max(1, 60.0 / fps));
        Assert.InRange(Math.Abs(Poses.Apart(drawn[^1].Turn, turn)), 0, 1.2);
    }

    [Theory]
    [MemberData(nameof(Throws))]
    public void InFlightItIsDrawnOutWhereItFlies(string item, int facing, double aim, int fps)
    {
        var (drawn, fist, _, _) = Thrown(item, facing, aim, fps);
        double farthest = drawn.Max(d => DroneFlight.Length(DroneFlight.Minus(d.Grip, fist)));
        Assert.InRange(farthest, Spec.Abilities.ThrowOf(item)!.Range * Px / 2, double.PositiveInfinity);
    }

    public static TheoryData<string, int, int, int> Walls()
    {
        var data = new TheoryData<string, int, int, int>();
        foreach (var spec in Spec.Abilities.Throws)
            foreach (int facing in new[] { 1, -1 })
                foreach (int cells in new[] { 1, 2, 3, 6 })
                    foreach (int fps in new[] { 30, 60, 144 }) data.Add(spec.Item, facing, cells, fps);
        return data;
    }

    [Theory]
    [MemberData(nameof(Walls))]
    public void FromAWallPointBlankItsGripNeverPassesBehindTheShoulder(string item, int facing, int cells, int fps)
    {
        var (drawn, fist, _, after) = Thrown(item, facing, 0, fps, wall: cells);
        Assert.All(drawn.Where(d => d.Leg == ThrowLeg.Home), d => Assert.InRange(d.Grip.X, -1, double.PositiveInfinity));
        Assert.InRange(DroneFlight.Length(DroneFlight.Minus(after[^1].Grip, fist)), 0, 0.5);
    }

    public static TheoryData<string, int, int, int> Moving()
    {
        var data = new TheoryData<string, int, int, int>();
        int top = Spec.Jetpack.Top;
        foreach (var spec in Spec.Abilities.Throws)
            foreach (var (x, y) in new[] { (top, 0), (-top, 0), (0, top), (0, -top) })
                foreach (int fps in new[] { 30, 60, 144 }) data.Add(spec.Item, x, y, fps);
        return data;
    }

    [Theory]
    [MemberData(nameof(Moving))]
    public void AHeroFlyingOnCatchesItInTheFistToo(string item, int vx, int vy, int fps)
    {
        var (drawn, fist, _, after) = Thrown(item, 1, 0, fps, moving: (vx, vy));
        double frames = Math.Max(1, 60.0 / fps);
        Assert.True(drawn.Count < 300, "never caught");
        Assert.InRange(DroneFlight.Length(DroneFlight.Minus(drawn[^1].Grip, fist)), 0, MostLeft * frames);
        Assert.InRange(DroneFlight.Length(DroneFlight.Minus(after[0].Grip, drawn[^1].Grip)), 0, MostCatchStep * frames * frames);
        Assert.InRange(DroneFlight.Length(DroneFlight.Minus(after[^1].Grip, fist)), 0, 0.5);
    }

    public static TheoryData<string, int> Packets()
    {
        var data = new TheoryData<string, int>();
        foreach (var spec in Spec.Abilities.Throws)
            for (int late = 0; late < 3; late++) data.Add(spec.Item, late);
        return data;
    }

    /// <summary>A friend hears of it 20 times a second and draws it 150 ms behind: caught there as here, wherever the catch falls between packets.</summary>
    [Theory]
    [MemberData(nameof(Packets))]
    public void AFriendSeesItCaughtInTheHand(string item, int late)
    {
        const double frame = 1000 / 60.0;
        var w = new TestWorld(Layout.Parse([.. Enumerable.Repeat(new string(' ', 48), 12)]).Doubled(), 1);
        var spec = Spec.Abilities.ThrowOf(item)!;
        var look = Strokes.Of(item)!.Look;
        var hero = new Hero(40_000, 12_500, 0, 0, false, 1);
        var hand = Aiming.HandOf(hero);
        var shoulder = new Vec(hand.X * Px, hand.Y * Px);
        var arm = AxeSwing.ArmAt(ThrownDrawing.ArmToward(1, 0), ThrownDrawing.OutElbow);
        var fist = Rig.FistOf(arm);
        var held = new HeldPlace(new Vec(shoulder.X + fist.X, shoulder.Y + fist.Y), Rig.HeldTurn(arm, look), 1);
        var thrown = new WeaponThrow();
        var drawing = new ThrownDrawing();
        var seen = new RemoteHeroes();
        Assert.True(thrown.Begin(new ThrowHands(item, true, false, 0), 500));
        Assert.True(thrown.Leave(hero, new Point(hand.X + 40 * Hero.Unit, hand.Y)));
        Vec? last = null;
        Vec at = held.Grip;
        for (int i = 0; i < 400; i++)
        {
            double now = 1000 + i * frame;
            if (i % 3 == late)
            {
                Busy? busy = thrown.Flying
                    ? new Busy(thrown.Hauled ? Handwork.Haul : Handwork.Throw, new Point((int)thrown.At.X, (int)thrown.At.Y), thrown.Count) : null;
                seen.Add(2, new SentHero(new Seen(hero, Wields: item, Busy: busy), now), now);
            }
            thrown.Step(frame, w.M, hero, false, 0, (_, _, _) => false);
            if (seen.At(now) is not [var shown, ..]) continue;
            Flight? flight = shown.Seen.Busy is { Flung: true } flung
                ? new Flight(new Vec(flung.At.X * Px, flung.At.Y * Px), flung.Swings, flung.Work == Handwork.Haul) : null;
            at = drawing.Step(frame, look, spec, flight, held, shoulder, default)?.Grip ?? held.Grip;
            if (flight != null) last = at;
            else if (last is { } was)
            {
                Assert.InRange(DroneFlight.Length(DroneFlight.Minus(was, held.Grip)), 0, MostLeft);
                Assert.InRange(DroneFlight.Length(DroneFlight.Minus(at, was)), 0, MostCatchStep);
                return;
            }
        }
        Assert.Fail("never caught");
    }
    /// <summary>How near the fist the last flown frame has the grip and how far the catch moves it, px; how far it turns a frame.</summary>
    private const double MostLeft = 16, MostCatchStep = 7, MostTurn = 0.8;
}
