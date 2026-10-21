using Stillwake.Game.Features.Building;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The build menu's fixed frame, its scrolling and the card under the pointer (0073).</summary>
public class CatalogueTests
{
    private static readonly Catalogue Normal = new(Slots.SizeOf(SlotScale.Normal)), Large = new(Slots.SizeOf(SlotScale.Large));

    [Fact]
    public void IsTheMockupsSizeAtTheNormalSlotAndGrowsOnlyWithTheSlot()
    {
        Assert.Equal((638, 204), (Normal.Width, Normal.Height));
        Assert.Equal((612, 128), (Normal.GridWidth, Normal.GridHeight));
        // Large slots: 14 × 60 − 4 across, 3 × 60 − 4 down, the strips round them the same.
        Assert.Equal((862, 252), (Large.Width, Large.Height));
        // At 1280×720 the large menu still fits beside the screen's margins.
        Assert.True(Large.Width + 2 * Slots.Margin <= 1280);
    }

    [Fact]
    public void LaysItsStripsInsideItsFrameTopToBottom()
    {
        foreach (var at in (Catalogue[])[Normal, Large])
        {
            Assert.Equal(Catalogue.Pad + Catalogue.TabHeight + Catalogue.Pad, at.GridTop);
            Assert.Equal(at.GridTop + at.GridHeight + Catalogue.Pad, at.FootTop);
            Assert.Equal(at.Height, at.FootTop + Catalogue.FootHeight + Catalogue.Pad);
            Assert.Equal(at.Width, at.BarLeft + Catalogue.BarWidth + Catalogue.Pad);
        }
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(42, 3, 0)]
    [InlineData(43, 4, 1)]
    [InlineData(70, 5, 2)]
    public void ScrollsOnlyAsFarAsTheLastRow(int count, int rows, int lastTop)
    {
        Assert.Equal(rows, Catalogue.RowsOf(count));
        Assert.Equal(lastTop, Catalogue.LastTop(count));
    }

    [Fact]
    public void ScrollsARowANotchKeptWithinTheCards()
    {
        Assert.Equal(1, Catalogue.Scrolled(0, 1, 70));
        Assert.Equal(2, Catalogue.Scrolled(2, 1, 70));
        Assert.Equal(0, Catalogue.Scrolled(0, -1, 70));
        // Every card fits: the wheel moves nothing.
        Assert.Equal(0, Catalogue.Scrolled(0, 1, 42));
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(0, 41, 0)]
    [InlineData(0, 42, 1)]
    [InlineData(0, 69, 2)]
    [InlineData(2, 3, 0)]
    [InlineData(2, 60, 2)]
    public void RevealsACardMovingAsLittleAsItCan(int top, int index, int revealed)
    {
        Assert.Equal(revealed, Catalogue.Revealing(top, index));
    }

    [Fact]
    public void FindsTheCardUnderThePointerAndNoneInAGapOrPastTheLast()
    {
        // The first card, its far corner, the gap after it, the next card.
        Assert.Equal(0, Normal.CardAt(0, 0, top: 0, count: 20));
        Assert.Equal(0, Normal.CardAt(39.9f, 39.9f, top: 0, count: 20));
        Assert.Null(Normal.CardAt(41, 10, top: 0, count: 20));
        Assert.Null(Normal.CardAt(10, 42, top: 0, count: 20));
        Assert.Equal(1, Normal.CardAt(44, 0, top: 0, count: 20));
        // The second row's first card; scrolled a row, the same spot is the third row's.
        Assert.Equal(14, Normal.CardAt(5, 50, top: 0, count: 60));
        Assert.Equal(28, Normal.CardAt(5, 50, top: 1, count: 60));
        // Past the last card, or off the grid.
        Assert.Null(Normal.CardAt(5, 50, top: 0, count: 14));
        Assert.Null(Normal.CardAt(-1, 5, top: 0, count: 20));
        Assert.Null(Normal.CardAt(Normal.GridWidth, 5, top: 0, count: 60));
        Assert.Null(Normal.CardAt(5, Normal.GridHeight, top: 0, count: 60));
        // The last column at the large slot.
        Assert.Equal(13, Large.CardAt(13 * 60 + 55, 55, top: 0, count: 14));
    }

    [Fact]
    public void PutsEachCardWhereThePointerFindsIt()
    {
        foreach (var at in (Catalogue[])[Normal, Large])
            for (int top = 0; top <= 2; top++)
                for (int index = top * Catalogue.Columns; index < (top + Catalogue.Rows) * Catalogue.Columns; index++)
                {
                    var (x, y) = at.SpotOf(top, index);
                    Assert.Equal(index, at.CardAt(x + at.Slot / 2, y + at.Slot / 2, top, 100));
                }
    }

    [Fact]
    public void ShowsAThumbOnlyWhenTheCardsOverflowRunningFromTopToBottom()
    {
        Assert.Null(Normal.Thumb(0, 42));
        var first = Normal.Thumb(0, 70)!.Value;
        var last = Normal.Thumb(2, 70)!.Value;
        // Three rows of five shown: three fifths of the track.
        Assert.Equal(Normal.GridHeight * 3f / 5, first.Length, 3);
        Assert.Equal(0, first.Top);
        Assert.Equal(Normal.GridHeight, last.Top + last.Length, 3);
        // However long the list, the thumb stays big enough to see.
        Assert.Equal(Catalogue.ThumbLeast, Normal.Thumb(0, 1000)!.Value.Length);
    }

    [Fact]
    public void SearchesEveryTabByNameCaseIgnored()
    {
        // Every wall and back wall of every material, whatever materials there are, and nothing else.
        var walls = Tools.Matching(Tools.All, " WALL ");
        Assert.Equal(Tools.All.Where(t => t.Label.Contains("wall", StringComparison.OrdinalIgnoreCase)), walls);
        Assert.Contains(walls, t => t.Id == "wallScrap");
        Assert.Contains(walls, t => t.Id == "backWallScrap");
        Assert.Equal(["generator", "burnerGenerator"], Tools.Matching(Tools.All, "generator").Select(t => t.Id));
        Assert.Empty(Tools.Matching(Tools.All, "no such thing"));
    }
}
