using Stillwake.Game.Features.Grid;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// What of a planet's grid is drawn as the view moves (0079): the screen whole at once, the ground round it ahead within a budget a
/// frame, so running into new ground never draws a whole column of chunks in one frame.
/// </summary>
public class ChunkRevealTests
{
    private const int Chunk = ChunkReveal.Chunk;

    /// <summary>A view the farthest zoom shows, 96 by 54 cells, with the view's pad, its top left at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static Box View(int x, int y) => new(x, y, x + 99, y + 57);

    /// <summary>A grid full in every cell: each row drawn costs its whole width.</summary>
    private static int Full(int y, int x0, int x1) => x1 - x0;

    private static void AssertDrawn(ChunkReveal reveal, int width, Box view)
    {
        for (int y = view.Top / Chunk * Chunk; y < (view.Bottom / Chunk + 1) * Chunk; y++)
            for (int x = view.Left / Chunk * Chunk; x < (view.Right / Chunk + 1) * Chunk; x++)
                Assert.True(reveal.Drawn(y * width + x), $"({x}, {y}) on screen and not drawn");
    }

    [Fact]
    public void EveryChunkOnScreenIsDrawnWholeAtOnceWhateverTheBudget()
    {
        var reveal = new ChunkReveal(512, 256);
        int drawn = reveal.Reveal(View(100, 70), Full);
        AssertDrawn(reveal, 512, View(100, 70));
        Assert.True(drawn > ChunkReveal.Budget);
    }

    [Fact]
    public void TheGroundRoundTheViewIsDrawnAheadABudgetAFrameThenNothing()
    {
        var reveal = new ChunkReveal(512, 256);
        reveal.Reveal(View(100, 70), Full);
        int frames = 0, drawn;
        while ((drawn = reveal.Reveal(View(100, 70), Full)) > 0)
        {
            // A row is finished once begun: at most a row past the budget.
            Assert.True(drawn <= ChunkReveal.Budget + Chunk, $"{drawn} cells drawn ahead in one frame");
            frames++;
        }
        Assert.True(frames > 1);
        AssertDrawn(reveal, 512, new Box(100 - ChunkReveal.Margin, 70 - ChunkReveal.Margin, 199 + ChunkReveal.Margin, 127 + ChunkReveal.Margin));
    }

    [Fact]
    public void RunningIntoNewGroundNeverDrawsMoreThanTheBudgetAFrame()
    {
        // A cell a frame, faster than a sprint at 60 fps (0.22 a step), across most of the grid and back.
        var reveal = new ChunkReveal(2048, 256);
        reveal.Reveal(View(0, 100), Full);
        for (int frame = 0; frame < 600; frame++) reveal.Reveal(View(0, 100), Full);
        foreach (int x in Enumerable.Range(0, 1800).Concat(Enumerable.Range(0, 1800).Reverse()))
        {
            int drawn = reveal.Reveal(View(x, 100), Full);
            Assert.True(drawn <= ChunkReveal.Budget + Chunk, $"{drawn} cells drawn at x {x}");
            AssertDrawn(reveal, 2048, View(x, 100));
        }
    }

    [Fact]
    public void NearestChunksAheadAreDrawnFirst()
    {
        // On chunk lines: the view spans chunks 8 to 10 across and 4 to 5 down; the margin reaches two chunks round it.
        var reveal = new ChunkReveal(512, 256);
        var view = new Box(256, 128, 351, 191);
        bool Whole(int cx, int cy) => reveal.Drawn((cy * Chunk + Chunk - 1) * 512 + cx * Chunk);
        bool Begun(int cx, int cy) => reveal.Drawn(cy * Chunk * 512 + cx * Chunk);
        var ring1 = Ring(7, 3, 11, 6).ToList();
        var ring2 = Ring(6, 2, 12, 7).ToList();
        do
            Assert.True(!ring2.Any(c => Begun(c.X, c.Y)) || ring1.All(c => Whole(c.X, c.Y)), "a chunk two away begun before those one away were done");
        while (reveal.Reveal(view, Full) > 0);
        Assert.True(ring2.All(c => Whole(c.X, c.Y)));
    }

    /// <summary>The chunks on the edge of the box from (<paramref name="l"/>, <paramref name="t"/>) to (<paramref name="r"/>, <paramref name="b"/>).</summary>
    private static IEnumerable<(int X, int Y)> Ring(int l, int t, int r, int b) =>
        from y in Enumerable.Range(t, b - t + 1) from x in Enumerable.Range(l, r - l + 1) where x == l || x == r || y == t || y == b select (x, y);

    [Fact]
    public void ACellIsDrawnOnceItsRowOfItsChunkIs()
    {
        var reveal = new ChunkReveal(512, 256);
        var view = new Box(256, 128, 351, 191);
        reveal.Reveal(view, Full);
        // The first look draws the screen and begins ahead: the first chunk ahead, (7, 3), has the budget's rows drawn, eight of 32 cells.
        int rows = ChunkReveal.Budget / Chunk, top = 3 * Chunk, left = 7 * Chunk;
        Assert.True(reveal.Drawn(top * 512 + left));
        Assert.True(reveal.Drawn((top + rows - 1) * 512 + left + Chunk - 1));
        Assert.False(reveal.Drawn((top + rows) * 512 + left));
    }

    [Fact]
    public void EmptyRowsCostNothingSoOpenSkyIsDrawnAheadAtOnce()
    {
        var reveal = new ChunkReveal(512, 256);
        reveal.Reveal(View(200, 100), (_, _, _) => 0);
        Assert.Equal(0, reveal.Reveal(View(200, 100), (_, _, _) => 0));
        AssertDrawn(reveal, 512, new Box(200 - ChunkReveal.Margin, 100 - ChunkReveal.Margin, 299 + ChunkReveal.Margin, 157 + ChunkReveal.Margin));
    }

    [Fact]
    public void AViewPastTheGridsEdgeDrawsOnlyWhatIsInIt()
    {
        var reveal = new ChunkReveal(100, 70);
        int drawn = reveal.Reveal(new Box(-50, -40, 150, 120), Full);
        Assert.Equal(100 * 70, drawn);
        Assert.True(reveal.Drawn(100 * 70 - 1));
    }
}
