using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A running tool hits with what is drawn (tool-edge): what its edge touches, where it first touches, and how it is seen at work.</summary>
public class ToolEdgeTests
{
    private const int Unit = Hero.Unit;
    private static readonly string Drill = Spec.Weapons.Drill.Item, Cutter = Spec.Weapons.PlasmaCutter.Item;
    private static readonly Func<Piece, double, double, bool> Clear = (_, _, _) => false;

    /// <summary>A picture opaque over its whole box, unturned, as one that will not load counts.</summary>
    private static readonly Func<Piece, double, double, bool> Opaque = (p, x, y) =>
        Math.Abs(x - p.X) <= Aiming.PictureOf(p.Item).HalfW && Math.Abs(y - p.Y) <= Aiming.PictureOf(p.Item).HalfH;

    /// <summary>
    /// A hull of one block in the corner and open space, 32 cells by 16, its first piece made a still hull section at (10, 8) and a
    /// second one six cells right of it, both out in space: the world and the two wrecks.
    /// </summary>
    private static (TestWorld W, Piece Near, Piece Far) Wrecks()
    {
        var w = new TestWorld(Layout.Parse(["#".PadRight(16), .. Enumerable.Repeat(new string(' ', 16), 7)]).Doubled(), 1);
        for (int i = 0; i < 4 * Spec.Debris.Every && w.M.Debris.Pieces.Count == 0; i++) w.Step();
        w.Edit(root =>
        {
            var pieces = root["slices"]!["debris"]!["pieces"]!.AsArray();
            var near = pieces[0]!;
            near["item"] = "hullSection";
            near["speed"] = 0;
            near["x"] = 10 * Unit;
            near["y"] = 8 * Unit;
            var far = near.DeepClone();
            far["id"] = 99;
            far["x"] = 16 * Unit;
            pieces.Add(far);
        });
        return (w, w.M.Debris.Pieces[0], w.M.Debris.Pieces.Single(p => p.Id == 99));
    }

    /// <summary>A flat edge from <paramref name="fromX"/> to <paramref name="toX"/>, in thousandths of a cell, along row <paramref name="y"/>.</summary>
    private static Edge Flat(double fromX, double toX, double y, bool beam = false) => new((fromX, y), (toX, y), 0, beam);

    [Fact]
    public void AnEdgeOverAWrecksBoxButOffItsPixelsTouchesNothing()
    {
        var (w, near, _) = Wrecks();
        var (halfW, _) = Aiming.PictureOf(near.Item);
        var edge = Flat(near.X - halfW - 500, near.X, near.Y);
        var off = ToolEdges.Hits(w.M, edge, outside: true, 0, Clear);
        Assert.Empty(off.Wrecks);
        Assert.Null(off.Contact?.Wreck);
        var on = ToolEdges.Hits(w.M, edge, outside: true, 0, Opaque);
        Assert.Equal([near], on.Wrecks);
        // It first touches where the picture begins: here, the box's edge.
        Assert.Equal(near.X - halfW, on.Contact!.Value.At.X, ToolEdges.Step);
    }

    [Fact]
    public void ATurnedPicturesCornerPastItsBoxIsAskedOfItsPixels()
    {
        var (w, near, _) = Wrecks();
        var (_, halfH) = Aiming.PictureOf(near.Item);
        // Just above the box over its middle, within the circle round it: where a turned picture's corner may be.
        var edge = Flat(near.X - 100, near.X + 100, near.Y - halfH - 200);
        Assert.Equal([near], ToolEdges.Hits(w.M, edge, outside: true, 0, (_, _, _) => true).Wrecks);
        Assert.Empty(ToolEdges.Hits(w.M, edge, outside: true, 0, Opaque).Wrecks);
    }

    [Fact]
    public void AnEdgeTouchesEveryWreckUnderIt()
    {
        var (w, near, far) = Wrecks();
        var hit = ToolEdges.Hits(w.M, Flat(near.X, far.X, near.Y), outside: true, 0, Opaque);
        Assert.Equal([near, far], hit.Wrecks);
    }

