using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which cells of the wreck show their age, on either grid, and that a tear lasts only with the sides it was drawn for.</summary>
public class WreckDressTests
{
    private const int Up = 1, Right = 2, Down = 4, Left = 8;

    private static readonly (int X, int Y) Alone = (0, 0);

    /// <summary>A grid cell of the wreck alone, of a cell of the drawing.</summary>
    private static (int X, int Y) InGrid(int x, int y) => (x + Wreck.Left, y + Wreck.Top);

    private static (string? Over, string? Torn) Of(int code, int x, int y, int open)
    {
        var (gx, gy) = InGrid(x, y);
        return WreckDress.Of(code, gx, gy, Alone, open);
    }

    private static IEnumerable<(int X, int Y, char Look)> Torn => WreckDress.Cells().Where(c => c.Look == 'T');

    private static Registry NewWorld()
    {
        var m = new Registry();
        Stillwake.Sim.World.Create(1, Registry.Wreck, m.All);
        return m;
    }

    [Fact]
    public void ANewWorldCentresTheMapsAndADevGridHoldsThemAsTheyAre()
    {
        Assert.Equal(Layout.OffsetInWorld(Wreck.Width, Wreck.Height), WreckDress.CornerIn(Registry.Wreck.Width, Registry.Wreck.Height));
        Assert.Equal(Alone, WreckDress.CornerIn(Wreck.Width, Wreck.Height));
    }

    [Fact]
    public void ACellOfTheRoofsStripReadsItsTearOnEitherGrid()
    {
        var corner = Layout.OffsetInWorld(Wreck.Width, Wreck.Height);
        Assert.Equal('T', WreckDress.At(29, 7));
        Assert.Equal(Up | Down, WreckDress.OpenAsDrawn(29, 7));
        Assert.Equal("wreck_torn_29_7", WreckDress.Of(WreckDress.Code, 29 + Wreck.Left + corner.X, 7 + Wreck.Top + corner.Y, corner, Up | Down).Torn);
        Assert.Equal("wreck_torn_29_7", Of(WreckDress.Code, 29, 7, Up | Down).Torn);
    }

    [Fact]
    public void PastTheMapThereIsNoDress()
    {
        Assert.Equal(' ', WreckDress.At(-Wreck.Left - 1, 0));
        Assert.Equal(' ', WreckDress.At(0, -Wreck.Top - 1));
        Assert.Equal(' ', WreckDress.At(Wreck.Width - Wreck.Left, 0));
        Assert.Equal(' ', WreckDress.At(0, Wreck.Height - Wreck.Top));
    }

    [Fact]
    public void ATearShowsWithExactlyTheSidesItWasDrawnFor()
    {
        Assert.Equal(17, Torn.Count());
        foreach (var (x, y, _) in Torn)
            for (int open = 0; open < 16; open++)
                Assert.Equal(open == WreckDress.OpenAsDrawn(x, y), Of(WreckDress.Code, x, y, open).Torn != null);
    }

    [Fact]
    public void EveryTearShowsOnANewWorldsGrid()
    {
        var grid = NewWorld().Grid;
        var corner = WreckDress.CornerIn(grid.Width, grid.Height);
        bool Walled(int x, int y) => Structures.Is(grid.StructureAt(y * grid.Width + x), Trait.Terrain);
        foreach (var (x, y, _) in Torn)
        {
            var (gx, gy) = (x + Wreck.Left + corner.X, y + Wreck.Top + corner.Y);
            int open = (Walled(gx, gy - 1) ? 0 : Up) | (Walled(gx + 1, gy) ? 0 : Right) | (Walled(gx, gy + 1) ? 0 : Down) | (Walled(gx - 1, gy) ? 0 : Left);
            Assert.NotNull(WreckDress.Of(grid.MaterialLayer[gy * grid.Width + gx], gx, gy, corner, open).Torn);
        }
    }

    [Fact]
    public void AWallBuiltInItsPlaceIsCleanAndTheBackWallReturns()
    {
        int scrap = Materials.Get("scrap").Code;
        Assert.All(WreckDress.Cells(), c => Assert.Equal(default, Of(scrap, c.X, c.Y, WreckDress.OpenAsDrawn(c.X, c.Y))));
    }

    [Fact]
    public void RustAndStripesLieOverTheWallWhateverItsSides()
    {
        var over = WreckDress.Cells().Where(c => c.Look != 'T').ToList();
        Assert.NotEmpty(over);
        Assert.All(over, c =>
        {
            Assert.Equal(WreckDress.OverName(c.X, c.Y), Of(WreckDress.Code, c.X, c.Y, 0).Over);
            Assert.Null(Of(WreckDress.Code, c.X, c.Y, 0).Torn);
        });
    }

    [Fact]
    public void AnOriginalPlateIsPickedAsScrapsCard()
    {
        var m = NewWorld();
        var corner = WreckDress.CornerIn(m.Grid.Width, m.Grid.Height);
        var (x, y, _) = WreckDress.Cells().First();
        int cell = (y + Wreck.Top + corner.Y) * m.Grid.Width + x + Wreck.Left + corner.X;
        Assert.Equal("wreckScrap", m.Grid.WallAt(cell)?.Id);
        Assert.Equal(Placement.Wall("scrap"), Tools.BuiltAt(cell, "structure", m.Grid));
    }

    [Fact]
    public void AMaskKeepsCutsAndPaints()
    {
        byte[] wall = [10, 20, 30, 255, 10, 20, 30, 255, 10, 20, 30, 255];
        byte[] mask = [0, 0, 0, 0, 255, 0, 255, 255, 150, 190, 214, 255];
        Assert.Equal([10, 20, 30, 255, 0, 0, 0, 0, 150, 190, 214, 255], WreckDress.Torn(wall, mask));
    }

    [Theory]
    [InlineData("wreck_dress")]
    [InlineData("wreck_tears")]
    public void ItsSheetsCoverTheDrawing(string name)
    {
        var head = File.ReadAllBytes(Path.Combine(Art, $"{name}.png")).AsSpan(16, 8);
        Assert.Equal(592, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(head));
        Assert.True(System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(head[4..]) >= WreckDress.Cells().Max(c => c.Y + 1) * 8);
    }

    private static string Here([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

    private static readonly string Art = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, "..", "game", "art"));
}
