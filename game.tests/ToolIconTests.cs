using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Render;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A tool's icon (2026-10-27-tool-icons): its held picture turned to point up-right, fitted inside its margin, drawn off the grid.</summary>
public class ToolIconTests
{
    private const int M = HeldLook.IconMargin;

    [Fact]
    public void TheHeadPointsUpRightAndTheHandleDownLeft()
    {
        // A bar one pixel high in clear room, its handle blue and its head, along +x, red.
        var bar = new Picture(20, 5);
        for (int x = 2; x < 18; x++) bar.Set(x, 2, x < 10 ? (0, 0, 255) : (255, 0, 0));
        var icon = Baked(bar, 32);
        Assert.Equal((255, 0, 0, 255), icon(25, 6));
        Assert.Equal((0, 0, 255, 255), icon(6, 25));
        Assert.Equal(0, icon(6, 6).A);
        Assert.Equal(0, icon(25, 25).A);
    }

    [Fact]
    public void ItsEdgesAreSmoothItsMarginClearAndASolidPixelKeepsItsColour()
    {
        var square = new Picture(2, 2);
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++) square.Set(x, y, (200, 100, 50));
        const int side = 64;
        var icon = Baked(square, side);
        Assert.Equal((200, 100, 50, 255), icon(side / 2, side / 2));
        bool partial = false;
        for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++)
            {
                int a = icon(x, y).A;
                partial |= a is > 0 and < 255;
                if (x < M || y < M || x >= side - M || y >= side - M) Assert.Equal(0, a);
            }
        Assert.True(partial, "no pixel at the edge holds part of the alpha: the staircase");
    }

    [Fact]
    public void ItFillsItsSquareToTheMargin()
    {
        // The turned box's corners touch the margin: a drawn pixel in its first and last columns inside it.
        var square = new Picture(2, 2);
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++) square.Set(x, y, (200, 100, 50));
        const int side = 40;
        var icon = Baked(square, side);
        bool Drawn(int x) => Enumerable.Range(0, side).Any(y => icon(x, y).A > 0);
        Assert.True(Drawn(M), "the left edge falls short of the margin");
        Assert.True(Drawn(side - M - 1), "the right edge falls short of the margin");
    }

    [Fact]
    public void AClearPixelInThePictureDarkensNoColour()
    {
        // A drawn block in clear room: averaged by alpha, every pixel, its soft edge and the clear ones, keeps the block's colour.
        var block = new Picture(4, 4);
        for (int y = 1; y < 3; y++)
            for (int x = 1; x < 3; x++) block.Set(x, y, (200, 100, 50));
        const int side = 32;
        var icon = Baked(block, side);
        for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++)
            {
                var (r, g, b, _) = icon(x, y);
                Assert.Equal((200, 100, 50), (r, g, b));
            }
    }

    [Fact]
    public void AClearPixelTakesTheColourOfTheNearestDrawnOne()
    {
        // A red dot at the handle's end, a green one at the head's: the clear corners by each take each.
        var dots = new Picture(9, 1);
        dots.Set(0, 0, (255, 0, 0));
        dots.Set(8, 0, (0, 255, 0));
        const int side = 32;
        var icon = Baked(dots, side);
        Assert.Equal((255, 0, 0, 0), icon(0, side - 1));
        Assert.Equal((0, 255, 0, 0), icon(side - 1, 0));
    }

    [Property(MaxTest = 200)]
    public Property EveryIconIsClearInsideItsMargin() => Prop.ForAll(
        (from w in Gen.Choose(1, 64)
         from h in Gen.Choose(1, 64)
         from side in Gen.Choose(8, 64)
         from seed in Gen.Choose(0, 1_000_000)
         select (w, h, side, seed)).ToArbitrary(),
        c =>
        {
            // The drawn pixels inside a clear border, the one pixel of the far corner always drawn.
            var random = new System.Random(c.seed);
            var picture = new Picture(c.w + 2, c.h + 2);
            for (int y = 1; y <= c.h; y++)
                for (int x = 1; x <= c.w; x++)
                    if (random.Next(3) > 0 || (x == c.w && y == c.h)) picture.Set(x, y, (random.Next(256), random.Next(256), random.Next(256)));
            var icon = Baked(picture, c.side);
            for (int y = 0; y < c.side; y++)
                for (int x = 0; x < c.side; x++)
                    if (x < M || y < M || x >= c.side - M || y >= c.side - M) Assert.Equal(0, icon(x, y).A);
        });

    [Fact]
    public void OnlyAToolHasNoIconDrawn()
    {
        string items = System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "items");
        Assert.All(Spec.Items.All, i => Assert.Equal(HeldLook.Of(i.Id) == null, System.IO.File.Exists(System.IO.Path.Combine(items, $"{i.Id}.png"))));
    }

    private static System.Func<int, int, (int R, int G, int B, int A)> Baked(Picture picture, int side)
    {
        var rgba = ToolIcon.Bake(picture.Rgba, picture.Width, picture.Height, side);
        return (x, y) =>
        {
            int at = (y * side + x) * 4;
            return (rgba[at], rgba[at + 1], rgba[at + 2], rgba[at + 3]);
        };
    }

    private sealed class Picture(int width, int height)
    {
        public int Width => width;
        public int Height => height;
        public byte[] Rgba { get; } = new byte[width * height * 4];

        public void Set(int x, int y, (int R, int G, int B) colour)
        {
            int at = (y * width + x) * 4;
            (Rgba[at], Rgba[at + 1], Rgba[at + 2], Rgba[at + 3]) = ((byte)colour.R, (byte)colour.G, (byte)colour.B, 255);
        }
    }
}
