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
    public void SlidesTheWholeDoorwaysPanelAsOneAcrossItsCells()
    {
        // A door two cells tall, each cell four pixels down and one across: a frame (F) round one panel, A to G.
        byte[] Pixels(params char[] p) => p.SelectMany(c => c == '.' ? new byte[4] : new byte[] { (byte)c, 0, 0, 255 }).ToArray();
        byte[] topShut = Pixels('F', 'A', 'B', 'C'), topOpen = Pixels('F', '.', '.', '.');
        byte[] bottomShut = Pixels('D', 'E', 'G', 'F'), bottomOpen = Pixels('.', '.', '.', 'F');
        // Risen two pixels: the top cell shows the panel's middle, the bottom one its end, then the hole.
        Assert.Equal(Pixels('F', 'C', 'D', 'E'), DoorShape.Slid(topShut, topOpen, topShut, topOpen, bottomShut, bottomOpen, 1, 2, hatch: false));
        Assert.Equal(Pixels('G', '.', '.', 'F'), DoorShape.Slid(bottomShut, bottomOpen, bottomShut, bottomOpen, null, null, 1, 2, hatch: false));
        // Risen a cell: the bottom cell's panel is over the top one, and the bottom one open.
        Assert.Equal(Pixels('F', 'E', 'G', '.'), DoorShape.Slid(topShut, topOpen, bottomShut, bottomOpen, null, null, 1, 0, hatch: false));
        // The same lying down, a hatch: it slides toward its left end.
        Assert.Equal(Pixels('F', 'C', 'D', 'E'), DoorShape.Slid(topShut, topOpen, topShut, topOpen, bottomShut, bottomOpen, 4, 2, hatch: true));
    }

    [Fact]
    public void NamesEachCellOfASlidingDoorwayByThePanelOverIt()
    {
        Assert.Equal("door_closed_4", DoorShape.Picture(false, 0, 3, 0));
        Assert.Equal("door_open_1", DoorShape.Picture(false, 2, 3, 48));
        Assert.Equal("door_slid_4_5_1_4", DoorShape.Picture(false, 0, 3, 20));
        Assert.Equal("door_slid_4_5_1_4", DoorShape.Picture(false, 0, 3, 21));
        Assert.Equal("door_open_5", DoorShape.Picture(false, 1, 3, 40));
        Assert.Equal("door_slid_1_1_x_8", DoorShape.Picture(false, 2, 3, 8));
        Assert.Equal("hatch_slid_2_10_8_2", DoorShape.Picture(true, 0, 3, 18));
        Assert.Equal("hatch_slid_0_0_x_6", DoorShape.Picture(true, 0, 1, 6));
    }

    [Fact]
    public void MakesEveryPictureASlideOfAnyLengthDraws()
    {
        var made = DoorShape.SlidPictures.Select(p => p.Name).ToHashSet();
        foreach (bool hatch in new[] { false, true })
            for (int length = hatch ? 1 : 2; length <= 7; length++)
                for (int index = 0; index < length; index++)
                    for (int shift = 0; shift <= length * 16; shift++)
                    {
                        string name = DoorShape.Picture(hatch, index, length, shift);
                        Assert.True(made.Contains(name) || !name.Contains("_slid_"), $"{name}: {length} long, cell {index}, {shift} px");
                    }
        Assert.All(DoorShape.SlidPictures, p => Assert.Equal(p.NextClosed == null, p.NextOpen == null));
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
