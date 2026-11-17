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
        Assert.Equal((702, 208), (Normal.Width, Normal.Height));
        Assert.Equal((676, 132), (Normal.GridWidth, Normal.GridHeight));
        // Two rows of cards a step of the art larger than a slot (0107): 56 px pictures at the normal slot, 68 at the large.
        Assert.Equal((56, 64, 68, 76), (Normal.Icon, Normal.Card, Large.Icon, Large.Card));
        // Large slots: 10 × 80 − 4 across, 2 × 80 − 4 down, the strips round them the same.
        Assert.Equal((822, 232), (Large.Width, Large.Height));
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
    [InlineData(20, 2, 0)]
    [InlineData(21, 3, 1)]
    [InlineData(50, 5, 3)]
    public void ScrollsOnlyAsFarAsTheLastRow(int count, int rows, int lastTop)
    {
        Assert.Equal(rows, Catalogue.RowsOf(count));
        Assert.Equal(lastTop, Catalogue.LastTop(count));
    }

    [Fact]
    public void ScrollsARowANotchKeptWithinTheCards()
    {
        Assert.Equal(1, Catalogue.Scrolled(0, 1, 50));
        Assert.Equal(3, Catalogue.Scrolled(3, 1, 50));
        Assert.Equal(0, Catalogue.Scrolled(0, -1, 50));
        // Every card fits: the wheel moves nothing.
        Assert.Equal(0, Catalogue.Scrolled(0, 1, 20));
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(0, 19, 0)]
    [InlineData(0, 20, 1)]
    [InlineData(0, 49, 3)]
    [InlineData(2, 3, 0)]
    [InlineData(3, 40, 3)]
    public void RevealsACardMovingAsLittleAsItCan(int top, int index, int revealed)
    {
        Assert.Equal(revealed, Catalogue.Revealing(top, index));
    }

    [Fact]
    public void FindsTheCardUnderThePointerAndNoneInAGapOrPastTheLast()
    {
        // The first card, its far corner, the gap after it, the next card.
        Assert.Equal(0, Normal.CardAt(0, 0, top: 0, count: 20));
        Assert.Equal(0, Normal.CardAt(63.9f, 63.9f, top: 0, count: 20));
        Assert.Null(Normal.CardAt(65, 10, top: 0, count: 20));
        Assert.Null(Normal.CardAt(10, 66, top: 0, count: 20));
        Assert.Equal(1, Normal.CardAt(68, 0, top: 0, count: 20));
        // The second row's first card; scrolled a row, the same spot is the third row's.
        Assert.Equal(10, Normal.CardAt(5, 72, top: 0, count: 60));
        Assert.Equal(20, Normal.CardAt(5, 72, top: 1, count: 60));
        // Past the last card, or off the grid.
        Assert.Null(Normal.CardAt(5, 72, top: 0, count: 10));
        Assert.Null(Normal.CardAt(-1, 5, top: 0, count: 20));
        Assert.Null(Normal.CardAt(Normal.GridWidth, 5, top: 0, count: 60));
        Assert.Null(Normal.CardAt(5, Normal.GridHeight, top: 0, count: 60));
        // The last column at the large slot.
        Assert.Equal(9, Large.CardAt(9 * 80 + 75, 75, top: 0, count: 10));
    }

    [Fact]
    public void PutsEachCardWhereThePointerFindsIt()
    {
        foreach (var at in (Catalogue[])[Normal, Large])
            for (int top = 0; top <= 2; top++)
                for (int index = top * Catalogue.Columns; index < (top + Catalogue.Rows) * Catalogue.Columns; index++)
                {
                    var (x, y) = at.SpotOf(top, index);
                    Assert.Equal(index, at.CardAt(x + at.Card / 2, y + at.Card / 2, top, 100));
                }
    }

    [Fact]
    public void ShowsAThumbOnlyWhenTheCardsOverflowRunningFromTopToBottom()
    {
        Assert.Null(Normal.Thumb(0, 20));
        var first = Normal.Thumb(0, 50)!.Value;
        var last = Normal.Thumb(3, 50)!.Value;
        // Two rows of five shown: two fifths of the track.
        Assert.Equal(Normal.GridHeight * 2f / 5, first.Length, 3);
        Assert.Equal(0, first.Top);
        Assert.Equal(Normal.GridHeight, last.Top + last.Length, 3);
        // However long the list, the thumb stays big enough to see.
        Assert.Equal(Catalogue.ThumbLeast, Normal.Thumb(0, 1000)!.Value.Length);
    }

    [Fact]
    public void SearchesEveryTabByNameCaseIgnored()
    {
        // Every wall of every material, whatever materials there are, and nothing else.
        var walls = Tools.Matching(Tools.All, " WALL ");
        Assert.Equal(Tools.All.Where(t => t.Label.Contains("wall", StringComparison.OrdinalIgnoreCase)), walls);
        Assert.Contains(walls, t => t.Id == "wallScrap");
        // A back wall has cards of its own (0108).
        Assert.Contains(walls, t => t.Id == "backWallScrap");
        // By the name a player reads, not the kind's id: the oxygen generator is the vaporizer (liquid-oxygen).
        Assert.Equal(["burnerGenerator", "biofuelGenerator"], Tools.Matching(Tools.All, "generator").Select(t => t.Id));
        Assert.Equal(["generator"], Tools.Matching(Tools.All, "Vaporizer").Select(t => t.Id));
        Assert.Empty(Tools.Matching(Tools.All, "no such thing"));
    }
}
