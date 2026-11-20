using Stillwake.Content;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The scorch round the core's hole: ember faces on the plates the path cut, soot on the plates near it, each gone with its plate.</summary>
public class CoreScorchTests
{
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

    private static CoreScorch Looked(Registry m)
    {
        var scorch = new CoreScorch(m.Grid);
        scorch.Look();
        return scorch;
    }

    [Fact]
    public void EveryFaceIsATornPlateBesideThePath()
    {
        Assert.NotEmpty(CoreScorch.Faces);
        foreach (var f in CoreScorch.Faces)
        {
            Assert.True(CoreScorch.OnPath(f.X, f.Y));
            Assert.False(CoreScorch.OnPath(f.Plate.X, f.Plate.Y));
            Assert.Equal('T', WreckDress.At(f.Plate.X, f.Plate.Y));
            Assert.Equal(1, Math.Abs(f.Dx) + Math.Abs(f.Dy));
        }
        // Each path cell's sides: those shared along the path and the open ends are no faces; a cell at a row's end faces the plate beside it.
        foreach (var (row, first, last) in Wreck.CorePath)
        {
            Assert.Contains(CoreScorch.Faces, f => (f.X, f.Y, f.Dx, f.Dy) == (first, row, -1, 0));
            Assert.Contains(CoreScorch.Faces, f => (f.X, f.Y, f.Dx, f.Dy) == (last, row, 1, 0));
        }
        Assert.DoesNotContain(CoreScorch.Faces, f => f.Dy == -1 && f.Y == Wreck.CorePath[0].Row);
        Assert.DoesNotContain(CoreScorch.Faces, f => f.Dy == 1 && f.Y == Wreck.CorePath[^1].Row);
    }

    [Fact]
    public void ANewWorldScorchesEveryFaceAndSootedPlate()
    {
        var scorch = Looked(NewWorld());
        Assert.Equal(CoreScorch.Faces, scorch.LiveFaces);
        Assert.Equal(CoreScorch.SootCells, scorch.LiveSoot);
        Assert.Equal(CoreScorch.Sparks, scorch.LiveSparks);
    }

    [Fact]
    public void SootLiesOnlyOnPlatesNearThePath()
    {
        Assert.NotEmpty(CoreScorch.SootCells);
        foreach (var (x, y) in CoreScorch.SootCells)
        {
            Assert.False(CoreScorch.OnPath(x, y));
            Assert.True(WreckDress.IsWall(x, y));
            Assert.InRange(WreckDress.At(x, y), 'A', 'z');
        }
        Assert.All(CoreScorch.Faces, f => Assert.Contains(f.Plate, CoreScorch.SootCells));
        // Three cells off the path is past its reach, even diagonally from the row above.
        Assert.DoesNotContain((Wreck.CorePath[2].Last + 4, Wreck.CorePath[2].Row), CoreScorch.SootCells);
        // Soot darkens the plate at the cut, none at all past its reach; the heat tint only at the very edge.
        var face = CoreScorch.Faces[0];
        var (bx, by) = CoreScorch.BandPixel(face, 12, 0);
        var edge = CoreScorch.SootAt(bx, by);
        Assert.True(edge.Keep < 0.5);
        Assert.True(edge.Tint.A > 0);
        int far = (Wreck.CorePath[2].Last + 4) * CoreScorch.CellFine;
        Assert.Equal((1.0, default((int, int, int, int))), CoreScorch.SootAt(far, Wreck.CorePath[2].Row * CoreScorch.CellFine));
    }

    [Fact]
    public void AFaceAndItsSootGoWhenItsPlateIsBroken()
    {
        var m = NewWorld();
        var scorch = Looked(m);
        var face = CoreScorch.Faces[3];
        m.Grid.Clear(CellOf(m, face.Plate), "structure");
        Assert.True(scorch.Look());
        Assert.DoesNotContain(face, scorch.LiveFaces);
        Assert.DoesNotContain(face.Plate, scorch.LiveSoot);
        Assert.DoesNotContain(scorch.LiveSparks, s => s.Face == face);
        Assert.Equal(CoreScorch.Faces.Count(f => f.Plate == face.Plate), CoreScorch.Faces.Count - scorch.LiveFaces.Count);
        Assert.Equal(CoreScorch.SootCells.Count - 1, scorch.LiveSoot.Count);
    }

    [Fact]
    public void AWallBuiltBackIsCleanScrapAndNeverSmoulders()
    {
        var m = NewWorld();
        var scorch = Looked(m);
        var face = CoreScorch.Faces[3];
        int cell = CellOf(m, face.Plate);
        m.Grid.Clear(cell, "structure");
        m.Grid.Place(cell, Placement.Wall("scrap"));
        scorch.Look();
        Assert.DoesNotContain(face, scorch.LiveFaces);
        Assert.DoesNotContain(face.Plate, scorch.LiveSoot);
    }

    [Fact]
    public void AWallBuiltInThePathCoversTheFacesBesideIt()
    {
        var m = NewWorld();
        var scorch = Looked(m);
        var face = CoreScorch.Faces[0];
        m.Grid.Place(CellOf(m, (face.X, face.Y)), Placement.Wall("scrap"));
        scorch.Look();
        Assert.DoesNotContain(scorch.LiveFaces, f => (f.X, f.Y) == (face.X, face.Y));
        Assert.Equal(CoreScorch.SootCells, scorch.LiveSoot);
    }

    [Fact]
    public void AChangeElsewhereRedrawsNothing()
    {
        var m = NewWorld();
        var scorch = Looked(m);
        m.Grid.Clear(CellOf(m, (Wreck.CorePath[0].First - 40, Wreck.CorePath[0].Row + 2)), "structure");
        Assert.False(scorch.Look());
    }

    [Fact]
    public void TheLineBreathesSlowlyAndTheSparksFlyDownLeftNowAndThen()
    {
        var f = CoreScorch.Faces[0];
        Assert.Equal(CoreScorch.Breath(f, 2, 1.0), CoreScorch.Breath(f, 2, 6.5), 9);
        Assert.NotEqual(CoreScorch.Breath(f, 2, 1.0), CoreScorch.Breath(f, 2, 3.0), 3);
        Assert.InRange(CoreScorch.Sparks.Count, 1, 8);
        foreach (var s in CoreScorch.Sparks)
        {
            Assert.True(s.Life < s.Period / 2);
            Assert.True(s.Vy > 0 || s.Vx < 0);
            int flying = Enumerable.Range(0, 1000).Count(i => CoreScorch.SparkAt(s, i * 0.05) != null);
            Assert.InRange(flying, 1, 500);
        }
    }
}
