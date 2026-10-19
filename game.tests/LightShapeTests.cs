using Stillwake.Game.Features.Light;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Tests;

/// <summary>Light along a wall cut to a shape (0069, part 2): what the lightmap's shader is told, and the starlight in its open half.</summary>
public class LightShapeTests
{
    [Fact]
    public void TellsTheShaderEachShapeOfWallApartFromAWholeOne()
    {
        int[] structure = [Structure.Wall, Structure.Wall, Structure.Wall, Structure.Wall, Structure.Wall, Structure.Empty];
        int[] shape = [Shape.None, Shape.BottomRight, Shape.BottomLeft, Shape.TopLeft, Shape.TopRight, Shape.None];
        var cells = new byte[6];
        Sources.Occupy(c => structure[c], c => shape[c], 6, 6, new Box(0, 0, 5, 0), cells);
        Assert.Equal(new byte[] { Sources.Wall, Sources.Slope + 1, Sources.Slope + 2, Sources.Slope + 3, Sources.Slope + 4, Sources.Open }, cells);
        // Apart from every other code the shader reads, as its decoding expects: 33 to 36.
        Assert.All(Shape.Cut, s => Assert.InRange(Sources.Slope + s, 33, 36));
        Assert.DoesNotContain(Sources.Hatch, cells);
    }

    // A room walled round with a shaped wall standing in it, every cell backed as a map draws it.
    private static readonly string[] Room = ["#######", "#.....#", "#../..#", "#.....#", "#######"];

    private static (byte[] Texels, Box Region) Starlight(string[] rows, bool backed)
    {
        var m = new Registry();
        World.Create(1, Layout.Parse(rows), m.All);
        if (!backed) m.Grid.Clear(2 * 7 + 3, "back");
        var field = new StarField();
        field.Work(m.Grid);
        return (field.Texels, field.Region);
    }

    private static byte RedAt((byte[] Texels, Box Region) f, int x, int y) =>
        f.Texels[((y - f.Region.Top) * f.Region.Width + x - f.Region.Left) * 4];

    [Fact]
    public void StarsShineIntoAShapedWallsOpenHalfWithNoBackWallButPassNoFurtherThanOutOfAWall()
    {
        var backed = Starlight(Room, backed: true);
        var open = Starlight(Room, backed: false);
        // Its face takes the stars whole, as a wall's face lit by them; backed, it is dark.
        Assert.Equal((byte)(Math.Min(1, L.Star.R / L.WallIn) * 255), RedAt(open, 3, 2));
        Assert.True(RedAt(backed, 3, 2) < RedAt(open, 3, 2) / 4, $"{RedAt(backed, 3, 2)}");
        // The room beside it keeps no more than what comes out of a wall, where a hole there would light it.
        var hole = Starlight(["#######", "#.....#", "#.....#", "#.....#", "#######"], backed: false);
        Assert.InRange(RedAt(open, 2, 2), 0, (int)(L.WallOut * L.Air * L.Star.R * 255) + 1);
        Assert.True(RedAt(hole, 2, 2) > 4 * RedAt(open, 2, 2), $"{RedAt(hole, 2, 2)} against {RedAt(open, 2, 2)}");
        // Its alpha says a wall, so a picture in the open beside it blends it not.
        Assert.Equal(255, open.Texels[((2 - open.Region.Top) * open.Region.Width + 3 - open.Region.Left) * 4 + 3]);
    }
}
