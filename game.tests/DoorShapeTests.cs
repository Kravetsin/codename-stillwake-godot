using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// One door, every shape (0067): which door cells are a door's and which a hatch's, and their pictures, the 16 px door's over
/// two by two cells (0104).
/// </summary>
public class DoorShapeTests
{
    /// <summary>
    /// Each cell of <paramref name="rows"/> a door (<c>D</c> shut, <c>d</c> open) as the grid draws it, else <c>null</c>: its
    /// picture and its quarter of it, <c>picture@x,y</c>, by where it lies in its doorway as the sim joins it.
    /// </summary>
    private static string?[,] Drawn(params string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        int StructureAt(int c) => rows[c / w][c % w] switch { 'D' => Structure.DoorClosed, 'd' => Structure.DoorOpen, _ => Structure.Empty };
        int LinksOf(int c) => Doorways.LinksOf(c, w, w * h, StructureAt);
        var drawn = new string?[h, w];
        for (int c = 0; c < w * h; c++)
            if (Doorways.IsDoor(StructureAt(c)))
            {
                var (picture, (x, y)) = DoorShape.TileOf(DoorShape.SpotOf(c, w, LinksOf), closed: StructureAt(c) == Structure.DoorClosed);
                drawn[c / w, c % w] = $"{picture}@{x},{y}";
            }
        return drawn;
    }

    private static IEnumerable<string?> Column(string?[,] drawn, int x) => Enumerable.Range(0, drawn.GetLength(0)).Select(y => drawn[y, x]);
    private static IEnumerable<string?> Row(string?[,] drawn, int y) => Enumerable.Range(0, drawn.GetLength(1)).Select(x => drawn[y, x]);

    [Fact]
    public void ADoorwayTwoThickAndSixTallIsADoorItsWindowAtTheTopAndItsStripesAtTheBottom()
    {
        var drawn = Drawn(".DD.", ".DD.", ".DD.", ".DD.", ".dd.", ".dd.");
        Assert.Equal(new string?[] { "door_closed_4@0,0", "door_closed_4@0,1", "door_closed_5@0,0", "door_closed_5@0,1", "door_open_1@0,0", "door_open_1@0,1" }, Column(drawn, 1));
        Assert.Equal(new string?[] { "door_closed_4@1,0", "door_closed_4@1,1", "door_closed_5@1,0", "door_closed_5@1,1", "door_open_1@1,0", "door_open_1@1,1" }, Column(drawn, 2));
    }

    [Fact]
    public void ADoorwaySixWideAndTwoTallIsAHatchJoinedEndToEnd()
    {
        var drawn = Drawn("......", "DDDDDD", "DDDDDD", "......");
        Assert.Equal(new string?[] { "hatch_closed_2@0,0", "hatch_closed_2@1,0", "hatch_closed_10@0,0", "hatch_closed_10@1,0", "hatch_closed_8@0,0", "hatch_closed_8@1,0" }, Row(drawn, 1));
        Assert.Equal(new string?[] { "hatch_closed_2@0,1", "hatch_closed_2@1,1", "hatch_closed_10@0,1", "hatch_closed_10@1,1", "hatch_closed_8@0,1", "hatch_closed_8@1,1" }, Row(drawn, 2));
        Assert.Equal("hatch_open_10@0,0", Drawn("dddddd", "dddddd")[0, 2]);
    }

    [Fact]
    public void ADoorTwoByTwoAloneIsAHatch()
    {
        // No hero fits through a door one 16 px cell tall; a shaft one wide is where a hatch goes (0005).
        var drawn = Drawn("....", ".DD.", ".DD.", "....");
        Assert.Equal(new string?[] { "hatch_closed_0@0,0", "hatch_closed_0@1,0", "hatch_closed_0@0,1", "hatch_closed_0@1,1" }, new[] { drawn[1, 1], drawn[1, 2], drawn[2, 1], drawn[2, 2] });
    }

    [Fact]
    public void ADoorAndAHatchTouchingAtACornerKeepTheirShapes()
    {
        var drawn = Drawn("DD....", "DD....", "DD....", "DD....", "..DDDD", "..DDDD");
        Assert.Equal(new string?[] { "door_closed_4@0,0", "door_closed_4@0,1", "door_closed_1@0,0", "door_closed_1@0,1", null, null }, Column(drawn, 0));
        Assert.Equal(new string?[] { null, null, "hatch_closed_2@0,0", "hatch_closed_2@1,0", "hatch_closed_8@0,0", "hatch_closed_8@1,0" }, Row(drawn, 4));
    }

    [Fact]
    public void AHatchMeetingADoorEndsBesideItAndTheDoorTakesTheCellsWhereTheyMeet()
    {
        // An L: a hatch six wide, a door hanging from its right end, whose top it is.
        var l = Drawn("DDDDDDDD", "DDDDDDDD", "......DD", "......DD");
        Assert.Equal(new string?[] { "hatch_closed_2@0,0", "hatch_closed_2@1,0", "hatch_closed_10@0,0", "hatch_closed_10@1,0", "hatch_closed_8@0,0", "hatch_closed_8@1,0",
            "door_closed_4@0,0", "door_closed_4@1,0" }, Row(l, 0));
        Assert.Equal(new string?[] { "door_closed_4@0,0", "door_closed_4@0,1", "door_closed_1@0,0", "door_closed_1@0,1" }, Column(l, 6));
    }

    [Fact]
    public void FindsWhereACellLiesAlongItsDoorwayAndAcrossIt()
    {
        int w = 4;
        string[] rows = [".DD.", ".DD.", ".DD.", ".DD.", ".DD.", ".DD."];
        int StructureAt(int c) => rows[c / w][c % w] == 'D' ? Structure.DoorClosed : Structure.Empty;
        int LinksOf(int c) => Doorways.LinksOf(c, w, w * rows.Length, StructureAt);
        Assert.Equal(new DoorShape.Spot(false, 3, 6, 1), DoorShape.SpotOf(3 * w + 2, w, LinksOf));
        Assert.Equal(new DoorShape.Spot(false, 0, 6, 0), DoorShape.SpotOf(1, w, LinksOf));
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
