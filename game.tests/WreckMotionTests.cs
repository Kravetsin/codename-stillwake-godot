using Stillwake.Content;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What still moves on the wreck lies on its tears, ends with them, keeps its rates and throws nothing off the screen.</summary>
public class WreckMotionTests
{
    private static readonly Func<double, double, bool> Everywhere = (_, _) => true, Nowhere = (_, _) => false;

    private static Registry NewWorld()
    {
        var m = new Registry();
        Stillwake.Sim.World.Create(1, Registry.Wreck, m.All);
        return m;
    }

    private static int CellOf(Registry m, (int X, int Y) drawn)
    {
        var corner = WreckDress.CornerIn(m.Grid.Width, m.Grid.Height);
        return (drawn.Y + Wreck.Top + corner.Y) * m.Grid.Width + drawn.X + Wreck.Left + corner.X;
    }

    private static (WreckMotion Motion, Glims Glims, Bolts Bolts) Made(Registry m)
    {
        var (glims, bolts) = (new Glims(Glims.Limit), new Bolts());
        return (new WreckMotion(m.Grid, glims, bolts, new Random(1).NextDouble), glims, bolts);
    }

    [Fact]
    public void EverySiteLiesOnATearOfTheMap()
    {
        foreach (var wire in WreckMotion.Wires) Assert.Equal('T', WreckDress.At(wire.Cell.X, wire.Cell.Y));
        Assert.Equal('T', WreckDress.At(WreckMotion.ThreadCell.X, WreckMotion.ThreadCell.Y));
        foreach (var (x, y) in WreckMotion.RoofCells) Assert.Equal('T', WreckDress.At(x, y));
    }

    [Fact]
    public void ANewWorldHasEveryWire()
    {
        var (motion, _, _) = Made(NewWorld());
        motion.Step(16, Everywhere);
        Assert.Equal(WreckMotion.Wires.Count, motion.LiveWires.Count);
    }

    [Fact]
    public void AWireEndsWhenItsCellIsTakenAway()
    {
        var m = NewWorld();
        var (motion, _, _) = Made(m);
        motion.Step(16, Everywhere);
        m.Grid.Clear(CellOf(m, WreckMotion.Wires[0].Cell), "structure");
        motion.Step(16, Everywhere);
        Assert.DoesNotContain(WreckMotion.Wires[0], motion.LiveWires);
        // Its neighbour's sides changed, so that tear went with it; the one past it stays.
        Assert.DoesNotContain(WreckMotion.Wires[2], motion.LiveWires);
        Assert.Contains(WreckMotion.Wires[1], motion.LiveWires);
    }

    [Fact]
    public void ATearGivingWayToPlainScrapEndsWhatHangsOnIt()
    {
        // A wall built below the keel's middle tear: the cell stands, its sides are not those it was drawn for.
        var m = NewWorld();
        var (motion, _, _) = Made(m);
        motion.Step(16, Everywhere);
        var long_ = WreckMotion.Wires[2];
        m.Grid.Place(CellOf(m, (long_.Cell.X, long_.Cell.Y + 1)), Placement.Wall("scrap"));
        motion.Step(16, Everywhere);
        Assert.DoesNotContain(long_, motion.LiveWires);
    }

    [Fact]
    public void AWiresRootStaysAndItsTipMoves()
    {
        var (motion, _, _) = Made(NewWorld());
        motion.Step(16, Everywhere);
        var wire = WreckMotion.Wires[2];
        var (before, after) = (new (double X, double Y)[WreckMotion.PointCount], new (double X, double Y)[WreckMotion.PointCount]);
        motion.PointsOf(wire, before);
        motion.Step(3000, Everywhere);
        motion.PointsOf(wire, after);
        Assert.Equal(before[0], after[0]);
        Assert.NotEqual(before[^1], after[^1]);
        double moved = Math.Sqrt(Math.Pow(after[^1].X - before[^1].X, 2) + Math.Pow(after[^1].Y - before[^1].Y, 2));
        Assert.InRange(moved, 0.1, 2 * wire.Swing);
    }

    [Fact]
    public void TheThreadComesAtItsRate()
    {
        var (motion, glims, _) = Made(NewWorld());
        for (int i = 0; i < 100; i++) motion.Step(30, Everywhere);
        Assert.InRange(glims.Live.Count, 19, 20);
        Assert.All(glims.Live, g => Assert.Equal(Luminous.WreckThread, g.Look));
    }

    [Fact]
    public void ArcsComeAtTheirRateAtBothSites()
    {
        var (motion, _, bolts) = Made(NewWorld());
        int arcs = 0, seen = 0;
        for (int i = 0; i < 20000; i++)
        {
            motion.Step(30, Everywhere);
            if (bolts.Live.Count > seen) arcs += bolts.Live.Count - seen;
            bolts.Step(30);
            seen = bolts.Live.Count;
        }
        // 600 s at each of two sites, an arc every 6 to 14 s.
        Assert.InRange(arcs, 2 * 600 / 14, 2 * 600 / 6);
    }

    [Fact]
    public void AnArcLastsForABlinkAndIsFine()
    {
        var (motion, _, bolts) = Made(NewWorld());
        for (int i = 0; i < 1000 && bolts.Live.Count == 0; i++) motion.Step(30, Everywhere);
        var bolt = Assert.Single(bolts.Live);
        Assert.True(bolt.Fine);
        Assert.InRange(bolt.Life, 9, 13);
    }

    [Fact]
    public void NothingIsThrownOffTheScreenAndNoBurstOnComingBack()
    {
        var (motion, glims, bolts) = Made(NewWorld());
        for (int i = 0; i < 2000; i++) motion.Step(30, Nowhere);
        Assert.Empty(glims.Live);
        Assert.Empty(bolts.Live);
        motion.Step(30, Everywhere);
        Assert.True(glims.Live.Count <= 1);
    }

    [Fact]
    public void AWorldWithoutTheWreckHasNothing()
    {
        var m = new Registry();
        Stillwake.Sim.World.Create(1, Layout.Parse("....\n...."), m.All);
        var (motion, glims, bolts) = Made(m);
        for (int i = 0; i < 1000; i++) motion.Step(30, Everywhere);
        Assert.Empty(motion.LiveWires);
        Assert.Empty(glims.Live);
        Assert.Empty(bolts.Live);
    }
}
