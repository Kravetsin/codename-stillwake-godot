using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which cells of the wreck show their age, on either grid, and that a tear lasts only with the sides it was drawn for.</summary>
public class WreckDressTests
{
    private const int Up = 1, Right = 2, Down = 4;

    private static (int X, int Y) ATear => WreckDress.Cells().First(c => c.Look == 'T') is var c ? (c.X, c.Y) : default;

    [Fact]
    public void ACellOfAWorldAndOfADevGridReadTheSameDress()
    {
        var (x, y, look) = WreckDress.Cells().First();
        var world = (Width: Registry.Wreck.Width, Height: Registry.Wreck.Height);
        var inWorld = WreckDress.CornerIn(world.Width, world.Height);
        Assert.Equal(Layout.OffsetInWorld(Wreck.Width, Wreck.Height), inWorld);
        Assert.Equal((x, y), WreckDress.DrawnAt(x + Wreck.Left + inWorld.X, y + Wreck.Top + inWorld.Y, inWorld));
        var alone = WreckDress.CornerIn(Wreck.Width, Wreck.Height);
        Assert.Equal((0, 0), alone);
        var drawn = WreckDress.DrawnAt(x + Wreck.Left, y + Wreck.Top, alone);
        Assert.Equal(look, WreckDress.At(drawn.X, drawn.Y));
    }

    [Fact]
    public void PastTheMapThereIsNoDress()
    {
        Assert.Equal(' ', WreckDress.At(-Wreck.Left - 1, 0));
        Assert.Equal(' ', WreckDress.At(0, -Wreck.Top - 1));
        Assert.Equal(' ', WreckDress.At(Wreck.Width, 0));
        Assert.Equal(' ', WreckDress.At(0, Wreck.Height));
    }

    [Fact]
    public void ATearShowsOnlyWithTheSidesItWasDrawnFor()
    {
        var (x, y) = ATear;
        int drawn = WreckDress.OpenAsDrawn(x, y);
        Assert.NotEqual(0, drawn);
        Assert.True(WreckDress.ShowsTear(x, y, drawn));
        Assert.False(WreckDress.ShowsTear(x, y, drawn ^ Right));
        Assert.False(WreckDress.ShowsTear(x, y, drawn ^ Up));
    }

    [Fact]
    public void AtTheRoofsStripTheTearOpensUpAndDown()
    {
        var strip = WreckDress.Cells().First(c => c.Look == 'T' && WreckDress.OpenAsDrawn(c.X, c.Y) == (Up | Down));
        Assert.True(WreckDress.ShowsTear(strip.X, strip.Y, Up | Down));
    }

    [Fact]
    public void OnlyATornCellShowsATear()
    {
        var rust = WreckDress.Cells().First(c => c.Look != 'T');
        Assert.False(WreckDress.ShowsTear(rust.X, rust.Y, WreckDress.OpenAsDrawn(rust.X, rust.Y)));
    }

    [Fact]
    public void AMaskKeepsCutsAndPaints()
    {
        byte[] wall = [10, 20, 30, 255, 10, 20, 30, 255, 10, 20, 30, 255];
        byte[] mask = [0, 0, 0, 0, 255, 0, 255, 255, 150, 190, 214, 255];
        Assert.Equal([10, 20, 30, 255, 0, 0, 0, 0, 150, 190, 214, 255], WreckDress.Torn(wall, mask));
    }
}
