using System.Linq;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>An effect's area is the picture's drawn pixels in its box, its outline only where the picture ends (spear-effects).</summary>
public class EffectAreaTests
{
    // A picture 8 by 4 of its own pixels, two to a pixel of the picture: drawn where x < 6 and 1 <= y < 3.
    private static bool Drawn(int x, int y) => x < 6 && y >= 1 && y < 3;

    [Fact]
    public void ItIsTheDrawnPixelsInItsBoxInPixelsOfThePicture()
    {
        var area = EffectArea.Of(8, 4, Drawn, 2, (0, 0, 4, 2));
        Assert.Equal(12, area.Inside.Count);
        Assert.All(area.Inside, p => Assert.True(p.X < 3 && p.Y > 0.5 && p.Y < 1.5));
        Assert.True(EffectArea.Of(8, 4, Drawn, 2, (3.5, 0, 4, 2)).Empty);
    }

    [Fact]
    public void ItsOutlineIsWhereThePictureEndsNotWhereTheBoxCutsIt()
    {
        // The box cuts the drawn strip at x = 2 (4 of its own pixels): no outline there, only along its top, bottom and nothing more.
        var area = EffectArea.Of(8, 4, Drawn, 2, (0, 0, 2, 2));
        Assert.Equal(8, area.Edge.Count);
        Assert.All(area.Edge, e => Assert.True(e.Ny is 1 or -1 || e.Nx < 0));
        Assert.DoesNotContain(area.Edge, e => e.Nx > 0);
        // The strip's own end, past x = 6 of its pixels: an outline pointing out to the right.
        var whole = EffectArea.Of(8, 4, Drawn, 2, (0, 0, 4, 2));
        Assert.Contains(whole.Edge, e => e.Nx > 0 && e.X > 2.5);
        Assert.All(whole.Edge, e => Assert.Equal(1, e.Nx * e.Nx + e.Ny * e.Ny, 9));
    }

    [Fact]
    public void ItsPlacesAreItsOwn()
    {
        var area = EffectArea.Of(8, 4, Drawn, 2, (0, 0, 4, 2));
        var random = new System.Random(5);
        for (int i = 0; i < 200; i++)
        {
            var (x, y) = area.AnyInside(random.NextDouble);
            Assert.InRange(x, 0, 3);
            Assert.InRange(y, 0.5, 1.5);
            var edge = area.AnyOnEdge(random.NextDouble);
            Assert.Contains(area.Edge, e => e == edge);
        }
    }
}