    [Fact]
    public void ABeamStopsAtTheFirstWreckAndEndsThere()
    {
        var (w, near, far) = Wrecks();
        var (halfW, _) = Aiming.PictureOf(near.Item);
        var hit = ToolEdges.Hits(w.M, Flat(near.X - halfW - 2000, far.X + 2000, near.Y, beam: true), outside: true, 0, Opaque);
        Assert.Equal([near], hit.Wrecks);
        Assert.Equal(near.X - halfW, hit.End.X, ToolEdges.Step);
        // At nothing it runs its whole length.
        var open = ToolEdges.Hits(w.M, Flat(near.X - halfW - 2000, near.X - halfW - 500, near.Y, beam: true), outside: true, 0, Opaque);
        Assert.Equal((near.X - halfW - 500, (double?)null), (open.End.X, open.Contact?.At.X));
    }

    [Fact]
    public void AboardItTouchesNoWreck()
    {
        var (w, near, _) = Wrecks();
        Assert.Empty(ToolEdges.Hits(w.M, Flat(near.X - 3000, near.X, near.Y), outside: false, 0, Opaque).Wrecks);
    }

    /// <summary>An open strip in soil a character a cell: open from column 1 to 30, rows 1 to 8.</summary>
    private static TestWorld Strip() => new(Layout.Parse("""
        ################################
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        #..............................#
        ################################
        """).WithMaterials("""
        ssssssssssssssssssssssssssssssss
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        s..............................s
        ssssssssssssssssssssssssssssssss
        """) with { Kind = Spec.Surfaces.TestStrip.Id });

    [Fact]
    public void TheCellsUnderTheEdgeAreListedAndItFirstTouchesTheWallsFace()
    {
        var w = Strip();
        int width = w.M.Grid.Width;
        var hit = ToolEdges.Hits(w.M, Flat(29_500, 31_500, 4_500), outside: false, 0, Opaque);
        Assert.Equal([4 * width + 29, 4 * width + 30, 4 * width + 31], hit.Cells);
        Assert.Equal((31_000, 4 * width + 31), (hit.Contact!.Value.At.X, hit.Contact.Value.Cell!.Value));
        Assert.Equal(4 * width + 31, ToolEdges.LeadOf(w.M, hit));
        // Short of the wall: no cell it strikes.
        Assert.Null(ToolEdges.LeadOf(w.M, ToolEdges.Hits(w.M, Flat(28_500, 30_500, 4_500), outside: false, 0, Opaque)));
    }

    [Fact]
    public void AWheelWorksOnlyItsHalfDiscAheadOfTheHub()
    {
        var w = Strip();
        int width = w.M.Grid.Width;
        // A wheel of a cell's radius, its hub just right of a cell's left side and at its middle: the wall's cell up and right
        // lies in the edge's square, but more than a radius from the hub.
        var wheel = new Edge((30_050, 4_500), (31_050, 4_500), 1000, false, Round: true);
        var hit = ToolEdges.Hits(w.M, wheel, outside: false, 0, Opaque);
        Assert.Contains(4 * width + 31, hit.Cells);
        Assert.Contains(3 * width + 30, hit.Cells);
        Assert.DoesNotContain(3 * width + 31, hit.Cells);
        Assert.Contains(3 * width + 31, ToolEdges.Hits(w.M, wheel with { Round = false }, outside: false, 0, Opaque).Cells);
    }

    [Fact]
    public void TheCuttersBeamRunsFromItsNozzleToItsReachFromTheHand()
    {
        var hero = new Hero(10 * Unit, 9 * Unit, 0, 0, false, 1);
        var hand = Aiming.HandOf(hero);
        var edge = ToolEdges.Of(Cutter, hero, (hand.X + 1500, hand.Y), (hand.X + 3000, hand.Y));
        Assert.True(edge.Beam);
        Assert.Equal((hand.X + 1500, hand.X + Spec.Weapons.PlasmaCutter.Range), (edge.From.X, edge.To.X));
        var drill = ToolEdges.Of(Drill, hero, (hand.X + 1500, hand.Y), (hand.X + 3000, hand.Y));
        Assert.Equal((false, hand.X + 3000), (drill.Beam, drill.To.X));
    }

    [Fact]
    public void AStreamThrowsAsMuchASecondAsTheHitsDidOneByOne()
    {
        foreach (var weapon in new[] { Spec.Weapons.Drill, Spec.Weapons.Grinder, Spec.Weapons.PlasmaCutter })
        {
            double hits = (double)Sim.World.TicksPerSecond / weapon.Pace * Features.Effects.Impacts.ShareOf(weapon.Item);
            Assert.Equal(hits, WorkLooks.ShareOf(weapon.Item) * WorkLooks.ThrowsPerSecond, 9);
        }
    }
}
