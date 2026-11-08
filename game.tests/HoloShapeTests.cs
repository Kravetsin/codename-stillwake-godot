using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Shared;
using Xunit;
using Machines = Stillwake.Content.Machines;
using MachineTrait = Stillwake.Content.MachineTrait;

namespace Stillwake.Game.Tests;

/// <summary>Where a machine window's hologram touches, leaves the machine and grows, and its tone (machine-holograms).</summary>
public class HoloShapeTests
{
    private const float Cell = 8;
    private static readonly HoloRect Window = new(10, 300, 400, 200);

    [Fact]
    public void BesideTheWindowTheBeamTouchesItsNearSideLevelWithTheMachine()
    {
        var box = new HoloRect(500, 380, 32, 32);
        Assert.Equal(new HoloTouch(410, 396, HoloEdge.Right), HoloShape.Touch(Window, box));
        // The emitter on the machine's side facing the window, a cell in.
        Assert.Equal((508f, 396f), HoloShape.Emitter(box, (410, 396), Cell));
        Assert.Equal(new HoloTouch(10, 396, HoloEdge.Left), HoloShape.Touch(Window, box with { X = -100 }));
        Assert.Equal(new HoloTouch(100, 300, HoloEdge.Top), HoloShape.Touch(Window, box with { X = 84, Y = 100 }));
    }

    [Fact]
    public void AtACornerTheBeamTouchesTheCornerOnTheSideItComesFromMost()
    {
        Assert.Equal(new HoloTouch(410, 500, HoloEdge.Right), HoloShape.Touch(Window, new HoloRect(600, 520, 16, 16)));
        Assert.Equal(new HoloTouch(410, 500, HoloEdge.Bottom), HoloShape.Touch(Window, new HoloRect(420, 700, 16, 16)));
    }

    [Fact]
    public void OverTheMachineTheBeamTouchesTheNearestSide()
    {
        // The machine's middle 20 px from the bottom, farther from the rest.
        var touch = HoloShape.Touch(Window, new HoloRect(200, 464, 16, 32));
        Assert.Equal(new HoloTouch(208, 500, HoloEdge.Bottom), touch);
        Assert.Equal((208f, 488f), HoloShape.Emitter(new HoloRect(200, 464, 16, 32), (touch.X, touch.Y), Cell));
    }

    [Fact]
    public void AMachineUnderTwoCellsSendsItsBeamFromItsMiddle()
    {
        var small = new HoloRect(500, 380, 12, 40);
        Assert.Equal((506f, 388f), HoloShape.Emitter(small, (0, 0), Cell));
    }

    [Fact]
    public void TheWindowGrowsFromTheTouchPointAlongItsEdgeThenAcross()
    {
        var touch = new HoloTouch(410, 396, HoloEdge.Right);
        Assert.Null(HoloShape.Grown(Window, touch, 0));
        var (line, isLine) = HoloShape.Grown(Window, touch, 0.01)!.Value;
        Assert.True(isLine);
        Assert.Equal((408f, 2f), (line.X, line.W));
        Assert.True(line.Y < 396 && line.Bottom > 396 && line.H < 20, $"a short line through the touch point, {line}");
        var (edge, _) = HoloShape.Grown(Window, touch, HoloLook.EdgeFirst - 1e-6)!.Value;
        Assert.Equal((300f, 500f), (Rounded(edge.Y), Rounded(edge.Bottom)));
        var (half, spread) = HoloShape.Grown(Window, touch, 0.6)!.Value;
        Assert.False(spread);
        Assert.Equal((300f, 200f, 410f), (half.Y, half.H, Rounded(half.Right)));
        Assert.True(half.W > 2 && half.W < 400);
        Assert.Equal((Window, false), HoloShape.Grown(Window, touch, 1));
        // From the top edge it spreads down.
        var (down, _) = HoloShape.Grown(Window, new HoloTouch(100, 300, HoloEdge.Top), 0.6)!.Value;
        Assert.Equal((10f, 300f, 400f), (down.X, down.Y, down.W));
    }

    [Theory]
    [InlineData("furnace", HoloTone.Warm)]
    [InlineData("burnerGenerator", HoloTone.Warm)]
    [InlineData("biofuelGenerator", HoloTone.Warm)]
    [InlineData("charger", HoloTone.Cold)]
    [InlineData("smelter", HoloTone.Cold)]
    [InlineData("former", HoloTone.Cold)]
    [InlineData("assembler", HoloTone.Cold)]
    [InlineData("droneBay", HoloTone.Cold)]
    [InlineData("crate", HoloTone.Neutral)]
    [InlineData("tradeStation", HoloTone.Neutral)]
    [InlineData("workbench", HoloTone.Neutral)]
    [InlineData("arcBench", HoloTone.Neutral)]
    [InlineData("lifepod", HoloTone.Neutral)]
    [InlineData("dropPod", HoloTone.Neutral)]
    public void AStationsToneIsItsFireElseItsPowerElseNeutral(string kind, HoloTone tone) =>
        Assert.Equal(tone, HoloShape.ToneOf(Machines.Get(kind)));

    [Fact]
    public void EveryMachineThatOpensAWindowGetsItsToneByTheRule()
    {
        foreach (var spec in Machines.All)
        {
            if (spec.Slots is not > 0) continue;
            var want = spec.Is(MachineTrait.Fire) ? HoloTone.Warm : spec.Makes != null || spec.Draws != null || spec.Store != null ? HoloTone.Cold : HoloTone.Neutral;
            Assert.True(want == HoloShape.ToneOf(spec), $"{spec.Kind}: {HoloShape.ToneOf(spec)}, not {want}");
        }
    }

    [Fact]
    public void TheBoxOfCellsIsInCells()
    {
        // Cells 21, 22, 32, 41 on a grid 10 wide: columns 1–2, rows 2–4.
        Assert.Equal((1, 2, 2, 3), HoloShape.BoxOf([21, 22, 32, 41], 10));
        Assert.Equal((5, 0, 1, 1), HoloShape.BoxOf([5], 10));
    }

    private static Gen<HoloRect> Rects =>
        from x in Gen.Choose(-2000, 2000)
        from y in Gen.Choose(-2000, 2000)
        from w in Gen.Choose(1, 800)
        from h in Gen.Choose(1, 800)
        select new HoloRect(x / 2f, y / 2f, w / 2f, h / 2f);

    [Property(MaxTest = 500)]
    public Property TheTouchLiesOnTheWindowsEdgeAndTheEmitterInsideTheBox() => Prop.ForAll(
        (from window in Rects from box in Rects from cell in Gen.Choose(1, 16) select (window, box, cell)).ToArbitrary(), c =>
        {
            var (window, box, cell) = c;
            var touch = HoloShape.Touch(window, box);
            bool within = touch.X >= window.X && touch.X <= window.Right && touch.Y >= window.Y && touch.Y <= window.Bottom;
            float onEdge = touch.Edge switch
            {
                HoloEdge.Left => touch.X - window.X,
                HoloEdge.Right => window.Right - touch.X,
                HoloEdge.Top => touch.Y - window.Y,
                _ => window.Bottom - touch.Y,
            };
            Assert.True(within && onEdge == 0, $"touch {touch} off {window}'s edge");
            var (ex, ey) = HoloShape.Emitter(box, (touch.X, touch.Y), cell);
            Assert.True(ex >= box.X && ex <= box.Right && ey >= box.Y && ey <= box.Bottom, $"emitter ({ex}, {ey}) outside {box}");
        });

    private static float Rounded(float x) => (float)System.Math.Round(x, 2);
}
