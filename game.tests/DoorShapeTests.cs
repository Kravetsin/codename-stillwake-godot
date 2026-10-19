using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>One door, every shape (0067): which door cells are a door's and which a hatch's, and their pictures.</summary>
public class DoorShapeTests
{
    /// <summary>
    /// Each cell of <paramref name="rows"/> a door (<c>D</c> shut, <c>d</c> open) as the grid draws it, else <c>null</c>:
    /// its picture, by the sides it joins its doorway on as the sim has them.
    /// </summary>
    private static string?[,] Drawn(params string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        int StructureAt(int c) => rows[c / w][c % w] switch { 'D' => Structure.DoorClosed, 'd' => Structure.DoorOpen, _ => Structure.Empty };
        var drawn = new string?[h, w];
        for (int c = 0; c < w * h; c++)
            if (Doorways.IsDoor(StructureAt(c)))
                drawn[c / w, c % w] = DoorShape.Picture(StructureAt(c) == Structure.DoorClosed, Doorways.LinksOf(c, w, w * h, StructureAt));
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
    public void AHatchMeetingADoorEndsBesideItAndTheDoorTakesTheCellWhereTheyMeet()
    {
        // An L: a hatch three wide, a door hanging from its right end's cell, which is the door's top.
        var l = Drawn("DDDD", "...D", "...D");
        Assert.Equal(new[] { "hatch_closed_2", "hatch_closed_10", "hatch_closed_8", "door_closed_4" }, new[] { l[0, 0], l[0, 1], l[0, 2], l[0, 3] });
        Assert.Equal(new[] { "door_closed_5", "door_closed_1" }, new[] { l[1, 3], l[2, 3] });
        // A T: the hatch's two arms end either side of the door.
        var t = Drawn("DDDDD", "..D..", "..D..");
        Assert.Equal(new[] { "hatch_closed_2", "hatch_closed_8", "door_closed_4", "hatch_closed_2", "hatch_closed_8" }, new[] { t[0, 0], t[0, 1], t[0, 2], t[0, 3], t[0, 4] });
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
        Assert.True(DoorShape.Leads(Doorways.Down));
        Assert.False(DoorShape.Leads(Doorways.Up | Doorways.Down));
        Assert.False(DoorShape.Leads(Doorways.Up));
        Assert.True(DoorShape.Leads(Doorways.Right));
        Assert.False(DoorShape.Leads(Doorways.Left | Doorways.Right));
        Assert.False(DoorShape.Leads(Doorways.Left));
        Assert.True(DoorShape.Leads(0));
    }

    [Fact]
    public void SlidesAPanelUpForADoorAndAlongForAHatchFromShutToOpen()
    {
        // Four pixels down, one across: a frame (F) round a panel (P) in the open picture's hole (.).
        byte[] Pixels(params char[] p) => p.SelectMany(c => c == '.' ? new byte[4] : new byte[] { (byte)c, 0, 0, 255 }).ToArray();
        byte[] shut = Pixels('F', 'A', 'B', 'F'), open = Pixels('F', '.', '.', 'F');
        Assert.Equal(shut, DoorShape.Slid(shut, open, 1, 0, hatch: false));
        Assert.Equal(Pixels('F', 'B', '.', 'F'), DoorShape.Slid(shut, open, 1, 1, hatch: false));
        Assert.Equal(open, DoorShape.Slid(shut, open, 1, 2, hatch: false));
        // The same row lying down, a hatch: it slides toward its left end.
        Assert.Equal(Pixels('F', 'B', '.', 'F'), DoorShape.Slid(shut, open, 4, 1, hatch: true));
    }

    [Fact]
    public void NamesEveryStepOfASlideShutAndOpenAtItsEnds()
    {
        Assert.Equal("door_closed_5", DoorShape.Picture(0, Doorways.Up | Doorways.Down));
        Assert.Equal("door_open_5", DoorShape.Picture(DoorShape.Steps, Doorways.Up | Doorways.Down));
        Assert.Equal("hatch_slid3_10", DoorShape.Picture(3, Doorways.Left | Doorways.Right));
        var slid = DoorShape.SlidPictures.ToList();
        // Three shapes of door (alone, a door cell is a hatch) and four of hatch, seven pictures between shut and open each.
        Assert.Equal(7 * (DoorShape.Steps - 1), slid.Select(s => s.Name).Distinct().Count());
        Assert.Contains(("door_slid4_1", "door_closed_1", "door_open_1", 8, false), slid);
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
