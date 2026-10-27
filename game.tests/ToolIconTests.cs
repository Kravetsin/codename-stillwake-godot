using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
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
    public void AClearPixelTakesTheColourOfTheNearestDrawnOne()
    {
        var dot = new Picture(1, 1);
        dot.Set(0, 0, (10, 200, 30));
        var icon = Baked(dot, 16);
        Assert.Equal((10, 200, 30, 0), icon(0, 0));
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
    public void AToolsIconFillsItsSlotAndAnyOtherSitsInTheInset()
    {
        Assert.Equal((0, 40), Slots.IconBox(Spec.Shots.Railgun.Item, Slots.SizeOf(SlotScale.Normal)));
        // The screwdriver is held as a picture, though its card is not a tool's.
        Assert.Equal((0, 56), Slots.IconBox(Spec.Screwdrivers.Basic.Item, Slots.SizeOf(SlotScale.Large)));
        Assert.Equal((Slots.Inset, 32), Slots.IconBox("scrap", Slots.SizeOf(SlotScale.Normal)));
        Assert.Equal((Slots.Inset, 48), Slots.IconBox(Spec.Shots.Nail.Id, Slots.SizeOf(SlotScale.Large)));
    }

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
