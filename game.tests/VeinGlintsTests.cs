using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Flora;
using Xunit;
using Faces = Stillwake.Game.Features.Flora.Planting.Faces;

namespace Stillwake.Game.Tests;

/// <summary>
/// Which vein faces glint (2026-11-07-ore-pockets): one cell in <see cref="CaveLooks.VeinGlintEvery"/>, by seed and cell; and
/// where on an open face a glint sits.
/// </summary>
public class VeinGlintsTests
{
    [Fact]
    public void AboutOneVeinCellInEveryGlints()
    {
        const int Cells = 30000;
        int picked = Enumerable.Range(0, Cells).Count(cell => VeinGlints.Picks(7, cell));
        int expected = Cells / CaveLooks.VeinGlintEvery;
        Assert.InRange(picked, expected * 9 / 10, expected * 11 / 10);
    }

    [Fact]
    public void AnotherSeedPicksOtherCellsAndNeighboursArePickedApart()
    {
        var first = Enumerable.Range(0, 3000).Where(cell => VeinGlints.Picks(7, cell)).ToHashSet();
        var other = Enumerable.Range(0, 3000).Where(cell => VeinGlints.Picks(8, cell)).ToHashSet();
        Assert.False(first.SetEquals(other));
        // Not a stripe: a run of vein cells glints here and there.
        Assert.Contains(first, cell => first.Contains(cell + 1));
        Assert.Contains(Enumerable.Range(0, 3000), cell => !first.Contains(cell) && !first.Contains(cell + 1) && !first.Contains(cell + 2));
    }

    [Fact]
    public void AGlintSitsInsideAnOpenFaceOfItsCellNeverAClosedOne()
    {
        const int Size = 8;
        Assert.Null(VeinGlints.FacePoint(5, 3, 1, 1, 0, 12345, Size));
        foreach (var face in (Faces[])[Faces.Up, Faces.Down, Faces.Left, Faces.Right])
            for (uint hash = 0; hash < 2000; hash += 7)
            {
                var (px, py) = VeinGlints.FacePoint(5, 3, 1, 1, face, hash, Size)!.Value;
                // Inside the cell, a pixel or two in from the open face, two or more from the face's ends.
                Assert.InRange(px, 5 * Size, 6 * Size);
                Assert.InRange(py, 3 * Size, 4 * Size);
                float into = face switch
                {
                    Faces.Up => py - 3 * Size,
                    Faces.Down => 4 * Size - py,
                    Faces.Left => px - 5 * Size,
                    _ => 6 * Size - px,
                };
                float along = face is Faces.Up or Faces.Down ? px - 5 * Size : py - 3 * Size;
                Assert.InRange(into, 1.5f, 2.5f);
                Assert.InRange(along, 2.5f, Size - 1.5f);
            }
    }

    [Fact]
    public void AGrowthsBlockKeepsItsGlintsAlongItsWholeFaceAndOnlyOnFacesOpen()
    {
        static List<(float X, float Y)> Points(Faces open) =>
            Enumerable.Range(0, 4000).Select(h => VeinGlints.FacePoint(2, 2, 2, 2, open, (uint)h, 8)!.Value).ToList();
        // A two by two block's top: along all 16 pixels but two at either end, a pixel or two down.
        var top = Points(Faces.Up);
        Assert.Equal((2 * 8 + 2.5f, 2 * 8 + 13.5f), (top.Min(p => p.X), top.Max(p => p.X)));
        Assert.All(top, p => Assert.InRange(p.Y, 2 * 8 + 1.5f, 2 * 8 + 2.5f));
        // Open above and to the right: glints on those two faces only, both of them.
        var two = Points(Faces.Up | Faces.Right);
        Assert.All(two, p => Assert.True(p.Y <= 2 * 8 + 2.5f || p.X >= 4 * 8 - 2.5f, $"{p} on a closed face"));
        Assert.Contains(two, p => p.X < 4 * 8 - 3);
        Assert.Contains(two, p => p.Y > 2 * 8 + 3);
    }
}
