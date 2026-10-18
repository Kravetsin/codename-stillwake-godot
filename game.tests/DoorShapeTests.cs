using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>One door, every shape (0067): which door cells are a door's and which a hatch's, and their pictures.</summary>
public class DoorShapeTests
{
    /// <summary>
    /// Each cell of <paramref name="rows"/> a door (<c>D</c> shut, <c>d</c> open) as the grid draws it, else <c>null</c>:
    /// its picture, joined to the door cells on its four sides.
    /// </summary>
    private static string?[,] Drawn(params string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        bool IsDoor(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && rows[y][x] is 'D' or 'd';
        var drawn = new string?[h, w];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!IsDoor(x, y)) continue;
                int linked = (IsDoor(x, y - 1) ? DoorShape.Up : 0) | (IsDoor(x + 1, y) ? DoorShape.Right : 0)
                    | (IsDoor(x, y + 1) ? DoorShape.Down : 0) | (IsDoor(x - 1, y) ? DoorShape.Left : 0);
                drawn[y, x] = DoorShape.Picture(rows[y][x] == 'D', linked);
            }
        return drawn;
    }

    [Fact]
    public void ADoorwayThreeTallIsADoorItsWindowAtTheTopAndItsStripesAtTheBottom()
    {
        var drawn = Drawn(".D.", ".D.", ".d.");
        Assert.Equal(new[] { "door_closed_4", "door_closed_5", "door_open_1" }, new[] { drawn[0, 1], drawn[1, 1], drawn[2, 1] });
    }

    [Fact]
    public void ADoorwayThreeWideIsAHatchJoinedEndToEnd()
    {
        var drawn = Drawn("...", "DDD", "...");
        Assert.Equal(new[] { "hatch_closed_2", "hatch_closed_10", "hatch_closed_8" }, new[] { drawn[1, 0], drawn[1, 1], drawn[1, 2] });
        Assert.Equal("hatch_open_10", Drawn("ddd")[0, 1]);
    }

    [Fact]
    public void ADoorCellAloneIsAHatch()
    {
        // No hero fits through a door one cell tall; a shaft one cell wide is where a hatch goes (0005).
        Assert.Equal("hatch_closed_0", Drawn("...", ".D.", "...")[1, 1]);
        Assert.Equal("hatch_open_0", Drawn("d")[0, 0]);
    }

    [Fact]
    public void ADoorAndAHatchTouchingAtACornerKeepTheirShapes()
    {
        var drawn = Drawn("D..", "D..", ".DD");
        Assert.Equal(new[] { "door_closed_4", "door_closed_1" }, new[] { drawn[0, 0], drawn[1, 0] });
        Assert.Equal(new[] { "hatch_closed_2", "hatch_closed_8" }, new[] { drawn[2, 1], drawn[2, 2] });
    }

    [Fact]
    public void ADeckTwoCellsThickDrawsItsHatchAsARowOfShortDoors()
    {
        var drawn = Drawn("DDD", "DDD");
        for (int x = 0; x < 3; x++)
            Assert.Equal(new[] { "door_closed_4", "door_closed_1" }, new[] { drawn[0, x], drawn[1, x] });
    }

    [Fact]
    public void ADoorwayIsHeardFromItsTopCellAndAHatchFromItsLeftEnd()
    {
        Assert.True(DoorShape.Leads(DoorShape.Down));
        Assert.False(DoorShape.Leads(DoorShape.Up | DoorShape.Down));
        Assert.False(DoorShape.Leads(DoorShape.Up));
        Assert.True(DoorShape.Leads(DoorShape.Right));
        Assert.False(DoorShape.Leads(DoorShape.Left | DoorShape.Right));
        Assert.False(DoorShape.Leads(DoorShape.Left));
        Assert.True(DoorShape.Leads(0));
    }

    [Fact]
    public void EveryHatchPictureIsInTheArtFolder()
    {
        string art = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art");
        var pictures = DoorShape.HatchPictures.ToList();
        Assert.Equal(8, pictures.Count);
        foreach (var picture in pictures) Assert.True(File.Exists(Path.Combine(art, picture + ".png")), picture);
    }
}
